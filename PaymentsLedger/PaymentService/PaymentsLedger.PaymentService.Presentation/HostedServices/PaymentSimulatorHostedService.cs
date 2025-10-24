using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PaymentsLedger.PaymentService.Presentation.DTO;
using PaymentsLedger.SharedKernel.Messaging;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Commands.PaymentCreated;

namespace PaymentsLedger.PaymentService.Presentation.HostedServices;

internal sealed class PaymentSimulatorHostedService(
    IInternalEventBus bus,
    ILogger<PaymentSimulatorHostedService> logger) : BackgroundService
{
    private static readonly Guid Merchant1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Merchant2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Random _random = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Payment simulator started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                const int batchSize = 10;
                for (var i = 0; i < batchSize; i++)
                {
                    var paymentId = Guid.NewGuid();
                    var merchantId = _random.Next(0, 2) == 0 ? Merchant1 : Merchant2;
                    var amount = Math.Round((decimal)(_random.NextDouble() * 1000.0 + 1.0), 2);
                    var currency = "EUR";

                    var payload = new PaymentCreationPayload(
                        PaymentId: paymentId,
                        MerchantId: merchantId,
                        Amount: amount,
                        Currency: currency);

                    var payment = payload.ToDomain();

                    var envelope = new InternalMessageEnvelope(
                        payload: payment,
                        handler: async (sp, ct) =>
                        {
                            var handler = sp.GetRequiredService<IPaymentCreatedCommandHandler>();
                            await handler.HandleAsync(payment, ct);
                        });

                    await bus.PublishAsync(envelope, stoppingToken);
                }

                logger.LogInformation("Payment simulator published {Count} messages.", batchSize);

                // Pace: 10 payments every 10 seconds
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in payment simulator loop.");
                // Small backoff before retrying the loop
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); } catch { }
            }
        }

        logger.LogInformation("Payment simulator stopped.");
    }
}
