using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PCWarmer.Core.Display;
using PCWarmer.Core.Inputs;
using PCWarmer.Core.Screensavers;
using PCWarmer.Core.Stress;
using PCWarmer.Core.Ui;

namespace PCWarmer.Screens;

/// <summary>
/// Shows a screensaver while a warming run loads the CPU, with a dashboard of what the run is doing. The dashboard shows
/// for a few seconds, then fades out so only the screensaver is on screen; moving the mouse brings it back. Any key,
/// click or gamepad button stops the run and returns to the previous screen; so does the run's time running out.
/// </summary>
class ScreensaverScreen : GameScreen
{

    private static readonly Buttons[] WatchedButtons =
    {
        Buttons.A, Buttons.B, Buttons.X, Buttons.Y, Buttons.Start, Buttons.Back,
    };

    private readonly StressSession session;
    private readonly IScreensaver screensaver;
    private readonly FpsCap fpsCap;
    private readonly bool fullScreen;
    private readonly IKeepAwake keepAwake;
    private readonly FrameCounter frameCounter = new();
    private readonly List<Keys> newKeys = new();
    private readonly List<Buttons> newButtons = new();

    private readonly HintFade dashboardFade = new(hold: TimeSpan.FromSeconds(3), fade: TimeSpan.FromSeconds(1));
    private bool firstInputFrame = true;
    private Dashboard dashboard;
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
    /// <param name="keepAwake">Keeps the display and PC awake while this screen is open.</param>
    public ScreensaverScreen(StressSession session, IScreensaver screensaver, FpsCap fpsCap, bool fullScreen, IKeepAwake keepAwake)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.screensaver = screensaver ?? throw new ArgumentNullException(nameof(screensaver));
        this.fpsCap = fpsCap;
        this.fullScreen = fullScreen;
        this.keepAwake = keepAwake ?? throw new ArgumentNullException(nameof(keepAwake));

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
        dashboard = new Dashboard(game.Services.GetService<UiFonts>(), ScreenManager.GraphicsDevice);

        keepAwake.Begin();
        dashboardFade.Show();
    }

    public override void UnloadContent()
    {
        keepAwake.End();
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
        dashboardFade.Update(gameTime.ElapsedGameTime);
        ScreenManager.Game.IsMouseVisible = dashboardFade.Opacity > 0;

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
        MouseState mouse = inputState.CurrentMouseState;
        MouseState lastMouse = inputState.LastMouseState;

        // The click or movement that started the run isn't a request to show the dashboard.
        Vector2 mouseDelta = firstInputFrame ? Vector2.Zero : new Vector2(mouse.X - lastMouse.X, mouse.Y - lastMouse.Y);
        firstInputFrame = false;

        switch (ScreensaverInput.Decide(newKeys, newButtons, NewClick(inputState), mouseDelta))
        {
            case ScreensaverAction.ShowDashboard:
                dashboardFade.Show();
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

        float opacity = dashboardFade.Opacity * TransitionAlpha;
        if (opacity > 0)
        {
            Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
            dashboard.Draw(spriteBatch, viewport, opacity, UiScale.For(new Point(viewport.Width, viewport.Height)),
                session.CpuDutyCycle, frameCounter.Fps, fpsCap, session.Remaining, session.CpuThreads, screensaver.LiveParticles);
        }
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

    /// <summary>Whether a left or right click went down this frame. Moving the mouse doesn't count.</summary>
    private static bool NewClick(InputState input)
    {
        MouseState current = input.CurrentMouseState;
        MouseState last = input.LastMouseState;

        return IsNewClick(current.LeftButton, last.LeftButton) || IsNewClick(current.RightButton, last.RightButton);
    }

    private static bool IsNewClick(ButtonState current, ButtonState last)
        => current == ButtonState.Pressed && last == ButtonState.Released;
}
