using MetricsCollector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var metricsStore = new List<MetricSnapshot>();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CustomMetricsReader>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder => resourceBuilder.AddService(nameof(MetricsCollector)))
    .WithMetrics(metrics =>
    {
        metrics
            .AddProcessInstrumentation()
            .AddRuntimeInstrumentation()
            //.AddReader<CustomMetricsReader>()
            // .AddView("process.memory.usage", new MetricStreamConfiguration()
            // {
            //     Name = "process.memory.usage1",
            //     Description = "The amount of memory used by the process.",
            //     CardinalityLimit = 1,
            //     TagKeys = ["process"],
            // })
            .AddInMemoryExporter(metricsStore, options =>
            {
                options.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
                options.TemporalityPreference = MetricReaderTemporalityPreference.Cumulative;
            })
            .AddPrometheusExporter(options =>
            {
                options.DisableTotalNameSuffixForCounters = true;
            })
            .AddRequiredMeters();
    });

var app = builder.Build();

app.MapGet("/metrics/cpu/current", context =>
    WriteMetricResponse(context, "process.cpu.usage", point => point.GetSumLong(), "percentage"));

app.MapGet("/metrics/cpu/usage-over-time", context =>
    WriteMetricResponse(context, "process.cpu.usage.over.time", point => point.GetSumLong(), "percentage"));

app.MapGet("/metrics/memory/current", context =>
    WriteMetricResponse(context, "process.memory.usage", point => point.GetSumLong(), "bytes"));

app.MapGet("/metrics/memory/usage-over-time", context =>
    WriteMetricResponse(context, "process.memory.usage.over.time", point => point.GetSumLong(), "bytes"));

app.MapGet("/metrics/fileio/total-reads", context =>
    WriteMetricResponse(context, "process.fileio.total.reads", point => point.GetSumLong(), "count"));

app.MapGet("/metrics/fileio/total-writes", context =>
    WriteMetricResponse(context, "process.fileio.total.writes", point => point.GetSumLong(), "count"));

app.MapGet("/metrics/fileio/read-speed", context =>
    WriteMetricResponse(context, "process.fileio.read.speed", point => point.GetSumLong(), "bytes/second"));

app.MapGet("/metrics/fileio/write-speed", context =>
    WriteMetricResponse(context, "process.fileio.write.speed", point => point.GetSumLong(), "bytes/second"));

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
        await context.Response.WriteAsync($"{metricName}: {Math.Round(value, 2)} {unit}");
    }
    else
    {
        await context.Response.WriteAsync($"No data available for {metricName}.");
    }

    metricsStore.Clear();
}
