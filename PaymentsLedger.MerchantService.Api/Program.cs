using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PaymentsLedger.MerchantService.Api.Data;
using PaymentsLedger.MerchantService.Api.Data.Identity;

var builder = WebApplication.CreateBuilder(args);

// Register EF Core DbContext via Aspire integration (connection from AppHost "merchantsdb")
builder.AddNpgsqlDbContext<MerchantDbContext>(connectionName: "merchantsdb");

// Register Azure Service Bus client via Aspire integration (connection from AppHost "messaging")
builder.AddAzureServiceBusClient("messaging");

builder.Services.AddIdentityCore<MerchantUser>(options =>
{
    // User
    options.User.RequireUniqueEmail = true;

    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Lockout policy
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
})
.AddRoles<MerchantRole>()
.AddEntityFrameworkStores<MerchantDbContext>()
.AddSignInManager();

// Required by SignInManager and Identity validators
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MerchantDbContext>();
    await dbContext.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<MerchantRole>>();

    async Task EnsureRoleAsync(string name)
    {
        if (!await roleManager.RoleExistsAsync(name))
        {
            var role = new MerchantRole
            {
                Name = name,
                NormalizedName = name.ToUpperInvariant()
            };

            await roleManager.CreateAsync(role);
        }
    }

    await EnsureRoleAsync(MerchantRole.Standard);
    await EnsureRoleAsync(MerchantRole.Premium);
}

app.MapGet("/", () => "Merchant Service up");

await app.RunAsync();
