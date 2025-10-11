var builder = WebApplication.CreateBuilder(args);

// Register Azure Service Bus client via Aspire integration (connection from AppHost "messaging")
builder.AddAzureServiceBusClient("messaging");

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
