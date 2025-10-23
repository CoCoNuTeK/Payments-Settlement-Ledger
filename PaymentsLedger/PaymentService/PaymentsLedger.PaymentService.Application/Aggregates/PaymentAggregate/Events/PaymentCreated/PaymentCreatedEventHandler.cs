using System.Text.Json;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate;

namespace PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Events.PaymentCreated;

public sealed class PaymentCreatedEventHandler(IPaymentRepository repository) : IPaymentCreatedEventHandler
{
    public Task HandleAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        const string eventName = "PaymentCreated";
        var eventContent = JsonSerializer.Serialize(payment);
        return repository.AddOutboxEventAsync(eventName, eventContent, cancellationToken);
    }
}
