using PCWarmer.Core.Localization;
using Microsoft.Xna.Framework;

namespace PCWarmer.Screens;

/// <summary>
/// The first screen PCWarmer shows. Its main button starts warming the room: it opens the screensaver
/// picker, where the hardware load (and, for Custom, the settings) is chosen.
/// </summary>
class StartScreen : MenuScreen
{
    public StartScreen()
        : base("PCWarmer")
    {
        MenuEntry warmMenuEntry = new MenuEntry("Start");
        MenuEntry exitMenuEntry = new MenuEntry(Resources.Exit);

        warmMenuEntry.Selected += WarmMenuEntrySelected;
        exitMenuEntry.Selected += OnCancel;

        MenuEntries.Add(warmMenuEntry);
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
