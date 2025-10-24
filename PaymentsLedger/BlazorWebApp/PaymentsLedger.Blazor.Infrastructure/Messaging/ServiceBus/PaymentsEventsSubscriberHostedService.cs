using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentsLedger.Blazor.Infrastructure.Messaging.InProc;

namespace PaymentsLedger.Blazor.Infrastructure.Messaging.ServiceBus;

internal sealed class PaymentsEventsSubscriberHostedService(
    ServiceBusClient client,
    IInternalEventBus bus,
    ILogger<PaymentsEventsSubscriberHostedService> logger) : BackgroundService
{
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Inbox subscriber starting (topic=payments, subscription=blazor-sub).");

        _processor = client.CreateProcessor("payments", "blazor-sub", new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = true,
            MaxConcurrentCalls = 1
        });

        _processor.ProcessMessageAsync += OnMessageAsync;
        _processor.ProcessErrorAsync += OnErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            // Keep running until cancellation
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(500, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            try { await _processor.StopProcessingAsync(); } catch { }
            try { await _processor.DisposeAsync(); } catch { }
            logger.LogInformation("Inbox subscriber stopped.");
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Service Bus inbox error. Entity={EntityPath} Namespace={Namespace}", args.EntityPath, args.FullyQualifiedNamespace);
        return Task.CompletedTask;
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();

        var envelope = new InternalMessageEnvelope(
            payload: body,
            handler: async (sp, ct) =>
            {
                // Default no-op handler; will be replaced by concrete handlers as needed
                logger.LogInformation("Inbox received message with Subject {Subject}", args.Message.Subject);
                await Task.CompletedTask;
            });

        await bus.PublishAsync(envelope);
    }
}
