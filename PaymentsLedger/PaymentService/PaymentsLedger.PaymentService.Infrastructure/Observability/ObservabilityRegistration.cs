using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace PaymentsLedger.PaymentService.Infrastructure.Observability;

public static class ObservabilityRegistration
{
    public static IHostApplicationBuilder AddObservability(this IHostApplicationBuilder builder)
    {
        const string serviceName = "PaymentsLedger.PaymentService";
        const string serviceNamespace = "PaymentsLedger";
        var serviceVersion = GetInformationalVersion() ?? "0.0.0";

        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(
            serviceName: serviceName,
            serviceNamespace: serviceNamespace,
            serviceVersion: serviceVersion,
            serviceInstanceId: Environment.MachineName);

        // OpenTelemetry SDK registration (Traces + Metrics)
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(
                serviceName: serviceName!,
                serviceNamespace: serviceNamespace,
                serviceVersion: serviceVersion,
                serviceInstanceId: Environment.MachineName))
            .WithTracing(tracer =>
            {
                tracer
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    // Capture custom Activities created with our ActivitySource
                    .AddSource("PaymentsLedger.PaymentService.Presentation")
                    .AddOtlpExporter();
            })
            .WithMetrics(meter =>
            {
                meter
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter();
            });

        // Logs via OpenTelemetry
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
            logging.SetResourceBuilder(resourceBuilder);
            logging.AddOtlpExporter();
        });

        return builder;
    }

    private static string? GetInformationalVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var attr = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return attr?.InformationalVersion;
    }
}
