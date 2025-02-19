using MetricsCollector.Meters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MetricsCollector;

public static class Extensions
{
    public static MeterProviderBuilder AddCustomInstrumentation(this MeterProviderBuilder builder)
    {
        _ = new DiskMeter();
        _ = new SystemMeter();

        return builder
            .AddMeter(DiskMeter.Name)
            .AddMeter(SystemMeter.Name);
    }

    public static MeterProviderBuilder AddCustomExporter(this MeterProviderBuilder builder, CustomExporter exporter) =>
        builder
            .AddReader(new PeriodicExportingMetricReader(exporter, exporter.Interval, exporter.Timeout));

    public static WebApplication UseOpenTelemetryCustomScrapingEndpoints(this WebApplication app)
    {
        app.MapGet("/custom/metrics", async (CustomExporter exporter, HttpContext context) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(exporter.GetAll().Select(data => data.ToDto()));
        });

        app.MapGet("/custom/metrics/names", async (CustomExporter exporter, HttpContext context) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(exporter.GetAllNames());
        });

        app.MapGet("/custom/metrics/{name}", async (string name, CustomExporter exporter, HttpContext context) =>
        {
            var metricData = exporter.GetMetric(name);
            if (metricData is null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"No data available for '{name}'.");
                return;
            }

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(metricData.ToDto());
        });

        return app;
    }
}
