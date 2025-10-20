using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PaymentsLedger.Blazor.Infrastructure.Identity;

internal sealed class UserSeedHostedService(IServiceProvider services) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<MerchantUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserSeedHostedService>>();

        // Default development users. Override via configuration later if needed.
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

                var createResult = await userManager.CreateAsync(user, u.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    logger.LogWarning("User seed: failed to create '{Email}'. Errors: {Errors}", u.Email, errors);
                    // If creation fails (e.g., password policy), skip role assignment.
                    continue;
                }

                var roleResult = await userManager.AddToRoleAsync(user, u.Role);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    logger.LogWarning("User seed: failed to add role '{Role}' to '{Email}'. Errors: {Errors}", u.Role, u.Email, errors);
                }
            }
            else
            {
                // Ensure role assignment is present
                if (!await userManager.IsInRoleAsync(existing, u.Role))
                {
                    var roleResult = await userManager.AddToRoleAsync(existing, u.Role);
                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join(", ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                        logger.LogWarning("User seed: failed to add role '{Role}' to existing user '{Email}'. Errors: {Errors}", u.Role, u.Email, errors);
                    }
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
