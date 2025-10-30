namespace PaymentsLedger.PaymentService.Infrastructure.Persistence;

public sealed class OutboxIntegrationEvent
{
    public Guid Id { get; private set; }
    public string EventName { get; private set; } = null!;
    public string EventContent { get; private set; } = null!;
    public string? TraceParent { get; private set; }
    public string? TraceState { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public bool Processed { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    private OutboxIntegrationEvent() { }

    public OutboxIntegrationEvent(string eventName, string eventContent, string? traceParent = null, string? traceState = null)
    {
        Id = Guid.NewGuid();
        EventName = eventName;
        EventContent = eventContent;
        TraceParent = traceParent;
        TraceState = traceState;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        Processed = false;
    }

    public void MarkProcessed()
    {
        Processed = true;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }
}
