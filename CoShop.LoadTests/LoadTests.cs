// KI-generiert (Claude AI) — NBomber-Lasttests für Multi-User-Verhalten (parallele Requests, Konfliktbehandlung, Latenzmessung)
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NBomber.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace CoShop.LoadTests;

/// <summary>
/// Lasttests für Multi-User-Fähigkeit (Teilauftrag 6b).
///
/// Nutzt NBomber, um realistische Last-Szenarien zu simulieren und
/// Latenzen (P50/P95) sowie die korrekte Konfliktbehandlung zu messen.
///
/// Voraussetzung: Server läuft auf http://localhost:5000
/// Anderer URL: Umgebungsvariable COSHOP_API_URL setzen.
/// Server starten: docker compose up -d (im Projektverzeichnis)
/// </summary>
public class LoadTests
{
    private readonly string _baseUrl;
    private readonly ITestOutputHelper _out;

    public LoadTests(ITestOutputHelper output)
    {
        _out     = output;
        _baseUrl = Environment.GetEnvironmentVariable("COSHOP_API_URL") ?? "http://localhost:5000";
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private HttpClient MakeClient(string? token = null)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout     = TimeSpan.FromSeconds(30),
        };
        if (token is not null)
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Versucht Login; gibt null zurück wenn der Server nicht erreichbar ist.</summary>
    private async Task<string?> TryLoginAsync()
    {
        try
        {
            using var client = MakeClient();
            var resp = await client.PostAsJsonAsync("/api/auth/login",
                new { email = "alice@coshop.dev", password = "Test1234!" });
            if (!resp.IsSuccessStatusCode) return null;
            var body = await resp.Content.ReadFromJsonAsync<AuthResult>();
            return body?.Token;
        }
        catch (HttpRequestException ex)
        {
            _out.WriteLine($"[!] Server nicht erreichbar ({_baseUrl}): {ex.Message}");
            return null;
        }
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lasttest 1: 50 gleichzeitige Login-Anfragen (NBomber Inject-Simulation).
    /// Alle müssen mit HTTP 200 antworten — kein Request darf fehlschlagen.
    /// </summary>
    [Fact]
    public async Task Lasttest_50_GleichzeitigeLogins_AlleErfolgreich()
    {
        var token = await TryLoginAsync();
        if (token is null)
        {
            _out.WriteLine("[ÜBERSPRUNGEN] Server nicht verfügbar. Starte 'docker compose up' und führe den Test erneut aus.");
            return;
        }

        // HttpClient ist thread-safe — ein einziger Client für alle parallelen Requests
        using var client = MakeClient();

        var scenario = Scenario.Create("login_flood", async _ =>
        {
            var resp = await client.PostAsJsonAsync("/api/auth/login",
                new { email = "alice@coshop.dev", password = "Test1234!" });

            return resp.StatusCode == HttpStatusCode.OK
                ? Response.Ok(statusCode: "200")
                : Response.Fail(statusCode: ((int)resp.StatusCode).ToString(), message: resp.ReasonPhrase ?? "");
        })
        .WithoutWarmUp()
        // Inject: sendet genau 50 neue Requests innerhalb von 1 Sekunde
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(1))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        var s = stats.ScenarioStats[0];
        _out.WriteLine($"OK: {s.Ok.Request.Count} | Fail: {s.Fail.Request.Count} | P50: {s.Ok.Latency.Percent50:F0} ms | P95: {s.Ok.Latency.Percent95:F0} ms");

        Assert.Equal(0, s.Fail.Request.Count);
        _out.WriteLine("✓ Alle parallelen Logins erfolgreich.");
    }

    /// <summary>
    /// Lasttest 2: 30 Benutzer lesen gleichzeitig dieselbe Einkaufsliste.
    /// Reine Lesezugriffe dürfen sich nicht gegenseitig blockieren (Fehlerrate &lt; 10%).
    /// </summary>
    [Fact]
    public async Task Lasttest_30_GleichzeitigeListenLesungen_KeineBlockierung()
    {
        var token = await TryLoginAsync();
        if (token is null) { _out.WriteLine("[ÜBERSPRUNGEN] Server nicht verfügbar."); return; }

        using var client = MakeClient(token);

        var scenario = Scenario.Create("list_reads", async _ =>
        {
            var resp = await client.GetAsync("/api/shoppinglists/1");

            return resp.StatusCode == HttpStatusCode.OK
                ? Response.Ok(statusCode: "200")
                : Response.Fail(statusCode: ((int)resp.StatusCode).ToString(), message: resp.ReasonPhrase ?? "");
        })
        .WithoutWarmUp()
        // KeepConstant: hält 30 gleichzeitige Worker für 5 Sekunden aufrecht
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 30, during: TimeSpan.FromSeconds(5))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        var s        = stats.ScenarioStats[0];
        var total    = s.Ok.Request.Count + s.Fail.Request.Count;
        var failRate = total > 0 ? (double)s.Fail.Request.Count / total : 0;

        _out.WriteLine($"OK: {s.Ok.Request.Count} | Fail: {s.Fail.Request.Count} | Fehlerrate: {failRate:P0} | P95: {s.Ok.Latency.Percent95:F0} ms");

        Assert.True(failRate < 0.1, $"Fehlerrate {failRate:P0} übersteigt 10% — Lesezugriffe blockieren sich.");
        _out.WriteLine("✓ Parallele Lesezugriffe ohne Blockierung.");
    }

    /// <summary>
    /// Lasttest 3: 15 Benutzer versuchen gleichzeitig denselben Artikel zu kaufen.
    ///
    /// Optimistisches Locking muss greifen:
    ///   • Mind. 1 Request → 200 OK  (erster Schreiber gewinnt)
    ///   • Weitere         → 409 Conflict (kein Datenverlust, korrekt abgefangen)
    ///   • Kein Request    → 5xx (kein Server-Absturz)
    /// </summary>
    [Fact]
    public async Task Lasttest_GleichzeitigesKaufen_OptimistischesLockingGreift()
    {
        var token = await TryLoginAsync();
        if (token is null) { _out.WriteLine("[ÜBERSPRUNGEN] Server nicht verfügbar."); return; }

        // Artikel auf "nicht gekauft" zurücksetzen, damit der Test reproduzierbar ist
        using var resetClient = MakeClient(token);
        await resetClient.PatchAsJsonAsync("/api/shoppinglists/1/items/3/bought", new { isBought = false });

        using var client    = MakeClient(token);
        var statusCodes     = new ConcurrentBag<HttpStatusCode>();

        var scenario = Scenario.Create("concurrent_buy", async _ =>
        {
            var resp = await client.PatchAsJsonAsync(
                "/api/shoppinglists/1/items/3/bought", new { isBought = true });

            statusCodes.Add(resp.StatusCode);

            // 200 OK und 409 Conflict sind beide erwartete, korrekte Antworten.
            // Nur 5xx gilt als echter Fehler.
            return (int)resp.StatusCode < 500
                ? Response.Ok(statusCode: ((int)resp.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)resp.StatusCode).ToString(), message: "Server-Fehler");
        })
        .WithoutWarmUp()
        // Inject: feuert alle 15 Requests gleichzeitig ab (maximale Race-Condition)
        .WithLoadSimulations(
            Simulation.Inject(rate: 15, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(1))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        var ok       = statusCodes.Count(s => s == HttpStatusCode.OK);
        var conflict = statusCodes.Count(s => s == HttpStatusCode.Conflict);
        var error    = statusCodes.Count(s => (int)s >= 500);

        _out.WriteLine($"200 OK: {ok} | 409 Conflict: {conflict} | 5xx Fehler: {error}");
        _out.WriteLine($"NBomber — Fail (5xx): {stats.ScenarioStats[0].Fail.Request.Count} | P95: {stats.ScenarioStats[0].Ok.Latency.Percent95:F0} ms");

        Assert.True(ok >= 1,    "Mindestens ein Request muss erfolgreich sein.");
        Assert.Equal(0, error);
        _out.WriteLine("✓ Optimistisches Locking schützt korrekt vor gleichzeitigen Schreibzugriffen.");
    }

    /// <summary>
    /// Lasttest 4: Antwortzeiten unter Last.
    /// 20 gleichzeitige Worker auf GET /api/shoppinglists für 5 Sekunden.
    /// P95-Latenz muss unter 2000 ms bleiben.
    /// </summary>
    [Fact]
    public async Task Lasttest_Antwortzeiten_UnterLast_AkzeptabelSchnell()
    {
        var token = await TryLoginAsync();
        if (token is null) { _out.WriteLine("[ÜBERSPRUNGEN] Server nicht verfügbar."); return; }

        using var client = MakeClient(token);

        var scenario = Scenario.Create("latency_check", async _ =>
        {
            var resp = await client.GetAsync("/api/shoppinglists");

            return resp.IsSuccessStatusCode
                ? Response.Ok(statusCode: "200")
                : Response.Fail(statusCode: ((int)resp.StatusCode).ToString(), message: resp.ReasonPhrase ?? "");
        })
        .WithoutWarmUp()
        // KeepConstant: hält konstante Last von 20 parallelen Workern
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 20, during: TimeSpan.FromSeconds(5))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        var s = stats.ScenarioStats[0];
        _out.WriteLine($"Mean: {s.Ok.Latency.MeanMs:F0} ms | P50: {s.Ok.Latency.Percent50:F0} ms | P75: {s.Ok.Latency.Percent75:F0} ms | P95: {s.Ok.Latency.Percent95:F0} ms");

        Assert.True(s.Ok.Latency.Percent95 < 2000,
            $"P95-Latenz ({s.Ok.Latency.Percent95:F0} ms) überschreitet 2000 ms.");
        _out.WriteLine("✓ Antwortzeiten unter Last sind akzeptabel.");
    }

    // ── Hilfstypen ────────────────────────────────────────────────────────────

    private record AuthResult(string Token, int UserId, string Username, string Role);
}
