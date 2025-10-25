using System.Text.Json;
using PaymentsLedger.Blazor.Application.UI.Events;
using PaymentsLedger.SharedKernel.Contracts.IntegrationEvents;

namespace PaymentsLedger.Blazor.Presentation.UI.Events;

public sealed class PaymentsEventHandler : IPaymentsEventHandler
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, PaymentStats> _byMerchant = new();

    public event Action? Changed;

    public void Handle(string subject, string body)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<PaymentCreatedEvent>(body);
            if (evt is not null)
            {
                lock (_gate)
                {
                    // Update per-merchant snapshot
                    if (_byMerchant.TryGetValue(evt.MerchantId, out var stats))
                    {
                        _byMerchant[evt.MerchantId] = stats with
                        {
                            ReceivedCount = stats.ReceivedCount + 1,
                            TotalAmount = stats.TotalAmount + evt.Amount,
                            LastCurrency = evt.Currency,
                            LastEventJson = body
                        };
                    }
                    else
                    {
                        _byMerchant[evt.MerchantId] = new PaymentStats(1, evt.Amount, evt.Currency, body);
                    }
                }
            }
        }
        catch
        {
            // Ignore parse errors for demo
        }

        Changed?.Invoke();
    }

    public PaymentStats GetStats(Guid merchantId)
    {
        lock (_gate)
        {
            return _byMerchant.TryGetValue(merchantId, out var stats)
                ? stats
                : default;
        }
    }
}
