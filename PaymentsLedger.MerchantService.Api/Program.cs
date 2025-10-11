using Microsoft.EntityFrameworkCore;
using PaymentsLedger.MerchantService.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Register EF Core DbContext via Aspire integration (connection from AppHost "merchantsdb")
builder.AddNpgsqlDbContext<MerchantDbContext>(connectionName: "merchantsdb");

// Register Azure Service Bus client via Aspire integration (connection from AppHost "messaging")
builder.AddAzureServiceBusClient("messaging");

var app = builder.Build();

app.MapGet("/", () => "Merchant Service up");

app.Run();
