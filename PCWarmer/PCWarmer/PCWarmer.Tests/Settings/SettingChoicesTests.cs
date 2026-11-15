using NUnit.Framework;
using PCWarmer.Core.Display;
using PCWarmer.Core.Settings;
using PCWarmer.Core.Stress;

namespace PCWarmer.Tests.Settings;

[TestFixture]
public class SettingChoicesTests
{
    [TestCase(HardwareLoad.None, ExpectedResult = HardwareLoad.Low)]
    [TestCase(HardwareLoad.Low, ExpectedResult = HardwareLoad.Medium)]
    [TestCase(HardwareLoad.Medium, ExpectedResult = HardwareLoad.High)]
    [TestCase(HardwareLoad.High, ExpectedResult = HardwareLoad.Custom)]
    [TestCase(HardwareLoad.Custom, ExpectedResult = HardwareLoad.None)]
    [TestCase((HardwareLoad)42, ExpectedResult = HardwareLoad.None)]
    public HardwareLoad NextHardwareLoad_CyclesInMenuOrder(HardwareLoad load) => SettingChoices.NextHardwareLoad(load);

    [TestCase(StressLevel.Low, ExpectedResult = StressLevel.Medium)]
    [TestCase(StressLevel.Medium, ExpectedResult = StressLevel.High)]
    [TestCase(StressLevel.High, ExpectedResult = StressLevel.Low)]
    [TestCase((StressLevel)99, ExpectedResult = StressLevel.Low)]
    public StressLevel NextCpuLoad_CyclesLowMediumHigh(StressLevel level) => SettingChoices.NextCpuLoad(level);

    [TestCase(StressLevel.Low, ExpectedResult = StressLevel.Low)]
    [TestCase(StressLevel.High, ExpectedResult = StressLevel.High)]
    [TestCase((StressLevel)99, ExpectedResult = StressLevel.Medium)]
    [TestCase((StressLevel)(-1), ExpectedResult = StressLevel.Medium)]
    public StressLevel LevelOrDefault_KeepsKnownLevelsElseMedium(StressLevel level) => SettingChoices.LevelOrDefault(level);

    [TestCase(FpsCap.Fps60, ExpectedResult = FpsCap.Fps120)]
    [TestCase(FpsCap.Fps120, ExpectedResult = FpsCap.Uncapped)]
    [TestCase(FpsCap.Uncapped, ExpectedResult = FpsCap.Fps60)]
    [TestCase((FpsCap)0, ExpectedResult = FpsCap.Fps60)]
    public FpsCap NextFpsCap_CyclesCaps(FpsCap cap) => SettingChoices.NextFpsCap(cap);

    [TestCase(0, ExpectedResult = FpsCap.Fps60)]    // the old Fps30
    [TestCase(2, ExpectedResult = FpsCap.Fps120)]   // the old Fps144
    [TestCase(42, ExpectedResult = FpsCap.Fps60)]
    public FpsCap FpsCapOrDefault_MapsOldValues(int raw) => SettingChoices.FpsCapOrDefault((FpsCap)raw);

    [TestCase(0, ExpectedResult = 15)]
    [TestCase(15, ExpectedResult = 30)]
    [TestCase(30, ExpectedResult = 60)]
    [TestCase(60, ExpectedResult = 120)]
    [TestCase(120, ExpectedResult = 0)]
    [TestCase(45, ExpectedResult = 0)]
    public int NextDuration_CyclesUnlimitedThenMinuteSteps(int minutes) => SettingChoices.NextDuration(minutes);

    [TestCase(0, 4, ExpectedResult = 1)]
    [TestCase(1, 4, ExpectedResult = 2)]
    [TestCase(3, 4, ExpectedResult = 4)]
    [TestCase(4, 4, ExpectedResult = 0)]
    [TestCase(999, 4, ExpectedResult = 0)]
    [TestCase(-2, 4, ExpectedResult = 0)]
    [TestCase(0, 1, ExpectedResult = 1)]
    [TestCase(1, 1, ExpectedResult = 0)]
    public int NextCpuThreads_CyclesAllCoresThenOneToCoreCount(int threads, int cores)
        => SettingChoices.NextCpuThreads(threads, cores);

    [Test]
    public void RunDuration_PositiveMinutesAreALimit()
        => Assert.That(SettingChoices.RunDuration(30), Is.EqualTo(TimeSpan.FromMinutes(30)));

    [TestCase(0)]
    [TestCase(-5)]
    public void RunDuration_ZeroOrLessIsUnlimited(int minutes)
        => Assert.That(SettingChoices.RunDuration(minutes), Is.Null);

    [TestCase(HardwareLoad.None, 1, ExpectedResult = HardwareLoad.Low)]
    [TestCase(HardwareLoad.Custom, 1, ExpectedResult = HardwareLoad.None)]
    [TestCase(HardwareLoad.None, -1, ExpectedResult = HardwareLoad.Custom)]
    [TestCase((HardwareLoad)99, 1, ExpectedResult = HardwareLoad.None)]
    public HardwareLoad Step_WrapsBothWays(HardwareLoad load, int direction)
        => SettingChoices.Step(SettingChoices.HardwareLoads, load, direction);

    [TestCase(0, 4, 1, ExpectedResult = 1)]
    [TestCase(4, 4, 1, ExpectedResult = 0)]
    [TestCase(0, 4, -1, ExpectedResult = 4)]
    [TestCase(9, 4, -1, ExpectedResult = 0)]
    public int StepCpuThreads_WrapsThroughAllCores(int threads, int cores, int direction)
        => SettingChoices.StepCpuThreads(threads, cores, direction);

    [TestCase(HardwareLoad.High, ExpectedResult = HardwareLoad.High)]
    [TestCase((HardwareLoad)99, ExpectedResult = HardwareLoad.Medium)]
    public HardwareLoad HardwareLoadOrDefault_FallsBackToMedium(HardwareLoad load) => SettingChoices.HardwareLoadOrDefault(load);

    [TestCase("embers", ExpectedResult = "embers")]
    [TestCase("gone", ExpectedResult = "particle-storm")]
    [TestCase(null, ExpectedResult = "particle-storm")]
    public string ScreensaverIdOrDefault_FallsBack(string? id) => SettingChoices.ScreensaverIdOrDefault(id!);
}
