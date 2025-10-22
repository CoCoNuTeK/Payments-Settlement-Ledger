using Microsoft.EntityFrameworkCore;
using PaymentsLedger.PaymentService.Infrastructure;
using PaymentsLedger.PaymentService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Register infrastructure services
builder.AddInfra();
var app = builder.Build();

// Ensure database is migrated; seeding is handled via UseSeeding/UseAsyncSeeding
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await db.Database.MigrateAsync();
}

app.MapGet("/", () => "Hello World!");

await app.RunAsync();
