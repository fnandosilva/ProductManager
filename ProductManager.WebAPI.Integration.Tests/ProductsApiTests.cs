using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ProductManager.Infrastructure.Data;
using ProductManager.Infrastructure.Data.Seed;

namespace ProductManager.WebAPI.Integration.Tests;

public class ProductsApiTests : IAsyncLifetime
{
    private readonly ProductManagerWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private HttpClient _anonymousClient = null!;

    public async Task InitializeAsync()
    {
        _anonymousClient = _factory.CreateClient();
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _anonymousClient.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task SeedDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DatabaseSeeder.SeedAsync(context);
    }

    // ---------- Authorization ----------

    [Fact]
    public async Task GetProducts_WithoutToken_ShouldReturn401()
    {
        var response = await _anonymousClient.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_WithoutToken_ShouldReturn401()
    {
        var response = await _anonymousClient.PostAsJsonAsync("/api/products", new
        {
            name = "Unauthorized Product",
            description = (string?)null,
            price = 10m,
            stock = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_WithInvalidToken_ShouldReturn401()
    {
        _anonymousClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await _anonymousClient.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- GET /api/products ----------

    [Fact]
    public async Task GetProducts_ShouldReturnSeededProductsWithStock()
    {
        await SeedDatabaseAsync();

        var response = await _client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        products.Should().NotBeNull();
        products!.Should().HaveCount(5);
        products.Should().OnlyContain(p => p.Stock >= 0);
    }

    [Fact]
    public async Task GetProducts_WithoutManualSeeding_ShouldStillReturnAutoSeededProducts()
    {
        // Program.cs seeds the database automatically on startup, so a brand-new
        // test database always contains the 5 seed products even without calling
        // DatabaseSeeder explicitly from the test.
        var response = await _client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        products.Should().HaveCount(5);
    }

    // ---------- GET /api/products/{id} ----------

    [Fact]
    public async Task GetById_WithExistingProduct_ShouldReturn200()
    {
        await SeedDatabaseAsync();

        var response = await _client.GetAsync("/api/products/100001");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        product!.Id.Should().Be(100_001);
    }

    [Fact]
    public async Task GetById_WithNonExistentProduct_ShouldReturn404()
    {
        var response = await _client.GetAsync("/api/products/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- POST /api/products ----------

    [Fact]
    public async Task CreateProduct_ShouldReturnSixDigitIdAndLocationHeader()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            name = "Integration Product",
            description = "Created in test",
            price = 25.00m,
            stock = 12
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        product.Should().NotBeNull();
        product!.Id.Should().BeInRange(100_000, 999_999);
        product.Stock.Should().Be(12);
    }

    [Fact]
    public async Task CreateProduct_CalledTwice_ShouldGenerateDifferentSequentialIds()
    {
        var first = await _client.PostAsJsonAsync("/api/products", new
        {
            name = "Product One",
            description = (string?)null,
            price = 10m,
            stock = 1
        });
        var second = await _client.PostAsJsonAsync("/api/products", new
        {
            name = "Product Two",
            description = (string?)null,
            price = 10m,
            stock = 1
        });

        var firstProduct = await first.Content.ReadFromJsonAsync<ProductResponse>();
        var secondProduct = await second.Content.ReadFromJsonAsync<ProductResponse>();

        secondProduct!.Id.Should().Be(firstProduct!.Id + 1);
    }

    [Theory]
    [InlineData("", 10, 5)]
    public async Task CreateProduct_WithEmptyName_ShouldReturn400(string name, double price, int stock)
    {
        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            name,
            description = (string?)null,
            price = (decimal)price,
            stock
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithZeroPrice_ShouldReturn400()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            name = "Invalid Price Product",
            description = (string?)null,
            price = 0m,
            stock = 5
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithNegativeStock_ShouldReturn400()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            name = "Invalid Stock Product",
            description = (string?)null,
            price = 10m,
            stock = -5
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- PUT /api/products/{id} ----------

    [Fact]
    public async Task UpdateProduct_WithExistingProduct_ShouldReturn200WithUpdatedData()
    {
        var created = await _client.PostAsJsonAsync("/api/products", new
        {
            name = "Original Name",
            description = "Original Description",
            price = 10m,
            stock = 5
        });
        var product = await created.Content.ReadFromJsonAsync<ProductResponse>();

        var response = await _client.PutAsJsonAsync($"/api/products/{product!.Id}", new
        {
            name = "Updated Name",
            description = "Updated Description",
            price = 20m,
            stock = 15
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ProductResponse>();
        updated!.Name.Should().Be("Updated Name");
        updated.Price.Should().Be(20m);
        updated.Stock.Should().Be(15);
    }

    [Fact]
    public async Task UpdateProduct_WithNonExistentProduct_ShouldReturn404()
    {
        var response = await _client.PutAsJsonAsync("/api/products/999999", new
        {
            name = "Doesn't matter",
            description = (string?)null,
            price = 10m,
            stock = 5
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_WithInvalidData_ShouldReturn400()
    {
        var created = await _client.PostAsJsonAsync("/api/products", new
        {
            name = "Original",
            description = (string?)null,
            price = 10m,
            stock = 5
        });
        var product = await created.Content.ReadFromJsonAsync<ProductResponse>();

        var response = await _client.PutAsJsonAsync($"/api/products/{product!.Id}", new
        {
            name = "",
            description = (string?)null,
            price = 10m,
            stock = 5
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- DELETE /api/products/{id} ----------

    [Fact]
    public async Task DeleteProduct_WithExistingProduct_ShouldReturn204AndRemoveIt()
    {
        var created = await _client.PostAsJsonAsync("/api/products", new
        {
            name = "To Delete",
            description = (string?)null,
            price = 10m,
            stock = 5
        });
        var product = await created.Content.ReadFromJsonAsync<ProductResponse>();

        var deleteResponse = await _client.DeleteAsync($"/api/products/{product!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/products/{product.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistentProduct_ShouldReturn404()
    {
        var response = await _client.DeleteAsync("/api/products/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- Stock management ----------

    [Fact]
    public async Task AddToStock_ShouldIncreaseStockAndReturn200()
    {
        var created = await _client.PostAsJsonAsync("/api/products", new
        {
            name = "Stock Product",
            description = (string?)null,
            price = 10m,
            stock = 5
        });
        var product = await created.Content.ReadFromJsonAsync<ProductResponse>();

        var response = await _client.PostAsync($"/api/products/{product!.Id}/add-to-stock/10", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/products/{product.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();
        updated!.Stock.Should().Be(15);
    }

    [Fact]
    public async Task AddToStock_WithNonExistentProduct_ShouldReturn404()
    {
        var response = await _client.PostAsync("/api/products/999999/add-to-stock/10", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DecrementStock_WithSufficientStock_ShouldDecreaseStockAndReturn200()
    {
        var created = await _client.PostAsJsonAsync("/api/products", new
        {
            name = "Stock Product",
            description = (string?)null,
            price = 10m,
            stock = 10
        });
        var product = await created.Content.ReadFromJsonAsync<ProductResponse>();

        var response = await _client.PostAsync($"/api/products/{product!.Id}/decrement-stock/4", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/products/{product.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();
        updated!.Stock.Should().Be(6);
    }

    [Fact]
    public async Task DecrementStock_WithInsufficientStock_ShouldReturn400()
    {
        var created = await _client.PostAsJsonAsync("/api/products", new
        {
            name = "Low Stock Product",
            description = (string?)null,
            price = 10m,
            stock = 2
        });
        var product = await created.Content.ReadFromJsonAsync<ProductResponse>();

        var response = await _client.PostAsync($"/api/products/{product!.Id}/decrement-stock/10", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DecrementStock_WithNonExistentProduct_ShouldReturn404()
    {
        var response = await _client.PostAsync("/api/products/999999/decrement-stock/1", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- Search ----------

    [Fact]
    public async Task Search_WithMatchingName_ShouldReturnMatchingProducts()
    {
        await SeedDatabaseAsync();

        var response = await _client.GetAsync("/api/products/search?name=Lens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        products.Should().Contain(p => p.Name.Contains("Lens"));
    }

    [Fact]
    public async Task Search_WithoutNameParameter_ShouldReturn400()
    {
        var response = await _client.GetAsync("/api/products/search?name=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- Stock level filter ----------

    [Fact]
    public async Task GetByStockLevel_ShouldReturnProductsWithinRange()
    {
        await SeedDatabaseAsync();

        var response = await _client.GetAsync("/api/products/stock-level?min=0&max=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        products.Should().OnlyContain(p => p.Stock >= 0 && p.Stock <= 50);
    }

    [Fact]
    public async Task GetByStockLevel_WithMaxLessThanMin_ShouldReturn400()
    {
        var response = await _client.GetAsync("/api/products/stock-level?min=100&max=10");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record ProductResponse(
        int Id,
        string Name,
        string? Description,
        decimal Price,
        int Stock);
}
