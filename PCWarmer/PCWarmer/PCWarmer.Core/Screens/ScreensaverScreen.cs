using System;
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
/// Any key, click or gamepad button stops the run and returns to the previous screen; so does the run's time running out.
/// </summary>
class ScreensaverScreen : GameScreen
{
    private static readonly Buttons[] StopButtons =
    {
        Buttons.A, Buttons.B, Buttons.X, Buttons.Y, Buttons.Start, Buttons.Back,
    };

    private readonly StressSession session;
    private readonly IScreensaver screensaver;
    private readonly FpsCap fpsCap;
    private readonly bool fullScreen;
    private readonly FrameCounter frameCounter = new();
    private readonly StringBuilder hud = new();

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

        session.Update();
        if (!session.IsRunning && !IsExiting)
            ExitScreen();

        base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
    }

    public override void HandleInput(GameTime gameTime, InputState inputState)
    {
        if (IsExiting || !AnyNewPress(inputState))
            return;

        session.Stop();
        ExitScreen();
    }

    public override void Draw(GameTime gameTime)
    {
        frameCounter.Add(gameTime.ElapsedGameTime);

        ScreenManager.GraphicsDevice.Clear(Color.Black);

        SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
        screensaver.Draw(spriteBatch, gameTime);

        spriteBatch.Begin();
        spriteBatch.DrawString(ScreenManager.Font, BuildHud(), new Vector2(16, 16), Color.White * TransitionAlpha);
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
        hud.Append("Press any key to stop");
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
    /// True on the frame a key, mouse button or gamepad button goes down. Mouse movement doesn't count,
    /// and neither does a key still held from the menu press that opened this screen.
    /// </summary>
    private bool AnyNewPress(InputState input)
    {
        Keys[] lastKeys = input.LastKeyboardStates[0].GetPressedKeys();
        foreach (Keys key in input.CurrentKeyboardStates[0].GetPressedKeys())
        {
            if (Array.IndexOf(lastKeys, key) < 0)
                return true;
        }

        if (IsNewClick(input.CurrentMouseState.LeftButton, input.LastMouseState.LeftButton)
            || IsNewClick(input.CurrentMouseState.RightButton, input.LastMouseState.RightButton))
            return true;

        foreach (Buttons button in StopButtons)
        {
            if (input.IsNewButtonPress(button, ControllingPlayer, out _))
                return true;
        }

        return false;
    }

    private static bool IsNewClick(ButtonState current, ButtonState last)
        => current == ButtonState.Pressed && last == ButtonState.Released;
}
