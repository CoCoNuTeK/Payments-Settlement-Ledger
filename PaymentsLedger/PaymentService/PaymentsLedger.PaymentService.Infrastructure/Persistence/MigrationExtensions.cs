using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PaymentsLedger.PaymentService.Infrastructure.Persistence;

public static class MigrationExtensions
{
    public static async Task ApplyPaymentDbMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PaymentDbContext>>();

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            await db.Database.MigrateAsync();
            logger.LogInformation("Payment database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying payment database migrations before app run.");
            throw;
        }
    }
}
