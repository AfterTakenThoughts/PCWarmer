using NUnit.Framework;
using PCWarmer.Core.Display;

namespace PCWarmer.Tests.Display;

[TestFixture]
public class FrameRatePolicyTests
{
    [TestCase(FpsCap.Fps30, 333_333L)]
    [TestCase(FpsCap.Fps60, 166_666L)]
    [TestCase(FpsCap.Fps144, 69_444L)]
    public void Capped_UsesFixedStepAtTheCapWithoutVSync(FpsCap cap, long frameTicks)
        => Assert.That(FrameRatePolicy.For(cap),
            Is.EqualTo(new FrameRateSettings(IsFixedTimeStep: true, VSync: false, TimeSpan.FromTicks(frameTicks))));

    [Test]
    public void Uncapped_DisablesFixedStepAndVSync()
    {
        FrameRateSettings settings = FrameRatePolicy.For(FpsCap.Uncapped);

        Assert.That(settings.IsFixedTimeStep, Is.False);
        Assert.That(settings.VSync, Is.False);
    }

    [Test]
    public void UndefinedCap_FallsBackTo60()
        => Assert.That(FrameRatePolicy.For((FpsCap)42),
            Is.EqualTo(new FrameRateSettings(true, false, TimeSpan.FromTicks(166_666L))));
}
