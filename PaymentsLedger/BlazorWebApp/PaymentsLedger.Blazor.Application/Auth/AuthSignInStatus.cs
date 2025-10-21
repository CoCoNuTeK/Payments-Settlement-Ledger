namespace PaymentsLedger.Blazor.Application.Auth;

public enum AuthSignInStatus
{
    Success,
    LockedOut,
    RequiresTwoFactor,
    NotAllowed,
    Failed
}

