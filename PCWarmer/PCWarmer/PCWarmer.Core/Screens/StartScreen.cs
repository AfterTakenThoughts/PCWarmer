using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using PCWarmer.Core.Display;
using PCWarmer.Core.Inputs;
using PCWarmer.Core.Localization;
using PCWarmer.Core.Screensavers;
using PCWarmer.Core.Settings;
using PCWarmer.Core.Stress;
using PCWarmer.Core.Ui;

namespace PCWarmer.Screens;

/// <summary>
/// The only menu screen. Left: the project name, Intensity (None, Low, Medium, High, Custom), the Custom options when
/// picked, the screensaver tiles and Exit. Right: the big red Start button, which never moves or changes size, with Run
/// length, Display and Reset settings under it. Every change is saved straight away. Start asks "Really warm your
/// room?" first, except for None.
/// </summary>
class StartScreen : MyraScreen
{
    private static readonly (HardwareLoad Load, string Text)[] Segments =
    {
        (HardwareLoad.None, "None"), (HardwareLoad.Low, "Low"), (HardwareLoad.Medium, "Medium"),
        (HardwareLoad.High, "High"), (HardwareLoad.Custom, "Custom"),
    };

    private static readonly (string Id, string ShortName)[] ShortNames =
    {
        ("particle-storm", "Storm"), ("embers", "Embers"), ("starfield", "Starfield"), ("bouncing-orbs", "Orbs"),
    };

    private SettingsManager<PCWarmerSettings> settingsManager;
    private StartMenuModel model;

    private PCWarmerSettings Settings => settingsManager.Settings;

    public StartScreen()
    {
        TransitionOnTime = TimeSpan.FromSeconds(0.5);
        TransitionOffTime = TimeSpan.FromSeconds(0.5);
    }

    public override void LoadContent()
    {
        settingsManager ??= ScreenManager.Game.Services.GetService<SettingsManager<PCWarmerSettings>>();
        model ??= new StartMenuModel(Settings, Environment.ProcessorCount);

        base.LoadContent();
    }

    public override void HandleInput(GameTime gameTime, InputState input)
    {
        if (IsExiting)
            return;

        if (input.IsMenuUp(ControllingPlayer))
            Refocus(() => model.MoveFocus(-1));
        else if (input.IsMenuDown(ControllingPlayer))
            Refocus(() => model.MoveFocus(1));
        else if (IsNew(input, Keys.Left, Buttons.DPadLeft))
            Change(() => model.ChangeValue(-1));
        else if (IsNew(input, Keys.Right, Buttons.DPadRight))
            Change(() => model.ChangeValue(1));
        else if (input.IsMenuSelect(ControllingPlayer, out PlayerIndex player))
            Activate(player);
        else if (input.IsMenuCancel(ControllingPlayer, out player))
            AskToExit(player);
    }

    protected override Widget BuildRoot(float scale)
    {
        int Px(float n) => Math.Max(1, (int)MathF.Round(n * scale));

        // The root fills the window and its one row fills the height, so the right column (and the Start button) stays
        // centred in the window however tall the left column gets when Custom opens.
        var root = new Grid
        {
            ColumnSpacing = Px(24),
            Padding = new Thickness(Px(28), Px(22)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        root.RowsProportions.Add(new Proportion(ProportionType.Fill));
        root.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1.35f));
        root.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1f));

        Widget left = BuildLeftColumn(Px);
        Grid.SetColumn(left, 0);
        root.Widgets.Add(left);

        Widget right = BuildRightColumn(Px);
        Grid.SetColumn(right, 1);
        root.Widgets.Add(right);

        return root;
    }

    private Widget BuildLeftColumn(Func<float, int> px)
    {
        bool custom = model.VisibleRows.Contains(StartMenuRow.CpuLoad);
        var column = new VerticalStackPanel { Spacing = px(custom ? 7 : 12), VerticalAlignment = VerticalAlignment.Center };

        // The project name, written exactly "PCWarmer" (never all caps).
        column.Widgets.Add(Ui.Text("PCWarmer", Fonts.SemiBold(px(22)), UiTheme.TextStrong));

        column.Widgets.Add(Section("INTENSITY", BuildIntensity(px), px));
        if (custom)
            column.Widgets.Add(BuildRows(px, StartMenuRow.CpuLoad, StartMenuRow.ScreensaverDetail, StartMenuRow.CpuThreads, StartMenuRow.FrameRate));
        column.Widgets.Add(Section("SCREENSAVER", BuildTiles(px, compact: custom), px));
        column.Widgets.Add(SecondaryButton("Exit", StartMenuRow.Exit, px));
        return column;
    }

    private Widget BuildRightColumn(Func<float, int> px)
    {
        var column = new VerticalStackPanel { Spacing = px(14), VerticalAlignment = VerticalAlignment.Center };

        // Fixed size whatever else is picked: the button is the screen's focus.
        // The red disc is 120 px across; the panel is bigger so its glow has room to show around it.
        var start = new Panel { Width = px(210), Height = px(210), HorizontalAlignment = HorizontalAlignment.Center };
        start.Widgets.Add(new EngineStartDisc { Diameter = px(120), Focused = model.Focused == StartMenuRow.Start });
        Label label = Centered(Ui.Text("Start", Fonts.SemiBold(px(21)), Color.White)) as Label;
        start.Widgets.Add(label);
        OnClick(start, () => ClickRow(StartMenuRow.Start));
        column.Widgets.Add(start);

        column.Widgets.Add(BuildRows(px, StartMenuRow.RunLength, StartMenuRow.Display));
        column.Widgets.Add(Centered(SecondaryButton("Reset settings", StartMenuRow.ResetSettings, px)));
        return column;
    }

    private Widget Section(string label, Widget content, Func<float, int> px)
    {
        var section = new VerticalStackPanel { Spacing = px(5) };
        section.Widgets.Add(Ui.Text(label, Fonts.Regular(px(10)), UiTheme.TextMuted));
        section.Widgets.Add(content);
        return section;
    }

    private Widget BuildIntensity(Func<float, int> px)
    {
        var segments = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
        HardwareLoad picked = SettingChoices.HardwareLoadOrDefault(Settings.HardwareLoad);

        for (int i = 0; i < Segments.Length; i++)
        {
            (HardwareLoad load, string text) = Segments[i];
            segments.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1f));

            bool isPicked = load == picked;
            Panel segment = Ui.Box(Centered(Ui.Text(text, Fonts.Regular(px(11)), isPicked ? Color.White : UiTheme.Text)),
                isPicked ? UiTheme.SurfaceSelected : Color.Transparent, Color.Transparent, new Thickness(px(4), px(7)), px(8));
            segment.HorizontalAlignment = HorizontalAlignment.Stretch;
            OnClick(segment, () => Change(() => { model.SetIntensity(load); return true; }));
            Grid.SetColumn(segment, i);
            segments.Widgets.Add(segment);
        }

        return Outlined(segments, StartMenuRow.Intensity, px, padding: 2);
    }

    private Widget BuildTiles(Func<float, int> px, bool compact)
    {
        var tiles = new Grid { ColumnSpacing = px(6), RowSpacing = px(6), HorizontalAlignment = HorizontalAlignment.Stretch };
        int columns = compact ? 4 : 2;
        for (int c = 0; c < columns; c++)
            tiles.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1f));

        string picked = SettingChoices.ScreensaverIdOrDefault(Settings.ScreensaverId);
        bool rowFocused = model.Focused == StartMenuRow.Screensaver;

        for (int i = 0; i < ScreensaverCatalog.Entries.Count; i++)
        {
            ScreensaverEntry entry = ScreensaverCatalog.Entries[i];
            bool isPicked = entry.Id == picked;
            string name = compact ? Array.Find(ShortNames, n => n.Id == entry.Id).ShortName ?? entry.Name : entry.Name;

            Color border = isPicked && rowFocused ? UiTheme.BorderFocused : isPicked ? UiTheme.Accent * 0.5f : UiTheme.Border;
            Panel tile = Ui.Box(Centered(Ui.Text(name, Fonts.Regular(px(compact ? 10 : 11)), isPicked ? Color.White : UiTheme.Text)),
                isPicked ? UiTheme.SurfaceSelected : Color.Transparent, border,
                new Thickness(px(4), px(compact ? 5 : 8)), px(9), UiTheme.BorderFocused);
            tile.HorizontalAlignment = HorizontalAlignment.Stretch;
            OnClick(tile, () => Change(() => { model.SetScreensaver(entry.Id); return true; }));

            Grid.SetColumn(tile, i % columns);
            Grid.SetRow(tile, i / columns);
            tiles.Widgets.Add(tile);
        }

        return tiles;
    }

    /// <summary>A panel of name-on-the-left, ‹ value › on-the-right rows.</summary>
    private Widget BuildRows(Func<float, int> px, params StartMenuRow[] rows)
    {
        var list = new VerticalStackPanel { Spacing = px(2) };
        foreach (StartMenuRow row in rows)
            list.Widgets.Add(BuildRow(row, px));

        return Ui.Box(list, Color.Transparent, UiTheme.Border, new Thickness(px(4), px(3)), px(UiTheme.CornerRadius));
    }

    private Widget BuildRow(StartMenuRow row, Func<float, int> px)
    {
        bool focused = model.Focused == row;
        var line = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
        line.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        line.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        Label name = Ui.Text(RowName(row), Fonts.Regular(px(11)), UiTheme.Text);
        Grid.SetColumn(name, 0);
        line.Widgets.Add(name);

        var value = new HorizontalStackPanel { Spacing = px(6) };
        value.Widgets.Add(Arrow("‹", row, -1, px));
        value.Widgets.Add(Ui.Text(model.ValueText(row), Fonts.Regular(px(11)), focused ? Color.White : UiTheme.Text));
        value.Widgets.Add(Arrow("›", row, 1, px));
        Grid.SetColumn(value, 1);
        line.Widgets.Add(value);

        Panel box = Ui.Box(line, focused ? UiTheme.Surface : Color.Transparent, focused ? UiTheme.BorderFocused : Color.Transparent,
            new Thickness(px(8), px(4)), px(7));
        box.HorizontalAlignment = HorizontalAlignment.Stretch;
        OnClick(box, () => Refocus(() => model.FocusRow(row)));
        return box;
    }

    private Widget Arrow(string glyph, StartMenuRow row, int direction, Func<float, int> px)
    {
        Label arrow = Ui.Text(glyph, Fonts.SemiBold(px(13)), UiTheme.TextMuted);
        OnClick(arrow, () => Change(() => { model.FocusRow(row); return model.ChangeValue(direction); }));
        return arrow;
    }

    /// <summary>Exit and Reset settings: rounded, dark grey, thin light border, never red.</summary>
    private Widget SecondaryButton(string text, StartMenuRow row, Func<float, int> px)
    {
        bool focused = model.Focused == row;
        Panel button = Ui.Box(Ui.Text(text, Fonts.SemiBold(px(12)), UiTheme.Text), UiTheme.SurfaceSelected,
            focused ? UiTheme.BorderFocused : UiTheme.SecondaryBorder, new Thickness(px(18), px(7)), px(UiTheme.CornerRadius),
            UiTheme.BorderFocused);
        OnClick(button, () => ClickRow(row));
        return button;
    }

    private Widget Outlined(Widget content, StartMenuRow row, Func<float, int> px, int padding)
    {
        Panel box = Ui.Box(content, Color.Transparent, model.Focused == row ? UiTheme.BorderFocused : UiTheme.Border,
            new Thickness(px(padding)), px(UiTheme.CornerRadius));
        box.HorizontalAlignment = HorizontalAlignment.Stretch;
        return box;
    }

    private static Widget Centered(Widget widget)
    {
        widget.HorizontalAlignment = HorizontalAlignment.Center;
        return widget;
    }

    private static string RowName(StartMenuRow row) => row switch
    {
        StartMenuRow.CpuLoad => "CPU load",
        StartMenuRow.ScreensaverDetail => "Screensaver detail",
        StartMenuRow.CpuThreads => "CPU threads",
        StartMenuRow.FrameRate => "Frame rate",
        StartMenuRow.RunLength => "Run length",
        StartMenuRow.Display => "Display",
        _ => row.ToString(),
    };

    private bool IsNew(InputState input, Keys key, Buttons button)
        => input.IsNewKeyPress(key, ControllingPlayer, out _) || input.IsNewButtonPress(button, ControllingPlayer, out _);

    private void ClickRow(StartMenuRow row)
    {
        model.FocusRow(row);
        Activate(ControllingPlayer ?? PlayerIndex.One);
    }

    private void Refocus(Action move)
    {
        move();
        Rebuild();
    }

    /// <summary>Applies a change; saves if it changed a setting. Rebuilds either way, since focus may have moved.</summary>
    private void Change(Func<bool> change)
    {
        if (change())
            settingsManager.Save();
        Rebuild();
    }

    private void Activate(PlayerIndex player)
    {
        switch (model.Activate())
        {
            case StartMenuCommand.StartRun:
                AskToStart(player);
                break;
            case StartMenuCommand.Exit:
                AskToExit(player);
                break;
            case StartMenuCommand.ResetSettings:
                Ask("Reset all settings to their defaults?", string.Empty, "Reset", accent: false, player, () =>
                {
                    Settings.ResetToDefaults();
                    settingsManager.Save();
                });
                break;
            default:
                settingsManager.Save();   // a value row stepped forward
                break;
        }

        Rebuild();
    }

    private void AskToStart(PlayerIndex player)
    {
        HardwareLoad load = SettingChoices.HardwareLoadOrDefault(Settings.HardwareLoad);
        string id = SettingChoices.ScreensaverIdOrDefault(Settings.ScreensaverId);

        if (RunConfirmation.For(load, StartMenuModel.ScreensaverName(id)) is { } question)
            Ask(question.Title, question.Detail, "Yes, start", accent: true, player, () => StartRun(load, id, player));
        else
            StartRun(load, id, player);   // None only runs the screensaver, so it doesn't ask
    }

    private void AskToExit(PlayerIndex player)
        => Ask(Resources.ExitQuestion, string.Empty, "Exit", accent: false, player, () => ScreenManager.Game.Exit());

    /// <summary>Shows a confirm box. While it's open, this screen ignores every input.</summary>
    private void Ask(string title, string detail, string yesText, bool accent, PlayerIndex player, Action accepted)
    {
        var box = new ConfirmScreen(title, detail, yesText, accent);
        model.Locked = true;
        box.Accepted += (sender, e) => { model.Locked = false; accepted(); Rebuild(); };
        box.Cancelled += (sender, e) => { model.Locked = false; Rebuild(); };
        ScreenManager.AddScreen(box, player);
    }

    private void StartRun(HardwareLoad load, string screensaverId, PlayerIndex player)
    {
        RunPlan plan = RunPlan.For(load, Settings, Environment.ProcessorCount);

        var clock = Stopwatch.StartNew();
        var session = new StressSession(plan.CreateCpuLoad(), () => clock.Elapsed);
        session.Start(plan.CpuDutyCycle, plan.Duration);

        // This screen stays underneath, so stopping the screensaver comes back here.
        ScreenManager.AddScreen(
            new ScreensaverScreen(session, ScreensaverCatalog.Create(screensaverId, plan.ScreensaverLevel), plan.FpsCap,
                plan.FullScreen, KeepAwake.ForCurrentPlatform()),
            player);
    }
}
