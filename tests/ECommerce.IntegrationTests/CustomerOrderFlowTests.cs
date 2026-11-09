using ECommerce.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerce.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public class CustomerOrderFlowTests : IAsyncLifetime
{
    private readonly SqlServerFixture _database;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];
    private ApiFactory _factory = null!;
    private HttpClient _admin = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public CustomerOrderFlowTests(SqlServerFixture database) => _database = database;

    public ValueTask InitializeAsync()
    {
        Assert.SkipWhen(_database.UnavailableReason is not null, _database.UnavailableReason ?? string.Empty);

        _factory = new ApiFactory(_database.ConnectionString, new Dictionary<string, string?>
        {
            ["Checkout:ShippingFee"] = "49.90",
            ["Checkout:FreeShippingThreshold"] = "750",
            ["Checkout:BankTransfer:Iban"] = "TR00 0000 0000 0000 0000 0000 00"
        });
        _admin = _factory.CreateAdminClient();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Customer_registers_orders_with_bank_transfer_and_admin_fulfils()
    {
        var (variantId, productId) = await CreatePublishedProductAsync(stock: 5, price: 150m);
        var customer = await RegisterCustomerAsync();

        var address = await customer.PostAsJsonAsync("/api/v1/customer/addresses",
            new { title = "Ev", fullName = "Ali Veli", phone = "05551112233", city = "İstanbul", district = "Kadıköy", addressLine = "Moda Cd. No:1" }, Ct);
        address.StatusCode.ShouldBe(HttpStatusCode.Created);
        var addressId = (await address.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetInt32();

        (await customer.PostAsJsonAsync("/api/v1/customer/cart/items", new { variantId, quantity = 2 }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var preview = await customer.GetFromJsonAsync<JsonElement>("/api/v1/customer/checkout/preview?paymentMethod=BankTransfer", Ct);
        preview.GetProperty("total").GetDecimal().ShouldBe(349.90m);

        var placed = await customer.PostAsJsonAsync("/api/v1/customer/orders", new { addressId, paymentMethod = "BankTransfer" }, Ct);
        placed.StatusCode.ShouldBe(HttpStatusCode.Created);
        var order = await placed.Content.ReadFromJsonAsync<JsonElement>(Ct);
        order.GetProperty("status").GetString().ShouldBe("PendingPayment");
        order.GetProperty("bankTransfer").GetProperty("iban").GetString().ShouldBe("TR00 0000 0000 0000 0000 0000 00");
        var orderId = order.GetProperty("id").GetInt32();
        var orderNumber = order.GetProperty("orderNumber").GetString()!;

        (await StockAsync(productId)).ShouldBe(3);
        (await customer.GetFromJsonAsync<JsonElement>("/api/v1/customer/cart", Ct)).GetProperty("items").GetArrayLength().ShouldBe(0);

        (await _admin.PostAsync($"/api/v1/orders/{orderId}/confirm-payment", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _admin.PostAsync($"/api/v1/orders/{orderId}/prepare", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _admin.PostAsJsonAsync($"/api/v1/orders/{orderId}/ship", new { cargoProvider = "Yurtiçi Kargo", trackingNumber = "TRK1" }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        var seen = await customer.GetFromJsonAsync<JsonElement>($"/api/v1/customer/orders/{orderNumber}", Ct);
        seen.GetProperty("status").GetString().ShouldBe("Shipped");
        seen.GetProperty("trackingNumber").GetString().ShouldBe("TRK1");
        seen.GetProperty("canCustomerCancel").GetBoolean().ShouldBeFalse();

        var lateCancel = await customer.PostAsync($"/api/v1/customer/orders/{orderNumber}/cancel", null, Ct);
        lateCancel.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Customer_cancellation_restores_stock()
    {
        var (variantId, productId) = await CreatePublishedProductAsync(stock: 4, price: 100m);
        var customer = await RegisterCustomerAsync();
        var addressId = await AddAddressAsync(customer);
        await customer.PostAsJsonAsync("/api/v1/customer/cart/items", new { variantId, quantity = 3 }, Ct);
        var order = await (await customer.PostAsJsonAsync("/api/v1/customer/orders", new { addressId, paymentMethod = "CashOnDelivery" }, Ct))
            .Content.ReadFromJsonAsync<JsonElement>(Ct);
        (await StockAsync(productId)).ShouldBe(1);

        var cancel = await customer.PostAsync($"/api/v1/customer/orders/{order.GetProperty("orderNumber").GetString()}/cancel", null, Ct);

        cancel.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await StockAsync(productId)).ShouldBe(4);
    }

    [Fact]
    public async Task Cart_rejects_more_than_stock()
    {
        var (variantId, _) = await CreatePublishedProductAsync(stock: 1, price: 100m);
        var customer = await RegisterCustomerAsync();

        var response = await customer.PostAsJsonAsync("/api/v1/customer/cart/items", new { variantId, quantity = 2 }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Customer_and_admin_tokens_are_not_interchangeable()
    {
        var customer = await RegisterCustomerAsync();

        (await customer.GetAsync("/api/v1/orders", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await customer.GetAsync("/api/v1/catalog/products", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _admin.GetAsync("/api/v1/customer/cart", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _factory.CreateClient().GetAsync("/api/v1/customer/cart", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Duplicate_registration_returns_409_and_login_works()
    {
        var email = $"musteri-{_suffix}@example.com";
        await RegisterCustomerAsync(email);

        var duplicate = await _factory.CreateClient().PostAsJsonAsync("/api/v1/customer/auth/register",
            new { email = email.ToUpperInvariant(), password = "Musteri-Parola1", firstName = "Ali", lastName = "Veli" }, Ct);
        var login = await _factory.CreateClient().PostAsJsonAsync("/api/v1/customer/auth/login", new { userName = email, password = "Musteri-Parola1" }, Ct);

        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        login.Headers.GetValues("Set-Cookie").ShouldContain(c => c.StartsWith("ecommerce_customer_refresh=") && c.Contains("path=/api/v1/customer/auth"));
    }

    private async Task<HttpClient> RegisterCustomerAsync(string? email = null)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/v1/customer/auth/register",
            new { email = email ?? $"musteri-{Guid.NewGuid():N}@example.com", password = "Musteri-Parola1", firstName = "Ali", lastName = "Veli" }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("accessToken").GetString();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<int> AddAddressAsync(HttpClient customer)
    {
        var response = await customer.PostAsJsonAsync("/api/v1/customer/addresses",
            new { title = "Ev", fullName = "Ali Veli", phone = "05551112233", city = "İstanbul", district = "Kadıköy", addressLine = "Moda Cd. No:1" }, Ct);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetInt32();
    }

    private async Task<(int VariantId, int ProductId)> CreatePublishedProductAsync(int stock, decimal price)
    {
        var id = Guid.NewGuid().ToString("N")[..10];
        var category = await (await _admin.PostAsJsonAsync("/api/v1/catalog/categories", new { name = $"Kategori {id}", sortOrder = 0, isActive = true }, Ct))
            .Content.ReadFromJsonAsync<JsonElement>(Ct);
        var brand = await (await _admin.PostAsJsonAsync("/api/v1/catalog/brands", new { name = $"Marka {id}", isActive = true }, Ct))
            .Content.ReadFromJsonAsync<JsonElement>(Ct);
        var created = await _admin.PostAsJsonAsync("/api/v1/catalog/products", new
        {
            name = $"Ürün {id}",
            productCode = "MODEL-1",
            brandId = brand.GetProperty("id").GetInt32(),
            categoryId = category.GetProperty("id").GetInt32(),
            vatRate = 20,
            variants = new[] { new { sku = $"SKU-{id}", barcode = $"BRK-{id}", price, stockQuantity = stock, attributes = new[] { new { name = "Beden", value = "M" } } } }
        }, Ct);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var product = await created.Content.ReadFromJsonAsync<JsonElement>(Ct);
        var productId = product.GetProperty("id").GetInt32();
        (await _admin.PostAsync($"/api/v1/catalog/products/{productId}/publish", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        return (product.GetProperty("variants")[0].GetProperty("id").GetInt32(), productId);
    }

    private async Task<int> StockAsync(int productId) =>
        (await _admin.GetFromJsonAsync<JsonElement>($"/api/v1/catalog/products/{productId}", Ct)).GetProperty("variants")[0].GetProperty("stockQuantity").GetInt32();
}
