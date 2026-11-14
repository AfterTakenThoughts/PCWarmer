using System;
using System.Diagnostics;
using PCWarmer.Core.Localization;
using PCWarmer.Core.Screensavers;
using PCWarmer.Core.Settings;
using PCWarmer.Core.Stress;

namespace PCWarmer.Screens;

/// <summary>
/// Lists the screensavers. Picking one asks the user to confirm, then starts a warming run with the saved
/// settings and shows that screensaver. When the run ends, the user is back on the start screen.
/// </summary>
class ScreensaverPickerScreen : MenuScreen
{
    public ScreensaverPickerScreen()
        : base("Choose a Screensaver")
    {
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

    private void Confirm(ScreensaverEntry entry, PlayerIndexEventArgs e)
    {
        string message = $"Really warm your room with {entry.Name}?"
            + $"{Environment.NewLine}Your CPU and GPU will run hard and your PC will get hot.";

        var confirmBox = new MessageBoxScreen(message);
        confirmBox.Accepted += (sender, accepted) => StartWarming(entry, accepted);
        ScreenManager.AddScreen(confirmBox, e.PlayerIndex);
    }

    private void StartWarming(ScreensaverEntry entry, PlayerIndexEventArgs e)
    {
        PCWarmerSettings settings = ScreenManager.Game.Services.GetService<SettingsManager<PCWarmerSettings>>().Settings;

        // CpuStressService treats 0 or out-of-range thread counts as one worker per core.
        var clock = Stopwatch.StartNew();
        var session = new StressSession(new CpuStressService(settings.CpuThreads), () => clock.Elapsed);
        session.Start(
            StressProfile.For(SettingChoices.CpuLoadOrDefault(settings.CpuLoad)).CpuDutyCycle,
            SettingChoices.RunDuration(settings.DurationMinutes));

        // Leave the picker so stopping the screensaver lands back on the start screen.
        ExitScreen();
        ScreenManager.AddScreen(
            new ScreensaverScreen(session, ScreensaverCatalog.Create(entry.Id), settings.FpsCap),
            e.PlayerIndex);
    }
}
