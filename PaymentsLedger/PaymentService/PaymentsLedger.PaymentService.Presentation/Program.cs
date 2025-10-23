using PaymentsLedger.PaymentService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register infrastructure services
builder.AddInfra();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

await app.RunAsync();
