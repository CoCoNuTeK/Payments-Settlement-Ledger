namespace PaymentsLedger.Blazor.Infrastructure.Messaging.InProc;

public interface IInternalEventBus
{
    Task PublishAsync(InternalMessageEnvelope message, CancellationToken cancellationToken = default);
}

