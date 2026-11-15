using NUnit.Framework;
using PCWarmer.Core.Stress;

namespace PCWarmer.Tests.Stress;

[TestFixture]
public class DivergentSeriesTests
{
    [Test]
    public void PartialSums_StartWithHarmonicValues()
    {
        var first = DivergentSeries.HarmonicPartialSums().Take(4).ToArray();

        Assert.That(first, Is.EqualTo(new[] { 1.0, 1.5, 11.0 / 6.0, 25.0 / 12.0 }).Within(1e-12));
    }

    [Test]
    public void PartialSums_AreStrictlyIncreasing()
    {
        var sums = DivergentSeries.HarmonicPartialSums().Take(10_000).ToArray();

        for (var i = 1; i < sums.Length; i++)
            Assert.That(sums[i], Is.GreaterThan(sums[i - 1]), $"H({i + 1}) is not greater than H({i})");
    }

    [Test]
    public void PartialSums_NeverEnd()
    {
        // An infinite series has no last term: asking for a million terms gets a million.
        Assert.That(DivergentSeries.HarmonicPartialSums().Take(1_000_000).Count(), Is.EqualTo(1_000_000));
    }

    [Test]
    public void PartialSums_GrowWithoutBound()
    {
        // H(n) ≈ ln(n) + γ, so H(1,000,000) ≈ 14.39: far past any convergent limit like 2 or e.
        var millionth = DivergentSeries.HarmonicPartialSums().Skip(999_999).First();

        Assert.That(millionth, Is.EqualTo(Math.Log(1_000_000) + 0.5772156649).Within(1e-5));
    }

    [TestCase(1.0, ExpectedResult = 2L)]
    [TestCase(2.0, ExpectedResult = 4L)]
    [TestCase(3.0, ExpectedResult = 11L)]
    [TestCase(4.0, ExpectedResult = 31L)]
    [TestCase(5.0, ExpectedResult = 83L)]
    [TestCase(10.0, ExpectedResult = 12_367L)]
    public long TermsToExceed_MatchesKnownHarmonicValues(double bound)
        => DivergentSeries.TermsToExceed(bound);

    [TestCase(0.0)]
    [TestCase(-5.0)]
    public void TermsToExceed_BoundBelowFirstTermNeedsOneTerm(double bound)
        => Assert.That(DivergentSeries.TermsToExceed(bound), Is.EqualTo(1));

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void TermsToExceed_RejectsNonFiniteBound(double bound)
        => Assert.That(() => DivergentSeries.TermsToExceed(bound), Throws.TypeOf<ArgumentOutOfRangeException>());
}
