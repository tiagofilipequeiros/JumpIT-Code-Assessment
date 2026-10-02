using Products.Domain.Entities;

namespace Products.Infrastructure.Persistence;

// Initial data, inserted by migrations. Dates are fixed so the migrations never change.
public static class SeedData
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private const int Objectives = 2;
    private const int Eyepieces = 3;
    private const int Illumination = 4;
    private const int Consumables = 5;
    private const int Accessories = 6;
    private const int Legacy = 7;

    public static readonly User[] Users =
    [
        new() { Id = 1, Name = "Alex Admin", Email = "admin@example.com", Role = Role.Admin, CreatedAt = SeedDate },
        new() { Id = 2, Name = "Erin Editor", Email = "editor@example.com", Role = Role.Editor, CreatedAt = SeedDate },
        new() { Id = 3, Name = "Sam User", Email = "sam@example.com", Role = Role.User, CreatedAt = SeedDate },
        new() { Id = 4, Name = "Taylor User", Email = "taylor@example.com", Role = Role.User, CreatedAt = SeedDate },
    ];

    public static readonly Category[] Categories =
    [
        CreateCategory(Category.UncategorizedId, "Uncategorized"),
        CreateCategory(Objectives, "Objectives"),
        CreateCategory(Eyepieces, "Eyepieces"),
        CreateCategory(Illumination, "Illumination"),
        CreateCategory(Consumables, "Consumables"),
        CreateCategory(Accessories, "Accessories"),
        CreateCategory(Legacy, "Legacy", isActive: false),
    ];

    public static readonly Product[] Products =
    [
        CreateProduct(100000, "Microscope Objective 10x", "Plan achromat objective, 10x magnification.", 249.90m, 42, Objectives),
        CreateProduct(100001, "Microscope Objective 40x", "Plan achromat objective, 40x magnification.", 389.00m, 18, Objectives),
        CreateProduct(100002, "Microscope Objective 100x Oil", "Oil immersion objective, 100x magnification.", 899.00m, 3, Objectives),
        CreateProduct(100003, "Eyepiece 10x", "Wide-field eyepiece, 10x magnification.", 79.50m, 120, Eyepieces),
        CreateProduct(100004, "LED Illuminator", "Replacement LED illumination module.", 159.00m, 0, Illumination),
        CreateProduct(100005, "Microscope Slides (50 pack)", "Pre-cleaned glass slides, 76 x 26 mm.", 12.90m, 500, Consumables),
        CreateProduct(100006, "Cover Glasses (100 pack)", "Square cover glasses, 22 x 22 mm.", 9.90m, 350, Consumables),
        CreateProduct(100007, "Immersion Oil 20 ml", "Low-fluorescence immersion oil.", 24.00m, 7, Consumables),
        CreateProduct(100008, "Camera Adapter C-Mount", "Adapter for mounting C-mount cameras.", 189.00m, 12, Accessories),
        CreateProduct(100009, "Lens Cleaning Kit", "Brush, blower and lens tissue.", 19.90m, 65, Accessories),
        CreateProduct(100010, "Stage Micrometer", "Calibration slide, 1 mm in 100 divisions.", 69.00m, 1, Category.UncategorizedId),
        CreateProduct(100011, "Dust Cover", "Vinyl dust cover for upright microscopes.", 29.00m, 0, Accessories, isActive: false),
        CreateProduct(100012, "Halogen Bulb 20 W", "Replaced by the LED illuminator.", 14.50m, 25, Legacy),
    ];

    private static Category CreateCategory(int id, string name, bool isActive = true) => new()
    {
        Id = id,
        Name = name,
        IsActive = isActive,
        CreatedAt = SeedDate,
        UpdatedAt = SeedDate,
    };

    private static Product CreateProduct(
        int id, string name, string description, decimal price, int stock, int categoryId, bool isActive = true) => new()
    {
        Id = id,
        Name = name,
        Description = description,
        Price = price,
        Stock = stock,
        CategoryId = categoryId,
        IsActive = isActive,
        CreatedAt = SeedDate,
        UpdatedAt = SeedDate,
    };
}
