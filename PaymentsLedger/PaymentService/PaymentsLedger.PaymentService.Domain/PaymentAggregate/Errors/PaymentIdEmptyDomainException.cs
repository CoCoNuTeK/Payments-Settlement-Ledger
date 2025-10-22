namespace PaymentsLedger.PaymentService.Domain.PaymentAggregate.Errors;

public sealed class PaymentIdEmptyDomainException : Exception
{
    public PaymentIdEmptyDomainException()
        : base("Payment Id cannot be empty.")
    {
    }
}

