using PaymentsLedger.PaymentService.Domain.PaymentAggregate;

namespace PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Events.PaymentCreated;

public interface IPaymentCreatedEventHandler
{
    Task HandleAsync(Payment payment, CancellationToken cancellationToken = default);
}
