using Microsoft.EntityFrameworkCore;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate;
using PaymentsLedger.PaymentService.Domain.PaymentAggregate.ValueObjects;

namespace PaymentsLedger.PaymentService.Infrastructure.Persistence;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OutboxIntegrationEvent> OutboxIntegrationEvents => Set<OutboxIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map value object Money under Payment as a complex property
        modelBuilder.Entity<Payment>().ComplexProperty(p => p.Amount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Amount");
            m.Property(x => x.Currency).HasColumnName("Currency");
        });
    }
}
