namespace PaymentsLedger.SharedKernel.Abstractions;

// Marker base class for aggregate roots
public abstract class AggregateRoot : Entity
{
    protected AggregateRoot() { }

    protected AggregateRoot(Guid id) : base(id) { }
}
