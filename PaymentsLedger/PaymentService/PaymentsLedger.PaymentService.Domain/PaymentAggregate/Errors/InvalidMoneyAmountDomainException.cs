namespace PaymentsLedger.PaymentService.Domain.PaymentAggregate.Errors;

public sealed class InvalidMoneyAmountDomainException : Exception
{
    public decimal Amount { get; }
    public InvalidMoneyAmountDomainException(decimal amount)
        : base($"Amount must be positive. Actual: {amount}.")
    {
        Amount = amount;
    }
}

