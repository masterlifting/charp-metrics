using System.Globalization;
using OpenTelemetry;

namespace MetricsCollector;

/// <summary>
/// Custom exporter to store metrics in memory and expose them through custom endpoints.
/// </summary>
/// <param name="interval">
/// Interval in milliseconds to export metrics.
/// </param>
/// <param name="timeout">
/// Timeout in milliseconds to export metrics.
/// </param>
public class CustomExporter(int interval, int timeout) : BaseExporter<Metric>
{
    /// Interval in milliseconds to export metrics.
    public int Interval { get; } = interval;

    /// Timeout in milliseconds to export metrics.
    public int Timeout { get; } = timeout;

    private readonly Dictionary<string, CustomMetric> _store = new();

    private static readonly HashSet<string> NecessaryMetrics =
    [
        // Memory metrics
        "process.memory.usage",
        "system.memory.usage",

        // CPU metrics
        "process.cpu.time",
        "process.cpu.count",
        "system.cpu.usage",

        // IO metrics
        "disk.total.reads",
        "disk.total.writes",
        "disk.read.speed",
        "disk.write.speed"
    ];

    public override ExportResult Export(in Batch<Metric> metrics)
    {
        foreach (var metric in metrics)
        {
            if (!NecessaryMetrics.Contains(metric.Name))
                continue;

            var values = new List<CustomMetricValue>(6); //6 is max capacity of most cases. We can improve this by using a pool or more complex logic

            foreach (var point in metric.GetMetricPoints())
            {
                var value = ToMetricValue(metric.MetricType, point);
                values.Add(value);
            }

            // If the metric already exists, update the values. Here we can extend the logic to keep track of the last N values
            _store[metric.Name] = new(metric.Name, metric.Description, metric.Unit, values);
        }

        return ExportResult.Success;
    }

    public List<string> GetAllNames() => _store.Keys.ToList();
    public List<CustomMetric> GetAll() => _store.Values.ToList();
    public CustomMetric? GetMetric(string name) => _store.GetValueOrDefault(name);

    private static CustomMetricValue ToMetricValue(MetricType type, MetricPoint point)
    {
        var value =
            type switch
            {
                MetricType.LongSum => point.GetSumLong().ToString(),
                MetricType.LongSumNonMonotonic => point.GetSumLong().ToString(),
                MetricType.DoubleSum => point.GetSumDouble().ToString(CultureInfo.InvariantCulture),
                MetricType.DoubleSumNonMonotonic => point.GetSumDouble().ToString(CultureInfo.InvariantCulture),
                MetricType.LongGauge => point.GetGaugeLastValueLong().ToString(),
                MetricType.DoubleGauge => point.GetGaugeLastValueDouble().ToString(CultureInfo.InvariantCulture),
                _ => $"Unhandled metric type: '{type}'"
            };

        return new CustomMetricValue(value, point.Tags, point.StartTime, point.EndTime);
    }
}
