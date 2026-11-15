using Microsoft.Xna.Framework;
using NUnit.Framework;
using PCWarmer.Screens;

namespace PCWarmer.Tests.Screens;

[TestFixture]
public class YesNoButtonsTests
{
    // A 400-wide row starting at x=100, y=300; buttons 120x40 with a 20 px gap.
    // Together they are 120 + 20 + 120 = 260 wide, so centered they start at 100 + (400 - 260) / 2 = 170.
    private static YesNoButtons Buttons() => new(new Rectangle(100, 300, 400, 40), new Point(120, 40), gap: 20);

    [Test]
    public void Layout_CentersYesThenNoSideBySide()
    {
        YesNoButtons buttons = Buttons();

        Assert.That(buttons.YesBounds, Is.EqualTo(new Rectangle(170, 300, 120, 40)));
        Assert.That(buttons.NoBounds, Is.EqualTo(new Rectangle(310, 300, 120, 40)));
    }

    [TestCase(171f, 301f, ExpectedResult = DialogChoice.Yes)]
    [TestCase(289f, 339f, ExpectedResult = DialogChoice.Yes)]
    [TestCase(310f, 320f, ExpectedResult = DialogChoice.No)]
    [TestCase(429f, 339f, ExpectedResult = DialogChoice.No)]
    public DialogChoice? HitTest_PointOnAButton_ReturnsIt(float x, float y)
        => Buttons().HitTest(new Vector2(x, y));

    [TestCase(300f, 320f, TestName = "In the gap between the buttons")]
    [TestCase(150f, 320f, TestName = "Left of Yes")]
    [TestCase(200f, 299f, TestName = "Just above Yes")]
    [TestCase(350f, 340f, TestName = "Just below No")]
    public void HitTest_PointOffTheButtons_ReturnsNull(float x, float y)
        => Assert.That(Buttons().HitTest(new Vector2(x, y)), Is.Null);

    [Test]
    public void Focus_StartsOnYes()
        => Assert.That(Buttons().Focused, Is.EqualTo(DialogChoice.Yes));

    [Test]
    public void MoveFocus_SwitchesBetweenTheButtons()
    {
        YesNoButtons buttons = Buttons();

        buttons.MoveFocus();
        Assert.That(buttons.Focused, Is.EqualTo(DialogChoice.No));

        buttons.MoveFocus();
        Assert.That(buttons.Focused, Is.EqualTo(DialogChoice.Yes));
    }
}
