using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentsLedger.Blazor.Infrastructure.Identity;
using PaymentsLedger.Blazor.Infrastructure.Persistence;

namespace PaymentsLedger.Blazor.Infrastructure.Seeding;

internal static class ApplicationDbContextSeeder
{
    public static void Seed(ApplicationDbContext context, bool isDesignTime)
    {
        // Synchronous wrapper for tooling. Delegate to async.
        SeedAsync(context, isDesignTime, CancellationToken.None).GetAwaiter().GetResult();
    }

    public static async Task SeedAsync(ApplicationDbContext context, bool isDesignTime, CancellationToken cancellationToken)
    {
        // Provide a scope to resolve Identity services
        var rootProvider = context.GetInfrastructure<IServiceProvider>();
        using var scope = rootProvider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<MerchantUser>>();
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("AppDbSeeder");

        // Ensure roles
        foreach (var role in MerchantRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var createRole = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                if (!createRole.Succeeded)
                {
                    logger?.LogWarning("Failed to create role {Role}: {Errors}", role, string.Join(", ", createRole.Errors.Select(e => e.Code)));
                }
            }
        }

        // Seed users
        var users = new[]
        {
            new { Email = "standard@demo.local", UserName = "standard@demo.local", Password = "Passw0rd!", Role = MerchantRoles.Standard },
            new { Email = "premium@demo.local",  UserName = "premium@demo.local",  Password = "Passw0rd!", Role = MerchantRoles.Premium  }
        };

        foreach (var u in users)
        {
            var existing = await userManager.FindByEmailAsync(u.Email);
            if (existing is null)
            {
                var user = new MerchantUser
                {
                    UserName = u.UserName,
                    Email = u.Email
                };

                var create = await userManager.CreateAsync(user, u.Password);
                if (!create.Succeeded)
                {
                    logger?.LogWarning("User seed: failed to create '{Email}'. Errors: {Errors}", u.Email, string.Join(", ", create.Errors.Select(e => e.Code)));
                    continue;
                }

                var addRole = await userManager.AddToRoleAsync(user, u.Role);
                if (!addRole.Succeeded)
                {
                    logger?.LogWarning("User seed: failed to add role '{Role}' to '{Email}'. Errors: {Errors}", u.Role, u.Email, string.Join(", ", addRole.Errors.Select(e => e.Code)));
                }
            }
            else if (!await userManager.IsInRoleAsync(existing, u.Role))
            {
                var addRole = await userManager.AddToRoleAsync(existing, u.Role);
                if (!addRole.Succeeded)
                {
                    logger?.LogWarning("User seed: failed to add role '{Role}' to existing '{Email}'. Errors: {Errors}", u.Role, u.Email, string.Join(", ", addRole.Errors.Select(e => e.Code)));
                }
            }
        }
    }
}
