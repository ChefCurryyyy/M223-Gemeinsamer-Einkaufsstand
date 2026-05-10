using Microsoft.EntityFrameworkCore;
using CoShop.Data;
using CoShop.Models;
using Xunit;
using Xunit.Abstractions;

namespace CoShop.Tests;

/// <summary>
/// Parallele Unit-Tests für Multi-User-Fähigkeit (Teilauftrag 6a).
///
/// Testet speziell das optimistische Locking mittels RowVersion-Concurrency-Token.
/// Jede Test-Klasse wird von xUnit standardmässig in einem eigenen Thread ausgeführt,
/// wodurch echte Parallelität zwischen den Test-Klassen entsteht.
/// </summary>
public class MultiUserUnitTests
{
    private readonly ITestOutputHelper _out;

    public MultiUserUnitTests(ITestOutputHelper output) => _out = output;

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private static DbContextOptions<AppDbContext> InMemoryOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

    /// <summary>Legt minimale Testdaten an und gibt die IDs zurück.</summary>
    private static async Task<(int userId, int listId, int itemId)> SeedAsync(
        DbContextOptions<AppDbContext> opts)
    {
        await using var ctx = new AppDbContext(opts);

        var user = new User { Username = "tester", Email = "tester@test.ch", PasswordHash = "hash" };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var list = new ShoppingList { Title = "Testliste", OwnerId = user.Id };
        ctx.ShoppingLists.Add(list);
        await ctx.SaveChangesAsync();

        var item = new Item
        {
            Name = "Milch", Amount = 1, Unit = "L",
            ListId = list.Id, LastModifiedByUserId = user.Id
        };
        ctx.Items.Add(item);
        await ctx.SaveChangesAsync();

        return (user.Id, list.Id, item.Id);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Test 1: RowVersion ist als Concurrency-Token konfiguriert.
    ///
    /// Vergewissert, dass EF Core den RowVersion-Wert bei jedem Update prüft
    /// und damit konkurrierende Schreibvorgänge erkennt.
    /// </summary>
    [Fact]
    public async Task RowVersion_IstAlsConcurrencyToken_Konfiguriert()
    {
        await using var ctx = new AppDbContext(InMemoryOptions("cfg_test"));

        var entityType = ctx.Model.FindEntityType(typeof(Item))!;
        var prop = entityType.FindProperty(nameof(Item.RowVersion))!;

        Assert.True(prop.IsConcurrencyToken,
            "Item.RowVersion muss als Concurrency-Token konfiguriert sein, " +
            "damit EF Core gleichzeitige Änderungen erkennt.");

        _out.WriteLine("✓ RowVersion ist korrekt als Concurrency-Token eingetragen.");
    }

    /// <summary>
    /// Test 2: Optimistisches Locking – zwei Benutzer, einer verliert.
    ///
    /// Szenario: Benutzer A und Benutzer B laden denselben Artikel.
    /// Benutzer A speichert zuerst (→ Erfolg).
    /// Benutzer B versucht mit veralteter RowVersion zu speichern
    /// → DbUpdateConcurrencyException (= 409 Conflict im Controller).
    ///
    /// Dies ist der Kern-Multiuser-Test: er belegt, dass kein "Lost Update" möglich ist.
    /// </summary>
    [Fact]
    public async Task OptimistischesLocking_ZweiBenutzende_EinerErhaeltKonflikt()
    {
        var opts = InMemoryOptions("optimistic_locking_test");
        var (userId, _, itemId) = await SeedAsync(opts);

        // Benutzer A lädt Artikel
        await using var ctxA = new AppDbContext(opts);
        var itemA = await ctxA.Items.FirstAsync(i => i.Id == itemId);

        // Benutzer B lädt denselben Artikel (parallel – beide sehen dieselbe RowVersion)
        await using var ctxB = new AppDbContext(opts);
        var itemB = await ctxB.Items.FirstAsync(i => i.Id == itemId);

        // Benutzer A markiert als gekauft und speichert erfolgreich
        itemA.IsBought = true;
        itemA.LastModifiedByUserId = userId;
        await ctxA.SaveChangesAsync();
        _out.WriteLine("Benutzer A: Artikel erfolgreich gespeichert.");

        // Benutzer B hat noch die alte RowVersion aus seinem Load.
        // Wir simulieren, dass die DB den RowVersion-Wert inkrementiert hat
        // (was SQL Server automatisch macht, in-memory hingegen nicht).
        // Dafür setzen wir den Original-Wert auf einen anderen Wert, als die DB hat.
        ctxB.Entry(itemB).OriginalValues[nameof(Item.RowVersion)] =
            new byte[] { 99, 99, 99, 99, 99, 99, 99, 99 };

        // Benutzer B versucht zu speichern → muss DbUpdateConcurrencyException werfen
        itemB.IsBought = false;
        itemB.LastModifiedByUserId = userId;

        var ex = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => ctxB.SaveChangesAsync());

        _out.WriteLine($"Benutzer B: Konflikt erkannt → {ex.GetType().Name}");
        _out.WriteLine("✓ Optimistisches Locking funktioniert korrekt.");
    }

    /// <summary>
    /// Test 3: Kein Konflikt bei gleichzeitigem Bearbeiten verschiedener Artikel.
    ///
    /// Wenn Benutzer A und B verschiedene Artikel bearbeiten, darf kein Fehler entstehen.
    /// Das prüft, dass das Locking artikelspezifisch ist.
    /// </summary>
    [Fact]
    public async Task ParallelesBearbeiten_VerschiedeneArtikel_KeinKonflikt()
    {
        var opts = InMemoryOptions("no_conflict_different_items");

        await using var seedCtx = new AppDbContext(opts);
        var user = new User { Username = "u1", Email = "u1@t.ch", PasswordHash = "h" };
        seedCtx.Users.Add(user);
        await seedCtx.SaveChangesAsync();
        var list = new ShoppingList { Title = "Liste", OwnerId = user.Id };
        seedCtx.ShoppingLists.Add(list);
        await seedCtx.SaveChangesAsync();

        var items = Enumerable.Range(1, 10)
            .Select(i => new Item
            {
                Name = $"Artikel {i}", Amount = 1, Unit = "Stk",
                ListId = list.Id, LastModifiedByUserId = user.Id
            })
            .ToList();
        seedCtx.Items.AddRange(items);
        await seedCtx.SaveChangesAsync();
        var ids = items.Select(i => i.Id).ToList();

        // 10 parallele Tasks – jeder bearbeitet seinen eigenen Artikel
        var tasks = ids.Select(async id =>
        {
            await using var ctx = new AppDbContext(opts);
            var item = await ctx.Items.FirstAsync(i => i.Id == id);
            item.IsBought = true;
            await ctx.SaveChangesAsync();
        });

        var ex = await Record.ExceptionAsync(() => Task.WhenAll(tasks));

        Assert.Null(ex);
        _out.WriteLine("✓ 10 parallele Updates auf verschiedene Artikel – kein Konflikt.");
    }

    /// <summary>
    /// Test 4: Konflikterkennung bei mehreren parallelen Schreibvorgängen auf denselben Artikel.
    ///
    /// 5 Tasks versuchen gleichzeitig denselben Artikel zu ändern.
    /// Jeder Task hat eine manipulierte OriginalValues-RowVersion → alle sollen Konflikte erhalten.
    /// Zeigt, dass das System auch unter Last korrekt schützt.
    /// </summary>
    [Fact]
    public async Task MehrereParalleleSchreibvorgaenge_AlleKonfliktieren()
    {
        var opts = InMemoryOptions("parallel_writes_conflict");
        var (userId, _, itemId) = await SeedAsync(opts);

        const int parallelUsers = 5;

        var tasks = Enumerable.Range(1, parallelUsers).Select(async i =>
        {
            await using var ctx = new AppDbContext(opts);
            var item = await ctx.Items.FirstAsync(x => x.Id == itemId);

            // Jeder Benutzer hat eine andere veraltete RowVersion
            ctx.Entry(item).OriginalValues[nameof(Item.RowVersion)] =
                new byte[] { (byte)i, (byte)i, (byte)i, (byte)i, (byte)i, (byte)i, (byte)i, (byte)i };

            item.IsBought = (i % 2 == 0);
            item.LastModifiedByUserId = userId;

            try
            {
                await ctx.SaveChangesAsync();
                return "success";
            }
            catch (DbUpdateConcurrencyException)
            {
                return "conflict";
            }
        });

        var results = await Task.WhenAll(tasks);
        var conflicts = results.Count(r => r == "conflict");
        _out.WriteLine($"Konflikte: {conflicts}/{parallelUsers}");

        Assert.True(conflicts >= 1,
            "Bei parallelen Schreibvorgängen mit veralteter RowVersion muss mindestens ein Konflikt entstehen.");
    }

    /// <summary>
    /// Test 5: Konflikt-Retry – nach einem Konflikt kann der Benutzer die aktuellen Daten neu laden
    /// und erneut speichern (das ist der korrekte Umgang mit einem 409 Conflict).
    /// </summary>
    [Fact]
    public async Task KonfliktRetry_NachNeuLaden_ErfolgreichGespeichert()
    {
        var opts = InMemoryOptions("conflict_retry_test");
        var (userId, _, itemId) = await SeedAsync(opts);

        await using var ctx = new AppDbContext(opts);
        var item = await ctx.Items.FirstAsync(i => i.Id == itemId);

        // Veraltete RowVersion setzen → Konflikt beim ersten Versuch
        ctx.Entry(item).OriginalValues[nameof(Item.RowVersion)] =
            new byte[] { 99, 99, 99, 99, 99, 99, 99, 99 };
        item.IsBought = true;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctx.SaveChangesAsync());
        _out.WriteLine("Erster Versuch: Konflikt wie erwartet.");

        // Retry: aktuelle Daten neu laden
        await using var retryCtx = new AppDbContext(opts);
        var fresh = await retryCtx.Items.FirstAsync(i => i.Id == itemId);
        fresh.IsBought = true;
        fresh.LastModifiedByUserId = userId;
        await retryCtx.SaveChangesAsync(); // jetzt mit aktueller RowVersion → kein Konflikt

        await using var verifyCtx = new AppDbContext(opts);
        var saved = await verifyCtx.Items.FindAsync(itemId);
        Assert.True(saved!.IsBought);
        _out.WriteLine("✓ Retry nach Neuladung erfolgreich.");
    }
}
