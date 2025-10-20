using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PaymentsLedger.Blazor.Infrastructure.Identity;

namespace PaymentsLedger.Blazor.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<MerchantUser, IdentityRole<Guid>, Guid>(options)
{
}
