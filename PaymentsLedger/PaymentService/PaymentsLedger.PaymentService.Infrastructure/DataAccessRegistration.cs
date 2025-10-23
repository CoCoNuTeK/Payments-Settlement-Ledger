using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentsLedger.PaymentService.Infrastructure.Persistence;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate.Commands.PaymentCreated;
using PaymentsLedger.PaymentService.Application.Aggregates.PaymentAggregate;
using PaymentsLedger.PaymentService.Infrastructure.Repositories;
using Aspire.Azure.Messaging.ServiceBus;

namespace PaymentsLedger.PaymentService.Infrastructure;

public static class DataAccessRegistration
{
    public static IHostApplicationBuilder AddInfra(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<PaymentDbContext>(connectionName: "paymentsdb");
        builder.AddAzureServiceBusClient(connectionName: "messaging");

        builder.Services.AddInProcMessaging();

        // Repositories
        builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

        // Apply EF Core migrations for PaymentDbContext at startup
        builder.Services.AddHostedService<PaymentDbMigratorHostedService>();

        return builder;
    }
}
