namespace PaymentsLedger.PaymentService.Application.MessagingDefinition;

public sealed class InternalMessageEnvelope
{
    public Guid Id { get; }
    public InternalMessageKind Kind { get; }
    public object Payload { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    // Optional per-message handler delegate for fast dispatch.
    // Consumer can create a scope and pass its IServiceProvider.
    public Func<IServiceProvider, CancellationToken, Task>? Handler { get; }

    public InternalMessageEnvelope(
        object payload,
        InternalMessageKind kind,
        Func<IServiceProvider, CancellationToken, Task>? handler = null,
        Guid? id = null,
        DateTimeOffset? createdAtUtc = null)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        Kind = kind;
        Handler = handler;
        Id = id ?? Guid.NewGuid();
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
    }
}
