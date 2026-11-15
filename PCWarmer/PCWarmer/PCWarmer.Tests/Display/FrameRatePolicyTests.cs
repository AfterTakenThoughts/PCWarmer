using NUnit.Framework;
using PCWarmer.Core.Display;

namespace PCWarmer.Tests.Display;

[TestFixture]
public class FrameRatePolicyTests
{
    [TestCase(FpsCap.Fps60, 166_666L)]
    [TestCase(FpsCap.Fps120, 83_333L)]
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

    [TestCase(0)]   // the old Fps30
    [TestCase(42)]
    public void UndefinedCap_FallsBackTo60(int raw)
        => Assert.That(FrameRatePolicy.For((FpsCap)raw),
            Is.EqualTo(new FrameRateSettings(true, false, TimeSpan.FromTicks(166_666L))));
}
