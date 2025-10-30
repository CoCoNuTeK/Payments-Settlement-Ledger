using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentsLedger.Blazor.Infrastructure.Messaging.InProc;
using PaymentsLedger.SharedKernel.Messaging;
using OpenTelemetry.Context.Propagation;

namespace PaymentsLedger.Blazor.Infrastructure.Messaging.ServiceBus;

internal sealed class PaymentsEventsSubscriberHostedService(
    ServiceBusClient client,
    IInternalEventBus bus,
    ILogger<PaymentsEventsSubscriberHostedService> logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("PaymentsLedger.Blazor.Infrastructure");
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
        var messageId = args.Message.MessageId;

        // Extract W3C context from Service Bus properties to create a link back to publisher trace
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (args.Message.ApplicationProperties.TryGetValue("traceparent", out var tpObj) && tpObj is string tpStr)
        {
            headers["traceparent"] = tpStr;
        }
        else if (args.Message.ApplicationProperties.TryGetValue("Diagnostic-Id", out var diagObj) && diagObj is string diagStr)
        {
            headers["traceparent"] = diagStr; // Azure SDK often sets Diagnostic-Id to Activity.Id (W3C)
        }
        if (args.Message.ApplicationProperties.TryGetValue("tracestate", out var tsObj) && tsObj is string tsStr)
        {
            headers["tracestate"] = tsStr;
        }

        var parent = Propagators.DefaultTextMapPropagator.Extract(
            default,
            headers,
            static (carrier, key) => carrier.TryGetValue(key, out var v) ? new[] { v } : Array.Empty<string>());
        var links = parent.ActivityContext != default
            ? new[] { new ActivityLink(parent.ActivityContext) }
            : Array.Empty<ActivityLink>();

        using var activity = ActivitySource.StartActivity(
            "servicebus.message.receive",
            ActivityKind.Consumer,
            parentContext: default,
            tags: null,
            links: links);

        activity?.SetTag("app.layer", "Infrastructure");
        activity?.SetTag("messaging.system", "azureservicebus");
        activity?.SetTag("messaging.destination.kind", "subscription");
        activity?.SetTag("messaging.destination.name", "payments/blazor-sub");
        activity?.SetTag("messaging.operation", "process");
        activity?.SetTag("messaging.message_id", messageId);
        activity?.SetTag("event.subject", subject);
        activity?.AddEvent(new ActivityEvent(
            "payments.event.received",
            tags: new ActivityTagsCollection
            {
                ["message.id"] = messageId,
                ["event.subject"] = subject
            }));

        if (IncomingIntegrationEventRouting.TryGetHandler(subject, out var invoker))
        {
            var envelope = new InternalMessageEnvelope(
                payload: body,
                handler: (sp, ct) => invoker(sp, subject, body, ct),
                parentContext: Activity.Current?.Context);

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
