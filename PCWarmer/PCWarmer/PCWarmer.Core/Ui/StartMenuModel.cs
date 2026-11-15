using System;
using System.Collections.Generic;
using System.Linq;
using PCWarmer.Core.Display;
using PCWarmer.Core.Screensavers;
using PCWarmer.Core.Settings;
using PCWarmer.Core.Stress;

namespace PCWarmer.Core.Ui
{
    /// <summary>The start screen's rows, in focus order.</summary>
    public enum StartMenuRow
    {
        Intensity,
        CpuLoad,
        ScreensaverDetail,
        CpuThreads,
        FrameRate,
        Screensaver,
        Start,
        RunLength,
        Display,
        ResetSettings,
        Exit,
    }

    /// <summary>What activating a row asks the start screen to do.</summary>
    public enum StartMenuCommand
    {
        None,
        StartRun,
        Exit,
        ResetSettings,
    }

    /// <summary>
    /// The start screen without any drawing: which rows show, which one has focus, and how each row changes the saved
    /// settings. The screen turns keys, buttons and clicks into calls on this, saves when a call reports a change, and
    /// draws what <see cref="ValueText"/> says.
    /// </summary>
    public sealed class StartMenuModel
    {
        private static readonly StartMenuRow[] CustomRows =
            { StartMenuRow.CpuLoad, StartMenuRow.ScreensaverDetail, StartMenuRow.CpuThreads, StartMenuRow.FrameRate };

        private readonly PCWarmerSettings settings;
        private readonly int cores;
        private StartMenuRow focused = StartMenuRow.Start;

        /// <param name="settings">The saved settings this menu changes.</param>
        /// <param name="cores">The machine's core count, for the CPU threads row.</param>
        public StartMenuModel(PCWarmerSettings settings, int cores)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.cores = Math.Max(1, cores);
        }

        /// <summary>While true (a confirm box is open), every call does nothing.</summary>
        public bool Locked { get; set; }

        /// <summary>The rows on screen in focus order. The Custom rows only show when the intensity is Custom.</summary>
        public IReadOnlyList<StartMenuRow> VisibleRows
        {
            get
            {
                var rows = new List<StartMenuRow> { StartMenuRow.Intensity };
                if (IsCustom)
                    rows.AddRange(CustomRows);
                rows.AddRange(new[]
                {
                    StartMenuRow.Screensaver, StartMenuRow.Start, StartMenuRow.RunLength, StartMenuRow.Display,
                    StartMenuRow.ResetSettings, StartMenuRow.Exit,
                });
                return rows;
            }
        }

        /// <summary>The row with keyboard focus. Start when the menu opens; Intensity if the focused row stops showing.</summary>
        public StartMenuRow Focused => VisibleRows.Contains(focused) ? focused : StartMenuRow.Intensity;

        private bool IsCustom => SettingChoices.HardwareLoadOrDefault(settings.HardwareLoad) == HardwareLoad.Custom;

        /// <summary>Moves focus up (negative) or down (positive) through the visible rows, wrapping at both ends.</summary>
        public void MoveFocus(int delta)
        {
            if (Locked || delta == 0)
                return;

            focused = SettingChoices.Step(VisibleRows, Focused, delta);
        }

        /// <summary>Focuses <paramref name="row"/>, if it's showing.</summary>
        public void FocusRow(StartMenuRow row)
        {
            if (!Locked && VisibleRows.Contains(row))
                focused = row;
        }

        /// <summary>Steps the focused row's setting forward (+1) or back (-1). True if a setting changed, so the caller saves.</summary>
        public bool ChangeValue(int direction)
        {
            if (Locked)
                return false;

            switch (Focused)
            {
                case StartMenuRow.Intensity:
                    settings.HardwareLoad = SettingChoices.Step(SettingChoices.HardwareLoads, settings.HardwareLoad, direction);
                    break;
                case StartMenuRow.CpuLoad:
                    settings.CpuLoad = SettingChoices.Step(SettingChoices.Levels, settings.CpuLoad, direction);
                    break;
                case StartMenuRow.ScreensaverDetail:
                    settings.ScreensaverLevel = SettingChoices.Step(SettingChoices.Levels, settings.ScreensaverLevel, direction);
                    break;
                case StartMenuRow.CpuThreads:
                    settings.CpuThreads = SettingChoices.StepCpuThreads(settings.CpuThreads, cores, direction);
                    break;
                case StartMenuRow.FrameRate:
                    settings.FpsCap = SettingChoices.Step(SettingChoices.FpsCaps, settings.FpsCap, direction);
                    break;
                case StartMenuRow.Screensaver:
                    settings.ScreensaverId = SettingChoices.Step(ScreensaverIds, settings.ScreensaverId, direction);
                    break;
                case StartMenuRow.RunLength:
                    settings.DurationMinutes = SettingChoices.Step(SettingChoices.Durations, settings.DurationMinutes, direction);
                    break;
                case StartMenuRow.Display:
                    settings.FullScreen = !settings.FullScreen;
                    break;
                default:
                    return false;
            }

            return true;
        }

        /// <summary>Enter or click on the focused row: a command for Start, Reset settings and Exit; a step forward on value rows.</summary>
        public StartMenuCommand Activate()
        {
            if (Locked)
                return StartMenuCommand.None;

            switch (Focused)
            {
                case StartMenuRow.Start: return StartMenuCommand.StartRun;
                case StartMenuRow.Exit: return StartMenuCommand.Exit;
                case StartMenuRow.ResetSettings: return StartMenuCommand.ResetSettings;
                default:
                    ChangeValue(1);
                    return StartMenuCommand.None;
            }
        }

        /// <summary>Picks an intensity directly (a click on its segment) and focuses the Intensity row.</summary>
        public void SetIntensity(HardwareLoad load)
        {
            if (Locked)
                return;

            settings.HardwareLoad = load;
            focused = StartMenuRow.Intensity;
        }

        /// <summary>Picks a screensaver directly (a click on its tile) and focuses the Screensaver row.</summary>
        public void SetScreensaver(string id)
        {
            if (Locked)
                return;

            settings.ScreensaverId = SettingChoices.ScreensaverIdOrDefault(id);
            focused = StartMenuRow.Screensaver;
        }

        /// <summary>What a row shows as its value. Bad saved values show the fallback a run would use.</summary>
        public string ValueText(StartMenuRow row) => row switch
        {
            StartMenuRow.Intensity => SettingChoices.HardwareLoadOrDefault(settings.HardwareLoad).ToString(),
            StartMenuRow.CpuLoad => SettingChoices.LevelOrDefault(settings.CpuLoad).ToString(),
            StartMenuRow.ScreensaverDetail => SettingChoices.LevelOrDefault(settings.ScreensaverLevel).ToString(),
            StartMenuRow.CpuThreads => settings.CpuThreads >= 1 && settings.CpuThreads <= cores
                ? settings.CpuThreads.ToString()
                : $"All cores ({cores})",
            StartMenuRow.FrameRate => FpsCapText.For(SettingChoices.FpsCapOrDefault(settings.FpsCap)),
            StartMenuRow.Screensaver => ScreensaverName(settings.ScreensaverId),
            StartMenuRow.RunLength => SettingChoices.RunDuration(settings.DurationMinutes) is { } duration
                ? $"{duration.TotalMinutes:0} min"
                : "Unlimited",
            StartMenuRow.Display => settings.FullScreen ? "Full screen" : "Window",
            _ => string.Empty,
        };

        /// <summary>The display name of a screensaver id, or of the default for unknown ids.</summary>
        public static string ScreensaverName(string id)
        {
            string known = SettingChoices.ScreensaverIdOrDefault(id);
            return ScreensaverCatalog.Entries.First(entry => entry.Id == known).Name;
        }

        private static IReadOnlyList<string> ScreensaverIds { get; } = ScreensaverCatalog.Entries.Select(entry => entry.Id).ToArray();
    }
}
