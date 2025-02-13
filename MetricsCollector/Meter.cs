namespace MetricsCollector;

public static class Extensions
{
    public static MeterProviderBuilder AddRequiredMeters(this MeterProviderBuilder builder) =>
        builder
            .AddMeter("process.memory.usage");

}
