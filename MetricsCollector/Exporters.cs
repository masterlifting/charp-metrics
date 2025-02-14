using System.Globalization;
using OpenTelemetry;

namespace MetricsCollector;

public record MetricValueDto(string Value, long TimeStamp);

public record MetricDto(string Name, string Unit, List<MetricValueDto> Values);

public record MetricDataItem(string Value, long TimeStamp)
{
    public MetricValueDto ToDto() => new(Value, TimeStamp);
}

public record MetricData(string Name, MetricType Type, string Unit, List<MetricDataItem> Values)
{
    public MetricDto ToDto() => new(Name, Unit, Values.Select(value => value.ToDto()).ToList());
}

public class CustomExporter : BaseExporter<Metric>
{
    private readonly Dictionary<string, MetricData> _store = new();

    public override ExportResult Export(in Batch<Metric> metrics)
    {
        foreach (var metric in metrics)
        {
            var items = new List<MetricDataItem>();

            foreach (var point in metric.GetMetricPoints())
            {
                items.Add(MapToMetricUnitValue(metric.MetricType, point));
            }

            _store[metric.Name] = new(metric.Name, metric.MetricType, metric.Unit, items);
        }

        return ExportResult.Success;
    }

    public MetricData? GetMetricData(string metricName) =>
        _store.GetValueOrDefault(metricName);

    public List<MetricData> GetAllData() => _store.Values.ToList();

    public List<string> GetMetricKeys() => _store.Keys.ToList();

    private static MetricDataItem MapToMetricUnitValue(MetricType type, MetricPoint point)
    {
        var value =
            type switch
            {
                MetricType.DoubleSum => point.GetSumDouble().ToString(CultureInfo.InvariantCulture),
                MetricType.LongSum => point.GetSumLong().ToString(),
                MetricType.LongSumNonMonotonic => point.GetSumLong().ToString(),
                MetricType.DoubleSumNonMonotonic => point.GetSumDouble().ToString(CultureInfo.InvariantCulture),
                _ => "Unhandled MetricType"
            };

        return new MetricDataItem(value, point.EndTime.ToUnixTimeMilliseconds());
    }
}
