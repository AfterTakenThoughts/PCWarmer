using PCWarmer.Core.Stress;

namespace PCWarmer.Tests.Stress;

/// <summary>Records what a test subject asks of the CPU load, without starting threads.</summary>
internal sealed class FakeCpuLoad : ICpuLoad
{
    public List<double> StartedWith { get; } = new();
    public int StopCount { get; private set; }
    public bool IsRunning { get; private set; }
    public int ThreadCount => 4;

    public void Start(double dutyCycle)
    {
        StartedWith.Add(dutyCycle);
        IsRunning = true;
    }

    public void Stop()
    {
        StopCount++;
        IsRunning = false;
    }
}
