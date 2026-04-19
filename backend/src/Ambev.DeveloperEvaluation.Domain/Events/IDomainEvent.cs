namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// Marker for domain events raised by aggregates (no infrastructure dependencies).
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
