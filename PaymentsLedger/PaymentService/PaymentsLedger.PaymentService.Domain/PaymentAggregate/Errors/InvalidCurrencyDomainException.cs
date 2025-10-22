namespace PaymentsLedger.PaymentService.Domain.PaymentAggregate.Errors;

public sealed class InvalidCurrencyDomainException : Exception
{
    public string? Currency { get; }
    public InvalidCurrencyDomainException(string? currency)
        : base($"Currency must be a 3-letter ISO code in upper-case. Actual: '{currency}'.")
    {
        Currency = currency;
    }
}

