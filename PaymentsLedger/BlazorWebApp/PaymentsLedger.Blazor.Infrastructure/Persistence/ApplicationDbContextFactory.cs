using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PaymentsLedger.Blazor.Infrastructure.Identity;

namespace PaymentsLedger.Blazor.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Design-time only: dummy connection string is sufficient for migrations generation
        const string cs = "Host=localhost;Database=dummy;Username=dummy;Password=dummy";
        optionsBuilder.UseNpgsql(cs);
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
