using Microsoft.EntityFrameworkCore;
using CoShop.Models;

namespace CoShop.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ShoppingListMember> ShoppingListMembers => Set<ShoppingListMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── User ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50).IsRequired();
            e.Property(u => u.Email).HasMaxLength(200).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role)
             .HasDefaultValue(UserRole.User)
             .IsRequired();
        });

        // ── ShoppingList ──────────────────────────────────────────────────────
        modelBuilder.Entity<ShoppingList>(e =>
        {
            e.Property(l => l.Title).HasMaxLength(100).IsRequired();

            e.HasOne(l => l.Owner)
             .WithMany(u => u.OwnedLists)
             .HasForeignKey(l => l.OwnerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Item ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<Item>(e =>
        {
            e.Property(i => i.Name).HasMaxLength(100).IsRequired();
            e.Property(i => i.Unit).HasMaxLength(30);
            e.Property(i => i.Amount).HasColumnType("decimal(10,2)");

            // SQL Server rowversion — DB auto-increments on every INSERT/UPDATE.
            // EF Core uses this for optimistic concurrency automatically.
            e.Property(i => i.RowVersion)
             .IsRowVersion()
             .IsRequired();

            // FK to list — cascade delete items when list is deleted
            e.HasOne(i => i.List)
             .WithMany(l => l.Items)
             .HasForeignKey(i => i.ListId)
             .OnDelete(DeleteBehavior.Cascade);

            // FK to last modifier — restrict to prevent accidental user deletion
            // SQL Server disallows multiple cascade paths, so this must be Restrict
            e.HasOne(i => i.LastModifiedByUser)
             .WithMany(u => u.LastModifiedItems)
             .HasForeignKey(i => i.LastModifiedByUserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(i => i.ListId);
        });

        // ── ShoppingListMember (composite PK) ─────────────────────────────────
        modelBuilder.Entity<ShoppingListMember>(e =>
        {
            e.HasKey(m => new { m.UserId, m.ListId });

            // SQL Server: only one cascade path per table allowed.
            // User → Member: Cascade (deleting a user removes their memberships)
            e.HasOne(m => m.User)
             .WithMany(u => u.SharedLists)
             .HasForeignKey(m => m.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            // List → Member: must be NoAction to avoid multiple cascade paths
            // (ShoppingList already cascades via OwnerId → Users → ShoppingListMembers)
            e.HasOne(m => m.ShoppingList)
             .WithMany(l => l.Members)
             .HasForeignKey(m => m.ListId)
             .OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(m => m.UserId);
        });
    }
}