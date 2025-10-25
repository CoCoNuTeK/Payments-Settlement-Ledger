using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentsLedger.Blazor.Infrastructure.Messaging.InProc;
using PaymentsLedger.SharedKernel.Messaging;

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
        var subject = args.Message.Subject;
        var body = args.Message.Body.ToString();

        if (IncomingIntegrationEventRouting.TryGetHandler(subject, out var invoker))
        {
            var envelope = new InternalMessageEnvelope(
                payload: body,
                handler: (sp, ct) => invoker(sp, subject, body, ct));

            await bus.PublishAsync(envelope);
            return;
        }

        // No mapping found: log and skip (no default handler)
        logger.LogWarning(
            "No handler mapping found for incoming event. Subject={Subject} MessageId={MessageId}",
            subject,
            args.Message.MessageId);
    }
}
