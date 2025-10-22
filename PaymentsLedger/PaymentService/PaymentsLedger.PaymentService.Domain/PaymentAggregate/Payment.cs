using PaymentsLedger.SharedKernel.Abstractions;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate.ValueObjects;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate.Errors;

namespace PaymentsLedger.PaymentService.Domain.PaymentAggregate;

public sealed class Payment : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }

    private Payment() { }

    public Payment(Guid id, Guid merchantId, Money amount) : base(id)
    {
        if (id == Guid.Empty)
        {
            throw new PaymentIdEmptyDomainException();
        }

        if (merchantId == Guid.Empty)
        {
            throw new MerchantIdEmptyDomainException();
        }

        MerchantId = merchantId;
        Amount = amount;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
