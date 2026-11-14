using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace PCWarmer.Core.Stress
{
    /// <summary>
    /// Heats the CPU with background worker threads. Each worker runs math in 100 ms windows:
    /// busy for <see cref="DutyCycle"/> of the window, asleep for the rest.
    /// </summary>
    public sealed class CpuStressService : ICpuLoad, IDisposable
    {
        private static readonly TimeSpan Window = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan JoinTimeout = TimeSpan.FromSeconds(1);

        private readonly object gate = new();
        private readonly List<Thread> workers = new();
        private CancellationTokenSource cancellation;
        private int activeWorkers;
        private long workUnitsCompleted;

        // Written by workers so the JIT can't optimize the math away.
        private volatile float sink;

        /// <param name="threadCount">Worker threads to run. 0 or less means one per core; more than the core count is clamped to it.</param>
        public CpuStressService(int threadCount)
        {
            int cores = Environment.ProcessorCount;
            ThreadCount = threadCount <= 0 || threadCount > cores ? cores : threadCount;
        }

        public int ThreadCount { get; }

        /// <summary>The fraction of each window workers spend busy, clamped to [0, 1].</summary>
        public double DutyCycle { get; private set; }

        public bool IsRunning { get; private set; }

        /// <summary>Workers currently running their loop.</summary>
        public int ActiveWorkers => Volatile.Read(ref activeWorkers);

        /// <summary>Total work units finished by all workers since construction.</summary>
        public long WorkUnitsCompleted => Interlocked.Read(ref workUnitsCompleted);

        public void Start(double dutyCycle)
        {
            lock (gate)
            {
                StopWorkers();

                DutyCycle = Math.Clamp(dutyCycle, 0.0, 1.0);
                cancellation = new CancellationTokenSource();
                CancellationToken token = cancellation.Token;
                TimeSpan busyTime = Window * DutyCycle;

                for (int i = 0; i < ThreadCount; i++)
                {
                    var worker = new Thread(() => Work(busyTime, token))
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.BelowNormal,
                        Name = $"PCWarmer CPU worker {i + 1}",
                    };
                    workers.Add(worker);
                    worker.Start();
                }

                IsRunning = true;
            }
        }

        public void Stop()
        {
            lock (gate)
            {
                StopWorkers();
            }
        }

        public void Dispose() => Stop();

        private void StopWorkers()
        {
            if (cancellation == null)
                return;

            cancellation.Cancel();
            foreach (Thread worker in workers)
                worker.Join(JoinTimeout);

            workers.Clear();
            cancellation.Dispose();
            cancellation = null;
            IsRunning = false;
        }

        private void Work(TimeSpan busyTime, CancellationToken token)
        {
            Interlocked.Increment(ref activeWorkers);
            try
            {
                var stopwatch = new Stopwatch();
                while (!token.IsCancellationRequested)
                {
                    stopwatch.Restart();
                    do
                    {
                        DoWorkUnit();
                    }
                    while (stopwatch.Elapsed < busyTime && !token.IsCancellationRequested);

                    TimeSpan rest = Window - stopwatch.Elapsed;
                    if (rest > TimeSpan.Zero)
                        token.WaitHandle.WaitOne(rest);
                }
            }
            finally
            {
                Interlocked.Decrement(ref activeWorkers);
            }
        }

        private void DoWorkUnit()
        {
            double acc = 0;
            for (int i = 1; i <= 10_000; i++)
                acc += Math.Sqrt(i) * Math.Sin(i);

            sink = (float)acc;
            Interlocked.Increment(ref workUnitsCompleted);
        }
    }
}
