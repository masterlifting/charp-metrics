using MetricsCollector;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

var customExporter = new CustomExporter(5000, 5000);

builder.Services.AddSingleton(customExporter);

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
        metrics
            .AddProcessInstrumentation()
            .AddCustomInstrumentation()
            .AddPrometheusExporter()
            .AddCustomExporter(customExporter));

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseOpenTelemetryCustomScrapingEndpoints();

app.Run();
