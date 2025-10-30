using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate;
using PaymentsLedger.PaymentService.Infrastructure.Persistence;

namespace PaymentsLedger.PaymentService.Infrastructure.Repositories;

internal sealed class PaymentRepository(PaymentDbContext dbContext) : IPaymentRepository
{
    private static readonly ActivitySource ActivitySource = new("PaymentsLedger.PaymentService.Infrastructure");
    private static readonly Meter Meter = new("PaymentsLedger.PaymentService.Infrastructure");
    private static readonly Counter<long> PaymentsCreatedCounter = Meter.CreateCounter<long>(
        name: "payments.created",
        unit: "count",
        description: "Number of payments successfully created.");
    private readonly PaymentDbContext _dbContext = dbContext;

    public async Task AddAsync(Payment payment, string outboxEventName, string outboxEventContent, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity(
            "infrastructure.payment.persist",
            ActivityKind.Internal);

        activity?.SetTag("app.layer", "Infrastructure");
        activity?.SetTag("payment.id", payment.Id);
        activity?.SetTag("merchant.id", payment.MerchantId);
        activity?.SetTag("payment.amount", payment.Amount.Amount);
        activity?.SetTag("payment.currency", payment.Amount.Currency);

        await _dbContext.Payments.AddAsync(payment, cancellationToken);

        // Capture current Activity context for outbox linkage (W3C trace headers)
        var traceParent = Activity.Current?.Id;
        var traceState = Activity.Current?.TraceStateString;

        await _dbContext.OutboxIntegrationEvents.AddAsync(
            new OutboxIntegrationEvent(outboxEventName, outboxEventContent, traceParent, traceState),
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Increment custom metric after successful persistence
        PaymentsCreatedCounter.Add(1);
    }
}
