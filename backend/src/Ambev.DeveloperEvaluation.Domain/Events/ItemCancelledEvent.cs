namespace Ambev.DeveloperEvaluation.Domain.Events;

public sealed class ItemCancelledEvent : IDomainEvent
{
    public Guid SaleId { get; }
    public Guid SaleItemId { get; }
    public DateTime OccurredOn { get; }

    public ItemCancelledEvent(Guid saleId, Guid saleItemId, DateTime occurredOn)
    {
        SaleId = saleId;
        SaleItemId = saleItemId;
        OccurredOn = occurredOn;
    }
}
