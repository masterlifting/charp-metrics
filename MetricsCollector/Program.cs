using MetricsCollector;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

var customMetricsExporter = new CustomMetricsExporter();

builder.Services.AddSingleton(customMetricsExporter);

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
        metrics
            .AddProcessInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter()
            .AddCustomExporter(customMetricsExporter));

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseOpenTelemetryCustomScrapingEndpoints();

app.Run();
