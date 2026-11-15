using NUnit.Framework;
using PCWarmer.Core.Display;

namespace PCWarmer.Tests.Display;

[TestFixture]
public class GaugeScaleTests
{
    [TestCase(0.6, 1.0, ExpectedResult = 0.6f)]
    [TestCase(300, 240, ExpectedResult = 1f)]
    [TestCase(-5, 60, ExpectedResult = 0f)]
    [TestCase(10, 0, ExpectedResult = 0f)]
    public float Fraction_IsClamped(double value, double full) => GaugeScale.Fraction(value, full);

    [TestCase(FpsCap.Fps60, ExpectedResult = 60d)]
    [TestCase(FpsCap.Fps120, ExpectedResult = 120d)]
    [TestCase(FpsCap.Uncapped, ExpectedResult = 240d)]
    [TestCase((FpsCap)0, ExpectedResult = 60d)]
    public double FpsFull_MatchesTheCap(FpsCap cap) => GaugeScale.FpsFull(cap);
}
