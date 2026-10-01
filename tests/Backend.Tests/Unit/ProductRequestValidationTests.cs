using System.ComponentModel.DataAnnotations;
using Backend.Dtos;
using Backend.Models;

namespace Backend.Tests.Unit;

public class ProductRequestValidationTests
{
    private static ProductRequest Valid(string name = "Lens", string? description = null, decimal? price = 10m, int? stock = 5, int? categoryId = 2) =>
        new() { Name = name, Description = description, Price = price, Stock = stock, CategoryId = categoryId };

    private static List<string> InvalidFields(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results.SelectMany(r => r.MemberNames).ToList();
    }

    [Fact]
    public void Valid_request_has_no_errors()
    {
        Assert.Empty(InvalidFields(Valid()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Name_too_short_is_invalid(string name)
    {
        Assert.Contains(nameof(ProductRequest.Name), InvalidFields(Valid(name: name)));
    }

    [Fact]
    public void Name_too_long_is_invalid()
    {
        var name = new string('a', ProductLimits.NameMaxLength + 1);
        Assert.Contains(nameof(ProductRequest.Name), InvalidFields(Valid(name: name)));
    }

    [Fact]
    public void Description_too_long_is_invalid()
    {
        var description = new string('a', ProductLimits.DescriptionMaxLength + 1);
        Assert.Contains(nameof(ProductRequest.Description), InvalidFields(Valid(description: description)));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.999)]
    [InlineData(100000000)]
    public void Invalid_price_is_rejected(double price)
    {
        Assert.Contains(nameof(ProductRequest.Price), InvalidFields(Valid(price: (decimal)price)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(ProductLimits.StockMax + 1)]
    public void Invalid_stock_is_rejected(int stock)
    {
        Assert.Contains(nameof(ProductRequest.Stock), InvalidFields(Valid(stock: stock)));
    }

    [Fact]
    public void Missing_required_fields_are_rejected()
    {
        var fields = InvalidFields(new ProductRequest { Name = "Lens" });
        Assert.Contains(nameof(ProductRequest.Price), fields);
        Assert.Contains(nameof(ProductRequest.Stock), fields);
        Assert.Contains(nameof(ProductRequest.CategoryId), fields);
    }
}
