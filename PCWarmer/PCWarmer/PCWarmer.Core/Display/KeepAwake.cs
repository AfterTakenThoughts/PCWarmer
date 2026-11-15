using System;

namespace PCWarmer.Core.Display
{
    public static class KeepAwake
    {
        /// <summary>The keep-awake that works on this operating system.</summary>
        public static IKeepAwake ForCurrentPlatform()
            => OperatingSystem.IsWindows() ? new WindowsKeepAwake() : new SdlKeepAwake();
    }
}
