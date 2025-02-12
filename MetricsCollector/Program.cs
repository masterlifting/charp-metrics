using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var metricsStore = new List<MetricSnapshot>();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder => resourceBuilder.AddService(nameof(MetricsCollector)))
    .WithMetrics(metrics =>
    {
        metrics
            .AddProcessInstrumentation()
            .AddRuntimeInstrumentation()
            .AddInMemoryExporter(metricsStore, options =>
            {
                options.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
                options.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
            })
            .AddPrometheusExporter();
    });

var app = builder.Build();


app.MapGet("/metrics/memory", context =>
    WriteMetricResponse(context, "process.memory.usage", point => point.GetSumLong() / (1024.0 * 1024.0), "MB"));

app.MapPrometheusScrapingEndpoint();

app.Run();
return;

async Task WriteMetricResponse(HttpContext context, string metricName, Func<MetricPoint, double> valueSelector, string unit)
{
    var metric = metricsStore.FirstOrDefault(m => m.Name == metricName);
    context.Response.ContentType = "text/plain";

    if (metric?.MetricPoints.Any() == true)
    {
        var value = valueSelector(metric.MetricPoints[0]);
        await context.Response.WriteAsync($"{metricName.Replace("process.", "").Replace(".", " ")}: {Math.Round(value, 2)} {unit}");
    }
    else
    {
        await context.Response.WriteAsync($"No data available for {metricName}.");
    }

    metricsStore.Clear();
}
