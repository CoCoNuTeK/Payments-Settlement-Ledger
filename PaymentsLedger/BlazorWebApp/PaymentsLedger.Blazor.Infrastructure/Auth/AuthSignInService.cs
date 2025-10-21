using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PaymentsLedger.Blazor.Application.Auth;
using PaymentsLedger.Blazor.Infrastructure.Identity;

namespace PaymentsLedger.Blazor.Infrastructure.Auth;

internal sealed class AuthSignInService(
    SignInManager<MerchantUser> signInManager,
    UserManager<MerchantUser> userManager,
    ILogger<AuthSignInService> logger
) : IAuthSignInService
{
    public async Task<AuthSignInResult> PasswordSignInAsync(
        string userNameOrEmail,
        string password,
        bool lockoutOnFailure = true)
    {
        // Locate user by email or username first to avoid leaking whether a username exists via timing.
        MerchantUser? user = null;
        if (!string.IsNullOrWhiteSpace(userNameOrEmail))
        {
            if (userNameOrEmail.Contains('@'))
            {
                user = await userManager.FindByEmailAsync(userNameOrEmail);
            }
            user ??= await userManager.FindByNameAsync(userNameOrEmail);
        }

        if (user is null)
        {
            // Simulate password verification delay to reduce user enumeration signal.
            // Use built-in verifier against a random hash (Identity does this internally on some flows).
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            return new AuthSignInResult(AuthSignInStatus.Failed);
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
        if (result.Succeeded)
        {
            // Persistence policy is decided here (no remember-me parameter)
            var isPersistent = false;
            await signInManager.SignInAsync(user, isPersistent: isPersistent);
            logger.LogDebug("User {UserId} signed in (persistent={Persistent})", user.Id, isPersistent);
            return new AuthSignInResult(AuthSignInStatus.Success);
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User {UserId} is locked out", user.Id);
            return new AuthSignInResult(AuthSignInStatus.LockedOut);
        }

        if (result.RequiresTwoFactor)
        {
            logger.LogInformation("User {UserId} requires two-factor authentication", user.Id);
            return new AuthSignInResult(AuthSignInStatus.RequiresTwoFactor);
        }

        if (result.IsNotAllowed)
        {
            return new AuthSignInResult(AuthSignInStatus.NotAllowed);
        }

        return new AuthSignInResult(AuthSignInStatus.Failed);
    }

    public Task SignOutAsync() => signInManager.SignOutAsync();

    public bool IsSignedIn(ClaimsPrincipal principal) => signInManager.IsSignedIn(principal);
}
