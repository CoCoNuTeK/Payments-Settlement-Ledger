using System;
using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentsLedger.Blazor.Infrastructure.Identity;
using PaymentsLedger.Blazor.Infrastructure.Persistence;

namespace PaymentsLedger.Blazor.Infrastructure;

public static class DataAccessRegistration
{
    public static IHostApplicationBuilder AddInfra(this IHostApplicationBuilder builder)
    {
        // Register EF Core DbContext using Aspire EF integration, bound to the
        // database resource name from AppHost ("blazordb"). Aspire supplies the
        // connection string at runtime; design-time uses appsettings/user-secrets.
        builder.AddNpgsqlDbContext<ApplicationDbContext>(connectionName: "blazordb");

        // Auth + IdentityCore wiring; provider-agnostic for Presentation
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        builder.Services.AddIdentityCore<MerchantUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // Apply pending EF Core migrations on startup (before seeding)
        builder.Services.AddHostedService<ApplyMigrationsHostedService>();

        // Seed required roles on startup
        builder.Services.AddHostedService<IdentitySeedHostedService>();

        return builder;
    }
}
