using System;
using System.Diagnostics;
using PCWarmer.Core.Localization;
using PCWarmer.Core.Screensavers;
using PCWarmer.Core.Settings;
using PCWarmer.Core.Stress;

namespace PCWarmer.Screens;

/// <summary>
/// Two sections: "Select Computer Load" and "Select a Screensaver". The hardware load is None, Low, Medium, High (presets) or Custom,
/// which shows a Settings entry for the custom options. Picking a screensaver asks the user to confirm, then starts
/// the run. When the run ends, the user is back on the start screen.
/// </summary>
class ScreensaverPickerScreen : MenuScreen
{
    private readonly MenuEntry hardwareLoadMenuEntry = new(string.Empty);
    private readonly MenuEntry settingsMenuEntry = new(Resources.Settings);

    private SettingsManager<PCWarmerSettings> settingsManager;

    private PCWarmerSettings Settings => settingsManager.Settings;

    public ScreensaverPickerScreen()
        : base(string.Empty)
    {
        hardwareLoadMenuEntry.Selected += (sender, e) =>
        {
            Settings.HardwareLoad = SettingChoices.NextHardwareLoad(Settings.HardwareLoad);
            settingsManager.Save();
            RefreshHardwareLoad();
        };
        settingsMenuEntry.Selected += (sender, e) => ScreenManager.AddScreen(new SettingsScreen(), e.PlayerIndex);

        MenuEntries.Add(new MenuHeading("Select Computer Load"));
        MenuEntries.Add(hardwareLoadMenuEntry);

        MenuEntries.Add(new MenuHeading(string.Empty));
        MenuEntries.Add(new MenuHeading("Select a Screensaver"));

        foreach (ScreensaverEntry entry in ScreensaverCatalog.Entries)
        {
            var menuEntry = new MenuEntry(entry.Name);
            menuEntry.Selected += (sender, e) => Confirm(entry, e);
            MenuEntries.Add(menuEntry);
        }

        var backMenuEntry = new MenuEntry(Resources.Back);
        backMenuEntry.Selected += OnCancel;
        MenuEntries.Add(backMenuEntry);
    }

    public override void LoadContent()
    {
        base.LoadContent();

        settingsManager ??= ScreenManager.Game.Services.GetService<SettingsManager<PCWarmerSettings>>();
        RefreshHardwareLoad();
    }

    /// <summary>Shows the current hardware load, and the Settings entry only when it's Custom.</summary>
    private void RefreshHardwareLoad()
    {
        HardwareLoad load = Settings.HardwareLoad;
        hardwareLoadMenuEntry.Text = $"Hardware Load: {HardwareLoadText(load)}";

        bool showSettings = load == HardwareLoad.Custom;
        bool settingsShown = MenuEntries.Contains(settingsMenuEntry);

        // The Settings entry goes right under Hardware Load, which is always the selected entry when this changes.
        if (showSettings && !settingsShown)
            MenuEntries.Insert(MenuEntries.IndexOf(hardwareLoadMenuEntry) + 1, settingsMenuEntry);
        else if (!showSettings && settingsShown)
            MenuEntries.Remove(settingsMenuEntry);
    }

    private static string HardwareLoadText(HardwareLoad load) => load switch
    {
        HardwareLoad.None => "None (screensaver only)",
        _ when Enum.IsDefined(load) => load.ToString(),
        _ => HardwareLoad.Medium.ToString(),
    };

    private void Confirm(ScreensaverEntry entry, PlayerIndexEventArgs e)
    {
        string warning = Settings.HardwareLoad == HardwareLoad.None
            ? "Only the screensaver will run, with no extra CPU load."
            : "Your CPU and GPU will run hard and your PC will get hot.";
        string message = $"Really warm your room with {entry.Name}?{Environment.NewLine}{warning}";

        var confirmBox = new MessageBoxScreen(message);
        confirmBox.Accepted += (sender, accepted) => StartWarming(entry, accepted);
        ScreenManager.AddScreen(confirmBox, e.PlayerIndex);
    }

    private void StartWarming(ScreensaverEntry entry, PlayerIndexEventArgs e)
    {
        RunPlan plan = RunPlan.For(Settings.HardwareLoad, Settings);

        var clock = Stopwatch.StartNew();
        var session = new StressSession(plan.CreateCpuLoad(), () => clock.Elapsed);
        session.Start(plan.CpuDutyCycle, plan.Duration);

        // Leave the picker so stopping the screensaver lands back on the start screen.
        ExitScreen();
        ScreenManager.AddScreen(
            new ScreensaverScreen(session, ScreensaverCatalog.Create(entry.Id, plan.ScreensaverLevel), plan.FpsCap, plan.FullScreen),
            e.PlayerIndex);
    }
}
