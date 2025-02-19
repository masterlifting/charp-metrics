namespace MetricsCollector.Meters;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public sealed class SystemMeter
{
    public const string Name = nameof(MetricsCollector) + "." + nameof(SystemMeter);

    private const string CpuUsage = "system.cpu.usage";
    private const string MemoryUsage = "system.memory.usage";
    private const string CpuUnit = "%";
    private const string MemoryUnit = "bytes";

    public SystemMeter()
    {
        var meter = new Meter(Name, "1.0");

        switch (Helpers.GetOperationSystem())
        {
            case OperationSystem.Windows:
                //var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                var memoryCounter = new PerformanceCounter("Memory", "Available Bytes");
                
                try
                {
                    //meter.CreateObservableGauge(CpuUsage, () => new Measurement<double>(GetWindowsCpuUsage()), CpuUnit);
                    meter.CreateObservableCounter(MemoryUsage, () => GetValue(memoryCounter), MemoryUnit);
                }
                catch (UnauthorizedAccessException)
                {
                    //cpuCounter.Dispose();
                    memoryCounter.Dispose();
                }

                break;
                
                Measurement<float> GetValue(PerformanceCounter counter) => new(counter.NextValue());

            case OperationSystem.Linux:
                //meter.CreateObservableGauge(CpuUsage, () => new Measurement<double>(GetLinuxCpuUsage()), CpuUnit);
                meter.CreateObservableGauge(MemoryUsage, () => new Measurement<long>(GetLinuxMemoryUsage()), MemoryUnit);
                break;
                
                static double GetLinuxCpuUsage()
                {
                    var cpuStats = File.ReadAllLines("/proc/stat")[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return cpuStats.Skip(1).Sum(long.Parse);
                }

                static long GetLinuxMemoryUsage()
                {
                    var memInfo = File.ReadAllLines("/proc/meminfo");
                    long totalMem = long.Parse(memInfo[0].Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]) * 1024;
                    long availableMem = long.Parse(memInfo[2].Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]) * 1024;
                    return totalMem - availableMem;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
