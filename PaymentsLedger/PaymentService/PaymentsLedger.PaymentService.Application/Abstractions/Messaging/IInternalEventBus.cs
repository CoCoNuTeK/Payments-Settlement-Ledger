namespace PaymentsLedger.PaymentService.Application.Abstractions.Messaging;

public interface IInternalEventBus
{
    Task PublishAsync(InternalMessageEnvelope message, CancellationToken cancellationToken = default);
}
