using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PaymentsLedger.PaymentService.Infrastructure.Persistence;

public sealed class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PaymentDbContext>();

        // Design-time only: dummy connection string is sufficient for migrations generation
        const string cs = "Host=localhost;Database=dummy;Username=dummy;Password=dummy";
        optionsBuilder.UseNpgsql(cs);
        return new PaymentDbContext(optionsBuilder.Options);
    }
}
