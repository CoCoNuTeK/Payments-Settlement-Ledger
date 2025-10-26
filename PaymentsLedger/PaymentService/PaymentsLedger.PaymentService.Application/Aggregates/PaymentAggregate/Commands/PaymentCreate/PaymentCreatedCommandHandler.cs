using System.Text.Json;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate;
using PaymentsLedger.SharedKernel.Contracts.IntegrationEvents;

namespace PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Commands.PaymentCreated;

public sealed class PaymentCreatedCommandHandler(
    IPaymentRepository paymentRepository) : IPaymentCreatedCommandHandler
{
    public async Task HandleAsync(Payment payment, CancellationToken cancellationToken = default)
    {
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
