using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PaymentsLedger.MerchantService.Api.Services;

public interface IMerchantServiceBusMessenger
{
    IDisposable SubscribeToMerchantCommands(Func<ServiceBusReceivedMessage, CancellationToken, Task> handler);
    Task PublishMerchantEventAsync(ServiceBusMessage message, CancellationToken cancellationToken = default);
}

public sealed class MerchantServiceBusMessenger : IHostedService, IMerchantServiceBusMessenger, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<MerchantServiceBusMessenger> _logger;
    private Func<ServiceBusReceivedMessage, CancellationToken, Task>? _commandHandler;
    private readonly object _handlerGate = new();

    private ServiceBusProcessor? _merchantCommandsProcessor;
    private ServiceBusSender? _merchantEventsSender;

    private const string MerchantCommandsQueueName = "merchant-commands";
    private const string MerchantEventsTopicName = "merchant-events";

    public MerchantServiceBusMessenger(ServiceBusClient client, ILogger<MerchantServiceBusMessenger> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _merchantEventsSender = _client.CreateSender(MerchantEventsTopicName);

        _merchantCommandsProcessor = _client.CreateProcessor(
            MerchantCommandsQueueName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

        _merchantCommandsProcessor.ProcessMessageAsync += OnCommandReceivedAsync;
        _merchantCommandsProcessor.ProcessErrorAsync += OnProcessingErrorAsync;

        await _merchantCommandsProcessor.StartProcessingAsync(cancellationToken);
        _logger.LogInformation("Merchant Service Bus messenger started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_merchantCommandsProcessor is not null)
        {
            await _merchantCommandsProcessor.StopProcessingAsync(cancellationToken);
            _merchantCommandsProcessor.ProcessMessageAsync -= OnCommandReceivedAsync;
            _merchantCommandsProcessor.ProcessErrorAsync -= OnProcessingErrorAsync;
        }

        _logger.LogInformation("Merchant Service Bus messenger stopped.");
    }

    public IDisposable SubscribeToMerchantCommands(Func<ServiceBusReceivedMessage, CancellationToken, Task> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));
        lock (_handlerGate)
        {
            if (_commandHandler is not null)
            {
                throw new InvalidOperationException("A merchant command handler is already registered.");
            }
            _commandHandler = handler;
        }

        return new Subscription(() =>
        {
            lock (_handlerGate)
            {
                _commandHandler = null;
            }
        });
    }

    public async Task PublishMerchantEventAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        if (_merchantEventsSender is null)
        {
            throw new InvalidOperationException("Service Bus messenger is not started yet.");
        }

        await _merchantEventsSender.SendMessageAsync(message, cancellationToken);
    }

    private async Task OnCommandReceivedAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var handler = _commandHandler;
            if (handler is null)
            {
                // No handlers attached; complete to avoid redelivery storms.
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            await handler(args.Message, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message from merchant-commands queue.");
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task OnProcessingErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processing error. Entity: {EntityPath}", args.EntityPath);
        return Task.CompletedTask;
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _onDispose;
        private bool _disposed;

        public Subscription(Action onDispose) => _onDispose = onDispose;

        public void Dispose()
        {
            if (_disposed) return;
            _onDispose();
            _disposed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_merchantCommandsProcessor is not null)
        {
            await _merchantCommandsProcessor.DisposeAsync();
        }

        if (_merchantEventsSender is not null)
        {
            await _merchantEventsSender.DisposeAsync();
        }
    }
}
