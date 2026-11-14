namespace PCWarmer.Core.Stress
{
    /// <summary>
    /// The load a <see cref="StressLevel"/> puts on the machine.
    /// </summary>
    /// <param name="CpuDutyCycle">Fraction of each time window a CPU worker spends busy, from 0 to 1.</param>
    /// <param name="ParticlesPerSecond">How fast a screensaver emits particles.</param>
    /// <param name="MaxParticles">The most particles a screensaver keeps alive at once.</param>
    public sealed record StressProfile(double CpuDutyCycle, int ParticlesPerSecond, int MaxParticles)
    {
        private static readonly StressProfile Low = new(0.25, 300, 3_000);
        private static readonly StressProfile Medium = new(0.60, 3_000, 30_000);
        private static readonly StressProfile High = new(1.00, 20_000, 150_000);

        /// <summary>The profile for <paramref name="level"/>. Undefined values (for example from a hand-edited settings file) get Medium.</summary>
        public static StressProfile For(StressLevel level) => level switch
        {
            StressLevel.Low => Low,
            StressLevel.High => High,
            _ => Medium,
        };
    }
}
