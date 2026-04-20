using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.ORM;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Infrastructure;

public sealed class ComposeHttpFixture : IAsyncLifetime
{
    public HttpClient Client { get; private set; } = null!;

    public Task InitializeAsync()
    {
        var baseUrl = Environment.GetEnvironmentVariable("INTEGRATION_BASE_URL") ?? "http://localhost:8080";
        Client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(20)
        };

        return InitializeInfrastructureAsync();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    private async Task WaitUntilHealthyAsync()
    {
        const int maxAttempts = 30;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var response = await Client.GetAsync("/health");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch
            {
                // Ignore transient startup failures while waiting for compose stack readiness.
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException("WebApi did not become healthy at /health within the expected time window.");
    }

    private async Task InitializeInfrastructureAsync()
    {
        await WaitUntilHealthyAsync();
        await EnsurePostgresMigrationsAsync();
        await AuthenticateWithDevelopmentSeedAsync();
    }

    private async Task AuthenticateWithDevelopmentSeedAsync()
    {
        using var response = await Client.PostAsJsonAsync(
            "/api/auth",
            new { email = DevelopmentAuthSeed.Email, password = DevelopmentAuthSeed.Password });

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Integration login failed: {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        }

        var loginBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(loginBody);
        var token = TryGetAuthToken(doc.RootElement)
            ?? throw new InvalidOperationException($"Login response missing token. Body: {loginBody}");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static string? TryGetAuthToken(JsonElement root)
    {
        if (!TryGetPropertyIgnoreCase(root, "data", out var data))
            return null;

        if (TryGetPropertyIgnoreCase(data, "token", out var tokenEl))
            return tokenEl.GetString();

        if (TryGetPropertyIgnoreCase(data, "data", out var inner) &&
            TryGetPropertyIgnoreCase(inner, "token", out tokenEl))
            return tokenEl.GetString();

        return null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
            return true;

        var pascal = char.ToUpperInvariant(name[0]) + name[1..];
        return element.TryGetProperty(pascal, out value);
    }

    private static async Task EnsurePostgresMigrationsAsync()
    {
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "developer_evaluation";
        var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "developer";
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "change_this_postgres_password";

        var connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password}";
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Ambev.DeveloperEvaluation.ORM"))
            .Options;

        await using var dbContext = new DefaultContext(options);
        await dbContext.Database.MigrateAsync();
    }
}
