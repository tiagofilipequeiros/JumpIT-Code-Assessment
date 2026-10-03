namespace Products.Domain.Entities;

// Single place for product field limits (used by the database and validation).
public static class ProductLimits
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    public const int PricePrecision = 10;
    public const int PriceScale = 2;
    public const string PriceMax = "99999999.99";

    public const int StockMax = 1_000_000;
    public const int QuantityMin = 1;
    public const int QuantityMax = 100_000;

    // At or below this (and above 0) a product counts as low stock.
    public const int LowStockThreshold = 5;

    // 6-digit product IDs.
    public const int IdMin = 100000;
    public const int IdMax = 999999;

    // 100000-100099 are reserved for seed data; the sequence starts after them.
    public const int SequenceStart = 100100;
}
