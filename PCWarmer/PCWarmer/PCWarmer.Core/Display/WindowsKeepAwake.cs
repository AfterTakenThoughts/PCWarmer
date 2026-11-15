using System;
using System.Runtime.InteropServices;

namespace PCWarmer.Core.Display
{
    /// <summary>Windows: <c>SetThreadExecutionState</c>, which keeps the display on and the PC out of sleep for this thread.</summary>
    public sealed class WindowsKeepAwake : IKeepAwake
    {
        /// <summary><c>ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED</c>.</summary>
        public const uint AwakeFlags = 0x80000003;

        /// <summary><c>ES_CONTINUOUS</c> alone, which clears the request.</summary>
        public const uint ReleaseFlags = 0x80000000;

        private readonly Func<uint, uint> setThreadExecutionState;
        private bool awake;

        /// <summary>Uses the real Windows call.</summary>
        public WindowsKeepAwake()
            : this(NativeSetThreadExecutionState)
        {
        }

        /// <param name="setThreadExecutionState">The call to make; tests pass a fake.</param>
        public WindowsKeepAwake(Func<uint, uint> setThreadExecutionState)
        {
            this.setThreadExecutionState = setThreadExecutionState ?? throw new ArgumentNullException(nameof(setThreadExecutionState));
        }

        public void Begin()
        {
            if (awake)
                return;

            setThreadExecutionState(AwakeFlags);
            awake = true;
        }

        public void End()
        {
            if (!awake)
                return;

            setThreadExecutionState(ReleaseFlags);
            awake = false;
        }

        [DllImport("kernel32.dll", EntryPoint = "SetThreadExecutionState")]
        private static extern uint NativeSetThreadExecutionState(uint flags);
    }
}
