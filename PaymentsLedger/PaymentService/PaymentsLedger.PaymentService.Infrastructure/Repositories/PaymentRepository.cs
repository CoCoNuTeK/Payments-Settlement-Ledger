using Microsoft.EntityFrameworkCore;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate;
using PaymentsLedger.PaymentService.Infrastructure.Persistence;

namespace PaymentsLedger.PaymentService.Infrastructure.Repositories;

internal sealed class PaymentRepository(PaymentDbContext dbContext) : IPaymentRepository
{
    private readonly PaymentDbContext _dbContext = dbContext;

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Payments.AddAsync(payment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddOutboxEventAsync(string eventName, string eventContent, CancellationToken cancellationToken = default)
    {
        await _dbContext.OutboxIntegrationEvents.AddAsync(new OutboxIntegrationEvent(eventName, eventContent), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
