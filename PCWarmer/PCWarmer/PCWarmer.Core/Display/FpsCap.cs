namespace PCWarmer.Core.Display
{
    /// <summary>The highest frame rate the screensaver renders at. Higher means more GPU load and more heat.</summary>
    public enum FpsCap
    {
        Fps30,
        Fps60,
        Fps144,
        Uncapped,
    }
}
