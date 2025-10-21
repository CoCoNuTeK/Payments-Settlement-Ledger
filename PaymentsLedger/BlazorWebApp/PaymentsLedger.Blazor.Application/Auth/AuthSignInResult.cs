namespace PaymentsLedger.Blazor.Application.Auth;

public sealed record AuthSignInResult(AuthSignInStatus Status)
{
    public bool Succeeded => Status == AuthSignInStatus.Success;
}

