using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using PaymentsLedger.PaymentService.Application.Abstractions.Messaging;
using PaymentsLedger.PaymentService.Infrastructure.Messaging.InProc;

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

            return Channel.CreateBounded<object>(options);
        });

        services.AddSingleton<IInternalEventBus, InProcChannel>();
        return services;
    }
}
