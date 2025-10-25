using Microsoft.Extensions.DependencyInjection;
using PaymentsLedger.Blazor.Application.UI.Events;

namespace PaymentsLedger.Blazor.Infrastructure.Messaging.ServiceBus;

// Static map for incoming integration events -> handler invokers
public static class IncomingIntegrationEventRouting
{
    public delegate Task IntegrationEventInvoker(IServiceProvider sp, string subject, string body, CancellationToken ct);

    private static readonly IReadOnlyDictionary<string, IntegrationEventInvoker> Map =
        new Dictionary<string, IntegrationEventInvoker>(StringComparer.OrdinalIgnoreCase)
        {
            ["PaymentCreated"] = (sp, subject, body, ct) =>
            {
                sp.GetService<IPaymentsEventHandler>()?.Handle(subject, body);
                return Task.CompletedTask;
            }
        };

    public static bool TryGetHandler(string eventName, out IntegrationEventInvoker invoker)
        => Map.TryGetValue(eventName, out invoker!);
}
