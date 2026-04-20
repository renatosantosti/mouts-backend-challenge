using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSaleHistory;

public sealed class GetSaleHistoryHandler : IRequestHandler<GetSaleHistoryQuery, IReadOnlyList<SaleHistoryEvent>>
{
    private readonly ISaleEventHistoryReader _historyReader;

    public GetSaleHistoryHandler(ISaleEventHistoryReader historyReader)
    {
        _historyReader = historyReader;
    }

    public Task<IReadOnlyList<SaleHistoryEvent>> Handle(GetSaleHistoryQuery request, CancellationToken cancellationToken)
    {
        return _historyReader.ListBySaleIdAsync(request.SaleId, cancellationToken);
    }
}
