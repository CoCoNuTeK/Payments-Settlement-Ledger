using PaymentsLedger.PaymentService.Domain.PaymentAggregate;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate.ValueObjects;

namespace PaymentsLedger.PaymentService.Presentation.DTO;

public sealed record PaymentCreationPayload(
    Guid PaymentId,
    Guid MerchantId,
    decimal Amount,
    string Currency
)
{
    public Payment ToDomain()
    {
        var money = new Money(Amount, Currency);
        return new Payment(PaymentId, MerchantId, money);
    }
}

