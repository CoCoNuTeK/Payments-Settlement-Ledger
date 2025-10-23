using PaymentsLedger.PaymentService.Domain.PaymentAggregate;

namespace PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Commands.PaymentCreated;

public sealed class PaymentCreatedHandler(IPaymentRepository paymentRepository) : IPaymentCreatedHandler
{
    public Task HandleAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        // Persist the payment. Further logic (events, notifications) can follow later.
        return paymentRepository.AddAsync(payment, cancellationToken);
    }
}
