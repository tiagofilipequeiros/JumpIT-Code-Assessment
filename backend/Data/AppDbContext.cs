using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private const int EnumMaxLength = 20;

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserMetric> UserMetrics => Set<UserMetric>();

    // All dates are stored in UTC. SQL Server does not keep that information, so mark dates as UTC when reading them back.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    private class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The database hands out product IDs, so multiple API instances never create duplicates.
        modelBuilder.HasSequence<int>("ProductIds")
            .StartsAt(ProductLimits.SequenceStart)
            .HasMin(ProductLimits.IdMin)
            .HasMax(ProductLimits.IdMax)
            .IsCyclic(false);

        modelBuilder.Entity<Product>(product =>
        {
            product.Property(p => p.Id)
                .HasDefaultValueSql("NEXT VALUE FOR ProductIds")
                .ValueGeneratedOnAdd();

            product.Property(p => p.Name)
                .HasMaxLength(ProductLimits.NameMaxLength)
                .IsRequired();

            product.Property(p => p.Description)
                .HasMaxLength(ProductLimits.DescriptionMaxLength);

            product.Property(p => p.Price)
                .HasPrecision(ProductLimits.PricePrecision, ProductLimits.PriceScale);

            // Existing rows (before categories existed) go to Uncategorized.
            product.Property(p => p.CategoryId)
                .HasDefaultValue(Category.UncategorizedId);

            // Changes on every update; used to detect conflicting edits.
            product.Property(p => p.RowVersion)
                .IsRowVersion();

            product.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            product.HasOne(p => p.UpdatedByUser)
                .WithMany()
                .HasForeignKey(p => p.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            product.ToTable(t =>
            {
                // SQL Server keeps every previous version of each row in ProductsHistory.
                t.IsTemporal();

                // Last line of defence: the database itself rejects invalid values.
                t.HasCheckConstraint("CK_Products_Price", "[Price] >= 0");
                t.HasCheckConstraint("CK_Products_Stock", $"[Stock] >= 0 AND [Stock] <= {ProductLimits.StockMax}");
            });

            product.HasData(SeedData.Products);
        });

        modelBuilder.Entity<Category>(category =>
        {
            category.Property(c => c.Name)
                .HasMaxLength(CategoryLimits.NameMaxLength)
                .IsRequired();

            category.HasIndex(c => c.Name).IsUnique();

            category.Property(c => c.RowVersion)
                .IsRowVersion();

            category.HasData(SeedData.Categories);
        });

        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.Name).HasMaxLength(100).IsRequired();
            user.Property(u => u.Email).HasMaxLength(200).IsRequired();
            user.HasIndex(u => u.Email).IsUnique();

            // Enums are stored as text so the database is readable and new values need no migration.
            user.Property(u => u.Role).HasConversion<string>().HasMaxLength(EnumMaxLength);

            user.HasData(SeedData.Users);
        });

        modelBuilder.Entity<UserMetric>(metric =>
        {
            metric.Property(m => m.Entity).HasConversion<string>().HasMaxLength(EnumMaxLength);
            metric.Property(m => m.Action).HasConversion<string>().HasMaxLength(EnumMaxLength);
            metric.Property(m => m.Details).HasMaxLength(UserMetric.DetailsMaxLength);

            metric.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // EntityId has no foreign key on purpose: it can point to any table, and history must survive deletes.
            metric.HasIndex(m => new { m.Entity, m.Action });
            metric.HasIndex(m => m.CreatedAt);
        });
    }
}
