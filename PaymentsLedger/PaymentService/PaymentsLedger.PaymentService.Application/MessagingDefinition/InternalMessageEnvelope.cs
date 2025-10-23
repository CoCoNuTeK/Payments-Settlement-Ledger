namespace PaymentsLedger.PaymentService.Application.MessagingDefinition;

public sealed class InternalMessageEnvelope(
    object payload,
    Func<IServiceProvider, CancellationToken, Task> handler,
    Guid? id = null,
    DateTimeOffset? createdAtUtc = null)
{
    public Guid Id { get; } = id ?? Guid.NewGuid();
    public object Payload { get; } = payload ?? throw new ArgumentNullException(nameof(payload));
    public DateTimeOffset CreatedAtUtc { get; } = createdAtUtc ?? DateTimeOffset.UtcNow;
    // Optional per-message handler delegate for fast dispatch.
    // Consumer can create a scope and pass its IServiceProvider.
    public Func<IServiceProvider, CancellationToken, Task> Handler { get; } = handler ?? throw new ArgumentNullException(nameof(handler));
}
