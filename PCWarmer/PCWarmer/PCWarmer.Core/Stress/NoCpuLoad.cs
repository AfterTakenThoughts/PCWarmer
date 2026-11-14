namespace PCWarmer.Core.Stress
{
    /// <summary>The CPU load for the None hardware load: it starts no threads and does no work.</summary>
    public sealed class NoCpuLoad : ICpuLoad
    {
        public bool IsRunning { get; private set; }

        public int ThreadCount => 0;

        public void Start(double dutyCycle) => IsRunning = true;

        public void Stop() => IsRunning = false;
    }
}
