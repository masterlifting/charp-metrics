using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using OpenTelemetry;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder =>
    {
        resourceBuilder.AddService(nameof(MetricsCollector));
    })
    .WithMetrics(metrics =>
    {
        metrics.AddProcessInstrumentation()
               .AddRuntimeInstrumentation()
               .AddReader(new PeriodicExportingMetricReader(new InMemoryMetricsExporter()))
               .AddPrometheusExporter();
    });

var app = builder.Build();

// Custom endpoint that returns metrics as JSON with values.
app.MapGet("/custom-metrics", async context =>
{
    var model = InMemoryMetricsExporter.GetMetrics();
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(model));
});

app.MapPrometheusScrapingEndpoint();

app.Run();

public class InMemoryMetricsExporter : BaseExporter<Metric>
{
    // Thread-safe collection to store metrics.
    public static ConcurrentBag<Metric> Metrics { get; } = new();

    public override ExportResult Export(in Batch<Metric> batch)
    {
        foreach (var metric in batch)
        {
            Metrics.Add(metric);
        }
        return ExportResult.Success;
    }

    public static CustomMetric[] GetMetrics()
    {
        return Metrics.Select(metric =>
        {
            // Build the custom metric and use a list to collect metric values.
            var values = new List<MetricValue>();

            foreach (var item in metric.GetMetricPoints())
            {
                var tags = new Dictionary<string, string>(item.Tags.Count);
                foreach (var tag in item.Tags)
                {
                    if (tag.Value != null)
                    {
                        tags.Add(tag.Key, tag.Value.ToString());
                    }
                }

                // Try to get the gauge value as a double.
                double value;
                try
                {
                    value = item.GetGaugeLastValueDouble();
                }
                catch (System.NotSupportedException)
                {
                    // If the gauge value is not supported for this metric type, skip this point.
                    continue;
                }

                values.Add(new MetricValue
                {
                    Value = value,
                    Labels = tags,
                    Timestamp = item.EndTime
                });
            }

            return new CustomMetric
            {
                Name = metric.Name,
                Values = values.ToArray()
            };

        }).ToArray();
    }
}

public class CustomMetric
{
    public string Name { get; set; }
    public MetricValue[] Values { get; set; }
}

public class MetricValue
{
    public double Value { get; set; }
    public Dictionary<string, string> Labels { get; set; }
    public System.DateTimeOffset Timestamp { get; set; }
}