using System;
using System.Collections.Generic;

namespace PCWarmer.Core.Ui
{
    /// <summary>
    /// Actions saved for later. Myra fires click handlers while it draws, and the screen manager is looping over its
    /// screens at that moment, so a click that adds or removes a screen has to wait until the next update.
    /// </summary>
    public sealed class DeferredActions
    {
        private readonly Queue<Action> pending = new();

        public void Post(Action action) => pending.Enqueue(action ?? throw new ArgumentNullException(nameof(action)));

        /// <summary>Runs everything posted so far, in order. Anything posted while running waits for the next call.</summary>
        public void RunPending()
        {
            for (int count = pending.Count; count > 0; count--)
                pending.Dequeue()();
        }
    }
}
