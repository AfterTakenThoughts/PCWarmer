using Microsoft.Xna.Framework;

namespace PCWarmer.Screens;

/// <summary>A button in a yes/no message box.</summary>
public enum DialogChoice
{
    Yes,
    No,
}

/// <summary>
/// Layout, mouse hit-testing and keyboard focus for a message box's Yes and No buttons.
/// Has no drawing, so it can be tested.
/// </summary>
public sealed class YesNoButtons
{
    /// <param name="row">The area the buttons are centered in, side by side.</param>
    /// <param name="buttonSize">The size of each button.</param>
    /// <param name="gap">Space between the two buttons.</param>
    public YesNoButtons(Rectangle row, Point buttonSize, int gap)
    {
        int totalWidth = buttonSize.X * 2 + gap;
        int left = row.X + (row.Width - totalWidth) / 2;
        int top = row.Y + (row.Height - buttonSize.Y) / 2;

        YesBounds = new Rectangle(left, top, buttonSize.X, buttonSize.Y);
        NoBounds = new Rectangle(left + buttonSize.X + gap, top, buttonSize.X, buttonSize.Y);
    }

    public Rectangle YesBounds { get; }

    public Rectangle NoBounds { get; }

    /// <summary>The button Enter or Space presses. Starts on Yes.</summary>
    public DialogChoice Focused { get; set; } = DialogChoice.Yes;

    /// <summary>The button under <paramref name="point"/>, or null if it's on neither.</summary>
    public DialogChoice? HitTest(Vector2 point)
    {
        if (YesBounds.Contains(point))
            return DialogChoice.Yes;
        if (NoBounds.Contains(point))
            return DialogChoice.No;
        return null;
    }

    /// <summary>Moves keyboard focus to the other button.</summary>
    public void MoveFocus()
        => Focused = Focused == DialogChoice.Yes ? DialogChoice.No : DialogChoice.Yes;
}
