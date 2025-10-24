using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PaymentsLedger.Blazor.Infrastructure.Messaging.InProc;

internal sealed class InProcMessagePump(
    Channel<InternalMessageEnvelope> channel,
    IServiceProvider serviceProvider,
    ILogger<InProcMessagePump> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Blazor in-proc message pump started.");
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
                        logger.LogDebug("Blazor pump processed message {MessageId} (PayloadType={PayloadType}).", message.Id, message.Payload.GetType().FullName);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing blazor in-proc message {MessageId}.", message.Id);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            logger.LogInformation("Blazor in-proc message pump stopped.");
        }
    }
}

