using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentsLedger.PaymentService.Infrastructure.Persistence;

namespace PaymentsLedger.PaymentService.Infrastructure;

public static class DataAccessRegistration
{
    public static IHostApplicationBuilder AddInfra(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<PaymentDbContext>(connectionName: "paymentsdb");

        builder.Services.AddInProcMessaging();

        // Apply EF Core migrations for PaymentDbContext at startup
        builder.Services.AddHostedService<PaymentDbMigratorHostedService>();

        return builder;
    }
}
