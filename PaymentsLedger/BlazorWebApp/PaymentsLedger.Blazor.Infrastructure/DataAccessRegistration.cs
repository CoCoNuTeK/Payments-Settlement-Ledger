using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentsLedger.Blazor.Infrastructure.Identity;
using PaymentsLedger.Blazor.Infrastructure.Auth;
using PaymentsLedger.Blazor.Infrastructure.Persistence;
using PaymentsLedger.Blazor.Application.Auth;

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
                options.SignIn.RequireConfirmedAccount = false; // Demo: no email confirmation required
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // App-facing auth abstraction mapping (Application -> Infrastructure)
        builder.Services.AddScoped<IAuthSignInService, AuthSignInService>();

        // Apply migrations + seed identity via hosted service
        builder.Services.AddHostedService<ApplyMigrationsHostedService>();

        return builder;
    }
}
