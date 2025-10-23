namespace PaymentsLedger.PaymentService.Application.Abstractions.Messaging;

public sealed class InternalMessageEnvelope
{
    public Guid Id { get; }
    public Guid? CorrelationId { get; }
    public Guid? CausationId { get; }
    public InternalMessageKind Kind { get; }
    public object Payload { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public InternalMessageEnvelope(
        object payload,
        InternalMessageKind kind,
        Guid? correlationId = null,
        Guid? causationId = null,
        Guid? id = null,
        DateTimeOffset? createdAtUtc = null)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        Kind = kind;
        CorrelationId = correlationId;
        CausationId = causationId;
        Id = id ?? Guid.NewGuid();
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
    }
}

