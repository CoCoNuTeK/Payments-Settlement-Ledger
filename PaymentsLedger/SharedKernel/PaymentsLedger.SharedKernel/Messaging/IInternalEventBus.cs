namespace PaymentsLedger.SharedKernel.Messaging;

public interface IInternalEventBus
{
    Task PublishAsync(InternalMessageEnvelope message, CancellationToken cancellationToken = default);
}

