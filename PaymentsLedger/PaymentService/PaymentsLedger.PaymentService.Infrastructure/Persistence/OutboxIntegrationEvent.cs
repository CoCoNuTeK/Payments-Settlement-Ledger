namespace PaymentsLedger.PaymentService.Infrastructure.Persistence;

public sealed class OutboxIntegrationEvent
{
    public Guid Id { get; private set; }
    public string EventName { get; private set; } = null!;
    public string EventContent { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private OutboxIntegrationEvent() { }

    public OutboxIntegrationEvent(string eventName, string eventContent)
    {
        Id = Guid.NewGuid();
        EventName = eventName;
        EventContent = eventContent;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }
}

