using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PaymentsLedger.Blazor.Infrastructure.Identity;
using PaymentsLedger.Blazor.Infrastructure.Seeding;

namespace PaymentsLedger.Blazor.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<MerchantUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSeeding((context, isDesignTime) => ApplicationDbContextSeeder.Seed((ApplicationDbContext)context, isDesignTime))
            .UseAsyncSeeding((context, isDesignTime, ct) => ApplicationDbContextSeeder.SeedAsync((ApplicationDbContext)context, isDesignTime, ct));
    }
}
