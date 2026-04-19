namespace Ambev.DeveloperEvaluation.Domain.Events;

public sealed class SaleModifiedEvent : IDomainEvent
{
    public Guid SaleId { get; }
    public DateTime OccurredOn { get; }
    public decimal TotalAmountAfterChange { get; }

    public SaleModifiedEvent(Guid saleId, DateTime occurredOn, decimal totalAmountAfterChange)
    {
        SaleId = saleId;
        OccurredOn = occurredOn;
        TotalAmountAfterChange = totalAmountAfterChange;
    }
}
