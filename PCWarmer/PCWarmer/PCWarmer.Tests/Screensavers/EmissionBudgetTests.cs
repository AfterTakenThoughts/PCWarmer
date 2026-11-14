using NUnit.Framework;
using PCWarmer.Core.Screensavers;

namespace PCWarmer.Tests.Screensavers;

[TestFixture]
public class EmissionBudgetTests
{
    [Test]
    public void Next_EmitsAtTheRatePerSecond()
        => Assert.That(new EmissionBudget(1_000, 10_000).Next(TimeSpan.FromMilliseconds(16), 0), Is.EqualTo(16));

    [Test]
    public void Next_CarriesFractionsToLaterFrames()
    {
        var budget = new EmissionBudget(100, 10_000);

        // 100/s over 5 ms is half a particle: none now, the other half completes one next frame.
        Assert.That(budget.Next(TimeSpan.FromMilliseconds(5), 0), Is.EqualTo(0));
        Assert.That(budget.Next(TimeSpan.FromMilliseconds(5), 0), Is.EqualTo(1));
    }

    [Test]
    public void Next_AtTheCap_EmitsNothing()
        => Assert.That(new EmissionBudget(1_000, 500).Next(TimeSpan.FromSeconds(1), 500), Is.EqualTo(0));

    [Test]
    public void Next_LongHitch_OnlyFillsUpToTheCap()
        => Assert.That(new EmissionBudget(20_000, 3_000).Next(TimeSpan.FromSeconds(10), 2_700), Is.EqualTo(300));

    [Test]
    public void Next_CappedFrame_DoesNotBankTheOverflow()
    {
        var budget = new EmissionBudget(1_000, 500);
        budget.Next(TimeSpan.FromSeconds(10), 500);

        // Room opens up again: only this frame's share is emitted, not the 10 seconds that were capped.
        Assert.That(budget.Next(TimeSpan.FromMilliseconds(16), 0), Is.EqualTo(16));
    }

    [Test]
    public void Next_LiveCountAboveCap_EmitsNothing()
        => Assert.That(new EmissionBudget(1_000, 500).Next(TimeSpan.FromSeconds(1), 650), Is.EqualTo(0));
}
