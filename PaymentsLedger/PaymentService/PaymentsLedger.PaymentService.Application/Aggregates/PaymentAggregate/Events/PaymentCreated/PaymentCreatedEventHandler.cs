using System.Text.Json;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate;
using PaymentsLedger.SharedKernel.Contracts.IntegrationEvents;

namespace PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Events.PaymentCreated;

public sealed class PaymentCreatedEventHandler(IPaymentRepository repository) : IPaymentCreatedEventHandler
{
    public Task HandleAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        const string eventName = "PaymentCreated";
        var dto = new PaymentCreatedEvent(
            PaymentId: payment.Id,
            MerchantId: payment.MerchantId,
            Amount: payment.Amount.Amount,
            Currency: payment.Amount.Currency,
            CreatedAtUtc: payment.CreatedAtUtc);

        var eventContent = JsonSerializer.Serialize(dto);
        return repository.AddOutboxEventAsync(eventName, eventContent, cancellationToken);
    }
}
