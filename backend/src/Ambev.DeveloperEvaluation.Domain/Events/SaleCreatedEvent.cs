namespace Ambev.DeveloperEvaluation.Domain.Events;

public sealed class SaleCreatedEvent : IDomainEvent
{
    public Guid SaleId { get; }
    public string SaleNumber { get; }
    public DateTime OccurredOn { get; }

    public SaleCreatedEvent(Guid saleId, string saleNumber, DateTime occurredOn)
    {
        SaleId = saleId;
        SaleNumber = saleNumber;
        OccurredOn = occurredOn;
    }
}
