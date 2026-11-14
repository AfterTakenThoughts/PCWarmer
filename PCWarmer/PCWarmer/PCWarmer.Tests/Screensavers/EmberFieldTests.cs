using Microsoft.Xna.Framework;
using NUnit.Framework;
using PCWarmer.Core.Screensavers;

namespace PCWarmer.Tests.Screensavers;

[TestFixture]
public class EmberFieldTests
{
    private static readonly Vector2 Screen = new(800, 600);
    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16);

    private static void Run(EmberField field, TimeSpan total)
    {
        for (var t = TimeSpan.Zero; t < total; t += Frame)
            field.Update(Frame);
    }

    [Test]
    public void Update_EmitsAtTheRateAlongTheBottomEdge()
    {
        var field = new EmberField(Screen, particlesPerSecond: 100, maxParticles: 1_000, new Random(1));

        field.Update(TimeSpan.FromMilliseconds(100));

        Assert.That(field.Count, Is.EqualTo(10));
        Assert.That(field.Embers.Select(e => e.Position.Y), Has.All.EqualTo(600f));
        Assert.That(field.Embers.Select(e => e.Position.X), Has.All.InRange(0f, 800f));
    }

    [Test]
    public void Update_EmbersRise()
    {
        var field = new EmberField(Screen, 100, 1_000, new Random(2));
        field.Update(TimeSpan.FromMilliseconds(100));
        float[] before = field.Embers.Select(e => e.Position.Y).ToArray();

        field.Update(Frame);

        for (int i = 0; i < before.Length; i++)
            Assert.That(field.Embers[i].Position.Y, Is.LessThan(before[i]), $"ember {i} did not rise");
    }

    [Test]
    public void Update_NeverExceedsTheCap()
    {
        var field = new EmberField(Screen, particlesPerSecond: 20_000, maxParticles: 500, new Random(3));

        field.Update(TimeSpan.FromSeconds(1));
        Assert.That(field.Count, Is.EqualTo(500));

        Run(field, TimeSpan.FromSeconds(2));
        Assert.That(field.Count, Is.LessThanOrEqualTo(500));
    }

    [Test]
    public void Update_RemovesEmbersThatBurnOut()
    {
        var field = new EmberField(Screen, 100, 10_000, new Random(4));

        // Embers live at most 4 seconds, so 20 seconds at 100/s settles at no more than ~400 alive (not 2,000).
        Run(field, TimeSpan.FromSeconds(20));

        Assert.That(field.Count, Is.LessThanOrEqualTo(402));
    }

    [Test]
    public void Update_RemovesEmbersThatLeaveTheTop()
    {
        // On a 10-pixel-tall screen an ember rising at 60 px/s or faster is gone in about a sixth of a second,
        // long before it would burn out.
        var field = new EmberField(new Vector2(800, 10), 100, 10_000, new Random(5));

        Run(field, TimeSpan.FromSeconds(3));

        Assert.That(field.Count, Is.LessThanOrEqualTo(22));
    }
}
