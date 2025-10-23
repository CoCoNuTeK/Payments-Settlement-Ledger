using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentsLedger.PaymentService.Application.MessagingDefinition;

namespace PaymentsLedger.PaymentService.Infrastructure.Messaging.InProc;

internal sealed class InProcMessagePump(
    Channel<InternalMessageEnvelope> channel,
    IServiceProvider serviceProvider,
    ILogger<InProcMessagePump> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("InProc message pump started.");
        var reader = channel.Reader;

        try
        {
            while (await reader.WaitToReadAsync(stoppingToken))
            {
                while (reader.TryRead(out var message))
                {
                    try
                    {
                        using var scope = serviceProvider.CreateScope();

                        await message.Handler(scope.ServiceProvider, stoppingToken);
                        logger.LogDebug("Processed message {MessageId} (PayloadType={PayloadType}).", message.Id, message.Payload.GetType().FullName);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        // graceful shutdown
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing message {MessageId}.", message.Id);
                        // For demo: swallow and continue. Add retries/poison handling if needed.
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutting down
        }
        finally
        {
            logger.LogInformation("InProc message pump stopped.");
        }
    }
}
