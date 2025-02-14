using MetricsCollector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

var customExporter = new CustomExporter();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder => resourceBuilder.AddService(nameof(MetricsCollector)))
    .WithMetrics(metrics =>
    {
        metrics
            .AddProcessInstrumentation()
            .AddRuntimeInstrumentation()
            .AddReader(new PeriodicExportingMetricReader(customExporter, 5000))
            .AddRequiredMeters();
    });

var app = builder.Build();

app.MapGet("/metrics", async context =>
{
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(customExporter.GetAllData().Select(data => data.ToDto()));
});

app.MapGet("/metrics/names", async context =>
{
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(customExporter.GetMetricKeys());
});

app.MapGet("/metrics/{name}", async (string name, HttpContext context) =>
{
    var metricData = customExporter.GetMetricData(name);
    if (metricData is null)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync($"No data available for '{name}'.");
        return;
    }

    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(metricData.ToDto());
});

app.Run();
