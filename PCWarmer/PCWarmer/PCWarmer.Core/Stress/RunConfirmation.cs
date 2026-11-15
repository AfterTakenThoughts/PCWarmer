namespace PCWarmer.Core.Stress
{
    /// <summary>What the "Really warm your room?" box says before a run starts.</summary>
    public static class RunConfirmation
    {
        /// <summary>The box's title and detail, or null for None, which only runs the screensaver and starts without asking.</summary>
        public static (string Title, string Detail)? For(HardwareLoad load, string screensaverName)
        {
            if (load == HardwareLoad.None)
                return null;

            return ($"Really warm your room with {screensaverName}?",
                    $"{load} intensity. Your CPU and GPU will run hard and your PC will get hot.");
        }
    }
}
