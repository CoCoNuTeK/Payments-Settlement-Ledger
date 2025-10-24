using PaymentsLedger.PaymentService.Infrastructure;
using PaymentsLedger.PaymentService.Infrastructure.Persistence;
using PaymentsLedger.PaymentService.Presentation.HostedServices;

var builder = WebApplication.CreateBuilder(args);

// Register infrastructure (includes messaging + handlers)
builder.AddInfra();

// Simulation hosted service
builder.Services.AddHostedService<PaymentSimulatorHostedService>();

var app = builder.Build();

// Apply EF Core migrations after building, before running
await app.Services.ApplyPaymentDbMigrationsAsync();

app.MapGet("/", () => "Hello World!");

await app.RunAsync();
