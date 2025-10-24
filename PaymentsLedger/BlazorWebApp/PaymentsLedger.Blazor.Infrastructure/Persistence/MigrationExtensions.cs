using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentsLedger.Blazor.Infrastructure.Identity;

namespace PaymentsLedger.Blazor.Infrastructure.Persistence;

public static class MigrationExtensions
{
    public static async Task ApplyBlazorDbMigrationsAndSeedAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            var dbContext = provider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();

            // Seed Identity roles and users
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = provider.GetRequiredService<UserManager<MerchantUser>>();

            foreach (var role in MerchantRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var createRole = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                    if (!createRole.Succeeded)
                    {
                        logger.LogWarning("Failed to create role {Role}: {Errors}", role, string.Join(", ", createRole.Errors.Select(e => e.Code)));
                    }
                }
            }

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
                    var user = new MerchantUser { UserName = u.UserName, Email = u.Email };
                    var create = await userManager.CreateAsync(user, u.Password);
                    if (!create.Succeeded)
                    {
                        logger.LogWarning("User seed: failed to create '{Email}'. Errors: {Errors}", u.Email, string.Join(", ", create.Errors.Select(e => e.Code)));
                        continue;
                    }

                    var addRole = await userManager.AddToRoleAsync(user, u.Role);
                    if (!addRole.Succeeded)
                    {
                        logger.LogWarning("User seed: failed to add role '{Role}' to '{Email}'. Errors: {Errors}", u.Role, u.Email, string.Join(", ", addRole.Errors.Select(e => e.Code)));
                    }
                }
                else if (!await userManager.IsInRoleAsync(existing, u.Role))
                {
                    var addRole = await userManager.AddToRoleAsync(existing, u.Role);
                    if (!addRole.Succeeded)
                    {
                        logger.LogWarning("User seed: failed to add role '{Role}' to existing '{Email}'. Errors: {Errors}", u.Role, u.Email, string.Join(", ", addRole.Errors.Select(e => e.Code)));
                    }
                }
            }

            logger.LogInformation("Blazor database migrations and identity seed completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying Blazor database migrations and seeding identity before app run.");
            throw;
        }
    }
}

