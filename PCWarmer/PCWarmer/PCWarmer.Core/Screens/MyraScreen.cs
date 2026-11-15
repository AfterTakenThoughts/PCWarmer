using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using PCWarmer.Core.Ui;

namespace PCWarmer.Screens;

/// <summary>
/// A screen whose widgets are laid out and drawn by Myra. Only the active screen lets Myra read the mouse, so clicks
/// never reach a screen that a popup (such as a confirm box) is covering. Keyboard and gamepad input go through
/// <see cref="GameScreen.HandleInput"/> as on every other screen; Myra never sees keys.
/// </summary>
abstract class MyraScreen : GameScreen
{
    private Desktop desktop;
    private Point builtFor;
    private bool rebuildPending;
    private readonly DeferredActions clicks = new();
    private MouseGate mouseGate;

    /// <summary>The menu scale for the current window (1 at 800×480).</summary>
    protected float Scale { get; private set; } = 1f;

    protected UiFonts Fonts => ScreenManager.Game.Services.GetService<UiFonts>();

    /// <summary>Builds the screen's widgets for <paramref name="scale"/>. Called on load and whenever the window size changes.</summary>
    protected abstract Widget BuildRoot(float scale);

    public override void LoadContent()
    {
        base.LoadContent();

        desktop ??= new Desktop();
        mouseGate = new MouseGate();   // a new screen ignores the click that opened it until it's released
        RebuildNow();
    }

    public override void UnloadContent()
    {
        desktop?.Dispose();
        desktop = null;

        base.UnloadContent();
    }

    /// <summary>
    /// Runs <paramref name="action"/> when <paramref name="widget"/> is clicked, at the next update rather than inside
    /// Myra's drawing, so the action may add or remove screens.
    /// </summary>
    protected void OnClick(Widget widget, System.Action action) => widget.TouchDown += (sender, e) => clicks.Post(action);

    /// <summary>
    /// Builds the widgets again before the next draw, for example after the values they show changed. Deferred so a
    /// click handler never swaps the widget tree while Myra is still handling that click.
    /// </summary>
    protected void Rebuild() => rebuildPending = true;

    private void RebuildNow()
    {
        rebuildPending = false;
        builtFor = WindowSize;
        Scale = UiScale.For(builtFor);
        desktop.Root = BuildRoot(Scale);
    }

    public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
    {
        clicks.RunPending();

        base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

        if (desktop != null && WindowSize != builtFor)
            RebuildNow();
    }

    public override void Draw(GameTime gameTime)
    {
        if (desktop == null)
            return;

        if (rebuildPending)
            RebuildNow();

        desktop.Opacity = TransitionAlpha;

        MouseState mouse = Mouse.GetState();
        bool anyButtonDown = mouse.LeftButton == ButtonState.Pressed || mouse.RightButton == ButtonState.Pressed
            || mouse.MiddleButton == ButtonState.Pressed;

        if (mouseGate.AllowInput(IsActive, anyButtonDown))
        {
            desktop.Render();
        }
        else
        {
            desktop.UpdateLayout();
            desktop.RenderVisual();
        }
    }

    private Point WindowSize
    {
        get
        {
            var viewport = ScreenManager.GraphicsDevice.Viewport;
            return new Point(viewport.Width, viewport.Height);
        }
    }
}
