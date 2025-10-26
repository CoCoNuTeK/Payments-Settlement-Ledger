using System.Threading.Channels;
using Aspire.Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentsLedger.SharedKernel.Messaging;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Commands.PaymentCreated;
using PaymentsLedger.PaymentService.Infrastructure.Messaging.InProc;
using PaymentsLedger.PaymentService.Infrastructure.Messaging.ServiceBus;

namespace PaymentsLedger.PaymentService.Infrastructure;

public static class MessagingRegistration
{
    public static IHostApplicationBuilder AddMessaging(this IHostApplicationBuilder builder)
    {
        // Azure Service Bus client (namespace configured in AppHost as "messaging")
        builder.AddAzureServiceBusClient(connectionName: "messaging");

        // In-proc channel for internal messages
        builder.Services.AddSingleton(sp =>
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false
            };

            return Channel.CreateBounded<InternalMessageEnvelope>(options);
        });

        // In-proc bus facade
        builder.Services.AddSingleton<IInternalEventBus, InProcChannel>();

        // Register application handlers used by the in-proc messaging system
        builder.Services.AddScoped<IPaymentCreatedCommandHandler, PaymentCreatedCommandHandler>();

        // Background consumer that drains the channel and invokes handlers
        builder.Services.AddHostedService<InProcMessagePump>();

        // Background publisher that reads outbox table and publishes to Service Bus
        builder.Services.AddHostedService<OutboxPublisherHostedService>();

        return builder;
    }
}
