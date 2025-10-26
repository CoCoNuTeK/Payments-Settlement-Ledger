using PaymentsLedger.PaymentService.Domain.PaymentAggregate;

namespace PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate;

public interface IPaymentRepository
{
    // Adds the payment and an associated outbox integration event atomically
    Task AddAsync(Payment payment, string outboxEventName, string outboxEventContent, CancellationToken cancellationToken = default);
}
