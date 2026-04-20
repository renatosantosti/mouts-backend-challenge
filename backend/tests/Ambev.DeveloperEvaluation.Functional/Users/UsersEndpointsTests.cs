using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Functional.TestInfrastructure;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Users;

public sealed class UsersEndpointsTests : IClassFixture<SalesApiFactory>
{
    private sealed record CreateUserRequest(
        string Username,
        string Password,
        string Phone,
        string Email,
        UserStatus Status,
        UserRole Role);

    private readonly HttpClient _client;

    public UsersEndpointsTests(SalesApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateAndGetAndDeleteUser_ShouldReturnSuccessfulContracts()
    {
        var request = BuildCreateUserRequest();
        var created = await _client.PostAsJsonAsync("/api/users", request);
        var createdBody = await created.Content.ReadAsStringAsync();
        created.StatusCode.Should().Be(HttpStatusCode.Created, $"response body: {createdBody}");

        var createJson = await ReadJsonAsync(created);
        ReadRequiredProperty(createJson.RootElement, "success").GetBoolean().Should().BeTrue();
        var userId = ExtractFirstGuid(createdBody);
        userId.Should().NotBeEmpty();

        var getResponse = await _client.GetAsync($"/api/users/{userId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getBody = await getResponse.Content.ReadAsStringAsync();

        var getJson = await ReadJsonAsync(getResponse);
        ReadRequiredProperty(getJson.RootElement, "success").GetBoolean().Should().BeTrue();
        getBody.Should().Contain(userId.ToString(), "payload should include the queried user id");
        getBody.Should().Contain(request.Email, "payload should include the created user email");

        var deleteResponse = await _client.DeleteAsync($"/api/users/{userId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteJson = await ReadJsonAsync(deleteResponse);
        ReadRequiredProperty(deleteJson.RootElement, "success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_WithInvalidPayload_ShouldReturnBadRequest()
    {
        var invalidRequest = new CreateUserRequest(
            Username: string.Empty,
            Password: string.Empty,
            Phone: "invalid-phone",
            Email: "invalid-email",
            Status: UserStatus.Unknown,
            Role: UserRole.None);

        var response = await _client.PostAsJsonAsync("/api/users", invalidRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetUser_WhenNotFound_ShouldReturnResourceNotFoundShape()
    {
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await AssertErrorShapeAsync(response, "ResourceNotFound");
    }

    [Fact]
    public async Task DeleteUser_WhenNotFound_ShouldReturnResourceNotFoundShape()
    {
        var response = await _client.DeleteAsync($"/api/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await AssertErrorShapeAsync(response, "ResourceNotFound");
    }

    private static CreateUserRequest BuildCreateUserRequest(string? password = null)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        return new CreateUserRequest(
            Username: $"user-{unique}",
            Password: password ?? "P@ssw0rd!234",
            Phone: "+5511999999999",
            Email: $"user-{unique}@mail.com",
            Status: UserStatus.Active,
            Role: UserRole.Customer);
    }

    private static async Task AssertErrorShapeAsync(HttpResponseMessage response, string expectedType)
    {
        var json = await ReadJsonAsync(response);
        ReadRequiredProperty(json.RootElement, "type").GetString().Should().Be(expectedType);
        ReadRequiredProperty(json.RootElement, "error").GetString().Should().NotBeNullOrWhiteSpace();
        ReadRequiredProperty(json.RootElement, "detail").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }

    private static Guid ExtractFirstGuid(string content)
    {
        var match = Regex.Match(content, @"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}");
        match.Success.Should().BeTrue($"response body should contain a guid: {content}");
        return Guid.Parse(match.Value);
    }

    private static JsonElement ReadRequiredProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value))
        {
            return value;
        }

        var pascalCaseName = char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
        if (element.TryGetProperty(pascalCaseName, out value))
        {
            return value;
        }

        throw new KeyNotFoundException($"Property '{propertyName}' was not found in JSON payload.");
    }
}
