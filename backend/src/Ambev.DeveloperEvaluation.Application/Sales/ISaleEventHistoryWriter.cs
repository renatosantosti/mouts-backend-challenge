using Ambev.DeveloperEvaluation.Application.Sales.Common;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public interface ISaleEventHistoryWriter
{
    Task AppendAsync(IReadOnlyCollection<SaleHistoryEvent> events, CancellationToken cancellationToken);
}
