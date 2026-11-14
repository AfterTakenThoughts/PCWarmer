using NUnit.Framework;
using PCWarmer.Core.Screensavers;

namespace PCWarmer.Tests.Screensavers;

[TestFixture]
public class ScreensaverCatalogTests
{
    [Test]
    public void Entries_ListEveryScreensaverInMenuOrder()
        => Assert.That(ScreensaverCatalog.Entries.Select(e => e.Id),
            Is.EqualTo(new[] { "particle-storm", "embers", "starfield", "bouncing-orbs" }));

    [TestCase("particle-storm")]
    [TestCase("embers")]
    [TestCase("starfield")]
    [TestCase("bouncing-orbs")]
    public void Create_KnownId_MakesThatScreensaverWithoutTheGpu(string id)
        => Assert.That(ScreensaverCatalog.Create(id).Id, Is.EqualTo(id));

    [TestCase("gone")]
    [TestCase(null)]
    public void Create_UnknownId_FallsBackToParticleStorm(string? id)
        => Assert.That(ScreensaverCatalog.Create(id).Id, Is.EqualTo("particle-storm"));
}

[TestFixture]
public class ScreensaverLifecycleTests
{
    private static readonly Microsoft.Xna.Framework.GameTime Frame =
        new(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(16));

    private static IEnumerable<string> Ids() => ScreensaverCatalog.Entries.Select(e => e.Id);

    // ScreensaverScreen can call Update after it has unloaded the screensaver (when its fade-out ends);
    // that used to crash Particle Storm.
    [TestCaseSource(nameof(Ids))]
    public void UpdateAndDraw_AfterUnload_DoNothing(string id)
    {
        IScreensaver screensaver = ScreensaverCatalog.Create(id);
        screensaver.UnloadContent();

        Assert.That(() => screensaver.Update(Frame), Throws.Nothing);
        Assert.That(() => screensaver.Draw(null!, Frame), Throws.Nothing);
        Assert.That(screensaver.LiveParticles, Is.EqualTo(0));
    }
}
