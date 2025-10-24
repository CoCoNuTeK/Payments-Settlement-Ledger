using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentsLedger.PaymentService.Infrastructure.Persistence;

namespace PaymentsLedger.PaymentService.Infrastructure.Messaging.ServiceBus;

internal sealed class OutboxPublisherHostedService(
    IServiceScopeFactory scopeFactory,
    ServiceBusClient client,
    IIntegrationEventRouter router,
    ILogger<OutboxPublisherHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox publisher started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

                    var pending = await db.OutboxIntegrationEvents
                        .Where(e => !e.Processed)
                        .OrderBy(e => e.CreatedAtUtc)
                        .Take(50)
                        .ToListAsync(stoppingToken);

                    if (pending.Count == 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        continue;
                    }

                    foreach (var evt in pending)
                    {
                        try
                        {
                            var topicName = router.GetTopicName(evt.EventName);
                            var sender = client.CreateSender(topicName);

                            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(evt.EventContent))
                            {
                                Subject = evt.EventName,
                                ContentType = "application/json"
                            };

                            await sender.SendMessageAsync(message, stoppingToken);

                            evt.MarkProcessed();
                            await db.SaveChangesAsync(stoppingToken);

                            logger.LogDebug("Published outbox event {EventName} (Id={Id}) to topic {Topic}.", evt.EventName, evt.Id, topicName);
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error publishing outbox event {EventName} (Id={Id}). Will retry next pass.", evt.EventName, evt.Id);
                            // leave as unprocessed; will retry on next loop
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error in outbox publisher loop. Retrying shortly.");
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutting down
        }
        finally
        {
            logger.LogInformation("Outbox publisher stopped.");
        }
    }
}
