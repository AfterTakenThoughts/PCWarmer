using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using PCWarmer.Core.Inputs;
using PCWarmer.Core.Ui;

namespace PCWarmer.Screens;

/// <summary>
/// A question in a dark rounded box over the dimmed screen, with a Yes button and a Cancel button. Enter or gamepad A
/// picks the focused button (Yes at first), Esc or B cancels, and Left/Right move between the two.
/// </summary>
class ConfirmScreen : MyraScreen
{
    private readonly string title;
    private readonly string detail;
    private readonly string yesText;
    private readonly bool yesIsAccent;
    private bool yesFocused = true;

    public event EventHandler<PlayerIndexEventArgs> Accepted;

    public event EventHandler<PlayerIndexEventArgs> Cancelled;

    /// <param name="yesIsAccent">True only for "Yes, start": the one red button. Other questions get a plain Yes.</param>
    public ConfirmScreen(string title, string detail, string yesText, bool yesIsAccent)
    {
        this.title = title;
        this.detail = detail;
        this.yesText = yesText;
        this.yesIsAccent = yesIsAccent;

        IsPopup = true;
        TransitionOnTime = TimeSpan.FromSeconds(0.2);
        TransitionOffTime = TimeSpan.FromSeconds(0.2);
    }

    protected override Widget BuildRoot(float scale)
    {
        int Px(float n) => (int)MathF.Round(n * scale);

        var content = new VerticalStackPanel { Spacing = Px(8), HorizontalAlignment = HorizontalAlignment.Center };
        content.Widgets.Add(Center(Ui.Text(title, Fonts.SemiBold(Px(15)), UiTheme.TextStrong)));
        if (!string.IsNullOrEmpty(detail))
            content.Widgets.Add(Center(Ui.Text(detail, Fonts.Regular(Px(12)), UiTheme.TextMuted)));

        var buttons = new HorizontalStackPanel { Spacing = Px(10), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, Px(10), 0, 0) };
        buttons.Widgets.Add(Button(yesText, yesIsAccent, yesFocused, Px, () => Finish(accepted: true, PlayerIndex.One)));
        buttons.Widgets.Add(Button("Cancel", false, !yesFocused, Px, () => Finish(accepted: false, PlayerIndex.One)));
        content.Widgets.Add(buttons);

        Panel box = Ui.Box(content, UiTheme.Surface, UiTheme.Border, new Thickness(Px(22)), Px(14));
        box.HorizontalAlignment = HorizontalAlignment.Center;
        box.VerticalAlignment = VerticalAlignment.Center;

        var root = new Panel();
        root.Widgets.Add(new BoxShape { Fill = Color.Black * 0.6f, Radius = 1 });
        root.Widgets.Add(box);
        return root;
    }

    public override void HandleInput(GameTime gameTime, InputState input)
    {
        if (IsExiting)
            return;

        if (input.IsNewKeyPress(Keys.Left, ControllingPlayer, out _) || input.IsNewKeyPress(Keys.Right, ControllingPlayer, out _)
            || input.IsNewButtonPress(Buttons.DPadLeft, ControllingPlayer, out _) || input.IsNewButtonPress(Buttons.DPadRight, ControllingPlayer, out _))
        {
            yesFocused = !yesFocused;
            Rebuild();
        }

        if (input.IsMenuSelect(ControllingPlayer, out PlayerIndex player))
            Finish(yesFocused, player);
        else if (input.IsMenuCancel(ControllingPlayer, out player))
            Finish(accepted: false, player);
    }

    private void Finish(bool accepted, PlayerIndex player)
    {
        if (IsExiting)
            return;

        (accepted ? Accepted : Cancelled)?.Invoke(this, new PlayerIndexEventArgs(player));
        ExitScreen();
    }

    private static Widget Center(Widget widget)
    {
        widget.HorizontalAlignment = HorizontalAlignment.Center;
        return widget;
    }

    private Widget Button(string text, bool accent, bool focused, Func<float, int> px, Action click)
    {
        Color fill = accent ? UiTheme.Accent : UiTheme.SurfaceSelected;
        Color border = focused ? UiTheme.BorderFocused : accent ? UiTheme.Accent : UiTheme.SecondaryBorder;
        Panel button = Ui.Box(Ui.Text(text, Fonts.SemiBold(px(13)), accent ? Color.White : UiTheme.Text),
            fill, border, new Thickness(px(24), px(9)), px(UiTheme.CornerRadius), UiTheme.BorderFocused);
        OnClick(button, () => click());
        return button;
    }
}
