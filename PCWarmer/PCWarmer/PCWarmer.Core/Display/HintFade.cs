using System;

namespace PCWarmer.Core.Display
{
    /// <summary>Timing for an on-screen hint: fully visible for a while, then fading out to nothing.</summary>
    public sealed class HintFade
    {
        private readonly TimeSpan hold;
        private readonly TimeSpan fade;
        private TimeSpan shownFor;
        private bool showing;

        /// <param name="hold">How long the hint stays fully visible.</param>
        /// <param name="fade">How long it then takes to fade out.</param>
        public HintFade(TimeSpan hold, TimeSpan fade)
        {
            this.hold = hold;
            this.fade = fade;
        }

        /// <summary>1 while held, falling evenly to 0 during the fade; 0 before it's shown, after the fade, or once hidden.</summary>
        public float Opacity
        {
            get
            {
                if (!showing || shownFor >= hold + fade)
                    return 0f;
                if (shownFor <= hold)
                    return 1f;
                return (float)(1 - (shownFor - hold) / fade);
            }
        }

        /// <summary>Shows the hint from the start of its hold.</summary>
        public void Show()
        {
            showing = true;
            shownFor = TimeSpan.Zero;
        }

        /// <summary>Hides the hint straight away.</summary>
        public void Hide() => showing = false;

        public void Update(TimeSpan delta)
        {
            if (showing)
                shownFor += delta;
        }
    }
}
