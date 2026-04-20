using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Functional.TestInfrastructure;
using Bogus;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

public sealed class SalesEndpointsTests : IClassFixture<SalesApiFactory>
{
    private sealed record CreateSaleRequest(
        string SaleNumber,
        DateTime Date,
        Guid CustomerId,
        string CustomerName,
        Guid BranchId,
        string BranchName);

    private sealed record AddSaleItemRequest(
        Guid ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice);

    private static readonly Faker<CreateSaleRequest> CreateSaleRequestFaker = new Faker<CreateSaleRequest>()
        .CustomInstantiator(f => new CreateSaleRequest(
            $"S-{f.Random.AlphaNumeric(14).ToUpperInvariant()}",
            f.Date.RecentOffset(7).UtcDateTime,
            f.Random.Guid(),
            f.Person.FullName,
            f.Random.Guid(),
            $"Branch {f.Address.City()}"));

    private static readonly Faker<AddSaleItemRequest> AddSaleItemRequestFaker = new Faker<AddSaleItemRequest>()
        .CustomInstantiator(f => new AddSaleItemRequest(
            f.Random.Guid(),
            f.Commerce.ProductName(),
            f.Random.Int(1, 20),
            Math.Round(f.Random.Decimal(1, 300), 2, MidpointRounding.AwayFromZero)));

    private readonly HttpClient _client;

    public SalesEndpointsTests(SalesApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // Create/Get
    [Fact]
    public async Task CreateSale_ShouldPersistHeaderAndReturnExpectedFields()
    {
        var customerId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var requestBody = BuildCreateSaleRequest(
            customerId: customerId,
            customerName: "Customer One",
            branchId: branchId,
            branchName: "Main Branch",
            date: DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync("/api/sales", requestBody);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("id").GetGuid().Should().NotBeEmpty();
        json.RootElement.GetProperty("saleNumber").GetString().Should().Be(requestBody.SaleNumber);
        json.RootElement.GetProperty("customerId").GetGuid().Should().Be(customerId);
        json.RootElement.GetProperty("customerName").GetString().Should().Be(requestBody.CustomerName);
        json.RootElement.GetProperty("branchId").GetGuid().Should().Be(branchId);
        json.RootElement.GetProperty("branchName").GetString().Should().Be(requestBody.BranchName);
        json.RootElement.GetProperty("totalAmount").GetDecimal().Should().Be(0);
        json.RootElement.GetProperty("isCancelled").GetBoolean().Should().BeFalse();
        json.RootElement.GetProperty("items").EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task GetSaleById_ShouldReturnCreatedSale()
    {
        var saleNumber = $"S-{Guid.NewGuid():N}";
        var saleId = await CreateSaleAsync(saleNumber);

        var response = await _client.GetAsync($"/api/sales/{saleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("id").GetGuid().Should().Be(saleId);
        json.RootElement.GetProperty("saleNumber").GetString().Should().Be(saleNumber);
        json.RootElement.GetProperty("isCancelled").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task GetSaleHistory_ShouldReturnCreatedAndModifiedEvents()
    {
        var saleId = await CreateSaleAsync($"S-{Guid.NewGuid():N}");
        await AddItemAsync(saleId, Guid.NewGuid(), "History Item", 4, 10m);

        var response = await _client.GetAsync($"/api/sales/{saleId}/history");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        var historyItems = json.RootElement.GetProperty("data").EnumerateArray().ToList();
        historyItems.Should().NotBeEmpty();
        historyItems.Should().OnlyContain(item => item.GetProperty("saleId").GetGuid() == saleId);
        historyItems.Should().Contain(item => item.GetProperty("eventType").GetString() == "SaleCreatedEvent");
        historyItems.Should().Contain(item => item.GetProperty("eventType").GetString() == "SaleModifiedEvent");
    }

    [Fact]
    public async Task GetSaleHistory_WhenSaleHasNoEvents_ShouldReturnEmptyData()
    {
        var response = await _client.GetAsync($"/api/sales/{Guid.NewGuid()}/history");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("data").EnumerateArray().Should().BeEmpty();
    }

    // DiscountRules
    [Fact]
    public async Task AddItem_QuantityBelow4_ShouldApplyNoDiscount()
    {
        var saleId = await CreateSaleAsync($"S-{Guid.NewGuid():N}");
        var response = await AddItemAsync(saleId, Guid.NewGuid(), "Item A", 3, 10m);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, $"response body: {body}");

        var json = await ReadJsonAsync(response);
        var item = json.RootElement.GetProperty("items")[0];
        item.GetProperty("discount").GetDecimal().Should().Be(0);
        item.GetProperty("totalAmount").GetDecimal().Should().Be(30);
        json.RootElement.GetProperty("totalAmount").GetDecimal().Should().Be(30);
    }

    [Fact]
    public async Task AddItem_QuantityBetween4And9_ShouldApply10PercentDiscount()
    {
        var saleId = await CreateSaleAsync($"S-{Guid.NewGuid():N}");
        var response = await AddItemAsync(saleId, Guid.NewGuid(), "Item B", 4, 10m);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        var item = json.RootElement.GetProperty("items")[0];
        item.GetProperty("discount").GetDecimal().Should().Be(4);
        item.GetProperty("totalAmount").GetDecimal().Should().Be(36);
        json.RootElement.GetProperty("totalAmount").GetDecimal().Should().Be(36);
    }

    [Fact]
    public async Task AddItem_QuantityBetween10And20_ShouldApply20PercentDiscount()
    {
        var saleId = await CreateSaleAsync($"S-{Guid.NewGuid():N}");
        var response = await AddItemAsync(saleId, Guid.NewGuid(), "Item C", 10, 10m);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        var item = json.RootElement.GetProperty("items")[0];
        item.GetProperty("discount").GetDecimal().Should().Be(20);
        item.GetProperty("totalAmount").GetDecimal().Should().Be(80);
        json.RootElement.GetProperty("totalAmount").GetDecimal().Should().Be(80);
    }

    [Fact]
    public async Task AddItem_QuantityAbove20_ShouldReturn400DomainError()
    {
        var saleId = await CreateSaleAsync($"S-{Guid.NewGuid():N}");
        var response = await AddItemAsync(saleId, Guid.NewGuid(), "Item X", 21, 10m);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertErrorShapeAsync(response, "DomainError");
    }

    // ItemAndSaleTotals
    [Fact]
    public async Task AddItem_ShouldReturnItemTotalsAndSaleTotalConsistently()
    {
        var saleId = await CreateSaleAsync($"S-{Guid.NewGuid():N}");
        var response = await AddItemAsync(saleId, Guid.NewGuid(), "Item D", 5, 20m);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        var item = json.RootElement.GetProperty("items")[0];

        item.GetProperty("unitPrice").GetDecimal().Should().Be(20m);
        item.GetProperty("discount").GetDecimal().Should().Be(10m);
        item.GetProperty("totalAmount").GetDecimal().Should().Be(90m);
        json.RootElement.GetProperty("totalAmount").GetDecimal().Should().Be(90m);
    }

    [Fact]
    public async Task AddItem_SameProduct_ShouldMergeLineAndRecalculateUsingLatestPrice()
    {
        var saleId = await CreateSaleAsync($"S-{Guid.NewGuid():N}");
        var productId = Guid.NewGuid();
        await AddItemAsync(saleId, productId, "Item E", 2, 10m);

        var response = await AddItemAsync(saleId, productId, "Item E Updated", 2, 20m);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToList();
        items.Should().HaveCount(1);

        var merged = items[0];
        merged.GetProperty("quantity").GetInt32().Should().Be(4);
        merged.GetProperty("unitPrice").GetDecimal().Should().Be(20m);
        merged.GetProperty("discount").GetDecimal().Should().Be(8m);
        merged.GetProperty("totalAmount").GetDecimal().Should().Be(72m);
        json.RootElement.GetProperty("totalAmount").GetDecimal().Should().Be(72m);
    }

    [Fact]
    public async Task AddItem_ToCancelledSale_ShouldReturn400DomainError()
    {
        var saleId = await CreateSaleAsync($"S-{Guid.NewGuid():N}");
        await CancelSaleAsync(saleId);

        var response = await AddItemAsync(saleId, Guid.NewGuid(), "Item F", 2, 10m);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertErrorShapeAsync(response, "DomainError");
    }

    // CancelFlows
    [Fact]
    public async Task CancelSale_ShouldMarkSaleCancelledAndZeroTotal()
    {
        var saleId = await CreateSaleAsync($"S-{Guid.NewGuid():N}");
        await AddItemAsync(saleId, Guid.NewGuid(), "Item G", 4, 10m);

        var response = await CancelSaleAsync(saleId);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("isCancelled").GetBoolean().Should().BeTrue();
        json.RootElement.GetProperty("totalAmount").GetDecimal().Should().Be(0);
    }

    [Fact]
    public async Task CancelSaleItem_ShouldCancelOnlyItemAndRecalculateSaleTotal()
    {
        var saleId = await CreateSaleAsync($"S-{Guid.NewGuid():N}");
        var firstProduct = Guid.NewGuid();
        var secondProduct = Guid.NewGuid();

        await AddItemAsync(saleId, firstProduct, "Item H", 4, 10m); // total 36
        var secondAddResponse = await AddItemAsync(saleId, secondProduct, "Item I", 2, 10m); // total 20
        var secondAddJson = await ReadJsonAsync(secondAddResponse);
        var itemToCancelId = secondAddJson.RootElement.GetProperty("items")
            .EnumerateArray()
            .Single(i => i.GetProperty("productId").GetGuid() == secondProduct)
            .GetProperty("id")
            .GetGuid();

        var cancelResponse = await CancelSaleItemAsync(saleId, itemToCancelId);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(cancelResponse);
        var cancelledItem = json.RootElement.GetProperty("items")
            .EnumerateArray()
            .Single(i => i.GetProperty("id").GetGuid() == itemToCancelId);

        cancelledItem.GetProperty("isCancelled").GetBoolean().Should().BeTrue();
        cancelledItem.GetProperty("totalAmount").GetDecimal().Should().Be(0);
        json.RootElement.GetProperty("totalAmount").GetDecimal().Should().Be(36m);
    }

    [Fact]
    public async Task CancelSaleItem_WhenItemNotFound_ShouldReturn404()
    {
        var saleId = await CreateSaleAsync($"S-{Guid.NewGuid():N}");
        var response = await CancelSaleItemAsync(saleId, Guid.NewGuid());
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await AssertErrorShapeAsync(response, "ResourceNotFound");
    }

    // List/Pagination/Filter
    [Fact]
    public async Task ListSales_ShouldApplyPageSizeOrderAndFilters()
    {
        var customerA = Guid.NewGuid();
        var customerB = Guid.NewGuid();

        await CreateSaleAsync($"S-{Guid.NewGuid():N}", DateTime.UtcNow.AddDays(-2), customerA, "Customer A");
        await CreateSaleAsync($"S-{Guid.NewGuid():N}", DateTime.UtcNow.AddDays(-1), customerB, "Customer B");
        await CreateSaleAsync($"S-{Guid.NewGuid():N}", DateTime.UtcNow, customerA, "Customer A");

        var response = await _client.GetAsync($"/api/sales?_page=1&_size=1&_order=date_desc&customerId={customerA}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("data").GetArrayLength().Should().Be(1);
        json.RootElement.GetProperty("totalItems").GetInt32().Should().Be(2);
        json.RootElement.GetProperty("currentPage").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("totalPages").GetInt32().Should().Be(2);

        var listedCustomer = json.RootElement.GetProperty("data")[0].GetProperty("customerId").GetGuid();
        listedCustomer.Should().Be(customerA);

        var listedDate = json.RootElement.GetProperty("data")[0].GetProperty("date").GetDateTime();
        listedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(2));
    }

    // ErrorContract
    [Fact]
    public async Task GetSaleById_WhenNotFound_ShouldReturnResourceNotFoundShape()
    {
        var response = await _client.GetAsync($"/api/sales/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await AssertErrorShapeAsync(response, "ResourceNotFound");
    }

    [Fact]
    public async Task CreateSale_WithInvalidPayload_ShouldReturnValidationErrorShape()
    {
        var invalidBody = BuildCreateSaleRequest(
            saleNumber: string.Empty,
            customerId: Guid.Empty,
            customerName: string.Empty,
            branchId: Guid.Empty,
            branchName: string.Empty,
            date: DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync("/api/sales", invalidBody);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertErrorShapeAsync(response, "ValidationError");
    }

    [Fact]
    public async Task ListSales_WithInvalidPageOrSize_ShouldReturnValidationErrorShape()
    {
        var response = await _client.GetAsync("/api/sales?_page=0&_size=101");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertErrorShapeAsync(response, "ValidationError");
    }

    [Fact]
    public async Task UnhandledError_ShouldReturn500WithDocumentedErrorShape()
    {
        var duplicateSaleNumber = $"S-{Guid.NewGuid():N}";
        await CreateSaleAsync(duplicateSaleNumber);
        var duplicateRequest = BuildCreateSaleRequest(
            saleNumber: duplicateSaleNumber,
            customerName: "Duplicate Customer",
            branchName: "Duplicate Branch",
            date: DateTime.UtcNow);
        var response = await _client.PostAsJsonAsync("/api/sales", duplicateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        await AssertErrorShapeAsync(response, "ServerError");
        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("error").GetString().Should().Be("Unexpected server error");
    }

    private async Task<Guid> CreateSaleAsync(
        string saleNumber,
        DateTime? date = null,
        Guid? customerId = null,
        string customerName = "Customer Seed")
    {
        var requestBody = BuildCreateSaleRequest(
            saleNumber: saleNumber,
            date: date ?? DateTime.UtcNow,
            customerId: customerId ?? Guid.NewGuid(),
            customerName: customerName,
            branchName: "Branch Seed");

        var response = await _client.PostAsJsonAsync("/api/sales", requestBody);
        response.EnsureSuccessStatusCode();

        var json = await ReadJsonAsync(response);
        return json.RootElement.GetProperty("id").GetGuid();
    }

    private Task<HttpResponseMessage> AddItemAsync(
        Guid saleId,
        Guid productId,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        var requestBody = BuildAddSaleItemRequest(
            productId: productId,
            productName: productName,
            quantity: quantity,
            unitPrice: unitPrice);

        return _client.PostAsJsonAsync($"/api/sales/{saleId}/items", requestBody);
    }

    private Task<HttpResponseMessage> CancelSaleAsync(Guid saleId) =>
        _client.PostAsync($"/api/sales/{saleId}/cancel", null);

    private Task<HttpResponseMessage> CancelSaleItemAsync(Guid saleId, Guid itemId) =>
        _client.PostAsync($"/api/sales/{saleId}/items/{itemId}/cancel", null);

    private static async Task AssertErrorShapeAsync(HttpResponseMessage response, string expectedType)
    {
        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("type").GetString().Should().Be(expectedType);
        json.RootElement.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();
        json.RootElement.GetProperty("detail").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }

    private static CreateSaleRequest BuildCreateSaleRequest(
        string? saleNumber = null,
        DateTime? date = null,
        Guid? customerId = null,
        string? customerName = null,
        Guid? branchId = null,
        string? branchName = null)
    {
        var generated = CreateSaleRequestFaker.Generate();
        return generated with
        {
            SaleNumber = saleNumber ?? generated.SaleNumber,
            Date = date ?? generated.Date,
            CustomerId = customerId ?? generated.CustomerId,
            CustomerName = customerName ?? generated.CustomerName,
            BranchId = branchId ?? generated.BranchId,
            BranchName = branchName ?? generated.BranchName
        };
    }

    private static AddSaleItemRequest BuildAddSaleItemRequest(
        Guid? productId = null,
        string? productName = null,
        int? quantity = null,
        decimal? unitPrice = null)
    {
        var generated = AddSaleItemRequestFaker.Generate();
        return generated with
        {
            ProductId = productId ?? generated.ProductId,
            ProductName = productName ?? generated.ProductName,
            Quantity = quantity ?? generated.Quantity,
            UnitPrice = unitPrice ?? generated.UnitPrice
        };
    }
}
