using Microsoft.Extensions.DependencyInjection;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Commands.PaymentCreated;

namespace PaymentsLedger.PaymentService.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddAppHandlers(this IServiceCollection services)
    {
        services.AddScoped<IPaymentCreatedHandler, PaymentCreatedHandler>();
        return services;
    }
}
