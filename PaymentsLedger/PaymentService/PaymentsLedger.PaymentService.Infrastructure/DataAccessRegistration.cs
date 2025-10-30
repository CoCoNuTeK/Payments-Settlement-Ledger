using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentsLedger.PaymentService.Infrastructure.Persistence;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Commands.PaymentCreated;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate;
using PaymentsLedger.PaymentService.Infrastructure.Repositories;
using Aspire.Azure.Messaging.ServiceBus;
using PaymentsLedger.PaymentService.Infrastructure.Observability;

namespace PaymentsLedger.PaymentService.Infrastructure;

public static class DataAccessRegistration
{
    public static IHostApplicationBuilder AddInfra(this IHostApplicationBuilder builder)
    {
        // Observability (OpenTelemetry) - traces, metrics, logs
        builder.AddObservability();

        builder.AddNpgsqlDbContext<PaymentDbContext>(
            connectionName: "paymentsdb",
            configureDbContextOptions: options =>
            {
                // Avoid throwing on PendingModelChangesWarning during runtime migrations
                options.ConfigureWarnings(w => w.Log(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });
        // Messaging (Service Bus client + in-proc bus + pumps)
        builder.AddMessaging();

        // Repositories
        builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

        return builder;
    }
}
