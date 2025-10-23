using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using PaymentsLedger.PaymentService.Application.MessagingDefinition;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Commands.PaymentCreated;
using PaymentsLedger.PaymentService.Infrastructure.Messaging.InProc;
using PaymentsLedger.PaymentService.Infrastructure.Messaging.ServiceBus;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Events.PaymentCreated;

namespace PaymentsLedger.PaymentService.Infrastructure;

public static class MessagingRegistration
{
    public static IServiceCollection AddInProcMessaging(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            };

            return Channel.CreateBounded<InternalMessageEnvelope>(options);
        });

        services.AddSingleton<IInternalEventBus, InProcChannel>();

        // Register application handlers used by the in-proc messaging system
        services.AddScoped<IPaymentCreatedCommandHandler, PaymentCreatedCommandHandler>();
        services.AddScoped<IPaymentCreatedEventHandler, PaymentCreatedEventHandler>();

        // Routing for external integration events (eventName -> topic/queue)
        services.AddSingleton<IIntegrationEventRouter, StaticIntegrationEventRouter>();
        
        // Background consumer that drains the channel and invokes handlers
        services.AddHostedService<InProcMessagePump>();

        // Background publisher that reads outbox table and publishes to Service Bus
        services.AddHostedService<OutboxPublisherHostedService>();
        return services;
    }
}
