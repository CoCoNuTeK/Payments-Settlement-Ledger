namespace PaymentsLedger.PaymentService.Application.Messages;

public sealed record PaymentCreationPayload(
    Guid PaymentId,
    Guid MerchantId,
    decimal Amount,
    string Currency
);

