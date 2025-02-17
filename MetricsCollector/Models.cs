using OpenTelemetry;

namespace MetricsCollector;

public record MetricValueDto(string Value, string Tag, long Start, long Finish);
public record MetricDto(string Name, string Description, string Unit, List<MetricValueDto> Values);

public record CustomMetricValue(string Value, ReadOnlyTagCollection Tags, DateTimeOffset StartTime, DateTimeOffset EndTime)
{
        public MetricValueDto ToDto()
        {
            var tag = String.Empty;

            if (Tags.Count > 0)
            {
                var isOne = true;
                
                // In most cases, the tags will be a single key-value pair
                foreach (var (key, value) in Tags)
                {
                    if (!isOne) 
                        tag += ", ";
                    
                    tag += $"{key}: {value}";
                    
                    isOne = false;
                }
            }

            return new MetricValueDto(Value, tag, StartTime.ToUnixTimeMilliseconds(), EndTime.ToUnixTimeMilliseconds());
        }
    }
public record CustomMetric(string Name, string Description, string Unit, List<CustomMetricValue> Values)
{
    public MetricDto ToDto() => new(Name, Description, Unit, Values.Select(value => value.ToDto()).ToList());
}
