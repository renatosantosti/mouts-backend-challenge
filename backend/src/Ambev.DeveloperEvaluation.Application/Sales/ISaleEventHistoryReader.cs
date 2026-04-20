using Ambev.DeveloperEvaluation.Application.Sales.Common;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public interface ISaleEventHistoryReader
{
    Task<IReadOnlyList<SaleHistoryEvent>> ListBySaleIdAsync(Guid saleId, CancellationToken cancellationToken);
}
