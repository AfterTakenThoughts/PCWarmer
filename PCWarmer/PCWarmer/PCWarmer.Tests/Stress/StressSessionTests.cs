using NUnit.Framework;
using PCWarmer.Core.Stress;

namespace PCWarmer.Tests.Stress;

[TestFixture]
public class StressSessionTests
{
    private TimeSpan now;
    private FakeCpuLoad cpu = null!;
    private StressSession session = null!;

    [SetUp]
    public void SetUp()
    {
        now = TimeSpan.FromSeconds(10);
        cpu = new FakeCpuLoad();
        session = new StressSession(cpu, () => now);
    }

    [TearDown]
    public void TearDown() => session.Dispose();

    [Test]
    public void IsIdleBeforeStart()
    {
        Assert.That(session.IsRunning, Is.False);
        Assert.That(session.Elapsed, Is.EqualTo(TimeSpan.Zero));
        Assert.That(cpu.StartedWith, Is.Empty);
    }

    [Test]
    public void Start_DrivesCpuAtTheGivenDutyCycle()
    {
        session.Start(0.75, duration: null);

        Assert.That(session.IsRunning, Is.True);
        Assert.That(session.CpuDutyCycle, Is.EqualTo(0.75));
        Assert.That(cpu.StartedWith, Is.EqualTo(new[] { 0.75 }));
    }

    [Test]
    public void Elapsed_IsMeasuredOnTheClockFromStart()
    {
        session.Start(0.5, duration: null);

        now = TimeSpan.FromSeconds(70);

        Assert.That(session.Elapsed, Is.EqualTo(TimeSpan.FromMinutes(1)));
    }

    [Test]
    public void Duration_EndsTheRunOnTheClockNotFrames()
    {
        session.Start(1.0, TimeSpan.FromMinutes(30));

        now += TimeSpan.FromMinutes(29);
        session.Update();
        Assert.That(session.IsRunning, Is.True);
        Assert.That(session.Remaining, Is.EqualTo(TimeSpan.FromMinutes(1)));

        // One Update after a long gap (the window was minimized) still ends the run.
        now += TimeSpan.FromMinutes(16);
        session.Update();
        session.Update();

        Assert.That(session.IsRunning, Is.False);
        Assert.That(cpu.StopCount, Is.EqualTo(1));
        Assert.That(session.Remaining, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void Unlimited_NeverStopsOnItsOwn()
    {
        session.Start(0.25, duration: null);

        now += TimeSpan.FromDays(10);
        session.Update();

        Assert.That(session.IsRunning, Is.True);
        Assert.That(session.Remaining, Is.Null);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Start_RejectsNonPositiveDuration(int minutes)
    {
        Assert.That(() => session.Start(0.5, TimeSpan.FromMinutes(minutes)), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(cpu.StartedWith, Is.Empty);
    }

    [Test]
    public void Stop_StopsCpuAndFreezesElapsed()
    {
        session.Start(0.5, duration: null);
        now = TimeSpan.FromSeconds(40);

        session.Stop();
        now = TimeSpan.FromHours(5);

        Assert.That(session.IsRunning, Is.False);
        Assert.That(cpu.IsRunning, Is.False);
        Assert.That(session.Elapsed, Is.EqualTo(TimeSpan.FromSeconds(30)));
    }

    [Test]
    public void Stop_Twice_StopsCpuOnce()
    {
        session.Start(0.5, duration: null);

        session.Stop();
        session.Stop();

        Assert.That(cpu.StopCount, Is.EqualTo(1));
    }

    [Test]
    public void Dispose_WhileRunning_StopsCpu()
    {
        session.Start(0.5, duration: null);

        session.Dispose();

        Assert.That(cpu.IsRunning, Is.False);
        Assert.That(session.IsRunning, Is.False);
    }
}
