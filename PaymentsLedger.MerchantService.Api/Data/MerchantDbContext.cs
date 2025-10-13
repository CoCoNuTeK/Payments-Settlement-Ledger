using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PaymentsLedger.MerchantService.Api.Data.Identity;

namespace PaymentsLedger.MerchantService.Api.Data;

public class MerchantDbContext(DbContextOptions<MerchantDbContext> options)
    : IdentityDbContext<MerchantUser, MerchantRole, Guid>(options)
{ }
