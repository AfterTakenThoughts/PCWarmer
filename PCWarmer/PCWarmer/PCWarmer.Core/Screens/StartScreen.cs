using PCWarmer.Core.Localization;
using Microsoft.Xna.Framework;

namespace PCWarmer.Screens;

/// <summary>
/// The first screen PCWarmer shows. Its main button starts warming the room: it opens the screensaver
/// picker, which asks for confirmation and then runs the CPU and GPU load.
/// </summary>
class StartScreen : MenuScreen
{
    public StartScreen()
        : base("PCWarmer")
    {
        MenuEntry warmMenuEntry = new MenuEntry("Start");
        MenuEntry settingsMenuEntry = new MenuEntry(Resources.Settings);
        MenuEntry exitMenuEntry = new MenuEntry(Resources.Exit);

        warmMenuEntry.Selected += WarmMenuEntrySelected;
        settingsMenuEntry.Selected += (sender, e) => ScreenManager.AddScreen(new SettingsScreen(), e.PlayerIndex);
        exitMenuEntry.Selected += OnCancel;

        MenuEntries.Add(warmMenuEntry);
        MenuEntries.Add(settingsMenuEntry);
        MenuEntries.Add(exitMenuEntry);
    }

    private void WarmMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        => ScreenManager.AddScreen(new ScreensaverPickerScreen(), e.PlayerIndex);

    /// <summary>
    /// When the user cancels the start screen, ask if they want to exit.
    /// </summary>
    protected override void OnCancel(PlayerIndex playerIndex)
    {
        var confirmExitMessageBox = new MessageBoxScreen(Resources.ExitQuestion);
        confirmExitMessageBox.Accepted += (sender, e) => ScreenManager.Game.Exit();
        ScreenManager.AddScreen(confirmExitMessageBox, playerIndex);
    }
}
