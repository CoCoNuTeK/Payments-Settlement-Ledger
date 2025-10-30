using System.Diagnostics;
using System.Text.Json;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate;
using PaymentsLedger.SharedKernel.Contracts.IntegrationEvents;

namespace PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Commands.PaymentCreated;

public sealed class PaymentCreatedCommandHandler(
    IPaymentRepository paymentRepository) : IPaymentCreatedCommandHandler
{
    private static readonly ActivitySource ActivitySource = new("PaymentsLedger.PaymentService.Application");

    public async Task HandleAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity(
            "application.payment.persist",
            ActivityKind.Consumer);

        activity?.SetTag("app.layer", "Application");
        activity?.SetTag("payment.id", payment.Id);
        activity?.SetTag("merchant.id", payment.MerchantId);
        activity?.SetTag("payment.amount", payment.Amount.Amount);
        activity?.SetTag("payment.currency", payment.Amount.Currency);

        const string eventName = "PaymentCreated";
        var dto = new PaymentCreatedEvent(
            PaymentId: payment.Id,
            MerchantId: payment.MerchantId,
            Amount: payment.Amount.Amount,
            Currency: payment.Amount.Currency,
            CreatedAtUtc: payment.CreatedAtUtc);

        var eventContent = JsonSerializer.Serialize(dto);

        // Persist payment and outbox together within a single SaveChanges
        await paymentRepository.AddAsync(payment, eventName, eventContent, cancellationToken);
    }
}
