using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry;

namespace PaymentsLedger.Blazor.Infrastructure.Observability;

public static class ObservabilityRegistration
{
    public static IHostApplicationBuilder AddObservability(this IHostApplicationBuilder builder)
    {
        const string serviceName = "PaymentsLedger.Blazor";
        const string serviceNamespace = "PaymentsLedger";
        var serviceVersion = GetInformationalVersion() ?? "0.0.0";

        var resource = ResourceBuilder.CreateDefault().AddService(
            serviceName: serviceName,
            serviceNamespace: serviceNamespace,
            serviceVersion: serviceVersion,
            serviceInstanceId: Environment.MachineName);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(
                serviceName: serviceName,
                serviceNamespace: serviceNamespace,
                serviceVersion: serviceVersion,
                serviceInstanceId: Environment.MachineName))
            .WithTracing(tracer =>
            {
                tracer
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource("PaymentsLedger.Blazor.Presentation")
                    .AddSource("PaymentsLedger.Blazor.Application")
                    .AddSource("PaymentsLedger.Blazor.Infrastructure")
                    .AddOtlpExporter();
            })
            .WithMetrics(meter =>
            {
                meter
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter("PaymentsLedger.Blazor.Infrastructure")
                    .AddOtlpExporter();
            });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
            logging.SetResourceBuilder(resource);
            logging.AddOtlpExporter();
        });

        return builder;
    }

    private static string? GetInformationalVersion()
        => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
}
