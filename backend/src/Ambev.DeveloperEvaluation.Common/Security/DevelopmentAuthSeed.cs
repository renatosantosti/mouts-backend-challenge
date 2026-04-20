namespace Ambev.DeveloperEvaluation.Common.Security;

/// <summary>
/// Fixed credentials for the Development-only bootstrap user (see WebApi Program seed).
/// </summary>
public static class DevelopmentAuthSeed
{
    public const string Email = "dev@local.test";

    public const string Password = "DevSeed_P@ssw0rd!";
}
