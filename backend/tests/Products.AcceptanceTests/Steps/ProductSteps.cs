using System.Net;
using Products.AcceptanceTests.Support;
using Products.Application.Dtos.Categories;
using Products.Application.Dtos.Products;
using Products.TestSupport;
using Reqnroll;
using Xunit;

namespace Products.AcceptanceTests.Steps;

[Binding]
public sealed class ProductSteps(ScenarioApi api)
{
    private const int Admin = 1;
    private readonly Dictionary<string, ProductResponse> _openedBy = [];

    [Given("I am signed in as {string}")]
    public void SignIn(string userName) => api.SignedInUserId = ScenarioApi.UserId(userName);

    [Given("the product {string} has {int} in stock")]
    public async Task SetStock(string name, int stock)
    {
        var product = await FindAsync(name);
        var response = await api.Client(Admin).PutJsonAsync($"/api/products/{product.Id}", UpdateOf(product, stock: stock));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [When("I remove {int} from the stock of {string}")]
    public async Task RemoveStock(int quantity, string name) =>
        api.Responses.Add(await api.Client().PostAsync($"/api/products/{(await FindAsync(name)).Id}/decrement-stock/{quantity}", null));

    [When("I add {int} to the stock of {string}")]
    public async Task AddStock(int quantity, string name) =>
        api.Responses.Add(await api.Client().PostAsync($"/api/products/{(await FindAsync(name)).Id}/add-to-stock/{quantity}", null));

    [When("{int} people each remove {int} from the stock of {string} at the same time")]
    public async Task RemoveStockInParallel(int people, int quantity, string name)
    {
        var id = (await FindAsync(name)).Id;
        var requests = Enumerable.Range(0, people)
            .Select(_ => api.Client().PostAsync($"/api/products/{id}/decrement-stock/{quantity}", null));
        api.Responses.AddRange(await Task.WhenAll(requests));
    }

    [When("I create a product {string} priced {decimal} with {int} in stock in {string}")]
    public async Task Create(string name, decimal price, int stock, string categoryName)
    {
        var categories = await (await api.Client(Admin).GetAsync("/api/categories?includeHidden=true")).ReadAsync<List<CategoryResponse>>();
        var categoryId = categories.Single(c => c.Name == categoryName).Id;
        api.Responses.Add(await api.Client().PostJsonAsync("/api/products", new { name, price, stock, categoryId }));
    }

    [Given("{string} and {string} both open the product {string}")]
    public async Task BothOpen(string firstUser, string secondUser, string name)
    {
        var product = await FindAsync(name);
        _openedBy[firstUser] = product;
        _openedBy[secondUser] = product;
    }

    [When("{string} saves the price {decimal}")]
    public async Task SavePrice(string userName, decimal price)
    {
        var product = _openedBy[userName];
        api.Responses.Add(await api.ClientFor(userName).PutJsonAsync($"/api/products/{product.Id}", UpdateOf(product, price: price)));
    }

    [When("I delete the product {string}")]
    public async Task Delete(string name) =>
        api.Responses.Add(await api.Client().DeleteAsync($"/api/products/{(await FindAsync(name)).Id}"));

    [Given("I disable the product {string}")]
    [When("I disable the product {string}")]
    public async Task Disable(string name) =>
        api.Responses.Add(await api.Client().PostAsync($"/api/products/{(await FindAsync(name)).Id}/disable", null));

    [When("I enable the product {string}")]
    public async Task Enable(string name) =>
        api.Responses.Add(await api.Client().PostAsync($"/api/products/{(await FindAsync(name)).Id}/enable", null));

    [When("I delete the category {string}")]
    public async Task DeleteCategory(string name)
    {
        var categories = await (await api.Client(Admin).GetAsync("/api/categories?includeHidden=true")).ReadAsync<List<CategoryResponse>>();
        api.Responses.Add(await api.Client().DeleteAsync($"/api/categories/{categories.Single(c => c.Name == name).Id}"));
    }

    [Then("the request succeeds")]
    public void RequestSucceeds() => Assert.True(api.LastResponse.IsSuccessStatusCode, $"Status was {api.LastResponse.StatusCode}.");

    [Then("the request is rejected with {string}")]
    [Then("the last request is rejected with {string}")]
    public async Task RequestRejected(string code)
    {
        Assert.False(api.LastResponse.IsSuccessStatusCode);
        Assert.Equal(code, await api.LastResponse.ErrorCodeAsync());
    }

    [Then("{int} requests succeed and {int} are rejected with {string}")]
    public async Task ParallelOutcome(int succeeded, int rejected, string code)
    {
        Assert.Equal(succeeded, api.Responses.Count(r => r.IsSuccessStatusCode));
        var failures = api.Responses.Where(r => !r.IsSuccessStatusCode).ToList();
        Assert.Equal(rejected, failures.Count);
        Assert.All(await Task.WhenAll(failures.Select(r => r.ErrorCodeAsync())), actual => Assert.Equal(code, actual));
    }

    [Then("the product {string} has {int} in stock")]
    public async Task HasStock(string name, int stock) => Assert.Equal(stock, (await FindAsync(name)).Stock);

    [Then("the product {string} costs {decimal}")]
    public async Task Costs(string name, decimal price) => Assert.Equal(price, (await FindAsync(name)).Price);

    [Then("the new product has a 6-digit id")]
    public async Task NewProductHasSixDigitId()
    {
        var product = await api.LastResponse.ReadAsync<ProductResponse>();
        Assert.InRange(product.Id, 100000, 999999);
    }

    [Then("searching for {string} finds {string}")]
    public async Task SearchFinds(string term, string name)
    {
        var products = await (await api.Client().GetAsync($"/api/products/search?name={term}")).ReadAsync<List<ProductResponse>>();
        Assert.Contains(products, p => p.Name == name);
    }

    [Then("{string} does not see {string}")]
    public async Task DoesNotSee(string userName, string name) =>
        Assert.DoesNotContain(await ListAsync(userName), p => p.Name == name);

    [Then("{string} sees {string}")]
    public async Task Sees(string userName, string name) =>
        Assert.Contains(await ListAsync(userName), p => p.Name == name);

    [Then("{string} sees {string} when showing hidden products")]
    public async Task SeesWhenShowingHidden(string userName, string name) =>
        Assert.Contains(await ListAsync(userName, "?statuses=Disabled"), p => p.Name == name);

    [Then("the product {string} is in the category {string}")]
    public async Task IsInCategory(string name, string categoryName) =>
        Assert.Equal(categoryName, (await FindAsync(name)).CategoryName);

    // Looks a product up by name as the admin, who sees everything.
    private async Task<ProductResponse> FindAsync(string name)
    {
        var products = await (await api.Client(Admin).GetAsync("/api/products")).ReadAsync<List<ProductResponse>>();
        return products.Single(p => p.Name == name);
    }

    private async Task<List<ProductResponse>> ListAsync(string userName, string query = "") =>
        await (await api.ClientFor(userName).GetAsync($"/api/products{query}")).ReadAsync<List<ProductResponse>>();

    private static object UpdateOf(ProductResponse product, decimal? price = null, int? stock = null) => new
    {
        name = product.Name,
        description = product.Description,
        price = price ?? product.Price,
        stock = stock ?? product.Stock,
        categoryId = product.CategoryId,
        rowVersion = product.RowVersion,
    };
}
