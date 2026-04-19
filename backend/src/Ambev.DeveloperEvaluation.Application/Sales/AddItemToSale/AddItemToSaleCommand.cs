using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.AddItemToSale;

public sealed record AddItemToSaleCommand(
    Guid SaleId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice) : IRequest<SaleResponse>;
