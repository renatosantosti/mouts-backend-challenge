using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Functional.TestInfrastructure;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Auth;

public sealed class AuthEndpointsTests : IClassFixture<SalesApiFactory>
{
    private sealed record CreateUserRequest(
        string Username,
        string Password,
        string Phone,
        string Email,
        UserStatus Status,
        UserRole Role);

    private readonly HttpClient _client;

    public AuthEndpointsTests(SalesApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AuthenticateUser_WithValidCredentials_ShouldReturnToken()
    {
        var password = "P@ssw0rd!234";
        var userRequest = BuildCreateUserRequest(password: password);
        var createResponse = await _client.PostAsJsonAsync("/api/users", userRequest);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, $"response body: {createBody}");

        var authResponse = await _client.PostAsJsonAsync("/api/auth", new
        {
            email = userRequest.Email,
            password = password
        });

        authResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var authBody = await authResponse.Content.ReadAsStringAsync();

        var authJson = await ReadJsonAsync(authResponse);
        ReadRequiredProperty(authJson.RootElement, "success").GetBoolean().Should().BeTrue();
        authBody.Should().Contain(userRequest.Email);
        authBody.Should().ContainEquivalentOf("token", "authentication payload should expose an access token");
    }

    [Fact]
    public async Task AuthenticateUser_WithInvalidPayload_ShouldReturnBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth", new
        {
            email = string.Empty,
            password = string.Empty
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Email is required");
    }

    [Fact]
    public async Task AuthenticateUser_WithInvalidCredentials_ShouldReturnAuthenticationErrorShape()
    {
        var password = "P@ssw0rd!234";
        var userRequest = BuildCreateUserRequest(password: password);
        var createResponse = await _client.PostAsJsonAsync("/api/users", userRequest);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, $"response body: {createBody}");

        var authResponse = await _client.PostAsJsonAsync("/api/auth", new
        {
            email = userRequest.Email,
            password = "wrong-password"
        });

        authResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertErrorShapeAsync(authResponse, "AuthenticationError");
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
