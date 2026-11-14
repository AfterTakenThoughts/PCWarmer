using Microsoft.Xna.Framework;
using NUnit.Framework;
using PCWarmer.Core.Screensavers;

namespace PCWarmer.Tests.Screensavers;

[TestFixture]
public class StarfieldTests
{
    private static readonly Vector2 Screen = new(800, 600);
    private static readonly Vector2 Center = new(400, 300);
    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16);

    private static bool OnScreen(Star star)
        => star.Position.X is >= 0 and <= 800 && star.Position.Y is >= 0 and <= 600;

    [Test]
    public void Constructor_FillsTheScreenWithStars()
    {
        var field = new Starfield(Screen, count: 500, new Random(1));

        Assert.That(field.Count, Is.EqualTo(500));
        Assert.That(field.Stars, Has.All.Matches<Star>(OnScreen));
    }

    [Test]
    public void Update_MovesEachStarAwayFromTheCenterOrBackToIt()
    {
        var field = new Starfield(Screen, 500, new Random(2));
        float[] before = field.Stars.Select(s => Vector2.Distance(s.Position, Center)).ToArray();

        field.Update(Frame);

        int movedOut = 0;
        for (int i = 0; i < before.Length; i++)
        {
            float after = Vector2.Distance(field.Stars[i].Position, Center);
            if (after > before[i])
                movedOut++;
            else
                Assert.That(after, Is.LessThanOrEqualTo(20f), $"star {i} moved inward without leaving the screen");
        }

        // Only stars right at the edge leave in one frame; the rest all move outward.
        Assert.That(movedOut, Is.GreaterThan(400));
    }

    [Test]
    public void Update_StarsThatLeaveTheScreenComeBackNearTheCenter()
    {
        var field = new Starfield(Screen, 500, new Random(3));

        // Ten seconds is long enough for every star to fly off the screen at least once.
        for (var t = TimeSpan.Zero; t < TimeSpan.FromSeconds(10); t += Frame)
            field.Update(Frame);

        Assert.That(field.Count, Is.EqualTo(500));
        Assert.That(field.Stars, Has.All.Matches<Star>(OnScreen));
    }
}
