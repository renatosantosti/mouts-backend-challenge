using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSaleById;

public sealed record GetSaleByIdQuery(Guid Id) : IRequest<SaleResponse>;
