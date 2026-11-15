using NUnit.Framework;
using PCWarmer.Core.Ui;

namespace PCWarmer.Tests.Ui;

[TestFixture]
public class ShapeMasksTests
{
    [Test]
    public void RoundedRect_CornersAreClearAndCentreIsSolid()
    {
        float[] mask = ShapeMasks.RoundedRect(40, 20, 10);

        Assert.That(mask.Length, Is.EqualTo(800));
        Assert.That(mask[0], Is.EqualTo(0f), "top-left corner pixel");
        Assert.That(mask[10 * 40 + 20], Is.EqualTo(1f), "centre");
        Assert.That(mask[10 * 40 + 0], Is.GreaterThan(0.5f), "middle of the left edge is inside");
    }

    [Test]
    public void Disc_IsSolidInsideAndClearOutside()
    {
        float[] mask = ShapeMasks.Disc(32);

        Assert.That(mask[16 * 32 + 16], Is.EqualTo(1f));
        Assert.That(mask[0], Is.EqualTo(0f));
    }

    [Test]
    public void Glow_FadesFromCentreToEdge()
    {
        float[] mask = ShapeMasks.Glow(32);

        Assert.That(mask[16 * 32 + 16], Is.GreaterThan(mask[16 * 32 + 28]));
        Assert.That(mask[16 * 32 + 31], Is.LessThan(0.1f));
    }

    [Test]
    public void RoundedOutline_IsOnlyTheEdge()
    {
        float[] mask = ShapeMasks.RoundedOutline(40, 40, 10, 1);

        Assert.That(mask[20 * 40 + 20], Is.EqualTo(0f), "the middle is see-through");
        Assert.That(mask[20 * 40 + 0], Is.GreaterThan(0.5f), "the left edge is drawn");
        Assert.That(mask[20 * 40 + 3], Is.EqualTo(0f), "just inside the edge is see-through");
        Assert.That(mask[0], Is.EqualTo(0f), "outside the rounded corner");
    }

    [Test]
    public void Halo_FadesEvenlySoItReachesFurtherThanGlow()
    {
        float[] halo = ShapeMasks.Halo(100);
        float[] glow = ShapeMasks.Glow(100);
        int halfway = 50 * 100 + 75;   // halfway from the centre to the right edge

        Assert.That(halo[halfway], Is.EqualTo(0.5f).Within(0.02f));
        Assert.That(halo[halfway], Is.GreaterThan(glow[halfway] * 1.8f));
        Assert.That(halo[50 * 100 + 99], Is.LessThan(0.05f), "fades to nothing at the edge");
    }
}
