using PaymentsLedger.PaymentService.Domain.PaymentAggregate;

namespace PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task AddOutboxEventAsync(string eventName, string eventContent, CancellationToken cancellationToken = default);
}
