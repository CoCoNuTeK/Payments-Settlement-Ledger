using Microsoft.EntityFrameworkCore;
using PaymentsLedger.PaymentService.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Register EF Core DbContext via Aspire integration (connection from AppHost "paymentsdb")
builder.AddNpgsqlDbContext<PaymentDbContext>(connectionName: "paymentsdb");

// Register Azure Service Bus client via Aspire integration (connection from AppHost "messaging")
builder.AddAzureServiceBusClient("messaging");

var app = builder.Build();

app.MapGet("/", () => "Payment Service up");

app.Run();
