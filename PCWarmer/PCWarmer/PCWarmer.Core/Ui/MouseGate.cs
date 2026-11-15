namespace PCWarmer.Core.Ui
{
    /// <summary>
    /// Decides when a Myra screen may read the mouse. Myra remembers the mouse from the last frame it read it, so a
    /// screen that was covered (or has just opened) would take a still-held button as a new click. After becoming
    /// active, input waits until every mouse button is up.
    /// </summary>
    public sealed class MouseGate
    {
        private bool waitingForRelease = true;

        /// <summary>Whether the screen may read the mouse this frame.</summary>
        public bool AllowInput(bool isActive, bool anyButtonDown)
        {
            if (!isActive)
            {
                waitingForRelease = true;
                return false;
            }

            if (waitingForRelease)
            {
                if (anyButtonDown)
                    return false;
                waitingForRelease = false;
            }

            return true;
        }
    }
}
