using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PaymentsLedger.MerchantService.Api.Data;
using PaymentsLedger.MerchantService.Api.Data.Identity;
using PaymentsLedger.MerchantService.Api.Services;

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

// Messaging: hosted messenger that receives merchant commands and can publish merchant events
builder.Services.AddSingleton<MerchantServiceBusMessenger>();
builder.Services.AddSingleton<IMerchantServiceBusMessenger>(sp => sp.GetRequiredService<MerchantServiceBusMessenger>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<MerchantServiceBusMessenger>());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MerchantDbContext>();
    await dbContext.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<MerchantRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<MerchantUser>>();

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

    // Dev convenience: seed a couple of merchant users that satisfy password policy
    if (app.Environment.IsDevelopment())
    {
        await EnsureUserAsync("standard@merchant.local", "DevPassword123A", MerchantRole.Standard);
        await EnsureUserAsync("premium@merchant.local", "DevPassword123A", MerchantRole.Premium);
    }

    async Task EnsureUserAsync(string email, string password, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new MerchantUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var create = await userManager.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                // If password policy blocks creation, do not throw — this is dev-only seed
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}

// Subscribe to merchant commands and handle login attempts
var messenger = app.Services.GetRequiredService<IMerchantServiceBusMessenger>();
var lifetime = app.Lifetime;

var subscription = messenger.SubscribeToMerchantCommands(async (msg, ct) =>
{
    // Only handle login messages for now
    if (!string.Equals(msg.Subject, "auth.login", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var correlationId = msg.CorrelationId;

    try
    {
        var dto = JsonSerializer.Deserialize<LoginAttemptDto>(msg.Body);
        if (dto is null || string.IsNullOrWhiteSpace(dto.email) || string.IsNullOrWhiteSpace(dto.password))
        {
            await PublishAsync("auth.login.result", correlationId, new LoginResultDto("InvalidRequest", null, null, Array.Empty<string>()));
            return;
        }

        await using var scopeInner = app.Services.CreateAsyncScope();
        var userManager = scopeInner.ServiceProvider.GetRequiredService<UserManager<MerchantUser>>();
        var signInManager = scopeInner.ServiceProvider.GetRequiredService<SignInManager<MerchantUser>>();

        var user = await userManager.FindByEmailAsync(dto.email);
        if (user is null)
        {
            await PublishAsync("auth.login.result", correlationId, new LoginResultDto("UserNotFound", null, null, Array.Empty<string>()));
            return;
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, dto.password, lockoutOnFailure: true);
        if (signInResult.Succeeded)
        {
            var roles = await userManager.GetRolesAsync(user);
            await PublishAsync("auth.login.result", correlationId, new LoginResultDto("Succeeded", user.Id, user.Email, roles.ToArray()));
            return;
        }
        if (signInResult.IsLockedOut)
        {
            await PublishAsync("auth.login.result", correlationId, new LoginResultDto("LockedOut", null, null, Array.Empty<string>()));
            return;
        }

        // NotAllowed/RequiresTwoFactor or invalid password
        await PublishAsync("auth.login.result", correlationId, new LoginResultDto("InvalidCredentials", null, null, Array.Empty<string>()));
    }
    catch (Exception)
    {
        await PublishAsync("auth.login.result", correlationId, new LoginResultDto("ServerError", null, null, Array.Empty<string>()));
    }

    async Task PublishAsync(string subject, string? corrId, LoginResultDto payload)
    {
        var body = BinaryData.FromString(JsonSerializer.Serialize(payload));
        var reply = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            Subject = subject,
            CorrelationId = corrId
        };

        await ((IMerchantServiceBusMessenger)messenger).PublishMerchantEventAsync(reply, ct);
    }
});

// Dispose subscription on shutdown
lifetime.ApplicationStopping.Register(() => subscription.Dispose());

app.MapGet("/", () => "Merchant Service up");

await app.RunAsync();

// Payload DTOs
file sealed record LoginAttemptDto(string type, string email, string password, string correlationId);
file sealed record LoginResultDto(string status, Guid? userId, string? email, string[] roles);
