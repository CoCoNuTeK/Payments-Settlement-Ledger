using PaymentsLedger.PaymentService.Domain.PaymentAggregate;
using PaymentsLedger.SharedKernel.Messaging;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Events.PaymentCreated;

namespace PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Commands.PaymentCreated;

public sealed class PaymentCreatedCommandHandler(
    IPaymentRepository paymentRepository,
    IInternalEventBus bus) : IPaymentCreatedCommandHandler
{
    public async Task HandleAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        // Persist the payment
        await paymentRepository.AddAsync(payment, cancellationToken);

        // Publish internal event to be handled and written to the outbox via repository
        var envelope = new InternalMessageEnvelope(
            payload: payment,
            handler: async (sp, ct) =>
            {
                var handler = (IPaymentCreatedEventHandler?)sp.GetService(typeof(IPaymentCreatedEventHandler))
                    ?? throw new InvalidOperationException("IPaymentCreatedEventHandler is not registered in DI.");
                await handler.HandleAsync(payment, ct);
            });

        await bus.PublishAsync(envelope, cancellationToken);
    }
}
