using Microsoft.EntityFrameworkCore;
using PaymentsLedger.PaymentService.Api.Data.Entities;

namespace PaymentsLedger.PaymentService.Api.Data;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var payment = modelBuilder.Entity<Payment>();
        payment.ToTable("payments");
        payment.HasKey(p => p.Id);
        payment.Property(p => p.MerchantReference).IsRequired().HasMaxLength(100);
        payment.Property(p => p.Amount).HasColumnType("numeric(18,2)").IsRequired();
        payment.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        payment.Property(p => p.CreatedAtUtc).IsRequired();
    }
}

