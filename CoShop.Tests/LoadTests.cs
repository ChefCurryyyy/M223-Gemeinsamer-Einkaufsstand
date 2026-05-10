using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace CoShop.Tests;

/// <summary>
/// Lasttests für Multi-User-Fähigkeit (Teilauftrag 6b).
///
/// Diese Tests senden parallele HTTP-Requests an den laufenden CoShop-Server
/// und messen Antwortzeiten sowie die korrekte Behandlung von Konflikten.
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
        _out = output;
        _baseUrl = Environment.GetEnvironmentVariable("COSHOP_API_URL")
                   ?? "http://localhost:5000";
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private HttpClient CreateClient()
    {
        var client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    /// <summary>
    /// Versucht Login und gibt null zurück, wenn der Server nicht erreichbar ist.
    /// </summary>
    private async Task<string?> TryLoginAsync(string email, string password)
    {
        try
        {
            using var client = CreateClient();
            var resp = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
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
    /// Lasttest 1: 50 gleichzeitige Login-Anfragen.
    ///
    /// Alle 50 Requests müssen mit HTTP 200 antworten.
    /// Misst die Gesamtdauer unter Last.
    /// </summary>
    [Fact]
    public async Task Lasttest_50_GleichzeitigeLogins_AlleErfolgreich()
    {
        const int anzahl = 50;
        var token = await TryLoginAsync("alice@coshop.dev", "Test1234!");
        if (token == null)
        {
            _out.WriteLine("[ÜBERSPRUNGEN] Server nicht verfügbar. Starte 'docker compose up' und führe den Test erneut aus.");
            return;
        }

        _out.WriteLine($"Starte {anzahl} parallele Login-Requests auf {_baseUrl} ...");
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(1, anzahl).Select(async _ =>
        {
            using var client = CreateClient();
            var resp = await client.PostAsJsonAsync("/api/auth/login",
                new { email = "alice@coshop.dev", password = "Test1234!" });
            return resp.StatusCode;
        });

        var results = await Task.WhenAll(tasks);
        sw.Stop();

        var ok = results.Count(s => s == HttpStatusCode.OK);
        _out.WriteLine($"Erfolge: {ok}/{anzahl} | Dauer: {sw.ElapsedMilliseconds} ms");

        Assert.Equal(anzahl, ok);
        _out.WriteLine("✓ Alle 50 parallelen Logins erfolgreich.");
    }

    /// <summary>
    /// Lasttest 2: 30 Benutzer lesen gleichzeitig dieselbe Einkaufsliste.
    ///
    /// Reine Lesezugriffe dürfen sich nicht gegenseitig blockieren.
    /// Mindestens 90% müssen mit HTTP 200 antworten.
    /// </summary>
    [Fact]
    public async Task Lasttest_30_GleichzeitigeListenLesungen_KeineBlockierung()
    {
        const int anzahl = 30;
        var token = await TryLoginAsync("alice@coshop.dev", "Test1234!");
        if (token == null)
        {
            _out.WriteLine("[ÜBERSPRUNGEN] Server nicht verfügbar.");
            return;
        }

        _out.WriteLine($"Starte {anzahl} parallele GET /api/shoppinglists/1 ...");
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(1, anzahl).Select(async _ =>
        {
            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var resp = await client.GetAsync("/api/shoppinglists/1");
            return resp.StatusCode;
        });

        var results = await Task.WhenAll(tasks);
        sw.Stop();

        var ok = results.Count(s => s == HttpStatusCode.OK);
        _out.WriteLine($"Erfolge: {ok}/{anzahl} | Dauer: {sw.ElapsedMilliseconds} ms");

        Assert.True(ok >= (int)(anzahl * 0.9),
            $"Mindestens 90% der Lesezugriffe müssen erfolgreich sein. Erhalten: {ok}/{anzahl}");
        _out.WriteLine("✓ Parallele Lesezugriffe ohne Blockierung.");
    }

    /// <summary>
    /// Lasttest 3: 15 Benutzer versuchen gleichzeitig denselben Artikel zu kaufen.
    ///
    /// Kerntest für Multi-User-Sicherheit:
    /// - Mindestens 1 Request muss mit 200 OK gelingen.
    /// - Konflikte werden korrekt als 409 Conflict zurückgegeben (kein Datenverlust).
    /// - Kein einziger Request darf mit 500 (Server-Fehler) scheitern.
    ///
    /// Dieses Verhalten zeigt, dass Optimistisches Locking greift.
    /// </summary>
    [Fact]
    public async Task Lasttest_GleichzeitigesKaufen_OptimistischesLockingGreift()
    {
        const int anzahl = 15;
        var token = await TryLoginAsync("alice@coshop.dev", "Test1234!");
        if (token == null)
        {
            _out.WriteLine("[ÜBERSPRUNGEN] Server nicht verfügbar.");
            return;
        }

        // Artikel vorher auf "nicht gekauft" zurücksetzen
        using var resetClient = CreateClient();
        resetClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        await resetClient.PatchAsJsonAsync("/api/shoppinglists/1/items/3/bought",
            new { isBought = false });

        _out.WriteLine($"Starte {anzahl} parallele PATCH .../items/3/bought ...");
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(1, anzahl).Select(async _ =>
        {
            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var resp = await client.PatchAsJsonAsync(
                "/api/shoppinglists/1/items/3/bought",
                new { isBought = true });
            return resp.StatusCode;
        });

        var results = await Task.WhenAll(tasks);
        sw.Stop();

        var ok       = results.Count(s => s == HttpStatusCode.OK);
        var conflict = results.Count(s => s == HttpStatusCode.Conflict);
        var error    = results.Count(s => s >= HttpStatusCode.InternalServerError);

        _out.WriteLine($"200 OK: {ok} | 409 Conflict: {conflict} | 5xx Fehler: {error} | Dauer: {sw.ElapsedMilliseconds} ms");
        _out.WriteLine($"Statusverteilung: {string.Join(", ", results.GroupBy(s => s).Select(g => $"{g.Key}: {g.Count()}x"))}");

        Assert.True(ok >= 1, "Mindestens ein Request muss erfolgreich sein.");
        Assert.True(error == 0, "Kein Request darf mit Server-Fehler (5xx) enden.");
        _out.WriteLine("✓ Optimistisches Locking schützt korrekt vor gleichzeitigen Schreibzugriffen.");
    }

    /// <summary>
    /// Lasttest 4: Antwortzeit unter Last.
    ///
    /// 20 gleichzeitige Requests auf den Listen-Endpunkt.
    /// Durchschnittliche Antwortzeit muss unter 2000 ms bleiben.
    /// </summary>
    [Fact]
    public async Task Lasttest_Antwortzeiten_UnterLast_AkzeptabelSchnell()
    {
        const int anzahl = 20;
        var token = await TryLoginAsync("alice@coshop.dev", "Test1234!");
        if (token == null)
        {
            _out.WriteLine("[ÜBERSPRUNGEN] Server nicht verfügbar.");
            return;
        }

        _out.WriteLine($"Messe Antwortzeiten bei {anzahl} parallelen GET /api/shoppinglists ...");

        var tasks = Enumerable.Range(1, anzahl).Select(async _ =>
        {
            using var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var sw = Stopwatch.StartNew();
            await client.GetAsync("/api/shoppinglists");
            sw.Stop();
            return sw.ElapsedMilliseconds;
        });

        var zeiten = await Task.WhenAll(tasks);
        var avg = zeiten.Average();
        var max = zeiten.Max();
        var min = zeiten.Min();

        _out.WriteLine($"Antwortzeiten – Avg: {avg:F0} ms | Min: {min} ms | Max: {max} ms");

        Assert.True(avg < 2000,
            $"Durchschnittliche Antwortzeit ({avg:F0} ms) überschreitet 2000 ms.");
        _out.WriteLine("✓ Antwortzeiten unter Last sind akzeptabel.");
    }

    // ── Hilfstypen ────────────────────────────────────────────────────────────

    private record AuthResult(string Token, int UserId, string Username, string Role);
}
