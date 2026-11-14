namespace PCWarmer.Core.Stress
{
    /// <summary>Something that loads the CPU. Lets code that drives a load be tested without real threads.</summary>
    public interface ICpuLoad
    {
        /// <summary>Starts (or restarts) the load, keeping each thread busy for <paramref name="dutyCycle"/> of the time.</summary>
        void Start(double dutyCycle);

        /// <summary>Stops the load. Safe to call more than once.</summary>
        void Stop();

        bool IsRunning { get; }

        int ThreadCount { get; }
    }
}
