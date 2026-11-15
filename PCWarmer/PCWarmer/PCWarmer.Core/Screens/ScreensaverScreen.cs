using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PCWarmer.Core.Display;
using PCWarmer.Core.Inputs;
using PCWarmer.Core.Screensavers;
using PCWarmer.Core.Stress;

namespace PCWarmer.Screens;

/// <summary>
/// Shows a screensaver while a warming run loads the CPU, with a HUD of what the run is doing.
/// H, gamepad Y or the Hide UI button hide the HUD (and the mouse cursor) so it looks like a real screensaver; a
/// "Press any key to show UI" hint shows for a few seconds, then fades out. While hidden, any key, click or button shows
/// the HUD again. With the HUD showing, any other key, click or button stops the run and returns to the previous screen;
/// so does the run's time running out. Hiding isn't remembered: every run starts with the HUD showing.
/// </summary>
class ScreensaverScreen : GameScreen
{
    private const string HideButtonText = "Hide UI (H)";
    private const int HideButtonPadding = 6;
    private const string ShowUiHint = "Press any key to show UI";

    private static readonly Buttons[] WatchedButtons =
    {
        Buttons.A, Buttons.B, Buttons.X, Buttons.Y, Buttons.Start, Buttons.Back,
    };

    private readonly StressSession session;
    private readonly IScreensaver screensaver;
    private readonly FpsCap fpsCap;
    private readonly bool fullScreen;
    private readonly FrameCounter frameCounter = new();
    private readonly StringBuilder hud = new();
    private readonly List<Keys> newKeys = new();
    private readonly List<Buttons> newButtons = new();

    private Texture2D blankTexture;
    private bool hudVisible = true;
    private readonly HintFade showUiHint = new(hold: TimeSpan.FromSeconds(3), fade: TimeSpan.FromSeconds(1));
    private Rectangle hideButtonBounds;
    private bool savedIsMouseVisible;

    private ContentManager content;
    private TimeSpan savedInactiveSleepTime;
    private bool savedIsFixedTimeStep;
    private TimeSpan savedTargetElapsedTime;
    private bool savedVSync;
    private DisplaySettings savedDisplay;

    /// <param name="session">A started run. This screen stops and disposes it when it closes.</param>
    /// <param name="screensaver">The graphics load to show. This screen loads and unloads it.</param>
    /// <param name="fpsCap">The frame-rate cap while this screen is open. The previous frame rate comes back when it closes.</param>
    /// <param name="fullScreen">Whether to cover the screen (borderless, at the desktop resolution) while this screen is open.</param>
    public ScreensaverScreen(StressSession session, IScreensaver screensaver, FpsCap fpsCap, bool fullScreen)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.screensaver = screensaver ?? throw new ArgumentNullException(nameof(screensaver));
        this.fpsCap = fpsCap;
        this.fullScreen = fullScreen;

        TransitionOnTime = TimeSpan.FromSeconds(0.5);
        TransitionOffTime = TimeSpan.FromSeconds(0.5);
    }

    public override void LoadContent()
    {
        base.LoadContent();

        content ??= new ContentManager(ScreenManager.Game.Services, "Content");

        Game game = ScreenManager.Game;
        GraphicsDeviceManager graphics = game.Services.GetService<GraphicsDeviceManager>();

        // Keep rendering at full speed when the window loses focus: that's when people leave it running.
        savedIsMouseVisible = game.IsMouseVisible;
        blankTexture = content.Load<Texture2D>("Sprites/blank");

        savedInactiveSleepTime = game.InactiveSleepTime;
        game.InactiveSleepTime = TimeSpan.Zero;

        savedIsFixedTimeStep = game.IsFixedTimeStep;
        savedTargetElapsedTime = game.TargetElapsedTime;
        savedVSync = graphics.SynchronizeWithVerticalRetrace;
        savedDisplay = new DisplaySettings(
            graphics.IsFullScreen,
            graphics.HardwareModeSwitch,
            new Point(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));

        DisplayMode desktop = ScreenManager.GraphicsDevice.Adapter.CurrentDisplayMode;
        ApplyGraphics(
            FrameRatePolicy.For(fpsCap),
            ScreensaverDisplay.For(fullScreen, new Point(desktop.Width, desktop.Height), savedDisplay.BackBufferSize));

        // Load after the display change, so the screensaver lays itself out for the full-screen size.
        screensaver.LoadContent(content, ScreenManager.GraphicsDevice);
    }

    public override void UnloadContent()
    {
        ScreenManager.Game.InactiveSleepTime = savedInactiveSleepTime;
        ScreenManager.Game.IsMouseVisible = savedIsMouseVisible;
        ApplyGraphics(new FrameRateSettings(savedIsFixedTimeStep, savedVSync, savedTargetElapsedTime), savedDisplay);

        screensaver.UnloadContent();
        session.Dispose();
        content?.Unload();

        base.UnloadContent();
    }

    public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
    {
        // Update the screensaver and session first: when the fade-out ends, base.Update removes this screen,
        // which unloads the screensaver and disposes the session.
        screensaver.Update(gameTime);
        showUiHint.Update(gameTime.ElapsedGameTime);

        session.Update();
        if (!session.IsRunning && !IsExiting)
            ExitScreen();

        base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
    }

    public override void HandleInput(GameTime gameTime, InputState inputState)
    {
        if (IsExiting)
            return;

        CollectNewPresses(inputState);
        Vector2? click = NewClick(inputState);

        switch (ScreensaverInput.Decide(newKeys, newButtons, click, hudVisible, hideButtonBounds))
        {
            case ScreensaverAction.ToggleHud:
                hudVisible = !hudVisible;
                ScreenManager.Game.IsMouseVisible = hudVisible;
                if (hudVisible)
                    showUiHint.Hide();
                else
                    showUiHint.Show();
                break;

            case ScreensaverAction.Stop:
                session.Stop();
                ExitScreen();
                break;
        }
    }

    public override void Draw(GameTime gameTime)
    {
        frameCounter.Add(gameTime.ElapsedGameTime);

        ScreenManager.GraphicsDevice.Clear(Color.Black);

        SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
        screensaver.Draw(spriteBatch, gameTime);

        if (hudVisible)
            DrawHud(spriteBatch);
        else if (showUiHint.Opacity > 0)
            DrawShowUiHint(spriteBatch);
    }

    private void DrawShowUiHint(SpriteBatch spriteBatch)
    {
        SpriteFont font = ScreenManager.Font;
        Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
        Vector2 size = font.MeasureString(ShowUiHint);
        var position = new Vector2((viewport.Width - size.X) / 2, (viewport.Height - size.Y) / 2);

        spriteBatch.Begin();
        spriteBatch.DrawString(font, ShowUiHint, position, Color.White * (showUiHint.Opacity * TransitionAlpha));
        spriteBatch.End();
    }

    private void DrawHud(SpriteBatch spriteBatch)
    {
        SpriteFont font = ScreenManager.Font;
        Color textColor = Color.White * TransitionAlpha;

        string text = BuildHud();
        var textPosition = new Vector2(16, 16);
        Vector2 textSize = font.MeasureString(text);

        // The Hide UI button sits under the HUD text, in window pixels (no menu scaling), like the mouse position.
        Vector2 labelSize = font.MeasureString(HideButtonText);
        hideButtonBounds = new Rectangle(
            (int)textPosition.X,
            (int)(textPosition.Y + textSize.Y + 8),
            (int)labelSize.X + HideButtonPadding * 2,
            (int)labelSize.Y + HideButtonPadding * 2);

        spriteBatch.Begin();
        spriteBatch.DrawString(font, text, textPosition, textColor);
        spriteBatch.Draw(blankTexture, hideButtonBounds, new Color(230, 110, 30) * (0.85f * TransitionAlpha));
        spriteBatch.DrawString(font, HideButtonText,
            new Vector2(hideButtonBounds.X + HideButtonPadding, hideButtonBounds.Y + HideButtonPadding), textColor);
        spriteBatch.End();
    }

    private void ApplyGraphics(FrameRateSettings frameRate, DisplaySettings display)
    {
        Game game = ScreenManager.Game;
        GraphicsDeviceManager graphics = game.Services.GetService<GraphicsDeviceManager>();

        game.IsFixedTimeStep = frameRate.IsFixedTimeStep;
        game.TargetElapsedTime = frameRate.TargetElapsedTime;
        graphics.SynchronizeWithVerticalRetrace = frameRate.VSync;

        graphics.HardwareModeSwitch = display.HardwareModeSwitch;
        graphics.IsFullScreen = display.IsFullScreen;
        graphics.PreferredBackBufferWidth = display.BackBufferSize.X;
        graphics.PreferredBackBufferHeight = display.BackBufferSize.Y;

        graphics.ApplyChanges();
    }

    private string BuildHud()
    {
        hud.Clear();
        hud.Append("FPS: ").Append(frameCounter.Fps.ToString("0")).AppendLine();
        hud.Append("Frame rate cap: ").Append(FpsCapText(fpsCap)).AppendLine();
        hud.Append("CPU load: ").Append((session.CpuDutyCycle * 100).ToString("0")).Append("% on ")
           .Append(session.CpuThreads).AppendLine(" threads");
        hud.Append("Particles: ").Append(screensaver.LiveParticles).AppendLine();
        hud.Append("Warming for: ").Append(session.Elapsed.ToString(@"hh\:mm\:ss")).AppendLine();
        hud.Append("Time left: ").Append(session.Remaining is { } left ? left.ToString(@"hh\:mm\:ss") : "Unlimited").AppendLine();
        hud.Append("Press H to hide this, any other key to stop");
        return hud.ToString();
    }

    /// <summary>How a frame-rate cap reads in menus and the HUD.</summary>
    public static string FpsCapText(FpsCap cap) => cap switch
    {
        FpsCap.Fps30 => "30",
        FpsCap.Fps144 => "144",
        FpsCap.Uncapped => "Uncapped",
        _ => "60",
    };

    /// <summary>
    /// Fills <see cref="newKeys"/> and <see cref="newButtons"/> with what went down this frame. A key still held
    /// from the menu press that opened this screen doesn't count.
    /// </summary>
    private void CollectNewPresses(InputState input)
    {
        newKeys.Clear();
        Keys[] lastKeys = input.LastKeyboardStates[0].GetPressedKeys();
        foreach (Keys key in input.CurrentKeyboardStates[0].GetPressedKeys())
        {
            if (Array.IndexOf(lastKeys, key) < 0)
                newKeys.Add(key);
        }

        newButtons.Clear();
        foreach (Buttons button in WatchedButtons)
        {
            if (input.IsNewButtonPress(button, ControllingPlayer, out _))
                newButtons.Add(button);
        }
    }

    /// <summary>Where a left or right click went down this frame, in window pixels; null if none. Moving the mouse doesn't count.</summary>
    private static Vector2? NewClick(InputState input)
    {
        MouseState current = input.CurrentMouseState;
        MouseState last = input.LastMouseState;

        bool clicked = IsNewClick(current.LeftButton, last.LeftButton) || IsNewClick(current.RightButton, last.RightButton);
        return clicked ? new Vector2(current.X, current.Y) : null;
    }

    private static bool IsNewClick(ButtonState current, ButtonState last)
        => current == ButtonState.Pressed && last == ButtonState.Released;
}
