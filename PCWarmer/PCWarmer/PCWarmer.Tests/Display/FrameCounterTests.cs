using NUnit.Framework;
using PCWarmer.Core.Display;

namespace PCWarmer.Tests.Display;

[TestFixture]
public class FrameCounterTests
{
    private static TimeSpan FrameAt(int fps) => TimeSpan.FromTicks(TimeSpan.TicksPerSecond / fps);

    [Test]
    public void Fps_WithNoFrames_IsZero()
        => Assert.That(new FrameCounter().Fps, Is.EqualTo(0));

    [Test]
    public void Fps_SteadySixtyFrames_ReportsSixty()
    {
        var counter = new FrameCounter();

        for (int i = 0; i < 60; i++)
            counter.Add(FrameAt(60));

        Assert.That(counter.Fps, Is.EqualTo(60).Within(0.5));
    }

    [Test]
    public void Fps_OnlyCountsTheLastSecond()
    {
        var counter = new FrameCounter();
        for (int i = 0; i < 60; i++)
            counter.Add(FrameAt(60));

        // A full second of slower frames pushes the fast ones out of the window.
        for (int i = 0; i < 30; i++)
            counter.Add(FrameAt(30));

        Assert.That(counter.Fps, Is.EqualTo(30).Within(0.5));
    }

    [Test]
    public void Fps_IgnoresZeroLengthFrames()
    {
        var counter = new FrameCounter();

        counter.Add(TimeSpan.Zero);

        Assert.That(counter.Fps, Is.EqualTo(0));
    }
}
