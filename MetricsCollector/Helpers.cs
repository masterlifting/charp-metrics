using System.Runtime.InteropServices;

namespace MetricsCollector;

public static class Helpers
{
    public static OperationSystem GetOperationSystem() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OperationSystem.Windows :
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OperationSystem.Linux :
        throw new PlatformNotSupportedException();
}
