using MetricsCollector.Meters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MetricsCollector;

public static class Extensions
{
    public static MeterProviderBuilder AddIOInstrumentation(this MeterProviderBuilder builder)
    {
        var collector = new IOMeter();
        return builder.AddMeter("IOMetrics");
    }
    
    public static MeterProviderBuilder AddCustomExporter(this MeterProviderBuilder builder, CustomMetricsExporter exporter) =>
        builder
            .AddReader(new PeriodicExportingMetricReader(exporter, exporter.Interval, exporter.Timeout));

    public static WebApplication UseOpenTelemetryCustomScrapingEndpoints(this WebApplication app)
    {
        app.MapGet("/custom/metrics", async (CustomMetricsExporter exporter, HttpContext context) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(exporter.GetAll().Select(data => data.ToDto()));
        });

        app.MapGet("/custom/metrics/names", async (CustomMetricsExporter exporter, HttpContext context) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(exporter.GetAllNames());
        });

        app.MapGet("/custom/metrics/{name}", async (string name, CustomMetricsExporter exporter, HttpContext context) =>
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
