using System;
using System.Collections.Generic;

namespace PCWarmer.Core.Display
{
    /// <summary>Rolling frames-per-second over the most recent second of frames.</summary>
    public sealed class FrameCounter
    {
        private static readonly TimeSpan Window = TimeSpan.FromSeconds(1);

        private readonly Queue<TimeSpan> frames = new();
        private TimeSpan total;

        /// <summary>Frames in the last second divided by their total time; 0 before any frames.</summary>
        public double Fps => total > TimeSpan.Zero ? frames.Count / total.TotalSeconds : 0;

        public void Add(TimeSpan frameTime)
        {
            if (frameTime <= TimeSpan.Zero)
                return;

            frames.Enqueue(frameTime);
            total += frameTime;

            // Drop the oldest frames while the rest still cover a full second.
            while (frames.Count > 1 && total - frames.Peek() >= Window)
                total -= frames.Dequeue();
        }
    }
}
