using Microsoft.EntityFrameworkCore;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate;

namespace PaymentsLedger.PaymentService.Infrastructure.Persistence;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OutboxIntegrationEvent> OutboxIntegrationEvents => Set<OutboxIntegrationEvent>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // EF Core 9+ seeding API. Keep both sync/async for tooling compatibility.
        optionsBuilder
            .UseSeeding((context, isDesignTime) => PaymentDbContextSeeder.Seed((PaymentDbContext)context, isDesignTime))
            .UseAsyncSeeding((context, isDesignTime, ct) => PaymentDbContextSeeder.SeedAsync((PaymentDbContext)context, isDesignTime, ct));
    }
}
