using Microsoft.EntityFrameworkCore;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate;
using PaymentsLedger.PaymentService.Infrastructure.Persistence;

namespace PaymentsLedger.PaymentService.Infrastructure.Repositories;

internal sealed class PaymentRepository(PaymentDbContext dbContext) : IPaymentRepository
{
    private readonly PaymentDbContext _dbContext = dbContext;

    public async Task AddAsync(Payment payment, string outboxEventName, string outboxEventContent, CancellationToken cancellationToken = default)
    {
        await _dbContext.Payments.AddAsync(payment, cancellationToken);
        await _dbContext.OutboxIntegrationEvents.AddAsync(new OutboxIntegrationEvent(outboxEventName, outboxEventContent), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
