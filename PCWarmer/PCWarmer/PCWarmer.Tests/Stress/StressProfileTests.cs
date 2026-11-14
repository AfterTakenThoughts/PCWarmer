using NUnit.Framework;
using PCWarmer.Core.Stress;

namespace PCWarmer.Tests.Stress;

[TestFixture]
public class StressProfileTests
{
    [TestCase(StressLevel.Low, 0.25, 300, 3_000)]
    [TestCase(StressLevel.Medium, 0.60, 3_000, 30_000)]
    [TestCase(StressLevel.High, 1.00, 20_000, 150_000)]
    public void Profile_MatchesTable(StressLevel level, double duty, int rate, int max)
        => Assert.That(StressProfile.For(level), Is.EqualTo(new StressProfile(duty, rate, max)));

    [Test]
    public void Profile_UndefinedLevelFallsBackToMedium()
        => Assert.That(StressProfile.For((StressLevel)99), Is.EqualTo(new StressProfile(0.60, 3_000, 30_000)));
}
