var builder = DistributedApplication.CreateBuilder(args);

var k8sEnv = builder
    .AddKubernetesEnvironment("PaymentsLedger")
    .WithProperties(k8s =>
    {
        k8s.HelmChartName = "PaymentsLedger";
    });

// ---------- Presentation (Blazor WASM) ----------
var blazorPresentation = builder
    .AddProject<Projects.PaymentsLedger_Blazor_Presentation>("blazor");
var paymentService = builder.AddProject<Projects.PaymentsLedger_PaymentService_Presentation>("payment-service");

// ---------- Datastore (PostgreSQL) ----------
var blazorPostgres = builder.AddPostgres("blazor-postgres")
    .WithHostPort(5433)
    .WithParentRelationship(blazorPresentation);
var blazorDb = blazorPostgres.AddDatabase("blazordb");

// ---------- Messaging (Azure Service Bus) ----------
var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();

var paymentsTopic = serviceBus.AddServiceBusTopic("payments");
var blazorSubscription = paymentsTopic.AddServiceBusSubscription("blazor-sub");

// ---------- Payment Service Datastore (PostgreSQL) ----------
var paymentPostgres = builder.AddPostgres("payment-postgres")
    .WithHostPort(5434)
    .WithParentRelationship(paymentService);
var paymentsDb = paymentPostgres.AddDatabase("paymentsdb");

// ---------- Wire dependencies ----------
blazorPresentation = blazorPresentation
    .WithReference(blazorDb)
    .WaitFor(blazorDb)
    // Subscriber: Blazor subscribes to the payments topic via subscription
    .WithReference(serviceBus)
    .WaitFor(blazorSubscription);

// Publisher: Payment Service publishes to the payments topic
paymentService = paymentService
    // Reference the namespace so client connectionName: "messaging" is valid
    .WithReference(serviceBus)
    .WaitFor(paymentsTopic)
    .WithReference(paymentsDb)
    .WaitFor(paymentsDb);

builder.Build().Run();
