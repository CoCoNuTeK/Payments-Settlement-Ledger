using System.Diagnostics;
using System.Text.Json;
using PaymentsLedger.Blazor.Application.UI.Events;
using PaymentsLedger.SharedKernel.Contracts.IntegrationEvents;

namespace PaymentsLedger.Blazor.Presentation.UI.Events;

public sealed class PaymentsEventHandler : IPaymentsEventHandler
{
    private static readonly ActivitySource ActivitySource = new("PaymentsLedger.Blazor.Presentation");
    private readonly object _gate = new();
    private readonly Dictionary<Guid, PaymentStats> _byMerchant = new();

    public event Action? Changed;

    public void Handle(string subject, string body)
    {
        using var activity = ActivitySource.StartActivity(
            "presentation.payments.event.handle",
            ActivityKind.Internal);

        activity?.SetTag("app.layer", "Presentation");
        activity?.SetTag("ui.component", nameof(PaymentsEventHandler));
        activity?.SetTag("event.subject", subject);

        try
        {
            var evt = JsonSerializer.Deserialize<PaymentCreatedEvent>(body);
            if (evt is not null)
            {
                activity?.AddEvent(new ActivityEvent(
                    "payments.event.parsed",
                    tags: new ActivityTagsCollection
                    {
                        ["payment.id"] = evt.PaymentId,
                        ["merchant.id"] = evt.MerchantId,
                        ["payment.amount"] = evt.Amount,
                        ["payment.currency"] = evt.Currency
                    }));

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

                activity?.AddEvent(new ActivityEvent(
                    "payments.ui.updated",
                    tags: new ActivityTagsCollection
                    {
                        ["merchant.id"] = evt.MerchantId,
                        ["received.count"] = _byMerchant[evt.MerchantId].ReceivedCount,
                        ["total.amount"] = _byMerchant[evt.MerchantId].TotalAmount
                    }));
            }
        }
        catch
        {
            // Ignore parse errors for demo
            activity?.AddEvent(new ActivityEvent("payments.event.parse_error"));
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
