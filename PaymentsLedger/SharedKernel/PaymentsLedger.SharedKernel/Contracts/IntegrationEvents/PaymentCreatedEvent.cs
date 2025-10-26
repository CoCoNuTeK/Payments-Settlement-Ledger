namespace PaymentsLedger.SharedKernel.Contracts.IntegrationEvents;

// Lightweight integration event contract shared across services.
public sealed record PaymentCreatedEvent(
    Guid PaymentId,
    Guid MerchantId,
    decimal Amount,
    string Currency,
    DateTime CreatedAtUtc);

