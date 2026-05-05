using CoShop.Data;
using CoShop.Models;
using Microsoft.EntityFrameworkCore;

namespace CoShop.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Only seed if database is empty
        if (await db.Users.AnyAsync()) return;

        // ── Users ─────────────────────────────────────────────────────────────
        // Admin account
        var admin = new User
        {
            Username = "admin",
            Email = "admin@coshop.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234!"),
            Role = CoShop.Models.UserRole.Admin
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        // Regular users
        var alice = new User
        {
            Username = "alice",
            Email = "alice@coshop.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234!")
        };
        var bob = new User
        {
            Username = "bob",
            Email = "bob@coshop.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234!")
        };
        var carol = new User
        {
            Username = "carol",
            Email = "carol@coshop.dev",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234!")
        };

        db.Users.AddRange(alice, bob, carol);
        await db.SaveChangesAsync();

        // ── Shopping Lists ────────────────────────────────────────────────────
        var weeklyList = new ShoppingList
        {
            Title = "Wocheneinkauf",
            OwnerId = alice.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };
        var partyList = new ShoppingList
        {
            Title = "Geburtstagsparty 🎉",
            OwnerId = alice.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var hardwareList = new ShoppingList
        {
            Title = "Baumarkt",
            OwnerId = bob.Id,
            CreatedAt = DateTime.UtcNow
        };

        db.ShoppingLists.AddRange(weeklyList, partyList, hardwareList);
        await db.SaveChangesAsync();

        // ── Members (shared lists) ────────────────────────────────────────────
        // Bob and Carol are members of Alice's weekly list
        db.ShoppingListMembers.AddRange(
            new ShoppingListMember { UserId = bob.Id,   ListId = weeklyList.Id, JoinedAt = DateTime.UtcNow.AddDays(-3) },
            new ShoppingListMember { UserId = carol.Id, ListId = weeklyList.Id, JoinedAt = DateTime.UtcNow.AddDays(-2) },
            // Alice is member of Bob's hardware list
            new ShoppingListMember { UserId = alice.Id, ListId = hardwareList.Id, JoinedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // ── Items: Weekly Shopping ────────────────────────────────────────────
        db.Items.AddRange(
            new Item { Name = "Vollmilch",        Amount = 2,   Unit = "L",   IsBought = true,  ListId = weeklyList.Id, LastModifiedByUserId = alice.Id },
            new Item { Name = "Butter",           Amount = 250, Unit = "g",   IsBought = true,  ListId = weeklyList.Id, LastModifiedByUserId = bob.Id   },
            new Item { Name = "Eier",             Amount = 12,  Unit = "Stk", IsBought = false, ListId = weeklyList.Id, LastModifiedByUserId = alice.Id },
            new Item { Name = "Vollkornbrot",     Amount = 1,   Unit = "Stk", IsBought = false, ListId = weeklyList.Id, LastModifiedByUserId = alice.Id },
            new Item { Name = "Hähnchenbrust",    Amount = 500, Unit = "g",   IsBought = false, ListId = weeklyList.Id, LastModifiedByUserId = bob.Id   },
            new Item { Name = "Tomaten",          Amount = 6,   Unit = "Stk", IsBought = false, ListId = weeklyList.Id, LastModifiedByUserId = carol.Id },
            new Item { Name = "Paprika",          Amount = 3,   Unit = "Stk", IsBought = false, ListId = weeklyList.Id, LastModifiedByUserId = carol.Id },
            new Item { Name = "Joghurt",          Amount = 4,   Unit = "Stk", IsBought = true,  ListId = weeklyList.Id, LastModifiedByUserId = alice.Id },
            new Item { Name = "Orangensaft",      Amount = 1,   Unit = "L",   IsBought = false, ListId = weeklyList.Id, LastModifiedByUserId = alice.Id },
            new Item { Name = "Müsli",            Amount = 1,   Unit = "Stk", IsBought = false, ListId = weeklyList.Id, LastModifiedByUserId = bob.Id   }
        );

        // ── Items: Party ──────────────────────────────────────────────────────
        db.Items.AddRange(
            new Item { Name = "Cola",             Amount = 4,   Unit = "L",   IsBought = false, ListId = partyList.Id, LastModifiedByUserId = alice.Id },
            new Item { Name = "Chips",            Amount = 5,   Unit = "Tüten",IsBought = false,ListId = partyList.Id, LastModifiedByUserId = alice.Id },
            new Item { Name = "Geburtstagstorte", Amount = 1,   Unit = "Stk", IsBought = false, ListId = partyList.Id, LastModifiedByUserId = alice.Id },
            new Item { Name = "Kerzen",           Amount = 24,  Unit = "Stk", IsBought = true,  ListId = partyList.Id, LastModifiedByUserId = alice.Id },
            new Item { Name = "Pappteller",       Amount = 20,  Unit = "Stk", IsBought = false, ListId = partyList.Id, LastModifiedByUserId = alice.Id },
            new Item { Name = "Bier",             Amount = 2,   Unit = "Kiste",IsBought = false, ListId = partyList.Id, LastModifiedByUserId = alice.Id },
            new Item { Name = "Wein",             Amount = 3,   Unit = "Flaschen",IsBought = false,ListId = partyList.Id, LastModifiedByUserId = alice.Id }
        );

        // ── Items: Hardware Store ─────────────────────────────────────────────
        db.Items.AddRange(
            new Item { Name = "Schrauben M6",     Amount = 50,  Unit = "Stk", IsBought = false, ListId = hardwareList.Id, LastModifiedByUserId = bob.Id },
            new Item { Name = "Dübel",            Amount = 20,  Unit = "Stk", IsBought = true,  ListId = hardwareList.Id, LastModifiedByUserId = bob.Id },
            new Item { Name = "Wandfarbe Weiss",  Amount = 5,   Unit = "L",   IsBought = false, ListId = hardwareList.Id, LastModifiedByUserId = bob.Id },
            new Item { Name = "Malerrolle",       Amount = 2,   Unit = "Stk", IsBought = false, ListId = hardwareList.Id, LastModifiedByUserId = alice.Id },
            new Item { Name = "Abdeckfolie",      Amount = 1,   Unit = "Rolle",IsBought = false, ListId = hardwareList.Id, LastModifiedByUserId = bob.Id }
        );

        await db.SaveChangesAsync();
    }
}