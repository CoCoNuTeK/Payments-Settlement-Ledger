using System.Threading.Channels;
using Aspire.Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentsLedger.Blazor.Infrastructure.Messaging.InProc;
using PaymentsLedger.Blazor.Infrastructure.Messaging.ServiceBus;
using PaymentsLedger.SharedKernel.Messaging;

namespace PaymentsLedger.Blazor.Infrastructure.Messaging;

public static class MessagingRegistration
{
    public static IHostApplicationBuilder AddMessaging(this IHostApplicationBuilder builder)
    {
        // Azure Service Bus client (namespace configured in AppHost as "messaging")
        builder.AddAzureServiceBusClient(connectionName: "messaging");

        // In-proc channel (single reader/writer for sequential processing)
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

        // Background pump for in-proc dispatch
        builder.Services.AddHostedService<InProcMessagePump>();

        // Subscriber that listens to payments topic subscription and publishes into the in-proc bus
        builder.Services.AddHostedService<PaymentsEventsSubscriberHostedService>();

        return builder;
    }
}
