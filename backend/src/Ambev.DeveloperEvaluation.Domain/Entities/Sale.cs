using System.Linq;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Exceptions;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Aggregate root for sales. External identities (customer, branch, product) are denormalized as snapshots.
/// </summary>
public class Sale : BaseEntity
{
    private readonly List<SaleItem> _items = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public string SaleNumber { get; private set; } = string.Empty;
    public DateTime Date { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public Guid BranchId { get; private set; }
    public string BranchName { get; private set; } = string.Empty;
    public bool IsCancelled { get; private set; }
    public decimal TotalAmount { get; private set; }

    public IReadOnlyCollection<SaleItem> Items => _items;
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    private Sale()
    {
    }

    public static Sale Create(
        string saleNumber,
        DateTime date,
        Guid customerId,
        string customerName,
        Guid branchId,
        string branchName)
    {
        ValidateHeader(saleNumber, customerId, customerName, branchId, branchName);

        var occurredOn = DateTime.UtcNow;
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = saleNumber.Trim(),
            Date = NormalizeUtc(date),
            CustomerId = customerId,
            CustomerName = customerName.Trim(),
            BranchId = branchId,
            BranchName = branchName.Trim(),
            IsCancelled = false,
            TotalAmount = 0
        };

        sale.RecordDomainEvent(new SaleCreatedEvent(sale.Id, sale.SaleNumber, occurredOn));
        return sale;
    }

    /// <summary>
    /// Adds quantity for a product. There is at most one active line per <paramref name="productId"/>:
    /// if an active line exists, quantity is merged in place and unit price/name come from this call;
    /// if the only line for that product was cancelled, a new active line is created.
    /// </summary>
    public void AddItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        EnsureSaleNotCancelled();

        if (quantity < 1)
            throw new DomainException("Quantity must be at least 1.");

        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");

        if (productId == Guid.Empty)
            throw new DomainException("Product is required.");

        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name is required.");

        var name = productName.Trim();
        var existing = _items.FirstOrDefault(i => !i.IsCancelled && i.ProductId == productId);

        if (existing is not null)
            existing.ConsolidateWith(quantity, unitPrice, name);
        else
            _items.Add(new SaleItem(productId, name, quantity, unitPrice));

        RecalculateTotal();

        RecordDomainEvent(new SaleModifiedEvent(Id, DateTime.UtcNow, TotalAmount));
    }

    /// <summary>
    /// Cancels a line item. If the item is already cancelled, the operation is ignored (idempotent) and no new event is raised.
    /// </summary>
    public void CancelItem(Guid itemId)
    {
        EnsureSaleNotCancelled();

        var item = GetRequiredItem(itemId);

        if (item.IsCancelled)
            return;

        item.Cancel();
        RecalculateTotal();

        RecordDomainEvent(new ItemCancelledEvent(Id, itemId, DateTime.UtcNow));
    }

    public void Cancel()
    {
        EnsureSaleNotCancelled();

        IsCancelled = true;
        RecalculateTotal();

        RecordDomainEvent(new SaleCancelledEvent(Id, DateTime.UtcNow));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void RecordDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    private SaleItem GetRequiredItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            throw new DomainException("Sale item was not found.");

        return item;
    }

    private void EnsureSaleNotCancelled()
    {
        if (IsCancelled)
            throw new DomainException("Cannot modify a cancelled sale.");
    }

    private void RecalculateTotal()
    {
        if (IsCancelled)
        {
            TotalAmount = 0;
            return;
        }

        var sum = _items.Where(i => !i.IsCancelled).Sum(i => i.TotalAmount);
        TotalAmount = RoundMoney(sum);
    }

    private static void ValidateHeader(
        string saleNumber,
        Guid customerId,
        string customerName,
        Guid branchId,
        string branchName)
    {
        if (string.IsNullOrWhiteSpace(saleNumber))
            throw new DomainException("Sale number is required.");

        if (customerId == Guid.Empty)
            throw new DomainException("Customer is required.");

        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("Customer name is required.");

        if (branchId == Guid.Empty)
            throw new DomainException("Branch is required.");

        if (string.IsNullOrWhiteSpace(branchName))
            throw new DomainException("Branch name is required.");
    }

    private static DateTime NormalizeUtc(DateTime date)
    {
        return date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };
    }

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
