namespace PCWarmer.Core.Display
{
    /// <summary>How a frame-rate cap reads in menus and the dashboard.</summary>
    public static class FpsCapText
    {
        /// <summary>"60", "120" or "Uncapped"; undefined values (such as the old 30 fps) read as "60", the cap a run falls back to.</summary>
        public static string For(FpsCap cap) => cap switch
        {
            FpsCap.Fps120 => "120",
            FpsCap.Uncapped => "Uncapped",
            _ => "60",
        };
    }
}
