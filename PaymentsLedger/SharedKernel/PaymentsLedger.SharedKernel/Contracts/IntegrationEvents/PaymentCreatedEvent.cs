namespace PaymentsLedger.SharedKernel.Contracts.IntegrationEvents;

// Lightweight integration event contract shared across services.
// Avoids leaking domain internals (like value objects) over the wire.
public sealed record PaymentCreatedEvent(
    Guid PaymentId,
    Guid MerchantId,
    decimal Amount,
    string Currency,
    DateTime CreatedAtUtc);

