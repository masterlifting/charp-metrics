namespace MetricsCollector;

public class Meter
{
    public const string Name = nameof(MetricsCollector);
    private readonly IMeterFactory _factory;


    public Meter(IMeterFactory factory)
    {
        _factory = factory;
    }
}
