using System.Diagnostics.CodeAnalysis;

namespace MetricsCollector.Meters;

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public sealed class DiskMeter
{
    public const string Name = nameof(MetricsCollector) + "." + nameof(DiskMeter);
    
    private const string DiskReads = "disk.total.reads";
    private const string DiskWrites = "disk.total.writes";
    private const string DiskReadSpeed = "disk.read.speed";
    private const string DiskWriteSpeed = "disk.write.speed";

    private const string OperationsUnit = "ops";
    private const string BytesPerSecondUnit = "bytes/s";

    public DiskMeter()
    {
        var meter = new Meter(Name, "1.0");

        switch (Helpers.GetOperationSystem())
        {
            case OperationSystem.Windows:
                const string CategoryName = "PhysicalDisk";
                const string InstanceName = "_Total";

                var readCounter = new PerformanceCounter(CategoryName, "Disk Reads/sec", InstanceName, readOnly: true);
                var writeCounter = new PerformanceCounter(CategoryName, "Disk Writes/sec", InstanceName, readOnly: true);
                var readBytesCounter = new PerformanceCounter(CategoryName, "Disk Read Bytes/sec", InstanceName, readOnly: true);
                var writeBytesCounter = new PerformanceCounter(CategoryName, "Disk Write Bytes/sec", InstanceName, readOnly: true);

                try
                {
                    meter.CreateObservableCounter(DiskReads, () => GetValue(readCounter), OperationsUnit);
                    meter.CreateObservableCounter(DiskWrites, () => GetValue(writeCounter), OperationsUnit);
                    meter.CreateObservableGauge(DiskReadSpeed, () => GetValue(readBytesCounter), BytesPerSecondUnit);
                    meter.CreateObservableGauge(DiskWriteSpeed, () => GetValue(writeBytesCounter), BytesPerSecondUnit);
                }
                catch (UnauthorizedAccessException)
                {
                    readCounter.Dispose();
                    writeCounter.Dispose();
                    readBytesCounter.Dispose();
                    writeBytesCounter.Dispose();
                }

                break;

                Measurement<float> GetValue(PerformanceCounter counter) => new(counter.NextValue());
            case OperationSystem.Linux:
                meter.CreateObservableCounter(DiskReads, GetDiskReads, OperationsUnit);
                meter.CreateObservableCounter(DiskWrites, GetDiskWrites, OperationsUnit);
                meter.CreateObservableGauge(DiskReadSpeed, GetDiskReadSpeed, BytesPerSecondUnit);
                meter.CreateObservableGauge(DiskWriteSpeed, GetDiskWriteSpeed, BytesPerSecondUnit);

                break;

                Measurement<long> GetDiskReads() => new(GetDiskStat(3));
                Measurement<long> GetDiskWrites() => new(GetDiskStat(7));
                Measurement<double> GetDiskReadSpeed() => new(GetDiskStat(5));
                Measurement<double> GetDiskWriteSpeed() => new(GetDiskStat(9));

                static long GetDiskStat(int columnIndex)
                {
                    const string StatsPath = "/proc/diskstats";

                    try
                    {
                        if (!File.Exists(StatsPath))
                            return 0;

                        var stat = File.ReadLines(StatsPath).FirstOrDefault(l => l.Contains("sda", StringComparison.Ordinal));
                        if (stat is null)
                            return 0;

                        var columns = stat.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        return columnIndex < columns.Length ? long.Parse(columns[columnIndex]) : 0;
                    }
                    catch
                    {
                        return 0;
                    }
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
