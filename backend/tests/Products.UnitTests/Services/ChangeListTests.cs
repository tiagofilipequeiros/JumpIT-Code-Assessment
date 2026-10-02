using Products.Application.Services;

namespace Products.UnitTests;

public class ChangeListTests
{
    [Fact]
    public void Lists_only_changed_fields()
    {
        var changes = new ChangeList();
        changes.Add("Name", "Lens", "Lens");
        changes.Add("Price", 10.00m, 12.50m);
        changes.Add("Stock", 5, 7);

        Assert.Equal("Price 10.00 → 12.50; Stock 5 → 7", changes.ToString());
    }

    [Fact]
    public void Says_when_nothing_changed()
    {
        Assert.Equal("No changes", new ChangeList().ToString());
    }
}
