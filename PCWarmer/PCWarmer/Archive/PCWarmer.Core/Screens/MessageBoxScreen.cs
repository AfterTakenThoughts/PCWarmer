using System;
using PCWarmer.Core;
using PCWarmer.Core.Inputs;
using PCWarmer.Core.Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PCWarmer.Screens;

/// <summary>
/// A popup message box screen, used to display messages and prompt user input.
/// A yes/no box shows Yes and No buttons: click one, or use Left/Right and Enter. Esc means No.
/// </summary>
class MessageBoxScreen : GameScreen
{
    private string message;
    private Texture2D gradientTexture;
    private Texture2D blankTexture;
    private readonly bool showButtons;
    private YesNoButtons buttons;
    private readonly bool toastMessage;
    private readonly TimeSpan toastDuration;
    private TimeSpan toastTimer;
    private Vector2 messageTextPosition;
    private Rectangle backgroundRectangle;

    /// <summary>
    /// Event raised when the user accepts the message box.
    /// </summary>
    public event EventHandler<PlayerIndexEventArgs> Accepted;

    /// <summary>
    /// Event raised when the user cancels the message box.
    /// </summary>
    public event EventHandler<PlayerIndexEventArgs> Cancelled;

    // The background includes a border somewhat larger than the text itself.
    private const int hPad = 32;
    private const int vPad = 16;

    // Yes/No buttons: padding around their text, the gap between them, and the space above them.
    private const int buttonHPad = 28;
    private const int buttonVPad = 8;
    private const int buttonGap = 24;
    private const int buttonTopMargin = 24;

    private static readonly Color ButtonColor = new(60, 60, 60);
    private static readonly Color FocusedButtonColor = new(230, 110, 30);

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBoxScreen"/> class with Yes and No buttons.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public MessageBoxScreen(string message)
        : this(message, true, TimeSpan.Zero) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBoxScreen"/> class, allowing customization.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="includeUsageText">Whether to show Yes and No buttons (ignored for toast messages).</param>
    /// <param name="toastDuration">The duration for toast messages.</param>
    /// <param name="toastMessage">Indicates whether this is a toast message.</param>
    public MessageBoxScreen(string message, bool includeUsageText, TimeSpan toastDuration, bool toastMessage = false)
    {
        this.message = message;
        showButtons = includeUsageText && !toastMessage;

        this.toastMessage = toastMessage;
        this.toastDuration = toastDuration;
        this.toastTimer = TimeSpan.Zero;

        IsPopup = true;

        TransitionOnTime = TimeSpan.FromSeconds(0.2);
        TransitionOffTime = TimeSpan.FromSeconds(0.2);
    }

    /// <summary>
    /// Loads graphics content for this screen.
    /// </summary>
    public override void LoadContent()
    {
        ContentManager content = ScreenManager.Game.Content;

        gradientTexture = content.Load<Texture2D>("Sprites/gradient");
        blankTexture = content.Load<Texture2D>("Sprites/blank");
    }

    /// <summary>
    /// Responds to user input, accepting or cancelling the message box.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    /// <param name="inputState">The current input state.</param>
    public override void HandleInput(GameTime gameTime, InputState inputState)
    {
        base.HandleInput(gameTime, inputState);

        // Ignore input if this is a ToastMessage
        if (toastMessage)
        {
            return;
        }

        // We pass in our ControllingPlayer, which may either be null (to
        // accept input from any player) or a specific index. If we pass a null
        // controlling player, the InputState helper returns to us which player
        // actually provided the input. We pass that through to our Accepted and
        // Cancelled events, so they can tell which player triggered them.
        PlayerIndex playerIndex;

        if (inputState.IsMenuCancel(ControllingPlayer, out playerIndex))
        {
            Choose(DialogChoice.No, playerIndex);
            return;
        }

        if (buttons == null)
        {
            // No buttons (or not laid out yet): Enter, Space or A accepts.
            if (!showButtons && inputState.IsMenuSelect(ControllingPlayer, out playerIndex))
                Choose(DialogChoice.Yes, playerIndex);
            return;
        }

        // Hovering highlights a button, but only when the mouse moves, so it doesn't fight the arrow keys.
        Vector2 cursor = inputState.TransformCursorLocation(new Vector2(inputState.CurrentMouseState.X, inputState.CurrentMouseState.Y));
        bool mouseMoved = inputState.CurrentMouseState.Position != inputState.LastMouseState.Position;
        if (mouseMoved && buttons.HitTest(cursor) is { } hovered)
            buttons.Focused = hovered;

        if (inputState.IsLeftMouseButtonClicked() && buttons.HitTest(cursor) is { } clicked)
        {
            Choose(clicked, ControllingPlayer ?? PlayerIndex.One);
            return;
        }

        if (inputState.IsNewKeyPress(Keys.Left, ControllingPlayer, out _)
            || inputState.IsNewKeyPress(Keys.Right, ControllingPlayer, out _)
            || inputState.IsNewButtonPress(Buttons.DPadLeft, ControllingPlayer, out _)
            || inputState.IsNewButtonPress(Buttons.DPadRight, ControllingPlayer, out _))
        {
            buttons.MoveFocus();
        }

        if (inputState.IsMenuSelect(ControllingPlayer, out playerIndex))
            Choose(buttons.Focused, playerIndex);
    }

    /// <summary>Raises Accepted (Yes) or Cancelled (No), then closes the box.</summary>
    private void Choose(DialogChoice choice, PlayerIndex playerIndex)
    {
        if (IsExiting)
            return;

        var args = new PlayerIndexEventArgs(playerIndex);
        if (choice == DialogChoice.Yes)
            Accepted?.Invoke(this, args);
        else
            Cancelled?.Invoke(this, args);

        ExitScreen();
    }

    /// <summary>
    /// Updates the screen, particularly for toast messages and positioning.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    /// <param name="otherScreenHasFocus">Indicates whether another screen has focus.</param>
    /// <param name="coveredByOtherScreen">Indicates whether the screen is covered by another screen.</param>
    public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
    {
        base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

        // Handle toast duration countdown.
        if (toastMessage)
        {
            toastTimer += gameTime.ElapsedGameTime;
            if (toastTimer >= toastDuration)
            {
                // Raise the accepted event, then exit the message box.
                Accepted?.Invoke(this, new PlayerIndexEventArgs(PlayerIndex.One));

                // Exit the screen when the toast time has elapsed.
                ExitScreen();
            }
        }

        // Center the message (and the buttons under it) in the BaseScreenSize.
        // The GlobalTransformation will scale everything for us.
        SpriteFont font = ScreenManager.Font;
        Vector2 textSize = font.MeasureString(message);

        Point buttonSize = Point.Zero;
        int buttonsHeight = 0;
        if (showButtons)
        {
            Vector2 labelSize = Vector2.Max(font.MeasureString("Yes"), font.MeasureString("No"));
            buttonSize = new Point((int)labelSize.X + buttonHPad * 2, (int)labelSize.Y + buttonVPad * 2);
            buttonsHeight = buttonTopMargin + buttonSize.Y;
        }

        var contentSize = new Vector2(
            Math.Max(textSize.X, buttonSize.X * 2 + buttonGap),
            textSize.Y + buttonsHeight);
        Vector2 contentPosition = (ScreenManager.BaseScreenSize - contentSize) / 2;
        messageTextPosition = new Vector2(contentPosition.X + (contentSize.X - textSize.X) / 2, contentPosition.Y);

        backgroundRectangle = new Rectangle((int)contentPosition.X - hPad,
                                            (int)contentPosition.Y - vPad,
                                            (int)contentSize.X + hPad * 2,
                                            (int)contentSize.Y + vPad * 2);

        // Lay the buttons out once; the layout doesn't change, and rebuilding would lose the focus.
        if (showButtons && buttons == null)
        {
            var row = new Rectangle((int)contentPosition.X,
                                    (int)(contentPosition.Y + textSize.Y + buttonTopMargin),
                                    (int)contentSize.X,
                                    buttonSize.Y);
            buttons = new YesNoButtons(row, buttonSize, buttonGap);
        }
    }

    /// <summary>
    /// Draws the message box.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public override void Draw(GameTime gameTime)
    {
        SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
        SpriteFont font = ScreenManager.Font;

        // Darken down any other screens that were drawn beneath the popup.
        ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

        // Fade the popup alpha during transitions.
        Color color = Color.White * TransitionAlpha;

        spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

        // Draw the background rectangle.
        spriteBatch.Draw(gradientTexture, backgroundRectangle, color);

        // Draw the message box text.
        spriteBatch.DrawString(font, message, messageTextPosition, color);

        if (buttons != null)
        {
            DrawButton(spriteBatch, font, "Yes", buttons.YesBounds, buttons.Focused == DialogChoice.Yes);
            DrawButton(spriteBatch, font, "No", buttons.NoBounds, buttons.Focused == DialogChoice.No);
        }

        spriteBatch.End();
    }

    private void DrawButton(SpriteBatch spriteBatch, SpriteFont font, string label, Rectangle bounds, bool focused)
    {
        Color fill = (focused ? FocusedButtonColor : ButtonColor) * TransitionAlpha;
        spriteBatch.Draw(blankTexture, bounds, fill);

        Vector2 labelSize = font.MeasureString(label);
        Vector2 labelPosition = new Vector2(bounds.X + (bounds.Width - labelSize.X) / 2, bounds.Y + (bounds.Height - labelSize.Y) / 2);
        spriteBatch.DrawString(font, label, labelPosition, Color.White * TransitionAlpha);
    }
}