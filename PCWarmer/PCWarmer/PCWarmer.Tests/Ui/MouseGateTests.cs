using NUnit.Framework;
using PCWarmer.Core.Ui;

namespace PCWarmer.Tests.Ui;

[TestFixture]
public class MouseGateTests
{
    [Test]
    public void NewScreen_WaitsForTheButtonThatOpenedIt()
    {
        var gate = new MouseGate();

        Assert.That(gate.AllowInput(isActive: true, anyButtonDown: true), Is.False, "the click that opened it is still held");
        Assert.That(gate.AllowInput(isActive: true, anyButtonDown: false), Is.True);
        Assert.That(gate.AllowInput(isActive: true, anyButtonDown: true), Is.True, "a fresh press after release counts");
    }

    [Test]
    public void BecomingActiveAgain_WaitsForRelease()
    {
        var gate = new MouseGate();
        gate.AllowInput(true, false);

        Assert.That(gate.AllowInput(isActive: false, anyButtonDown: false), Is.False, "covered by a popup");
        Assert.That(gate.AllowInput(isActive: true, anyButtonDown: true), Is.False, "the click that closed the popup");
        Assert.That(gate.AllowInput(isActive: true, anyButtonDown: true), Is.False, "still held");
        Assert.That(gate.AllowInput(isActive: true, anyButtonDown: false), Is.True);
    }

    [Test]
    public void Inactive_NeverAllowsInput()
        => Assert.That(new MouseGate().AllowInput(isActive: false, anyButtonDown: false), Is.False);
}
