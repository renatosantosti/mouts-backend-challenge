using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public sealed record CreateSaleCommand(
    string SaleNumber,
    DateTime Date,
    Guid CustomerId,
    string CustomerName,
    Guid BranchId,
    string BranchName) : IRequest<SaleResponse>;
