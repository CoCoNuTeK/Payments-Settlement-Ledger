using System;
using Microsoft.AspNetCore.Identity;
namespace PaymentsLedger.Blazor.Infrastructure.Identity;

public sealed class MerchantUser : IdentityUser<Guid>
{
}
