using Microsoft.Xna.Framework;
using NUnit.Framework;
using PCWarmer.Core.Screensavers;

namespace PCWarmer.Tests.Screensavers;

[TestFixture]
public class OrbFieldTests
{
    private static readonly Vector2 Screen = new(800, 600);

    private static OrbField Single(Vector2 position, Vector2 velocity, float radius = 10)
        => new(Screen, new[] { new Orb { Position = position, Velocity = velocity, Radius = radius } });

    [Test]
    public void Update_InOpenSpace_MovesInAStraightLine()
    {
        var field = Single(new Vector2(400, 300), new Vector2(100, 50));

        field.Update(TimeSpan.FromMilliseconds(100));

        Assert.That(field.Orbs[0].Position.X, Is.EqualTo(410f).Within(0.001f));
        Assert.That(field.Orbs[0].Position.Y, Is.EqualTo(305f).Within(0.001f));
    }

    // Radius 10, so the orb's edge touches a wall when its center is 10 px away from it.
    [TestCase(785f, 300f, 100f, 0f, TestName = "Right wall")]
    [TestCase(15f, 300f, -100f, 0f, TestName = "Left wall")]
    [TestCase(400f, 15f, 0f, -100f, TestName = "Top wall")]
    [TestCase(400f, 585f, 0f, 100f, TestName = "Bottom wall")]
    public void Update_HittingAWall_BouncesBackInside(float x, float y, float vx, float vy)
    {
        var field = Single(new Vector2(x, y), new Vector2(vx, vy));

        field.Update(TimeSpan.FromMilliseconds(100));

        Orb orb = field.Orbs[0];
        Assert.That(orb.Velocity, Is.EqualTo(new Vector2(-vx, -vy)), "velocity should reverse toward the inside");
        Assert.That(orb.Position.X, Is.InRange(10f, 790f));
        Assert.That(orb.Position.Y, Is.InRange(10f, 590f));
    }

    [Test]
    public void RandomField_StaysOnScreenForever()
    {
        var field = OrbField.CreateRandom(Screen, count: 300, new Random(1));

        for (int i = 0; i < 600; i++)
            field.Update(TimeSpan.FromMilliseconds(16));

        Assert.That(field.Count, Is.EqualTo(300));
        foreach (Orb orb in field.Orbs)
        {
            Assert.That(orb.Position.X, Is.InRange(orb.Radius, 800 - orb.Radius));
            Assert.That(orb.Position.Y, Is.InRange(orb.Radius, 600 - orb.Radius));
        }
    }
}
