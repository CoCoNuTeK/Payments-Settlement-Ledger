var builder = DistributedApplication.CreateBuilder(args);

// ---------- Presentation (Blazor WASM) ----------
var blazorPresentation = builder.AddProject<Projects.PaymentsLedger_Blazor_Presentation>("blazor");

// ---------- Datastore (PostgreSQL) ----------
var blazorPostgres = builder.AddPostgres("blazor-postgres")
    .WithParentRelationship(blazorPresentation);
var blazorDb = blazorPostgres.AddDatabase("blazordb");

// ---------- Wire dependencies ----------
blazorPresentation = blazorPresentation
    .WithReference(blazorDb)
    .WaitFor(blazorDb);

builder.Build().Run();
