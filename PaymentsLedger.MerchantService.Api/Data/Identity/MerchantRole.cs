using Microsoft.AspNetCore.Identity;

namespace PaymentsLedger.MerchantService.Api.Data.Identity;

public class MerchantRole : IdentityRole<Guid>
{
    public const string Standard = "merchant_basic";
    public const string Premium = "merchant_premium";
}
