using Microsoft.EntityFrameworkCore;
using PaymentsLedger.MerchantService.Api.Data.Entities;

namespace PaymentsLedger.MerchantService.Api.Data;

public class MerchantDbContext(DbContextOptions<MerchantDbContext> options) : DbContext(options)
{
    public DbSet<Merchant> Merchants => Set<Merchant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var merchant = modelBuilder.Entity<Merchant>();
        merchant.ToTable("merchants");
        merchant.HasKey(m => m.Id);
        merchant.Property(m => m.Name).IsRequired().HasMaxLength(200);
        merchant.Property(m => m.CreatedAtUtc).IsRequired();
    }
}

