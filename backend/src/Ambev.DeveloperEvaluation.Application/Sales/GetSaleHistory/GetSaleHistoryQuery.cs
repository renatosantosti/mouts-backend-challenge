using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSaleHistory;

public sealed record GetSaleHistoryQuery(Guid SaleId) : IRequest<IReadOnlyList<SaleHistoryEvent>>;
