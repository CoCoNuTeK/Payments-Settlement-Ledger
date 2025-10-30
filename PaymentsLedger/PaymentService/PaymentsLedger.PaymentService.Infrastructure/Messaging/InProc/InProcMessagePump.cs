using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentsLedger.SharedKernel.Messaging;

namespace PaymentsLedger.PaymentService.Infrastructure.Messaging.InProc;

internal sealed class InProcMessagePump(
    Channel<InternalMessageEnvelope> channel,
    IServiceProvider serviceProvider,
    ILogger<InProcMessagePump> logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("PaymentsLedger.PaymentService.Infrastructure");
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
                        // Prefer parent/child correlation for in-proc: use ParentContext when present
                        var parentContext = message.ParentContext;
                        using var activity = parentContext is { } ctx && ctx != default
                            ? ActivitySource.StartActivity(
                                "inproc.message.process",
                                ActivityKind.Consumer,
                                ctx)
                            : ActivitySource.StartActivity(
                                "inproc.message.process",
                                ActivityKind.Consumer);

                        activity?.SetTag("app.layer", "Infrastructure");
                        activity?.SetTag("messaging.system", "inproc");
                        activity?.SetTag("messaging.operation", "process");
                        activity?.SetTag("messaging.destination.name", "inproc-channel");
                        activity?.SetTag("messaging.message_id", message.Id);
                        activity?.SetTag("message.payload_type", message.Payload.GetType().FullName);

                        activity?.AddEvent(new ActivityEvent(
                            "message.dequeued",
                            tags: new ActivityTagsCollection
                            {
                                ["message.id"] = message.Id,
                                ["payload.type"] = message.Payload.GetType().FullName
                            }));

                        using var scope = serviceProvider.CreateScope();
                        await message.Handler(scope.ServiceProvider, stoppingToken);

                        activity?.AddEvent(new ActivityEvent(
                            "message.processed",
                            tags: new ActivityTagsCollection
                            {
                                ["message.id"] = message.Id
                            }));
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        // graceful shutdown
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Attach error details to current activity if present
                        Activity.Current?.AddEvent(new ActivityEvent(
                            "message.error",
                            tags: new ActivityTagsCollection
                            {
                                ["message.id"] = message.Id,
                                ["exception.type"] = ex.GetType().FullName,
                                ["exception.message"] = ex.Message
                            }));

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
