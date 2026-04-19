using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public sealed record ListSalesQuery(
    int Page = 1,
    int Size = 10,
    string? Order = null,
    IReadOnlyDictionary<string, string>? Filters = null) : IRequest<ListSalesResult>;
