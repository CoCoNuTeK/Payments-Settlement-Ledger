

var builder = DistributedApplication.CreateBuilder(args);

// ---------- Services ----------
var merchantService = builder.AddProject<Projects.PaymentsLedger_MerchantService_Api>("merchantservice");
var paymentService = builder.AddProject<Projects.PaymentsLedger_PaymentService_Api>("paymentservice");
var worldSimulatorService = builder.AddProject<Projects.PaymentsLedger_WorldSimulator_Api>("worldsimulator");
var blazorService = builder.AddProject<Projects.PaymentsLedger_Blazor>("blazor");

// ---------- Datastores ----------
var merchantPostgres = builder.AddPostgres("merchant-postgres")
    .WithParentRelationship(merchantService);
var merchantsDb = merchantPostgres.AddDatabase("merchantsdb");

var paymentPostgres = builder.AddPostgres("payment-postgres")
    .WithParentRelationship(paymentService);
var paymentsDb = paymentPostgres.AddDatabase("paymentsdb");

// ---------- Messaging ----------
var messaging = builder.AddAzureServiceBus("messaging").RunAsEmulator();

var merchantCmdQueue = messaging.AddServiceBusQueue("merchant-commands")
    .WithProperties(p => p.RequiresDuplicateDetection = true)
    .WithParentRelationship(merchantService);

var paymentCmdQueue = messaging.AddServiceBusQueue("payment-commands")
    .WithProperties(p => p.RequiresDuplicateDetection = true)
    .WithParentRelationship(paymentService);

var blazorRepliesQueue = messaging.AddServiceBusQueue("blazor-replies")
    .WithParentRelationship(blazorService);

var merchantEventsTopic = messaging.AddServiceBusTopic("merchant-events")
    .WithParentRelationship(merchantService);

var paymentEventsTopic = messaging.AddServiceBusTopic("payment-events")
    .WithParentRelationship(paymentService);

var simulatorEventsTopic = messaging.AddServiceBusTopic("simulator-events")
    .WithParentRelationship(worldSimulatorService);

var merchantOnPaymentSub = paymentEventsTopic.AddServiceBusSubscription("merchant-on-payment")
    .WithParentRelationship(merchantService);

var paymentOnMerchantSub = merchantEventsTopic.AddServiceBusSubscription("payment-on-merchant")
    .WithParentRelationship(paymentService);

var merchantOnSimulatorSub = simulatorEventsTopic.AddServiceBusSubscription("merchant-on-simulator")
    .WithParentRelationship(merchantService);

var paymentOnSimulatorSub = simulatorEventsTopic.AddServiceBusSubscription("payment-on-simulator")
    .WithParentRelationship(paymentService);

// ---------- Wire dependencies ----------
merchantService = merchantService
    .WithReference(merchantsDb)
    .WaitFor(merchantsDb)
    .WaitFor(merchantCmdQueue)
    .WithReference(merchantCmdQueue)
    .WithReference(paymentCmdQueue)
    .WithReference(merchantEventsTopic)
    .WithReference(merchantOnPaymentSub)
    .WithReference(merchantOnSimulatorSub);

paymentService = paymentService
    .WithReference(paymentsDb)
    .WaitFor(paymentsDb)
    .WaitFor(paymentCmdQueue)
    .WithReference(paymentCmdQueue)
    .WithReference(merchantCmdQueue)
    .WithReference(paymentEventsTopic)
    .WithReference(paymentOnMerchantSub)
    .WithReference(paymentOnSimulatorSub);

worldSimulatorService = worldSimulatorService
    .WaitFor(merchantCmdQueue)
    .WaitFor(paymentCmdQueue)
    .WaitFor(simulatorEventsTopic)
    .WithReference(merchantCmdQueue)
    .WithReference(paymentCmdQueue)
    .WithReference(simulatorEventsTopic);

blazorService = blazorService
    .WaitFor(merchantService)
    .WaitFor(paymentService)
    .WaitFor(worldSimulatorService)
    .WaitFor(blazorRepliesQueue)
    .WithReference(merchantCmdQueue)
    .WithReference(paymentCmdQueue)
    .WithReference(blazorRepliesQueue)
    .WithReference(merchantEventsTopic)
    .WithReference(paymentEventsTopic)
    .WithReference(simulatorEventsTopic);

builder.Build().Run();
