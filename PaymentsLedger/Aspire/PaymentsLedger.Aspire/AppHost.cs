using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

// ---------- Presentation (Blazor WASM) ----------
var blazorPresentation = builder.AddProject<Projects.PaymentsLedger_Blazor_Presentation>("blazor");
var paymentService = builder.AddProject<Projects.PaymentsLedger_PaymentService_Presentation>("payment-service");

// ---------- Datastore (PostgreSQL) ----------
var blazorPostgres = builder.AddPostgres("blazor-postgres")
    .WithParentRelationship(blazorPresentation);
var blazorDb = blazorPostgres.AddDatabase("blazordb");

// ---------- Messaging (Azure Service Bus) ----------
var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();

var paymentsTopic = serviceBus.AddServiceBusTopic("payments");
var blazorSubscription = paymentsTopic.AddServiceBusSubscription("blazor-sub");

// ---------- Wire dependencies ----------
blazorPresentation = blazorPresentation
    .WithReference(blazorDb)
    .WaitFor(blazorDb)
    // Subscriber: Blazor subscribes to the payments topic via subscription
    .WithReference(blazorSubscription)
    .WaitFor(blazorSubscription);

// Publisher: Payment Service publishes to the payments topic
paymentService = paymentService
    .WithReference(paymentsTopic)
    .WaitFor(paymentsTopic);

builder.Build().Run();
