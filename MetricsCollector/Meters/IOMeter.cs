using System.Diagnostics.CodeAnalysis;

namespace MetricsCollector.Meters;

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public sealed class IOMeter
{
    private const string DiskReads = "disk.total.reads";
    private const string DiskWrites = "disk.total.writes";
    private const string DiskReadSpeed = "disk.read.speed";
    private const string DiskWriteSpeed = "disk.write.speed";
    
    private const string Operations = "ops";
    private const string BytesPerSecond = "bytes/s";

    public IOMeter()
    {
        var meter = new Meter("IOMetrics", "1.0");

        switch (RuntimeInformation.OSDescription)
        {
            case var os when os.Contains("Windows", StringComparison.OrdinalIgnoreCase):
                PerformanceCounter? readCounter = null;
                PerformanceCounter? writeCounter = null;
                PerformanceCounter? readBytesCounter = null;
                PerformanceCounter? writeBytesCounter = null;
                
                try
                {
                    readCounter = new PerformanceCounter("PhysicalDisk", "Disk Reads/sec", "_Total", readOnly: true);
                    writeCounter = new PerformanceCounter("PhysicalDisk", "Disk Writes/sec", "_Total", readOnly: true);
                    readBytesCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total", readOnly: true);
                    writeBytesCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total", readOnly: true);

                    meter.CreateObservableCounter(
                        DiskReads,
                        () => new Measurement<float>(readCounter.NextValue()),
                        Operations);

                    meter.CreateObservableCounter(
                        DiskWrites,
                        () => new Measurement<float>(writeCounter.NextValue()),
                        Operations);

                    meter.CreateObservableGauge(
                        DiskReadSpeed,
                        () => new Measurement<float>(readBytesCounter.NextValue()),
                        BytesPerSecond);

                    meter.CreateObservableGauge(
                        DiskWriteSpeed,
                        () => new Measurement<float>(writeBytesCounter.NextValue()),
                        BytesPerSecond);
                }
                catch (UnauthorizedAccessException)
                {
                    readCounter?.Dispose();
                    writeCounter?.Dispose();
                    readBytesCounter?.Dispose();
                    writeBytesCounter?.Dispose();
                }
                break;
            case var os when os.Contains("Linux", StringComparison.OrdinalIgnoreCase):
                meter.CreateObservableCounter(DiskReads, GetLinuxDiskReads, Operations);
                meter.CreateObservableCounter(DiskWrites, GetLinuxDiskWrites, Operations);
                meter.CreateObservableGauge(DiskReadSpeed, GetLinuxDiskReadSpeed, BytesPerSecond);
                meter.CreateObservableGauge(DiskWriteSpeed, GetLinuxDiskWriteSpeed, BytesPerSecond);

                break;

                Measurement<long> GetLinuxDiskReads() => new(GetLinuxDiskStat(3));
                Measurement<long> GetLinuxDiskWrites() => new(GetLinuxDiskStat(7));
                Measurement<double> GetLinuxDiskReadSpeed() => new(GetLinuxDiskStat(5));
                Measurement<double> GetLinuxDiskWriteSpeed() => new(GetLinuxDiskStat(9));

                static long GetLinuxDiskStat(int columnIndex)
                {
                    const string DiskStatsPath = "/proc/diskstats";
                    
                    try
                    {
                        if (!File.Exists(DiskStatsPath))
                            return 0;

                        var lines = File.ReadLines(DiskStatsPath);
                        var diskLine = lines.FirstOrDefault(l => l.Contains("sda", StringComparison.Ordinal));
                        if (diskLine == null)
                            return 0;

                        var columns = diskLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        return columnIndex < columns.Length ? long.Parse(columns[columnIndex]) : 0;
                    }
                    catch
                    {
                        return 0;
                    }
                }
            default:
                throw new PlatformNotSupportedException("Unsupported OS.");
        }
    }
}
