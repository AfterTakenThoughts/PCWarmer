using System;
using System.Runtime.InteropServices;

namespace PCWarmer.Core.Display
{
    /// <summary>
    /// Linux and macOS (DesktopGL): asks SDL, which MonoGame ships, to block the screensaver. If the SDL library
    /// can't be found, this does nothing.
    /// </summary>
    public sealed class SdlKeepAwake : IKeepAwake
    {
        private bool awake;

        public void Begin()
        {
            if (awake)
                return;

            TryCall(SDL_DisableScreenSaver);
            awake = true;
        }

        public void End()
        {
            if (!awake)
                return;

            TryCall(SDL_EnableScreenSaver);
            awake = false;
        }

        private static void TryCall(Action call)
        {
            try
            {
                call();
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        [DllImport("SDL2")]
        private static extern void SDL_DisableScreenSaver();

        [DllImport("SDL2")]
        private static extern void SDL_EnableScreenSaver();
    }
}
