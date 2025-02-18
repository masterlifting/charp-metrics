using System.Globalization;
using MetricsCollector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

var customMetricsExporter = new CustomMetricsExporter(5000, 5000);

builder.Services.AddSingleton(customMetricsExporter);

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
        metrics
            .AddProcessInstrumentation()
            .AddRuntimeInstrumentation()
            .AddIOInstrumentation()
            .AddPrometheusExporter()
            .AddCustomExporter(customMetricsExporter));

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseOpenTelemetryCustomScrapingEndpoints();

app.MapGet("/custom/metrics/startio", async context =>
{
    File.WriteAllText("startio.txt", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
    await context.Response.WriteAsync("startio.txt created.");
});

app.Run();
