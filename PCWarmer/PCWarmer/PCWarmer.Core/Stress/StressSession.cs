using System;

namespace PCWarmer.Core.Stress
{
    /// <summary>
    /// One warming run: drives the CPU load and measures how long it has run, ending it when its duration is up.
    /// Time comes from a wall clock, not frames, so it keeps counting while the window is minimized.
    /// </summary>
    public sealed class StressSession : IDisposable
    {
        private readonly ICpuLoad cpu;
        private readonly Func<TimeSpan> clock;
        private TimeSpan startedAt;
        private TimeSpan stoppedElapsed;

        /// <param name="cpu">The CPU load to drive.</param>
        /// <param name="clock">Current time; in production, the elapsed time of a started <see cref="System.Diagnostics.Stopwatch"/>.</param>
        public StressSession(ICpuLoad cpu, Func<TimeSpan> clock)
        {
            this.cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public bool IsRunning { get; private set; }

        /// <summary>The fraction of each time window the CPU workers spend busy.</summary>
        public double CpuDutyCycle { get; private set; }

        public int CpuThreads => cpu.ThreadCount;

        /// <summary>How long the run lasts; null means unlimited.</summary>
        public TimeSpan? Duration { get; private set; }

        /// <summary>How long the run has lasted. Frozen once the run stops.</summary>
        public TimeSpan Elapsed => IsRunning ? clock() - startedAt : stoppedElapsed;

        /// <summary>Time left in the run, never negative; null when unlimited.</summary>
        public TimeSpan? Remaining => Duration is { } duration
            ? (duration > Elapsed ? duration - Elapsed : TimeSpan.Zero)
            : null;

        /// <param name="cpuDutyCycle">Fraction of each time window the CPU workers spend busy.</param>
        /// <param name="duration">How long to run; null means until stopped.</param>
        public void Start(double cpuDutyCycle, TimeSpan? duration)
        {
            if (duration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "A run must last longer than zero.");

            CpuDutyCycle = cpuDutyCycle;
            Duration = duration;
            startedAt = clock();
            cpu.Start(cpuDutyCycle);
            IsRunning = true;
        }

        /// <summary>Call every frame: ends the run once its duration is up.</summary>
        public void Update()
        {
            if (IsRunning && Elapsed >= Duration)
                Stop();
        }

        /// <summary>Stops the run. Safe to call more than once.</summary>
        public void Stop()
        {
            if (!IsRunning)
                return;

            stoppedElapsed = Elapsed;
            IsRunning = false;
            cpu.Stop();
        }

        public void Dispose() => Stop();
    }
}
