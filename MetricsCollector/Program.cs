var builder = Host.CreateApplicationBuilder(args);

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

    metrics.AddConsoleExporter(); //change to AddPrometheusExporter
});

var app = builder.Build();
app.Run();