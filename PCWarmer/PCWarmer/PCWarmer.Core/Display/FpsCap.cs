namespace PCWarmer.Core.Display
{
    /// <summary>The highest frame rate the screensaver renders at. Higher means more GPU load and more heat.</summary>
    /// <remarks>The numbers are fixed so old settings files map themselves: the old Fps144 (2) reads as Fps120, and the old Fps30 (0) is undefined, so it falls back to 60.</remarks>
    public enum FpsCap
    {
        Fps60 = 1,
        Fps120 = 2,
        Uncapped = 3,
    }
}
