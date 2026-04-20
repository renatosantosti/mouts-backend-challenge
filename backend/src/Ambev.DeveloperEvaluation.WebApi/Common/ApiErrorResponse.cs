namespace Ambev.DeveloperEvaluation.WebApi.Common;

public sealed class ApiErrorResponse
{
    public string Type { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
}
