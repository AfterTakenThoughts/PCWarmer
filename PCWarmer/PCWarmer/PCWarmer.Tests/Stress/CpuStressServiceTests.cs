using NUnit.Framework;
using PCWarmer.Core.Stress;

namespace PCWarmer.Tests.Stress;

[TestFixture]
public class CpuStressServiceTests
{
    [TestCase(0)]
    [TestCase(-3)]
    [TestCase(9_999)]
    public void Constructor_ClampsThreadCountToCoreCount(int requested)
    {
        using var cpu = new CpuStressService(requested);

        Assert.That(cpu.ThreadCount, Is.EqualTo(Environment.ProcessorCount));
    }

    [Test]
    public void Constructor_KeepsValidThreadCount()
    {
        using var cpu = new CpuStressService(1);

        Assert.That(cpu.ThreadCount, Is.EqualTo(1));
    }

    [Test]
    public void Start_RunsOneWorkerPerThreadDoingWork()
    {
        using var cpu = new CpuStressService(2);

        cpu.Start(1.0);

        Assert.That(cpu.IsRunning, Is.True);
        Assert.That(() => cpu.ActiveWorkers, Is.EqualTo(2).After(2_000, 10));
        Assert.That(() => cpu.WorkUnitsCompleted, Is.GreaterThan(0).After(2_000, 10));
    }

    [Test]
    public void Start_Twice_RestartsWithoutAddingWorkers()
    {
        using var cpu = new CpuStressService(2);

        cpu.Start(0.5);
        cpu.Start(1.0);

        Assert.That(cpu.DutyCycle, Is.EqualTo(1.0));
        Assert.That(() => cpu.ActiveWorkers, Is.EqualTo(2).After(2_000, 10));
        Thread.Sleep(100);
        Assert.That(cpu.ActiveWorkers, Is.EqualTo(2));
    }

    [TestCase(1.5, 1.0)]
    [TestCase(-0.5, 0.0)]
    public void Start_ClampsDutyCycle(double requested, double expected)
    {
        using var cpu = new CpuStressService(1);

        cpu.Start(requested);

        Assert.That(cpu.DutyCycle, Is.EqualTo(expected));
    }

    [Test]
    public void Stop_EndsAllWorkAndIsSafeToRepeat()
    {
        using var cpu = new CpuStressService(2);
        cpu.Start(1.0);
        Assert.That(() => cpu.WorkUnitsCompleted, Is.GreaterThan(0).After(2_000, 10));

        cpu.Stop();
        var doneAtStop = cpu.WorkUnitsCompleted;
        Thread.Sleep(100);

        Assert.That(cpu.IsRunning, Is.False);
        Assert.That(cpu.ActiveWorkers, Is.EqualTo(0));
        Assert.That(cpu.WorkUnitsCompleted, Is.EqualTo(doneAtStop));
        Assert.That(() => cpu.Stop(), Throws.Nothing);
    }

    [Test]
    public void Dispose_StopsAllWorkers()
    {
        var cpu = new CpuStressService(2);
        cpu.Start(1.0);
        Assert.That(() => cpu.ActiveWorkers, Is.EqualTo(2).After(2_000, 10));

        cpu.Dispose();

        Assert.That(cpu.IsRunning, Is.False);
        Assert.That(cpu.ActiveWorkers, Is.EqualTo(0));
    }
}
