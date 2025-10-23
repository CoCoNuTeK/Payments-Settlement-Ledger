using PaymentsLedger.PaymentService.Domain.PaymentAggregate;

namespace PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Commands.PaymentCreated;

public sealed class PaymentCreatedHandler : IPaymentCreatedHandler
{
    public Task HandleAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        // TODO: Implement application logic for handling a newly created payment
        // e.g., validation, domain services, repository writes, publishing events, etc.
        return Task.CompletedTask;
    }
}
