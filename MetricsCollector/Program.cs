using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMetrics();
builder.Services.AddOpenTelemetry()
.ConfigureResource(resourceBuilder =>
{
    resourceBuilder
        .AddService(nameof(MetricsCollector));
})
.WithMetrics(metrics =>
{
    metrics
    .AddProcessInstrumentation()
    .AddRuntimeInstrumentation();

    metrics.AddPrometheusExporter();
});

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

app.Run();