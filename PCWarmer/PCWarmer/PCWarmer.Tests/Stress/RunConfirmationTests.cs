using NUnit.Framework;
using PCWarmer.Core.Stress;

namespace PCWarmer.Tests.Stress;

[TestFixture]
public class RunConfirmationTests
{
    [Test]
    public void None_HasNoConfirmation()
        => Assert.That(RunConfirmation.For(HardwareLoad.None, "Embers"), Is.Null);

    [Test]
    public void High_AsksWithTheScreensaverAndIntensity()
        => Assert.That(RunConfirmation.For(HardwareLoad.High, "Embers"), Is.EqualTo((
            "Really warm your room with Embers?",
            "High intensity. Your CPU and GPU will run hard and your PC will get hot.")));

    [Test]
    public void Custom_SaysCustomIntensity()
        => Assert.That(RunConfirmation.For(HardwareLoad.Custom, "Starfield")?.Detail,
            Does.StartWith("Custom intensity."));
}
