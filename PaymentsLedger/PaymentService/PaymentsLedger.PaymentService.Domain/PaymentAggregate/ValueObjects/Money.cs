using PaymentsLedger.SharedKernel.Abstractions;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate.Errors;

namespace PaymentsLedger.PaymentService.Domain.PaymentAggregate.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount <= 0)
        {
            throw new InvalidMoneyAmountDomainException(amount);
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3 || currency != currency.ToUpperInvariant())
        {
            throw new InvalidCurrencyDomainException(currency);
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

}
