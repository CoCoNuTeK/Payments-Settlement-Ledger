using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PaymentsLedger.Blazor.Services;

public interface IBlazorServiceBusMessenger
{
    Task SendMerchantCommandAsync(ServiceBusMessage message, CancellationToken cancellationToken = default);
    Task SendPaymentCommandAsync(ServiceBusMessage message, CancellationToken cancellationToken = default);
    IDisposable SubscribeToReplies(Func<ServiceBusReceivedMessage, CancellationToken, Task> handler);
}

public sealed class BlazorServiceBusMessenger : IHostedService, IBlazorServiceBusMessenger, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<BlazorServiceBusMessenger> _logger;
    private readonly ConcurrentDictionary<Guid, Func<ServiceBusReceivedMessage, CancellationToken, Task>> _replyHandlers = new();
    private ServiceBusSender? _merchantSender;
    private ServiceBusSender? _paymentSender;
    private ServiceBusProcessor? _merchantEventsProcessor;
    private ServiceBusProcessor? _paymentEventsProcessor;

    private const string MerchantCommandsQueueName = "merchant-commands";
    private const string PaymentCommandsQueueName = "payment-commands";
    private const string MerchantEventsTopicName = "merchant-events";
    private const string PaymentEventsTopicName = "payment-events";
    private const string BlazorOnMerchantSubscription = "blazor-on-merchant";
    private const string BlazorOnPaymentSubscription = "blazor-on-payment";

    public BlazorServiceBusMessenger(ServiceBusClient client, ILogger<BlazorServiceBusMessenger> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _merchantSender = _client.CreateSender(MerchantCommandsQueueName);
        _paymentSender = _client.CreateSender(PaymentCommandsQueueName);

        _merchantEventsProcessor = _client.CreateProcessor(
            MerchantEventsTopicName,
            BlazorOnMerchantSubscription,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

        _paymentEventsProcessor = _client.CreateProcessor(
            PaymentEventsTopicName,
            BlazorOnPaymentSubscription,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

        _merchantEventsProcessor.ProcessMessageAsync += OnEventReceivedAsync;
        _merchantEventsProcessor.ProcessErrorAsync += OnProcessingErrorAsync;
        _paymentEventsProcessor.ProcessMessageAsync += OnEventReceivedAsync;
        _paymentEventsProcessor.ProcessErrorAsync += OnProcessingErrorAsync;

        await _merchantEventsProcessor.StartProcessingAsync(cancellationToken);
        await _paymentEventsProcessor.StartProcessingAsync(cancellationToken);
        _logger.LogInformation("Blazor Service Bus messenger started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_merchantEventsProcessor is not null)
        {
            await _merchantEventsProcessor.StopProcessingAsync(cancellationToken);
            _merchantEventsProcessor.ProcessMessageAsync -= OnEventReceivedAsync;
            _merchantEventsProcessor.ProcessErrorAsync -= OnProcessingErrorAsync;
        }

        if (_paymentEventsProcessor is not null)
        {
            await _paymentEventsProcessor.StopProcessingAsync(cancellationToken);
            _paymentEventsProcessor.ProcessMessageAsync -= OnEventReceivedAsync;
            _paymentEventsProcessor.ProcessErrorAsync -= OnProcessingErrorAsync;
        }

        _logger.LogInformation("Blazor Service Bus messenger stopped.");
    }

    public async Task SendMerchantCommandAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        if (_merchantSender is null)
        {
            throw new InvalidOperationException("Service Bus messenger is not started yet.");
        }

        await _merchantSender.SendMessageAsync(message, cancellationToken);
    }

    public async Task SendPaymentCommandAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        if (_paymentSender is null)
        {
            throw new InvalidOperationException("Service Bus messenger is not started yet.");
        }

        await _paymentSender.SendMessageAsync(message, cancellationToken);
    }

    public IDisposable SubscribeToReplies(Func<ServiceBusReceivedMessage, CancellationToken, Task> handler)
    {
        var key = Guid.NewGuid();
        _replyHandlers[key] = handler;
        return new Subscription(_replyHandlers, key);
    }

    private async Task OnEventReceivedAsync(ProcessMessageEventArgs args)
    {
        try
        {
            if (_replyHandlers.IsEmpty)
            {
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            foreach (var handler in _replyHandlers.Values)
            {
                await handler(args.Message, args.CancellationToken);
            }

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message from replies queue.");
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
        private readonly ConcurrentDictionary<Guid, Func<ServiceBusReceivedMessage, CancellationToken, Task>> _handlers;
        private readonly Guid _key;
        private bool _disposed;

        public Subscription(ConcurrentDictionary<Guid, Func<ServiceBusReceivedMessage, CancellationToken, Task>> handlers, Guid key)
        {
            _handlers = handlers;
            _key = key;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _handlers.TryRemove(_key, out _);
            _disposed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_merchantEventsProcessor is not null)
        {
            await _merchantEventsProcessor.DisposeAsync();
        }

        if (_paymentEventsProcessor is not null)
        {
            await _paymentEventsProcessor.DisposeAsync();
        }

        if (_merchantSender is not null)
        {
            await _merchantSender.DisposeAsync();
        }

        if (_paymentSender is not null)
        {
            await _paymentSender.DisposeAsync();
        }
    }
}
