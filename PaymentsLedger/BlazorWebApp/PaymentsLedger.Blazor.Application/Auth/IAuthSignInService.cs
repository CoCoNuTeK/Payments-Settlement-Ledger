using System.Security.Claims;

namespace PaymentsLedger.Blazor.Application.Auth;

/// <summary>
/// Authentication abstraction consumed by higher layers.
/// Implemented in Infrastructure via ASP.NET Core Identity.
/// </summary>
public interface IAuthSignInService
{
    /// <summary>
    /// Attempts to sign in a user by username or email with the provided password.
    /// The implementation decides cookie persistence policy (no remember-me parameter).
    /// </summary>
    Task<AuthSignInResult> PasswordSignInAsync(string userNameOrEmail, string password, bool lockoutOnFailure = true);

    /// <summary>
    /// Signs the current user out (clears application auth cookies).
    /// </summary>
    Task SignOutAsync();

    /// <summary>
    /// Returns true if the provided principal is authenticated.
    /// </summary>
    bool IsSignedIn(ClaimsPrincipal principal);
}
