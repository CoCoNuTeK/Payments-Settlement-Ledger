using Microsoft.EntityFrameworkCore;
using PaymentsLedger.Blazor.Presentation.Components;
using PaymentsLedger.Blazor.Infrastructure;
using PaymentsLedger.Blazor.Infrastructure.Persistence;
var builder = WebApplication.CreateBuilder(args);

// Infrastructure wiring (Aspire-backed Npgsql, DbContext, Identity)
builder.AddInfra();

// Blazor server components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Apply EF Core migrations and seed Identity after build, before run
await app.Services.ApplyBlazorDbMigrationsAndSeedAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Static + Blazor endpoints
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
