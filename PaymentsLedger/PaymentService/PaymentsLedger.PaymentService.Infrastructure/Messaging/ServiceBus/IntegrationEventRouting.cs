namespace PaymentsLedger.PaymentService.Infrastructure.Messaging.ServiceBus;

public interface IIntegrationEventRouter
{
    string GetTopicName(string eventName);
}

internal sealed class StaticIntegrationEventRouter : IIntegrationEventRouter
{
    private static readonly IReadOnlyDictionary<string, string> Map =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PaymentCreated"] = "payments"
        };

    public string GetTopicName(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Event name cannot be null or whitespace.", nameof(eventName));
        }

        if (Map.TryGetValue(eventName, out var topic))
        {
            return topic;
        }

        throw new InvalidOperationException($"No topic mapping found for event '{eventName}'.");
    }
}

