using System;
using System.Collections.Generic;

namespace PCWarmer.Core.Stress
{
    /// <summary>
    /// The harmonic series 1 + 1/2 + 1/3 + ..., which diverges: its partial sums grow without bound
    /// (roughly ln(n) + 0.5772), just very slowly. That makes it endless CPU work.
    /// </summary>
    public static class DivergentSeries
    {
        /// <summary>Lazily yields H(1), H(2), H(3), ... forever.</summary>
        public static IEnumerable<double> HarmonicPartialSums()
        {
            var sum = 0.0;
            for (long n = 1; ; n++)
            {
                sum += 1.0 / n;
                yield return sum;
            }
        }

        /// <summary>The smallest n for which H(n) is greater than <paramref name="bound"/>.</summary>
        public static long TermsToExceed(double bound)
        {
            if (!double.IsFinite(bound))
                throw new ArgumentOutOfRangeException(nameof(bound), bound, "The bound must be a finite number.");

            long n = 0;
            foreach (var sum in HarmonicPartialSums())
            {
                n++;
                if (sum > bound)
                    return n;
            }

            throw new InvalidOperationException("Unreachable: the harmonic series never ends.");
        }
    }
}
