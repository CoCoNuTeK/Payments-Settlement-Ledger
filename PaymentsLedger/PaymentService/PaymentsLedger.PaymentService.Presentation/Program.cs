using PaymentsLedger.PaymentService.Infrastructure;
using PaymentsLedger.PaymentService.Application;
using PaymentsLedger.PaymentService.Presentation.HostedServices;

var builder = WebApplication.CreateBuilder(args);

// Register infrastructure + application handlers
builder.AddInfra();
builder.Services.AddAppHandlers();

// Simulation hosted service
builder.Services.AddHostedService<PaymentSimulatorHostedService>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

await app.RunAsync();
