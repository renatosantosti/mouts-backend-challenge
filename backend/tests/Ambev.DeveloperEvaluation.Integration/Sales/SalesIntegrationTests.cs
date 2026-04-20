using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Integration.Infrastructure;
using Bogus;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

public sealed class SalesIntegrationTests : IClassFixture<ComposeHttpFixture>
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

    private readonly HttpClient _client;

    public SalesIntegrationTests(ComposeHttpFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task SalesFlow_EndToEnd_ShouldSucceedAgainstComposeWebApi()
    {
        var createBody = BuildCreateSaleRequest(
            saleNumber: $"S-{Guid.NewGuid():N}",
            customerName: "Integration Customer");

        var createResponse = await _client.PostAsJsonAsync("/api/sales", createBody);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await ReadJsonAsync(createResponse);
        var saleId = created.RootElement.GetProperty("id").GetGuid();
        saleId.Should().NotBeEmpty();

        var addResponse = await AddItemAsync(saleId, Guid.NewGuid(), "Integration Item", 4, 10m);
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var addJson = await ReadJsonAsync(addResponse);
        var addedItemId = addJson.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
        addJson.RootElement.GetProperty("totalAmount").GetDecimal().Should().Be(36m);

        var getResponse = await _client.GetAsync($"/api/sales/{saleId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = await ReadJsonAsync(getResponse);
        getJson.RootElement.GetProperty("id").GetGuid().Should().Be(saleId);

        var cancelItemResponse = await _client.PostAsync($"/api/sales/{saleId}/items/{addedItemId}/cancel", null);
        cancelItemResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelSaleResponse = await _client.PostAsync($"/api/sales/{saleId}/cancel", null);
        cancelSaleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelledJson = await ReadJsonAsync(cancelSaleResponse);
        cancelledJson.RootElement.GetProperty("isCancelled").GetBoolean().Should().BeTrue();
        cancelledJson.RootElement.GetProperty("totalAmount").GetDecimal().Should().Be(0m);

        var listResponse = await _client.GetAsync($"/api/sales?_page=1&_size=10&customerName={Uri.EscapeDataString(createBody.CustomerName)}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listJson = await ReadJsonAsync(listResponse);
        listJson.RootElement.GetProperty("data").EnumerateArray()
            .Any(item => item.GetProperty("id").GetGuid() == saleId)
            .Should().BeTrue();

        var historyResponse = await _client.GetAsync($"/api/sales/{saleId}/history");
        var historyBody = await historyResponse.Content.ReadAsStringAsync();
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"response body: {historyBody}");

        var historyJson = await ReadJsonAsync(historyResponse);
        var history = historyJson.RootElement.GetProperty("data").EnumerateArray().ToList();
        history.Should().NotBeEmpty();
        history.Should().Contain(item => item.GetProperty("eventType").GetString() == "SaleCreatedEvent");
        history.Should().Contain(item => item.GetProperty("eventType").GetString() == "SaleModifiedEvent");
    }

    [Fact]
    public async Task CreateSale_WithInvalidPayload_ShouldReturnValidationError()
    {
        var invalid = BuildCreateSaleRequest(
            saleNumber: string.Empty,
            customerId: Guid.Empty,
            customerName: string.Empty,
            branchId: Guid.Empty,
            branchName: string.Empty);

        var response = await _client.PostAsJsonAsync("/api/sales", invalid);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertErrorShapeAsync(response, "ValidationError");
    }

    [Fact]
    public async Task AddItem_WithQuantityAbove20_ShouldReturnDomainError()
    {
        var saleId = await CreateSaleAsync($"S-{Guid.NewGuid():N}");

        var response = await AddItemAsync(saleId, Guid.NewGuid(), "Too Many", 21, 10m);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertErrorShapeAsync(response, "DomainError");
    }

    [Fact]
    public async Task GetSaleById_WhenMissing_ShouldReturnResourceNotFound()
    {
        var response = await _client.GetAsync($"/api/sales/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await AssertErrorShapeAsync(response, "ResourceNotFound");
    }

    private async Task<Guid> CreateSaleAsync(string saleNumber)
    {
        var response = await _client.PostAsJsonAsync("/api/sales", BuildCreateSaleRequest(saleNumber: saleNumber));
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
        var body = new AddSaleItemRequest(productId, productName, quantity, unitPrice);
        return _client.PostAsJsonAsync($"/api/sales/{saleId}/items", body);
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
}
