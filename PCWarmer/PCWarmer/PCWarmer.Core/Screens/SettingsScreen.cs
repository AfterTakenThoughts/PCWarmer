using System;
using PCWarmer.Core.Localization;
using PCWarmer.Core.Settings;
using Microsoft.Xna.Framework;

namespace PCWarmer.Screens;

/// <summary>
/// Options for warming runs: how hard the CPU works (Low, Medium or High), the screensaver's frame-rate cap, how long a run lasts,
/// how many CPU threads it uses, and whether the screensaver is full screen. The menus themselves always stay windowed. Selecting an entry steps it to its next value,
/// which is saved straight away. Reset to Defaults (after a confirmation) puts them all back.
/// </summary>
class SettingsScreen : MenuScreen
{
    private readonly MenuEntry fullscreenMenuEntry = new(string.Empty);
    private readonly MenuEntry cpuLoadMenuEntry = new(string.Empty);
    private readonly MenuEntry frameRateMenuEntry = new(string.Empty);
    private readonly MenuEntry durationMenuEntry = new(string.Empty);
    private readonly MenuEntry cpuThreadsMenuEntry = new(string.Empty);
    private readonly MenuEntry resetMenuEntry = new("Reset to Defaults");
    private readonly MenuEntry backMenuEntry = new(string.Empty);

    private SettingsManager<PCWarmerSettings> settingsManager;

    private PCWarmerSettings Settings => settingsManager.Settings;

    public SettingsScreen()
        : base(Resources.Settings)
    {
        fullscreenMenuEntry.Selected += (s, e) => Change(() => Settings.FullScreen = !Settings.FullScreen);
        cpuLoadMenuEntry.Selected += (s, e) => Change(() => Settings.CpuLoad = SettingChoices.NextCpuLoad(Settings.CpuLoad));
        frameRateMenuEntry.Selected += (s, e) => Change(() => Settings.FpsCap = SettingChoices.NextFpsCap(Settings.FpsCap));
        durationMenuEntry.Selected += (s, e) => Change(() => Settings.DurationMinutes = SettingChoices.NextDuration(Settings.DurationMinutes));
        cpuThreadsMenuEntry.Selected += (s, e) => Change(() => Settings.CpuThreads = SettingChoices.NextCpuThreads(Settings.CpuThreads, Environment.ProcessorCount));
        resetMenuEntry.Selected += ResetMenuEntrySelected;
        backMenuEntry.Selected += OnCancel;

        MenuEntries.Add(cpuLoadMenuEntry);
        MenuEntries.Add(frameRateMenuEntry);
        MenuEntries.Add(durationMenuEntry);
        MenuEntries.Add(cpuThreadsMenuEntry);
        MenuEntries.Add(fullscreenMenuEntry);
        MenuEntries.Add(resetMenuEntry);
        MenuEntries.Add(backMenuEntry);
    }

    public override void LoadContent()
    {
        base.LoadContent();

        settingsManager ??= ScreenManager.Game.Services.GetService<SettingsManager<PCWarmerSettings>>();

        RefreshText();
    }

    private void ResetMenuEntrySelected(object sender, PlayerIndexEventArgs e)
    {
        var confirmBox = new MessageBoxScreen("Reset all settings to their defaults?");
        confirmBox.Accepted += (s, accepted) => Change(Settings.ResetToDefaults);
        ScreenManager.AddScreen(confirmBox, e.PlayerIndex);
    }

    /// <summary>Applies a change to the settings, saves them and shows the new values.</summary>
    private void Change(Action change)
    {
        change();
        settingsManager.Save();
        RefreshText();
    }

    private void RefreshText()
    {
        // Show what a run will actually use, so a bad hand-edited value shows its fallback.
        cpuLoadMenuEntry.Text = $"CPU Load: {SettingChoices.CpuLoadOrDefault(Settings.CpuLoad)}";

        frameRateMenuEntry.Text = $"Frame Rate: {ScreensaverScreen.FpsCapText(Settings.FpsCap)}";

        durationMenuEntry.Text = SettingChoices.RunDuration(Settings.DurationMinutes) is { } duration
            ? $"Duration: {duration.TotalMinutes:0} min"
            : "Duration: Unlimited";

        int threads = Settings.CpuThreads;
        int cores = Environment.ProcessorCount;
        cpuThreadsMenuEntry.Text = threads >= 1 && threads <= cores
            ? $"CPU Threads: {threads}"
            : $"CPU Threads: All Cores ({cores})";

        fullscreenMenuEntry.Text = $"Screensaver: {(Settings.FullScreen ? "Full Screen" : "Windowed")}";
        backMenuEntry.Text = Resources.Back;
    }
}
