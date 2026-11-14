# PCWarmer Stress Engine Design

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the MonoGame platformer template with a stress engine that heats the room by loading the CPU (worker threads) and the GPU (screensavers), with CPU load, frame-rate, duration and thread-count options.

**Status:** Tasks 1–6 are done. Task 7 (removing the platformer template) is still open. Part 2 records what each task built and where it differs from the original plan. The [roadmap](roadmap.md) tracks open work.

**Architecture:** Keep the existing shared core (`PCWarmer.Core`) plus thin platform heads (DesktopGL and WindowsDX), the screen stack and settings persistence. A `Stress/` module holds the CPU worker threads and a session that owns timing. A `Screensavers/` module holds pluggable GPU loads hosted by one `GameScreen`. All load logic that doesn't need a `GraphicsDevice` lives in plain classes so `PCWarmer.Tests` can test it.

**Tech stack:** .NET 8, MonoGame 3.8.x (DesktopGL, plus a Windows-only DirectX head), System.Text.Json (settings), NUnit 4 (tests).

**Spec:** Part 1 of this document ("Architecture Design") plus [user-stories.md](user-stories.md).

---

## Part 1: Architecture Design

### User stories this serves

| Story | Served by |
|---|---|
| Max heats the house | `High` CPU load: every core busy 100% of the time, plus an uncapped screensaver |
| QA tests the PC under extreme stress | CPU thread count, duration timer, HUD showing FPS, CPU load, threads and elapsed time |
| Student leaves something running while idling | `Low` CPU load, capped FPS, a nice-looking screensaver, and a plain **Start** button |

### Module layout (inside `PCWarmer.Core`)

```
Stress/
  StressLevel.cs              enum Low / Medium / High
  StressProfile.cs            what each level means in numbers (CPU duty cycle, particle rate and cap)
  ICpuLoad.cs                 seam so StressSession can be tested without real threads
  CpuStressService.cs         background worker threads that burn CPU
  StressSession.cs            one warming run: start/stop, wall-clock duration, elapsed and remaining time
  DivergentSeries.cs          harmonic series as endless CPU work (tested; not yet used by CpuStressService)
Screensavers/
  IScreensaver.cs             LoadContent / Update / Draw / UnloadContent
  ScreensaverCatalog.cs       id -> screensaver, plus the picker's entries; unknown ids fall back to the default
  EmissionBudget.cs           how many particles to emit this frame (rate plus cap)
  GlowTexture.cs              generated soft-glow texture shared by Embers and Bouncing Orbs
  ParticleStormScreensaver.cs
  EmbersScreensaver.cs        + EmberField.cs (motion, tested without a GPU)
  StarfieldScreensaver.cs     + Starfield.cs (motion, tested without a GPU)
  BouncingOrbsScreensaver.cs  + OrbField.cs (motion, tested without a GPU)
Display/
  FpsCap.cs                   Fps30 / Fps60 / Fps144 / Uncapped
  FrameRatePolicy.cs          FpsCap -> fixed-step, vsync and target frame time
  FrameCounter.cs             rolling FPS for the HUD
Settings/
  PCWarmerSettings.cs         saved options (existing POCO, extended)
  SettingChoices.cs           the values each option cycles through, and fallbacks for bad saved values
Screens/
  StartScreen.cs              first screen: Start, Settings, Exit
  ScreensaverPickerScreen.cs  pick a screensaver, confirm, then start the run
  SettingsScreen.cs           CPU load, frame rate, duration, CPU threads, display mode
  ScreensaverScreen.cs        hosts the session, the screensaver and the HUD; any key stops the run
  YesNoButtons.cs             layout for the confirm box's Yes/No buttons (tested)
```

### Key decisions

1. **CPU load runs on dedicated background threads, never in `Update()`.** One worker per requested thread runs real math in 100 ms windows: busy for `dutyCycle × 100 ms`, then asleep for the rest of the window. Workers are `IsBackground = true` (they can never keep the process alive) and `ThreadPriority.BelowNormal` (the UI and the OS stay responsive at 100%).
2. **A session is created for each run and owned by `ScreensaverScreen`.** `ScreensaverPickerScreen` builds a `CpuStressService` and a `StressSession` from the saved settings, starts the session, then pushes the screen. When the screen unloads, it disposes the session. There is no long-lived global stress service.
3. **The duration timer uses wall-clock time, not `GameTime`.** It stays correct when the window is unfocused or minimized, which is exactly when Max and the student leave it running.
4. **The GPU load is bounded.** A screensaver emits particles at a per-second rate (not per frame) up to a per-level cap on live particles. Uncapped FPS raises GPU load without growing memory.
5. **Frame-rate options use MonoGame's own switches.** A cap uses `IsFixedTimeStep = true` and `TargetElapsedTime`. `Uncapped` turns off fixed-step. VSync is always off, so a cap above the monitor's refresh rate really renders more frames. While a screensaver runs, `Game.InactiveSleepTime` is zero so an unfocused window keeps rendering.
6. **Settings extend the existing `PCWarmerSettings` POCO.** Values are always sanitized where they are used (`SettingChoices`, `StressProfile.For`, `FrameRatePolicy.For`, `ScreensaverCatalog.Create`, the `CpuStressService` thread clamp), so an old or hand-edited JSON file can never crash the app.
7. **The screensaver is picked for each run, not saved.** `ScreensaverPickerScreen` lists `ScreensaverCatalog.Entries`, so there is no `ScreensaverId` setting.
8. **Screensavers currently run at the Medium GPU profile.** `ScreensaverCatalog.Create` uses each screensaver's default `StressLevel.Medium`, so the CPU load setting changes the CPU duty cycle but not the particle rate, star count or orb count.
9. **The platformer code is to be deleted (Task 7, open).** DesktopGL and WindowsDX are both supported. The Android and iOS heads have been removed.

### Stress levels

| Level | CPU duty cycle | Particles per second | Max live particles |
|---|---|---|---|
| Low | 0.25 | 300 | 3,000 |
| Medium | 0.60 | 3,000 | 30,000 |
| High | 1.00 | 20,000 | 150,000 |

Starfield uses half the max live particles as its star count, and Bouncing Orbs uses a tenth as its orb count.

### Settings defaults

| Setting | Default | Choices |
|---|---|---|
| `CpuLoad` | Medium | Low, Medium, High |
| `FpsCap` | Fps60 | 30, 60, 144, Uncapped |
| `DurationMinutes` | 30 | 0 (unlimited), 15, 30, 60, 120 |
| `CpuThreads` | 0 (all cores) | 0, then 1 … `Environment.ProcessorCount` |
| `FullScreen` | true | on / off |

The game applies the saved `FullScreen` value at startup in `PCWarmerGame`.

---

## Part 2: Implementation

All commands run from `PCWarmer/PCWarmer/`. Run `dotnet test` and `dotnet build` against the `.csproj` files, not the `.sln` (the WindowsDX head only builds on Windows).

## Global Constraints

- Target `net8.0`. MonoGame packages stay at `3.8.*`.
- New code lives in `PCWarmer.Core` under the namespaces `PCWarmer.Core.Stress`, `PCWarmer.Core.Screensavers`, `PCWarmer.Core.Display` and `PCWarmer.Core.Settings`. Screens use the existing `PCWarmer.Screens` namespace.
- Types the tests use are `public`.
- PCWarmer is English only. New UI strings go only into the default `Resources.resx` (and `Resources.Designer.cs`), or are hardcoded in English. No translations.
- Every content asset that is loaded must be listed in `PCWarmer.Core/Content/PCWarmer.mgcb`.
- Stress worker threads: `IsBackground = true`, `Priority = ThreadPriority.BelowNormal`.
- Frame times are built with `TimeSpan.FromTicks(TimeSpan.TicksPerSecond / fps)`.
- Tests use NUnit 4 with `Assert.That` constraints.

## Review Focus

1. **The window loses focus or is minimized mid-run.** CPU load and the duration timer keep going, and the run ends on time. Pinned by `StressSessionTests.Duration_EndsTheRunOnTheClockNotFrames`.
2. **The settings JSON is old, missing fields, or holds bad values** (for example `"CpuLoad": 99` or `"CpuThreads": 999`). The app falls back to defaults and doesn't crash. Pinned by `PCWarmerSettingsTests` (`OldFileWithoutStressFields_LoadsWithDefaults`, `FileFromThePercentVersion_LoadsWithTheDefaultLevel`), `StressProfileTests.Profile_UndefinedLevelFallsBackToMedium`, `FrameRatePolicyTests.UndefinedCap_FallsBackTo60`, `CpuStressServiceTests.Constructor_ClampsThreadCountToCoreCount` and `ScreensaverCatalogTests.Create_UnknownId_FallsBackToParticleStorm`.
3. **The user quits mid-run (any key, or closing the window).** Every worker thread stops and no CPU load lingers. Pinned by `CpuStressServiceTests.Dispose_StopsAllWorkers` and `StressSessionTests.Dispose_WhileRunning_StopsCpu`.
4. **Uncapped FPS, or a long frame hitch.** The live particle count never exceeds the level's cap. Pinned by `EmissionBudgetTests` (`Next_AtTheCap_EmitsNothing`, `Next_LongHitch_OnlyFillsUpToTheCap`, `Next_CappedFrame_DoesNotBankTheOverflow`).
5. **A machine with 1 core, or `CpuThreads = 0`.** The thread count resolves to between 1 and `Environment.ProcessorCount`. Pinned by `CpuStressServiceTests.Constructor_ClampsThreadCountToCoreCount`.

---

### Task 1: Test project, solution cleanup, stress levels (done)

**Files:**
- `PCWarmer.Tests/PCWarmer.Tests.csproj`: NUnit 4, `net8.0`, references `PCWarmer.Core` and `MonoGame.Framework.DesktopGL 3.8.*` (because Core marks MonoGame `PrivateAssets=All`). It is part of `PCWarmer.sln`, so C# Dev Kit's Test Explorer finds it.
- `PCWarmer.sln`: Android and iOS removed. WindowsDX kept.
- `PCWarmer.Core/Stress/StressLevel.cs`, `StressProfile.cs`
- Tests: `PCWarmer.Tests/Stress/StressProfileTests.cs`

**Interfaces:**
- `public enum StressLevel { Low, Medium, High }`
- `public sealed record StressProfile(double CpuDutyCycle, int ParticlesPerSecond, int MaxParticles)` with `public static StressProfile For(StressLevel level)`. Values come from the Part 1 table. An undefined value returns Medium.

**Changed from the plan:** NUnit instead of xUnit. The top level is `High`, not `Max`. WindowsDX stayed.

---

### Task 2: CPU stress service (done)

**Files:** `PCWarmer.Core/Stress/ICpuLoad.cs`, `CpuStressService.cs`. Tests: `PCWarmer.Tests/Stress/CpuStressServiceTests.cs`.

**Interfaces:**
- `public interface ICpuLoad { void Start(double dutyCycle); void Stop(); bool IsRunning { get; } int ThreadCount { get; } }`
- `public sealed class CpuStressService : ICpuLoad, IDisposable`
  - `CpuStressService(int threadCount)`: 0 or less means `Environment.ProcessorCount`. Values above that are clamped to it.
  - `double DutyCycle` (clamped to [0, 1]), `int ActiveWorkers`, `long WorkUnitsCompleted` (updated with `Interlocked`).
  - `Start` while running restarts with the new duty cycle and never has more than `ThreadCount` workers.
  - `Stop` cancels the workers and joins each with a 1-second timeout. Safe to call more than once. `Dispose` calls `Stop`.
- The work unit is `Math.Sqrt(i) * Math.Sin(i)` accumulated into a value the JIT can't drop.

**Also added:** `DivergentSeries` (harmonic partial sums as endless work), with `DivergentSeriesTests`. It isn't wired into `CpuStressService` yet.

---

### Task 3: Stress session (done)

**Files:** `PCWarmer.Core/Stress/StressSession.cs`. Tests: `PCWarmer.Tests/Stress/StressSessionTests.cs` and `FakeCpuLoad.cs`.

**Interfaces:** `public sealed class StressSession : IDisposable`
- `StressSession(ICpuLoad cpu, Func<TimeSpan> clock)`. In production the clock is a started `Stopwatch`'s `Elapsed`.
- `void Start(double cpuDutyCycle, TimeSpan? duration)`, where `null` means unlimited. Throws `ArgumentOutOfRangeException` if `duration <= TimeSpan.Zero`.
- `void Update()` stops the run once `Elapsed >= Duration`. `void Stop()` is safe to call more than once. `Dispose` calls `Stop`.
- Properties: `bool IsRunning`, `double CpuDutyCycle`, `int CpuThreads` (from `cpu.ThreadCount`), `TimeSpan? Duration`, `TimeSpan Elapsed` (frozen at stop), `TimeSpan? Remaining` (null when unlimited, never negative).

**Changed from the plan:** `Start` takes a CPU duty cycle instead of a `StressLevel`; the caller looks it up with `StressProfile.For`. The session has no `Level`, `Profile` or `Stopped` event.

---

### Task 4: Settings and frame-rate policy (done)

**Files:** `PCWarmer.Core/Display/FpsCap.cs`, `FrameRatePolicy.cs`, `PCWarmer.Core/Settings/PCWarmerSettings.cs`, `SettingChoices.cs`. Tests: `PCWarmer.Tests/Display/FrameRatePolicyTests.cs`, `PCWarmer.Tests/Settings/PCWarmerSettingsTests.cs`, `SettingChoicesTests.cs`.

**Interfaces:**
- `public enum FpsCap { Fps30, Fps60, Fps144, Uncapped }`
- `public readonly record struct FrameRateSettings(bool IsFixedTimeStep, bool VSync, TimeSpan TargetElapsedTime)` and `public static class FrameRatePolicy { public static FrameRateSettings For(FpsCap cap); }`. A cap returns `(true, false, ticks/fps)`. `Uncapped` returns `(false, false, ticks/60)`. An undefined value returns the `Fps60` result.
- New `PCWarmerSettings` properties: `StressLevel CpuLoad`, `FpsCap FpsCap`, `int DurationMinutes`, `int CpuThreads`. Defaults are in the Part 1 table.
- `SettingChoices`: `NextCpuLoad`, `NextFpsCap`, `NextDuration`, `NextCpuThreads` (what the settings screen cycles through), `CpuLoadOrDefault` and `RunDuration` (saved value to run parameter, with fallbacks).

**Changed from the plan:** the level setting is `CpuLoad`, not `StressLevel`. There's no `ScreensaverId` setting (see decision 7). The default duration is 30 minutes, not unlimited, and full screen is on by default.

---

### Task 5: Screensaver engine and screen (done)

**Files:**
- `PCWarmer.Core/Screensavers/`: `IScreensaver`, `EmissionBudget`, `ScreensaverCatalog`, `GlowTexture`, and four screensavers (Particle Storm, Embers, Starfield, Bouncing Orbs) with their motion classes `EmberField`, `Starfield` and `OrbField`.
- `PCWarmer.Core/Display/FrameCounter.cs`
- `PCWarmer.Core/Screens/ScreensaverScreen.cs`
- Tests: `PCWarmer.Tests/Screensavers/EmissionBudgetTests.cs`, `ScreensaverCatalogTests.cs`, `EmberFieldTests.cs`, `StarfieldTests.cs`, `OrbFieldTests.cs`, and `PCWarmer.Tests/Display/FrameCounterTests.cs`.

**Interfaces:**
- `public interface IScreensaver { string Id { get; } int LiveParticles { get; } void LoadContent(ContentManager content, GraphicsDevice device); void Update(GameTime gameTime); void Draw(SpriteBatch spriteBatch, GameTime gameTime); void UnloadContent(); }`
- Each screensaver's constructor takes an optional `StressLevel` (default Medium) that sizes its load through `StressProfile.For`. Constructors don't touch the GPU.
- `public sealed class EmissionBudget(int particlesPerSecond, int maxParticles)` with `int Next(TimeSpan delta, int liveParticles)`. Fractions carry over between frames. The result is always between 0 and `maxParticles - liveParticles`.
- `public static class ScreensaverCatalog { public const string DefaultId = "particle-storm"; public static IReadOnlyList<ScreensaverEntry> Entries { get; } public static IScreensaver Create(string id); }`, with `public sealed record ScreensaverEntry(string Id, string Name)`. Unknown or null ids create Particle Storm.
- `public sealed class FrameCounter` with `void Add(TimeSpan frameTime)` and `double Fps` (frames in the last second divided by their total time, or 0 when empty).
- `class ScreensaverScreen : GameScreen` with `ScreensaverScreen(StressSession session, IScreensaver screensaver, FpsCap fpsCap)`:
  - `LoadContent` saves and then applies the frame-rate switches from `FrameRatePolicy.For(fpsCap)`, sets `InactiveSleepTime` to zero and loads the screensaver.
  - `Update` updates the screensaver and the session, and exits when the session stops.
  - `HandleInput`: a new key press, left or right click, or gamepad A/B/X/Y/Start/Back stops the session and exits. Mouse movement and a key still held from the menu don't count.
  - `Draw` draws the screensaver, then a HUD: FPS, frame-rate cap, CPU load % and threads, live particles, time warming, time left (or "Unlimited"), and "Press any key to stop".
  - `UnloadContent` restores the saved frame-rate switches, unloads the screensaver and disposes the session.

**Changed from the plan:** `IScreensaver` has no `SetLevel`; the level is a constructor argument. There are four screensavers instead of one, and the catalog exposes `Entries` (id and display name) instead of `Ids`. The HUD strings are hardcoded in English.

---

### Task 6: Start screen, screensaver picker and settings screen (done)

The plan was a single `StressMenuScreen` reached from the template's main menu. What was built instead:

**Files:** `PCWarmer.Core/Screens/StartScreen.cs`, `ScreensaverPickerScreen.cs`, `SettingsScreen.cs` (reworked), `MessageBoxScreen.cs` and `YesNoButtons.cs` (Yes/No buttons). `PCWarmerGame.Initialize` pushes `BackgroundScreen` and then `StartScreen`. Tests: `PCWarmer.Tests/Screens/YesNoButtonsTests.cs`.

- **`StartScreen`:** **Start** opens `ScreensaverPickerScreen`. **Settings** opens `SettingsScreen`. **Exit** (or cancel) asks "Are you sure you want to exit?".
- **`ScreensaverPickerScreen`:** lists `ScreensaverCatalog.Entries` plus Back. Picking one asks "Really warm your room with {name}? Your CPU and GPU will run hard and your PC will get hot." On yes, it builds `new CpuStressService(settings.CpuThreads)` and `new StressSession(cpu, () => stopwatch.Elapsed)`, calls `Start(StressProfile.For(SettingChoices.CpuLoadOrDefault(settings.CpuLoad)).CpuDutyCycle, SettingChoices.RunDuration(settings.DurationMinutes))`, exits itself, and pushes `new ScreensaverScreen(session, ScreensaverCatalog.Create(entry.Id), settings.FpsCap)`. Stopping the screensaver therefore lands back on the start screen.
- **`SettingsScreen`:** CPU Load, Frame Rate, Duration, CPU Threads and Display Mode. Each entry cycles with `SettingChoices` and saves straight away. The template's language and particle-effect options were removed from this screen.

**Changed from the plan:** no `StressMenuScreen` and no new resource keys. Menu text is hardcoded in English or reuses existing keys (`Settings`, `Exit`, `Back`, `ExitQuestion`). The run options live on the settings screen, and the screensaver is chosen per run.

**Manual checks** (outside the dev container, which can't run the game):
- Each setting cycles, and its value is still there after a restart.
- Start → pick a screensaver → Yes shows the screensaver and HUD.
- Task Manager shows CPU use rising: about 100% on all cores at High with all cores.
- Uncapped shows FPS above 60.
- Alt-tabbing away keeps the CPU load and rendering going.
- Any key returns to the start screen, and CPU use drops back to idle within about a second.
- A run ends on its own. To check quickly, set `"DurationMinutes": 1` in the settings JSON under `%AppData%`.

---

### Task 7: Remove the platformer template (open)

`StartScreen` replaced `MainMenuScreen` at startup, so the template's screens are no longer reachable but are still in the build.

**Files:**
- Delete: `PCWarmer.Core/Game/` (whole folder), `Screens/GameplayScreen.cs`, `Screens/EndOfLevelMessageState.cs`, `Screens/PauseScreen.cs`, `Screens/MainMenuScreen.cs`, `Screens/LoadingScreen.cs`, `Screens/AboutScreen.cs` (only reachable from `MainMenuScreen`), `Settings/PCWarmerLeaderboard.cs`.
- Delete content and its `PCWarmer.mgcb` entries: `Levels/`, `Tiles/`, `Overlays/`, the player, monster and gem sprites, the backgrounds other than `Backgrounds/Layer0_2`, and the gameplay-only sounds (`MonsterKilled`, `PlayerExitReached`, `PlayerFall`, `PlayerGemCollected`, `PlayerJump`, `PlayerKilled`, `PlayerPowerUp`). Keep `Backgrounds/Layer0_2` (`BackgroundScreen`), `Sprites/blank`, `Sprites/gradient`, `Fonts/Hud` and anything else the menus or screensavers load. Decide whether `Sounds/Music.mp3` stays.
- Modify: `PCWarmerGame.cs` (remove the leaderboard manager and its service registration).
- Modify: `CLAUDE.md` (describe the new modules, and remove the notes about the placeholder gameplay code and the leaderboard).

**Interfaces:** none new.

- [ ] **Step 1: Delete the files and remove the references** listed above.
- [ ] **Step 2: Run `dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj`.** Expected: everything builds and all tests pass. Then run `dotnet build PCWarmer.DesktopGL/PCWarmer.DesktopGL.csproj` on a desktop machine. Expected: no new warnings about removed types.
- [ ] **Step 3: Run `dotnet run --project PCWarmer.DesktopGL`** on a desktop machine. Expected: the app starts with no `ContentLoadException`, and the start screen, Settings and a full warming run all work.
- [ ] **Step 4: Commit** with `refactor: remove platformer template code and content`.
