using System.Globalization;
using OpenTelemetry;

namespace MetricsCollector;

public class CustomMetricsExporter : BaseExporter<Metric>
{
    private readonly Dictionary<string, CustomMetric> _store = new();

    private static readonly HashSet<string> NecessaryMetrics =
    [
        // Process Memory metrics
        "process.memory.usage",
        "process.memory.virtual",
        "dotnet.process.memory.working_set",
        "dotnet.gc.heap.total_allocated",
        "dotnet.gc.last_collection.memory.committed_size",
        "dotnet.gc.last_collection.heap.size",
    
        // Process CPU metrics
        "process.cpu.time",
        "process.cpu.count",
        "dotnet.process.cpu.count",
        "dotnet.process.cpu.time",
        
        // Process IO metrics
        "process.io.read.bytes",
        "process.io.write.bytes",
        "process.io.read.operations",
        "process.io.write.operations",
    ];

    public override ExportResult Export(in Batch<Metric> metrics)
    {
        foreach (var metric in metrics)
        {
            if (!NecessaryMetrics.Contains(metric.Name))
            {
                continue;
            }

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
    public CustomMetric? GetMetric(string metricName) => _store.GetValueOrDefault(metricName);

    private static CustomMetricValue ToMetricValue(MetricType type, MetricPoint point)
    {
        var value =
            type switch
            {
                MetricType.LongSum => point.GetSumLong().ToString(),
                MetricType.LongSumNonMonotonic => point.GetSumLong().ToString(),
                MetricType.DoubleSum => point.GetSumDouble().ToString(CultureInfo.InvariantCulture),
                MetricType.DoubleSumNonMonotonic => point.GetSumDouble().ToString(CultureInfo.InvariantCulture),
                _ => $"Unhandled metric type: {type}"
            };

        return new CustomMetricValue(value, point.Tags, point.StartTime, point.EndTime);
    }
}
