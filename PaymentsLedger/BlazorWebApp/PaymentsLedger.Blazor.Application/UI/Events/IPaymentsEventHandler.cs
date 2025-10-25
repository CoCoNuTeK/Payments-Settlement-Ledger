namespace PaymentsLedger.Blazor.Application.UI.Events;

public readonly record struct PaymentStats(int ReceivedCount, decimal TotalAmount, string? LastCurrency, string? LastEventJson);

public interface IPaymentsEventHandler
{
    // Per-merchant view only
    PaymentStats GetStats(Guid merchantId);

    // Called by infrastructure when a new event arrives
    void Handle(string subject, string body);

    // UI can observe changes to refresh state
    event Action? Changed;
}
