using PaymentsLedger.PaymentService.Domain.PaymentAggregate;

namespace PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Commands.PaymentCreated;

public interface IPaymentCreatedHandler
{
    Task HandleAsync(Payment payment, CancellationToken cancellationToken = default);
}

