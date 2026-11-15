using NUnit.Framework;
using PCWarmer.Core.Display;

namespace PCWarmer.Tests.Display;

[TestFixture]
public class HintFadeTests
{
    private static HintFade ShownFor(double seconds)
    {
        var hint = new HintFade(hold: TimeSpan.FromSeconds(3), fade: TimeSpan.FromSeconds(1));
        hint.Show();
        hint.Update(TimeSpan.FromSeconds(seconds));
        return hint;
    }

    [Test]
    public void BeforeShow_IsInvisible()
        => Assert.That(new HintFade(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)).Opacity, Is.EqualTo(0f));

    [TestCase(0.0)]
    [TestCase(2.9)]
    public void DuringTheHold_IsFullyVisible(double seconds)
        => Assert.That(ShownFor(seconds).Opacity, Is.EqualTo(1f));

    [TestCase(3.25, 0.75f)]
    [TestCase(3.5, 0.5f)]
    [TestCase(3.75, 0.25f)]
    public void DuringTheFade_FadesEvenlyToNothing(double seconds, float expected)
        => Assert.That(ShownFor(seconds).Opacity, Is.EqualTo(expected).Within(0.001f));

    [TestCase(4.0)]
    [TestCase(60.0)]
    public void AfterTheFade_IsInvisible(double seconds)
        => Assert.That(ShownFor(seconds).Opacity, Is.EqualTo(0f));

    [Test]
    public void Show_Again_RestartsTheHold()
    {
        HintFade hint = ShownFor(10);

        hint.Show();
        hint.Update(TimeSpan.FromSeconds(1));

        Assert.That(hint.Opacity, Is.EqualTo(1f));
    }

    [Test]
    public void Hide_MakesItInvisibleRightAway()
    {
        HintFade hint = ShownFor(1);

        hint.Hide();

        Assert.That(hint.Opacity, Is.EqualTo(0f));
    }
}
