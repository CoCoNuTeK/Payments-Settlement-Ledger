namespace PaymentsLedger.PaymentService.Application.Abstractions.Messaging;

public interface IInternalEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
}
