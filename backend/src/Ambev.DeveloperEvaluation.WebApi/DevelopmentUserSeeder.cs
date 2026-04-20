using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.WebApi;

/// <summary>
/// Ensures a known Development user exists so JWT-protected routes can be exercised locally and in integration tests.
/// </summary>
internal static class DevelopmentUserSeeder
{
    public static async Task EnsureSeedUserAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var userRepository = services.GetRequiredService<IUserRepository>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();

        if (await userRepository.GetByEmailAsync(DevelopmentAuthSeed.Email, cancellationToken) is not null)
            return;

        var user = new User
        {
            Username = "Development Seed",
            Email = DevelopmentAuthSeed.Email,
            Phone = "+5511999999999",
            Password = passwordHasher.HashPassword(DevelopmentAuthSeed.Password),
            Status = UserStatus.Active,
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow
        };

        await userRepository.CreateAsync(user, cancellationToken);
    }
}
