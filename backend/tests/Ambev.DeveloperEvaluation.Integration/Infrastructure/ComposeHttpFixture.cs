using System.Net;
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
