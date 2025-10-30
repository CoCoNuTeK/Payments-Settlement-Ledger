using System.Diagnostics;
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
    ILogger<OutboxPublisherHostedService> logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("PaymentsLedger.PaymentService.Infrastructure");
    private const int BatchSize = 10;
    private readonly Dictionary<string, ServiceBusSender> _senderCache = new(StringComparer.OrdinalIgnoreCase);

    private ServiceBusSender GetOrCreateSender(string topicName)
    {
        if (_senderCache.TryGetValue(topicName, out var sender))
        {
            return sender;
        }

        var created = client.CreateSender(topicName);
        _senderCache[topicName] = created;
        return created;
    }
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
                        .Take(BatchSize)
                        .ToListAsync(stoppingToken);

                    if (pending.Count == 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        continue;
                    }

                    var evt = pending[0];
                    try
                    {
                        var topicName = IntegrationEventRouting.GetTopicName(evt.EventName);
                        var sender = GetOrCreateSender(topicName);

                        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(evt.EventContent))
                        {
                            Subject = evt.EventName,
                            ContentType = "application/json",
                            MessageId = evt.Id.ToString()
                        };

                        // Build ActivityLink from outbox stored W3C headers to relate this publish to the handler span
                        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        if (!string.IsNullOrWhiteSpace(evt.TraceParent)) headers["traceparent"] = evt.TraceParent!;
                        if (!string.IsNullOrWhiteSpace(evt.TraceState)) headers["tracestate"] = evt.TraceState!;
                        var parent = OpenTelemetry.Context.Propagation.Propagators.DefaultTextMapPropagator.Extract(
                            default,
                            headers,
                            static (carrier, key) => carrier.TryGetValue(key, out var v) ? new[] { v } : Array.Empty<string>());
                        var links = parent.ActivityContext != default
                            ? new[] { new ActivityLink(parent.ActivityContext) }
                            : Array.Empty<ActivityLink>();

                        using var activity = ActivitySource.StartActivity(
                            "servicebus.outbox.publish",
                            ActivityKind.Producer,
                            parentContext: default,
                            tags: null,
                            links: links);

                        activity?.SetTag("app.layer", "Infrastructure");
                        activity?.SetTag("messaging.system", "azureservicebus");
                        activity?.SetTag("messaging.destination.kind", "topic");
                        activity?.SetTag("messaging.destination.name", topicName);
                        activity?.SetTag("messaging.operation", "publish");
                        activity?.SetTag("messaging.message_id", message.MessageId);
                        activity?.SetTag("messaging.message.conversation_id", evt.Id.ToString());
                        activity?.AddEvent(new ActivityEvent(
                            "outbox.event.ready",
                            tags: new ActivityTagsCollection
                            {
                                ["event.name"] = evt.EventName,
                                ["event.id"] = evt.Id.ToString(),
                                ["topic"] = topicName
                            }));

                        await sender.SendMessageAsync(message, stoppingToken);

                        evt.MarkProcessed();
                        await db.SaveChangesAsync(stoppingToken);

                        activity?.AddEvent(new ActivityEvent(
                            "outbox.event.published",
                            tags: new ActivityTagsCollection
                            {
                                ["event.name"] = evt.EventName,
                                ["event.id"] = evt.Id.ToString(),
                                ["topic"] = topicName
                            }));

                        // Prefer Activity event over debug log
                        activity?.AddEvent(new ActivityEvent(
                            "outbox.event.debug",
                            tags: new ActivityTagsCollection
                            {
                                ["event.name"] = evt.EventName,
                                ["event.id"] = evt.Id.ToString(),
                                ["topic"] = topicName,
                                ["message.id"] = message.MessageId
                            }));
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Activity.Current?.AddEvent(new ActivityEvent(
                            "outbox.event.error",
                            tags: new ActivityTagsCollection
                            {
                                ["event.id"] = evt.Id.ToString(),
                                ["exception.type"] = ex.GetType().FullName,
                                ["exception.message"] = ex.Message
                            }));
                        // Prefer Activity event over error log; keeping entity unprocessed for retry
                        Activity.Current?.AddEvent(new ActivityEvent(
                            "outbox.event.error",
                            tags: new ActivityTagsCollection
                            {
                                ["event.name"] = evt.EventName,
                                ["event.id"] = evt.Id.ToString(),
                                ["exception.type"] = ex.GetType().FullName,
                                ["exception.message"] = ex.Message
                            }));
                        // leave as unprocessed; will retry on next loop
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    using var activity = ActivitySource.StartActivity("servicebus.outbox.loop");
                    activity?.AddEvent(new ActivityEvent(
                        "outbox.loop.error",
                        tags: new ActivityTagsCollection
                        {
                            ["exception.type"] = ex.GetType().FullName,
                            ["exception.message"] = ex.Message
                        }));
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
            foreach (var s in _senderCache.Values)
            {
                try { s.DisposeAsync().AsTask().GetAwaiter().GetResult(); } catch { }
            }
            logger.LogInformation("Outbox publisher stopped.");
        }
    }
}
