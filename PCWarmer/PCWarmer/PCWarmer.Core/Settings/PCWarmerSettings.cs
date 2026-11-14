using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PCWarmer.Core.Display;
using PCWarmer.Core.Effects;
using PCWarmer.Core.Stress;

namespace PCWarmer.Core.Settings;

/// <summary>
/// Represents the game settings, including display, language, and particle effects.
/// This class implements <see cref="INotifyPropertyChanged"/> to notify subscribers
/// when a property value changes, enabling data binding and UI updates.
/// </summary>
public class PCWarmerSettings : INotifyPropertyChanged
{
    private bool fullScreen = true;
    private int language = 2; // Default to English for now
    private ParticleEffectType particleEffect = ParticleEffectType.Fireworks;
    private StressLevel cpuLoad = StressLevel.Medium;
    private FpsCap fpsCap = FpsCap.Fps60;
    private int durationMinutes = 30;
    private int cpuThreads;
    private HardwareLoad hardwareLoad = HardwareLoad.Medium;

    /// <summary>
    /// Gets or sets whether the game is in full-screen mode.
    /// </summary>
    /// <value>
    /// <c>true</c> if the game is in full-screen mode; otherwise, <c>false</c>.
    /// </value>
    public bool FullScreen
    {
        get => fullScreen;
        set
        {
            if (fullScreen != value)
            {
                fullScreen = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the language setting for the game.
    /// </summary>
    /// <value>
    /// An integer representing the selected language. The value corresponds to a language
    /// option in the game's localization system.
    /// </value>
    public int Language
    {
        get => language;
        set
        {
            if (language != value)
            {
                language = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the type of particle effect used in the game.
    /// </summary>
    /// <value>
    /// A <see cref="ParticleEffectType"/> value representing the current particle effect.
    /// </value>
    public ParticleEffectType ParticleEffect
    {
        get => particleEffect;
        set
        {
            if (particleEffect != value)
            {
                particleEffect = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// How hard a warming run works the CPU: Low, Medium or High (see <see cref="StressProfile"/> for each level's duty cycle).
    /// </summary>
    public StressLevel CpuLoad
    {
        get => cpuLoad;
        set => SetField(ref cpuLoad, value);
    }

    /// <summary>The screensaver's frame-rate cap. Higher means more GPU load.</summary>
    public FpsCap FpsCap
    {
        get => fpsCap;
        set => SetField(ref fpsCap, value);
    }

    /// <summary>How long a warming run lasts, in minutes. 0 means unlimited. Defaults to 30 so a forgotten run stops on its own.</summary>
    public int DurationMinutes
    {
        get => durationMinutes;
        set => SetField(ref durationMinutes, value);
    }

    /// <summary>CPU worker threads for a warming run. 0 means one per core.</summary>
    public int CpuThreads
    {
        get => cpuThreads;
        set => SetField(ref cpuThreads, value);
    }

    /// <summary>The hardware load picked on the screensaver screen. Only Custom uses the options on the settings screen.</summary>
    public HardwareLoad HardwareLoad
    {
        get => hardwareLoad;
        set => SetField(ref hardwareLoad, value);
    }

    /// <summary>
    /// Puts every option on the settings screen back to its default. Language isn't on that screen, so it's kept.
    /// </summary>
    public void ResetToDefaults()
    {
        var defaults = new PCWarmerSettings();

        CpuLoad = defaults.CpuLoad;
        FpsCap = defaults.FpsCap;
        DurationMinutes = defaults.DurationMinutes;
        CpuThreads = defaults.CpuThreads;
        FullScreen = defaults.FullScreen;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// Event triggered when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event to notify subscribers that a property value has changed.
    /// </summary>
    /// <param name="propertyName">
    /// The name of the property that changed. If not provided, the name of the calling member is used.
    /// </param>
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}