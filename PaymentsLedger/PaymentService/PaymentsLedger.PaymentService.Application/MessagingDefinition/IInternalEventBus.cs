namespace PaymentsLedger.PaymentService.Application.MessagingDefinition;

public interface IInternalEventBus
{
    Task PublishAsync(InternalMessageEnvelope message, CancellationToken cancellationToken = default);
}
