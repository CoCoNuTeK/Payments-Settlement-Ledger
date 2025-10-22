namespace PaymentsLedger.PaymentService.Domain.PaymentAggregate.Errors;

public sealed class MerchantIdEmptyDomainException : Exception
{
    public MerchantIdEmptyDomainException()
        : base("MerchantId cannot be empty.")
    {
    }
}

