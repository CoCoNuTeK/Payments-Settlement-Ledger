using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentsLedger.Blazor.Infrastructure.Identity;
using PaymentsLedger.Blazor.Infrastructure.Auth;
using PaymentsLedger.Blazor.Infrastructure.Persistence;
using PaymentsLedger.Blazor.Application.Auth;
using Aspire.Azure.Messaging.ServiceBus;
using PaymentsLedger.Blazor.Infrastructure.Messaging;
using PaymentsLedger.Blazor.Infrastructure.Observability;

namespace PaymentsLedger.Blazor.Infrastructure;

public static class DataAccessRegistration
{
    public static IHostApplicationBuilder AddInfra(this IHostApplicationBuilder builder)
    {
        // Observability (OpenTelemetry) - traces, metrics, logs for Blazor server
        builder.AddObservability();
        // Register EF Core DbContext using Aspire EF integration, bound to the
        // database resource name from AppHost ("blazordb"). Aspire supplies the
        // connection string at runtime; design-time uses appsettings/user-secrets.
        builder.AddNpgsqlDbContext<ApplicationDbContext>(
            connectionName: "blazordb",
            configureDbContextOptions: options =>
            {
                // Avoid throwing on PendingModelChangesWarning during runtime migrations
                options.ConfigureWarnings(w => w.Log(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });

        // Messaging: Service Bus client + in-proc bus + inbox subscriber
        builder.AddMessaging();

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

        return builder;
    }
}
