namespace PCWarmer.Core.Display
{
    /// <summary>Keeps the display and the PC from sleeping while a run is going. Both calls are safe to repeat.</summary>
    public interface IKeepAwake
    {
        /// <summary>Starts keeping the display and the PC awake. A second call while already awake does nothing.</summary>
        void Begin();

        /// <summary>Lets normal power settings apply again. Does nothing unless <see cref="Begin"/> was called.</summary>
        void End();
    }
}
