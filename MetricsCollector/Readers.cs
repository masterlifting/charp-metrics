using OpenTelemetry;

namespace MetricsCollector;

public class CustomMetricsReader : PeriodicExportingMetricReader
{
    public CustomMetricsReader(BaseExporter<Metric> exporter, int exportIntervalMilliseconds = 60000, int exportTimeoutMilliseconds = 30000) : base(exporter, exportIntervalMilliseconds, exportTimeoutMilliseconds)
    {
    }
}
