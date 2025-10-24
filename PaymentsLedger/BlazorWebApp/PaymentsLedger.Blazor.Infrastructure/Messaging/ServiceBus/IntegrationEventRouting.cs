namespace PaymentsLedger.Blazor.Infrastructure.Messaging.ServiceBus;

public static class IntegrationEventRouting
{
    private static readonly IReadOnlyDictionary<string, string> Map =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PaymentCreated"] = "payments"
        };

    public static string GetTopicName(string eventName)
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

    public static bool TryGetTopicName(string eventName, out string topicName)
        => Map.TryGetValue(eventName, out topicName!);
}

