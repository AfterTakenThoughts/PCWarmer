# PCWarmer Stress Engine Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the MonoGame platformer template with a stress engine that heats the room by loading the CPU (worker threads) and the GPU (screensavers), with intensity, frame-rate, duration and thread-count options.

**Architecture:** Keep the existing shared core (`PCWarmer.Core`) plus thin platform heads, the screen stack, settings persistence and localization. Add a `Stress/` module (CPU worker threads plus a session that owns timing) and a `Screensavers/` module (pluggable GPU loads hosted by one `GameScreen`). All load logic that does not need a `GraphicsDevice` stays in plain classes so a new xUnit project can test it.

**Tech Stack:** .NET 8, MonoGame 3.8.x (DesktopGL), System.Text.Json (existing settings), xUnit (new).

**Spec:** Part 1 of this document ("Architecture Design") plus [user-stories.md](../user-stories.md).

---

## Part 1: Architecture Design

### User stories this serves

| Story | Served by |
|---|---|
| Max heats the house | `Max` level: every CPU core at 100% plus an uncapped screensaver |
| QA tests the PC under extreme stress | CPU thread count, duration timer, HUD showing FPS, threads and elapsed time |
| Student leaves something running while idling | `Low` level, capped FPS, a nice-looking screensaver |

### Module layout (inside `PCWarmer.Core`)

```
Stress/
  StressLevel.cs          enum Low / Medium / Max
  StressProfile.cs        what each level means in numbers (CPU duty cycle, particle rate and cap)
  ICpuLoad.cs             seam so StressSession can be tested without real threads
  CpuStressService.cs     background worker threads that burn CPU
  StressSession.cs        one warming run: start/stop, wall-clock duration, elapsed and remaining time
Screensavers/
  IScreensaver.cs         LoadContent / SetLevel / Update / Draw / UnloadContent
  ScreensaverCatalog.cs   id -> screensaver; unknown ids fall back to the default
  EmissionBudget.cs       how many particles to emit this frame (rate plus cap)
  ParticleStormScreensaver.cs
Display/
  FpsCap.cs               Fps30 / Fps60 / Fps144 / Uncapped
  FrameRatePolicy.cs      FpsCap -> fixed-step, vsync and target frame time
  FrameCounter.cs         rolling FPS for the HUD
Screens/
  StressMenuScreen.cs     choose screensaver, intensity, FPS cap, duration and threads, then Start
  ScreensaverScreen.cs    hosts the session, the screensaver and the HUD; any key stops the run
```

### Key decisions

1. **CPU load runs on dedicated background threads, never in `Update()`.** One worker per requested thread runs real math in 100 ms windows: busy for `dutyCycle × 100 ms`, then asleep for the rest of the window. Workers are `IsBackground = true` (they can never keep the process alive) and `ThreadPriority.BelowNormal` (the UI and the OS stay responsive at 100%).
2. **A session is created for each run and owned by `ScreensaverScreen`.** The menu builds a `CpuStressService` and a `StressSession` from the saved settings, then pushes the screen. When the screen exits, it disposes both. There is no long-lived global stress service.
3. **The duration timer uses wall-clock time, not `GameTime`.** It stays correct when the window is unfocused or minimized, which is exactly when Max and the student leave it running.
4. **The GPU load is bounded.** A screensaver emits particles at a per-second rate (not per frame) up to a per-level cap on live particles. Uncapped FPS raises GPU load without growing memory.
5. **Frame-rate options use MonoGame's own switches.** A cap uses `IsFixedTimeStep = true` and `TargetElapsedTime`. `Uncapped` turns off fixed-step and `SynchronizeWithVerticalRetrace`. While a screensaver runs, `Game.InactiveSleepTime` is zero so an unfocused window keeps rendering.
6. **Settings extend the existing `PCWarmerSettings` POCO.** Values are always sanitized where they are used (unknown enum values, unknown screensaver id, out-of-range thread count), so an old or hand-edited JSON file can never crash the app.
7. **The platformer code is deleted.** That covers `Game/`, `GameplayScreen`, the leaderboard and the level content. Only DesktopGL is supported: the Android, iOS and WindowsDX heads don't exist on disk and are removed from `PCWarmer.sln`.

### Stress levels

| Level | CPU duty cycle | Particles per second | Max live particles |
|---|---|---|---|
| Low | 0.25 | 300 | 3,000 |
| Medium | 0.60 | 3,000 | 30,000 |
| Max | 1.00 | 20,000 | 150,000 |

---

## Part 2: Implementation Plan

All commands run from `PCWarmer/PCWarmer/`. Run `dotnet test` and `dotnet build` against the `.csproj` files, not the `.sln`.

## Global Constraints

- Target `net8.0`. MonoGame packages stay at `3.8.*`.
- New code lives in `PCWarmer.Core` under the namespaces `PCWarmer.Core.Stress`, `PCWarmer.Core.Screensavers` and `PCWarmer.Core.Display`. New screens use the existing `PCWarmer.Screens` namespace.
- Types the tests use are `public`.
- Every new UI string goes into `Resources.resx`, `Resources.es-ES.resx`, `Resources.fr-FR.resx` and `Resources.Designer.cs`.
- Every content asset that is loaded must be listed in `PCWarmer.Core/Content/PCWarmer.mgcb`.
- Stress worker threads: `IsBackground = true`, `Priority = ThreadPriority.BelowNormal`.
- Frame times are built with `TimeSpan.FromTicks(TimeSpan.TicksPerSecond / fps)`.

## Review Focus

1. **The window loses focus or is minimized mid-run.** CPU load and the duration timer keep going, and the run ends on time. Pinned by `Session_UsesClockNotFrames` (Task 3).
2. **The settings JSON is old, missing fields, or holds bad values** (e.g. `"StressLevel": 99`, `"CpuThreads": 999`, `"ScreensaverId": "gone"`). The app falls back to defaults and doesn't crash. Pinned by `Settings_MissingFieldsGetDefaults` (Task 4), `Profile_UndefinedLevelFallsBackToMedium` (Task 1), `Policy_UndefinedCapFallsBackTo60` (Task 4), `Constructor_ClampsThreadCount` (Task 2) and `Catalog_UnknownIdFallsBackToDefault` (Task 5).
3. **The user quits mid-run (Esc, or closing the window).** Every worker thread stops and no CPU load lingers. Pinned by `Dispose_StopsAllWorkers` (Task 2).
4. **Uncapped FPS, or a long frame hitch.** The live particle count never exceeds the level's cap. Pinned by `Budget_NeverExceedsCap` and `Budget_LongHitchIsCapped` (Task 5).
5. **A machine with 1 core, or `CpuThreads = 0`.** The thread count resolves to between 1 and `Environment.ProcessorCount`. Pinned by `Constructor_ClampsThreadCount` (Task 2).

---

### Task 1: Test project, solution cleanup, stress levels

**Files:**
- Create: `PCWarmer.Tests/PCWarmer.Tests.csproj` (xUnit, `net8.0`, references `../PCWarmer.Core/PCWarmer.Core.csproj` and `MonoGame.Framework.DesktopGL 3.8.*`, because Core marks MonoGame `PrivateAssets=All`)
- Modify: `PCWarmer.sln`: remove the `PCWarmer.Android`, `PCWarmer.iOS` and `PCWarmer.WindowsDX` projects (their folders don't exist) and add `PCWarmer.Tests`
- Create: `PCWarmer.Core/Stress/StressLevel.cs`, `PCWarmer.Core/Stress/StressProfile.cs`
- Test: `PCWarmer.Tests/Stress/StressProfileTests.cs`

**Interfaces:**
- Produces: `public enum StressLevel { Low, Medium, Max }`
- Produces: `public sealed record StressProfile(double CpuDutyCycle, int ParticlesPerSecond, int MaxParticles)` with `public static StressProfile For(StressLevel level)`. The values come from the Part 1 table. An undefined enum value returns the Medium profile.

- [ ] **Step 1: Write the failing tests**

```csharp
[Theory]
[InlineData(StressLevel.Low, 0.25, 300, 3_000)]
[InlineData(StressLevel.Medium, 0.60, 3_000, 30_000)]
[InlineData(StressLevel.Max, 1.00, 20_000, 150_000)]
public void Profile_MatchesTable(StressLevel level, double duty, int rate, int max)
    => Assert.Equal(new StressProfile(duty, rate, max), StressProfile.For(level));

[Fact]
public void Profile_UndefinedLevelFallsBackToMedium()
    => Assert.Equal(StressProfile.For(StressLevel.Medium), StressProfile.For((StressLevel)99));
```

- [ ] **Step 2: Run `dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj`.** Expected: a compile failure, because `StressLevel` is not defined.
- [ ] **Step 3: Implement `StressLevel` and `StressProfile.For`** with a `switch` expression whose `_` arm returns Medium.
- [ ] **Step 4: Run `dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj`.** Expected: 4 tests pass. Also run `dotnet build PCWarmer.DesktopGL/PCWarmer.DesktopGL.csproj`. Expected: the build succeeds.
- [ ] **Step 5: Commit** with `feat: add test project and stress level profiles`.

---

### Task 2: CPU stress service

**Files:**
- Create: `PCWarmer.Core/Stress/ICpuLoad.cs`, `PCWarmer.Core/Stress/CpuStressService.cs`
- Test: `PCWarmer.Tests/Stress/CpuStressServiceTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `public interface ICpuLoad { void Start(double dutyCycle); void Stop(); bool IsRunning { get; } int ThreadCount { get; } }`
- Produces: `public sealed class CpuStressService : ICpuLoad, IDisposable`
  - `CpuStressService(int threadCount)`: a value of 0 or less means `Environment.ProcessorCount`. Values above that are clamped to it.
  - `double DutyCycle { get; }` is clamped to [0, 1].
  - `int ActiveWorkers { get; }`
  - `long WorkUnitsCompleted { get; }`, incremented with `Interlocked`.
  - `Start` while already running restarts with the new duty cycle and never ends up with more than `ThreadCount` workers.
  - `Stop` signals a `CancellationTokenSource` and joins each worker with a 1-second timeout. It is safe to call more than once. `Dispose` calls `Stop`.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public void Constructor_ClampsThreadCount()
{
    Assert.Equal(Environment.ProcessorCount, new CpuStressService(0).ThreadCount);
    Assert.Equal(Environment.ProcessorCount, new CpuStressService(-3).ThreadCount);
    Assert.Equal(Environment.ProcessorCount, new CpuStressService(9_999).ThreadCount);
    Assert.Equal(1, new CpuStressService(1).ThreadCount);
}

[Fact] public async Task Start_DoesWorkOnEveryThread()
{
    using var cpu = new CpuStressService(2);
    cpu.Start(1.0);
    await Task.Delay(300);
    Assert.True(cpu.IsRunning);
    Assert.Equal(2, cpu.ActiveWorkers);
    Assert.True(cpu.WorkUnitsCompleted > 0);
}

[Fact] public void Start_Twice_DoesNotAddWorkers()
{
    using var cpu = new CpuStressService(2);
    cpu.Start(0.5); cpu.Start(1.0);
    Assert.Equal(2, cpu.ActiveWorkers);
    Assert.Equal(1.0, cpu.DutyCycle);
}

[Fact] public void Start_ClampsDutyCycle()
{
    using var cpu = new CpuStressService(1);
    cpu.Start(1.5);
    Assert.Equal(1.0, cpu.DutyCycle);
}

[Fact] public void Dispose_StopsAllWorkers()
{
    var cpu = new CpuStressService(2);
    cpu.Start(1.0);
    cpu.Dispose();
    Assert.False(cpu.IsRunning);
    Assert.Equal(0, cpu.ActiveWorkers);
    cpu.Stop(); // second stop is a no-op
}
```

- [ ] **Step 2: Run `dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj --filter CpuStressServiceTests`.** Expected: a compile failure.
- [ ] **Step 3: Implement `CpuStressService`.** The worker loop repeats 100 ms windows: it runs work units (for example, 10,000 iterations of `Math.Sqrt`/`Math.Sin` accumulated into a local that's written to a `volatile` field so the JIT can't drop it) until a `Stopwatch` reaches `DutyCycle × 100 ms`, then sleeps `Thread.Sleep` for the rest of the window. When the duty cycle is 1.0, the worker skips the sleep. Workers follow the thread rules in Global Constraints.
- [ ] **Step 4: Run the same filter.** Expected: 5 tests pass.
- [ ] **Step 5: Commit** with `feat: add CPU stress worker service`.

---

### Task 3: Stress session

**Files:**
- Create: `PCWarmer.Core/Stress/StressSession.cs`
- Test: `PCWarmer.Tests/Stress/StressSessionTests.cs`, including a `FakeCpuLoad : ICpuLoad` that records `LastDutyCycle` and `StopCount`

**Interfaces:**
- Consumes: `ICpuLoad` and `StressProfile.For` (Tasks 1 and 2).
- Produces: `public sealed class StressSession : IDisposable`
  - `StressSession(ICpuLoad cpu, Func<TimeSpan> clock)`. In production, the clock is `() => stopwatch.Elapsed` from a started `Stopwatch`.
  - `void Start(StressLevel level, TimeSpan? duration)`, where `null` means unlimited. It throws `ArgumentOutOfRangeException` if `duration <= TimeSpan.Zero`.
  - `void Update()` stops the session once `Elapsed >= Duration`.
  - `void Stop()` is safe to call more than once. `Dispose` calls `Stop` and then disposes the CPU load if it is `IDisposable`.
  - Properties: `bool IsRunning`, `StressLevel Level`, `TimeSpan? Duration`, `TimeSpan Elapsed` (frozen at stop), `TimeSpan? Remaining` (null when unlimited, never negative), `StressProfile Profile`, `int CpuThreads` (passes through `cpu.ThreadCount`).
  - `event EventHandler Stopped` is raised exactly once per run.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public void Start_DrivesCpuAtLevelDutyCycle()
{ /* Start(Medium, null) -> fake.LastDutyCycle == 0.60, session.IsRunning */ }

[Fact] public void Session_UsesClockNotFrames()
{
    var now = TimeSpan.Zero; var fake = new FakeCpuLoad();
    var s = new StressSession(fake, () => now);
    var stopped = 0; s.Stopped += (_, _) => stopped++;
    s.Start(StressLevel.Max, TimeSpan.FromMinutes(30));
    now = TimeSpan.FromMinutes(29); s.Update();
    Assert.True(s.IsRunning); Assert.Equal(TimeSpan.FromMinutes(1), s.Remaining);
    now = TimeSpan.FromMinutes(45); s.Update(); s.Update();   // one Update after a long gap
    Assert.False(s.IsRunning); Assert.Equal(1, stopped); Assert.Equal(1, fake.StopCount);
    Assert.Equal(TimeSpan.Zero, s.Remaining);
}

[Fact] public void Unlimited_NeverStopsOnItsOwn()
{ /* Start(Low, null); now = 10 days; Update -> IsRunning, Remaining == null */ }

[Fact] public void Start_RejectsNonPositiveDuration()
{ /* Assert.Throws<ArgumentOutOfRangeException> for TimeSpan.Zero and -1 minute */ }

[Fact] public void Stop_IsIdempotent()
{ /* Start, Stop, Stop -> Stopped raised once, Elapsed unchanged by later clock moves */ }
```

- [ ] **Step 2: Run `dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj --filter StressSessionTests`.** Expected: a compile failure.
- [ ] **Step 3: Implement `StressSession`.**
- [ ] **Step 4: Run the same filter.** Expected: 5 tests pass.
- [ ] **Step 5: Commit** with `feat: add stress session with wall-clock duration`.

---

### Task 4: Settings and frame-rate policy

**Files:**
- Create: `PCWarmer.Core/Display/FpsCap.cs`, `PCWarmer.Core/Display/FrameRatePolicy.cs`
- Modify: `PCWarmer.Core/Settings/PCWarmerSettings.cs` (add five properties that follow the existing `OnPropertyChanged` pattern)
- Test: `PCWarmer.Tests/Display/FrameRatePolicyTests.cs`, `PCWarmer.Tests/Settings/PCWarmerSettingsTests.cs`

**Interfaces:**
- Produces: `public enum FpsCap { Fps30, Fps60, Fps144, Uncapped }`
- Produces: `public readonly record struct FrameRateSettings(bool IsFixedTimeStep, bool VSync, TimeSpan TargetElapsedTime)` with `public static class FrameRatePolicy { public static FrameRateSettings For(FpsCap cap); }`. A capped value returns `(true, false, ticks/fps)`. `Uncapped` returns `(false, false, ticks/60)`. An undefined value returns the `Fps60` result.
- Produces these new `PCWarmerSettings` properties and defaults: `StressLevel StressLevel = Medium`, `FpsCap FpsCap = Fps60`, `string ScreensaverId = "particle-storm"`, `int CpuThreads = 0` (all cores), `int DurationMinutes = 0` (unlimited).

- [ ] **Step 1: Write the failing tests**

```csharp
[Theory]
[InlineData(FpsCap.Fps30, 30)] [InlineData(FpsCap.Fps60, 60)] [InlineData(FpsCap.Fps144, 144)]
public void Policy_CappedUsesFixedStep(FpsCap cap, int fps)
    => Assert.Equal(new FrameRateSettings(true, false, TimeSpan.FromTicks(TimeSpan.TicksPerSecond / fps)),
                    FrameRatePolicy.For(cap));

[Fact] public void Policy_UncappedDisablesFixedStepAndVSync()
{ /* IsFixedTimeStep == false && VSync == false */ }

[Fact] public void Policy_UndefinedCapFallsBackTo60()
    => Assert.Equal(FrameRatePolicy.For(FpsCap.Fps60), FrameRatePolicy.For((FpsCap)42));

[Fact] public void Settings_MissingFieldsGetDefaults()
{
    var s = JsonSerializer.Deserialize<PCWarmerSettings>("""{"FullScreen":true}""")!;
    Assert.True(s.FullScreen);
    Assert.Equal(StressLevel.Medium, s.StressLevel); Assert.Equal(FpsCap.Fps60, s.FpsCap);
    Assert.Equal("particle-storm", s.ScreensaverId);
    Assert.Equal(0, s.CpuThreads); Assert.Equal(0, s.DurationMinutes);
}

[Fact] public void Settings_RoundTrip()
{ /* set all five to non-defaults, Serialize, Deserialize, assert equal */ }
```

- [ ] **Step 2: Run `dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj --filter "FrameRatePolicyTests|PCWarmerSettingsTests"`.** Expected: a compile failure.
- [ ] **Step 3: Implement `FpsCap`, `FrameRatePolicy` and the settings properties.**
- [ ] **Step 4: Run the same filter.** Expected: 7 tests pass.
- [ ] **Step 5: Commit** with `feat: add stress settings and frame-rate policy`.

---

### Task 5: Screensaver engine and screen

**Files:**
- Create: `PCWarmer.Core/Screensavers/IScreensaver.cs`, `EmissionBudget.cs`, `ScreensaverCatalog.cs`, `ParticleStormScreensaver.cs`
- Create: `PCWarmer.Core/Display/FrameCounter.cs`
- Create: `PCWarmer.Core/Screens/ScreensaverScreen.cs`
- Test: `PCWarmer.Tests/Screensavers/EmissionBudgetTests.cs`, `ScreensaverCatalogTests.cs`, `PCWarmer.Tests/Display/FrameCounterTests.cs`

**Interfaces:**
- Consumes: `StressSession`, `StressProfile` and `StressLevel` (Tasks 1 and 3), and `FrameRatePolicy` (Task 4).
- Produces: `public interface IScreensaver { string Id { get; } int LiveParticles { get; } void LoadContent(ContentManager content, GraphicsDevice device); void SetLevel(StressLevel level); void Update(GameTime gameTime); void Draw(SpriteBatch spriteBatch, GameTime gameTime); void UnloadContent(); }`
- Produces: `public sealed class EmissionBudget(int particlesPerSecond, int maxParticles)` with `int Next(TimeSpan delta, int liveParticles)`. The fractional remainder carries over to later frames. The result is always between 0 and `maxParticles - liveParticles`.
- Produces: `public static class ScreensaverCatalog { public const string DefaultId = "particle-storm"; public static IReadOnlyList<string> Ids { get; } public static IScreensaver Create(string? id); }`. Unknown or null ids create the default. The constructor must not touch the GPU.
- Produces: `public sealed class FrameCounter` with `void Add(TimeSpan frameTime)` and `double Fps` (frames in the last 1 second divided by their total time, or 0 when empty).
- Produces: `class ScreensaverScreen : GameScreen` with constructor `ScreensaverScreen(StressSession session, IScreensaver screensaver, FpsCap fpsCap)`.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public void Budget_EmitsAtRate()        => Assert.Equal(16, new EmissionBudget(1000, 10_000).Next(TimeSpan.FromMilliseconds(16), 0));
[Fact] public void Budget_CarriesFractions()
{ var b = new EmissionBudget(100, 10_000);
  Assert.Equal(0, b.Next(TimeSpan.FromMilliseconds(5), 0)); Assert.Equal(1, b.Next(TimeSpan.FromMilliseconds(5), 0)); }
[Fact] public void Budget_NeverExceedsCap()     => Assert.Equal(0, new EmissionBudget(1000, 500).Next(TimeSpan.FromSeconds(1), 500));
[Fact] public void Budget_LongHitchIsCapped()   => Assert.Equal(300, new EmissionBudget(20_000, 3_000).Next(TimeSpan.FromSeconds(10), 2_700));

[Fact] public void Catalog_UnknownIdFallsBackToDefault()
{ Assert.Equal("particle-storm", ScreensaverCatalog.Create("gone").Id);
  Assert.Equal("particle-storm", ScreensaverCatalog.Create(null).Id);
  Assert.Contains("particle-storm", ScreensaverCatalog.Ids); }

[Fact] public void FrameCounter_Reports60() { /* Add 60 x ticks/60 -> Fps within 0.5 of 60 */ }
[Fact] public void FrameCounter_EmptyIsZero() => Assert.Equal(0, new FrameCounter().Fps);
```

- [ ] **Step 2: Run `dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj --filter "EmissionBudgetTests|ScreensaverCatalogTests|FrameCounterTests"`.** Expected: a compile failure.
- [ ] **Step 3: Implement `EmissionBudget`, `FrameCounter`, `ScreensaverCatalog` and `ParticleStormScreensaver`.** `ParticleStormScreensaver` owns its own `ParticleManager` built on `Sprites/blank`. Each frame, it emits `budget.Next(...)` particles. It spreads them over random screen positions by setting `ParticleManager.Position` before each `Emit` batch, cycles through `ParticleEffectType` values, and draws with `BlendState.Additive`. `SetLevel` rebuilds the budget from `StressProfile.For(level)`.
- [ ] **Step 4: Run the same filter.** Expected: 7 tests pass.
- [ ] **Step 5: Implement `ScreensaverScreen`.**
  - `LoadContent` applies `FrameRatePolicy.For(fpsCap)` to `Game.IsFixedTimeStep`, `Game.TargetElapsedTime` and `GraphicsDeviceManager.SynchronizeWithVerticalRetrace` (then calls `ApplyChanges()`). It also sets `Game.InactiveSleepTime = TimeSpan.Zero`, after saving the previous values. Then it loads the screensaver and calls `SetLevel(session.Level)`.
  - `Update` calls `session.Update()` and `screensaver.Update`. When `!session.IsRunning`, it calls `ExitScreen()`.
  - `HandleInput`: any key, mouse click, gamepad button or touch calls `session.Stop()` and then `ExitScreen()`.
  - `Draw` draws the screensaver and then a HUD in the top-left corner with `ScreenManager.Font`, one item per line: FPS (from `FrameCounter`, fed `gameTime.ElapsedGameTime`), the level, `session.Profile.CpuDutyCycle` as a percentage, `session.CpuThreads`, `screensaver.LiveParticles`, elapsed time, remaining time or "Unlimited", and "Press any key to stop".
  - `UnloadContent` restores the saved frame-rate values, unloads the screensaver and disposes the session.
- [ ] **Step 6: Run `dotnet build PCWarmer.DesktopGL/PCWarmer.DesktopGL.csproj`.** Expected: the build succeeds. The screen is first launched in Task 6.
- [ ] **Step 7: Commit** with `feat: add particle storm screensaver and screensaver screen`.

---

### Task 6: Stress menu and main menu wiring

**Files:**
- Create: `PCWarmer.Core/Screens/StressMenuScreen.cs`
- Modify: `PCWarmer.Core/Screens/MainMenuScreen.cs` (the Play entry opens `StressMenuScreen`)
- Modify: `PCWarmer.Core/Localization/Resources.resx`, `Resources.es-ES.resx`, `Resources.fr-FR.resx`, `Resources.Designer.cs`

**Interfaces:**
- Consumes: `SettingsManager<PCWarmerSettings>` (from `Game.Services`), `CpuStressService`, `StressSession`, `ScreensaverCatalog`, `ScreensaverScreen` and `FpsCap`.
- Produces: `class StressMenuScreen : MenuScreen`.

New resource keys, with their English values. The es-ES and fr-FR files get translations:

| Key | English |
|---|---|
| `WarmUp` | Warm Up |
| `StartWarming` | Start Warming |
| `Screensaver` | Screensaver |
| `Intensity` | Intensity |
| `FrameRate` | Frame Rate |
| `Duration` | Duration |
| `CpuThreads` | CPU Threads |
| `AllCores` | All Cores |
| `Unlimited` | Unlimited |
| `Uncapped` | Uncapped |
| `StressLow` / `StressMedium` / `StressMax` | Low / Medium / Max |
| `PressAnyKeyToStop` | Press any key to stop |

- [ ] **Step 1: Add the resource keys** to all three `.resx` files and add a property for each key to `Resources.Designer.cs`. Switch the HUD strings in `ScreensaverScreen` to these keys.
- [ ] **Step 2: Implement `StressMenuScreen`.** The title is `Resources.WarmUp`. Each entry cycles through its values when selected and saves the change through `settingsManager.Save()`, following the pattern in `SettingsScreen`. The entries are:
  - Screensaver: `ScreensaverCatalog.Ids`
  - Intensity: Low, Medium, Max
  - Frame Rate: 30, 60, 144, Uncapped
  - Duration: Unlimited, 15, 30, 60, 120 minutes
  - CPU Threads: All Cores, 1 … `Environment.ProcessorCount`
  - Start Warming

  Selecting Start Warming builds a `new CpuStressService(settings.CpuThreads)` and a `new StressSession(cpu, () => sw.Elapsed)` from a started `Stopwatch`. It then calls `Start(settings.StressLevel, settings.DurationMinutes > 0 ? TimeSpan.FromMinutes(settings.DurationMinutes) : null)` and pushes `new ScreensaverScreen(session, ScreensaverCatalog.Create(settings.ScreensaverId), settings.FpsCap)`.
- [ ] **Step 3: Rewire the Play entry in `MainMenuScreen`.** `PlayMenuEntrySelected` now calls `ScreenManager.AddScreen(new StressMenuScreen(), e.PlayerIndex)`. Remove the `readyToPlay` and `LoadingScreen` path.
- [ ] **Step 4: Verify by hand.** Run `dotnet run --project PCWarmer.DesktopGL` and check each of these:
  - Play opens Warm Up.
  - Each option cycles, and the value is still there after a restart.
  - Start shows particles and the HUD.
  - Task Manager shows CPU use rising: about 100% on all cores at Max with All Cores.
  - Uncapped shows FPS above 60.
  - Alt-tabbing away keeps the CPU load and rendering going.
  - Any key returns to the menu, and CPU use drops back to idle within about a second.
  - A 15-minute run ends on its own. To check this quickly, set `"DurationMinutes": 1` in `%AppData%` settings.
- [ ] **Step 5: Commit** with `feat: add warm-up menu and start stress runs from main menu`.

---

### Task 7: Remove the platformer template

**Files:**
- Delete: `PCWarmer.Core/Game/` (whole folder), `Screens/GameplayScreen.cs`, `Screens/EndOfLevelMessageState.cs`, `Settings/PCWarmerLeaderboard.cs`, and `Screens/PauseScreen.cs` if it is only referenced from `GameplayScreen`
- Delete: `Content/Levels/`, `Content/Tiles/`, and the gameplay-only sounds (`MonsterKilled`, `PlayerExitReached`, `PlayerFall`, `PlayerGemCollected`, `PlayerJump`, `PlayerKilled`, `PlayerPowerUp`), along with their entries in `PCWarmer.mgcb`. Keep `Sprites/blank`, `Sprites/gradient`, `Fonts/*`, `Sounds/Music.mp3`, and anything `BackgroundScreen` or the menus load.
- Modify: `PCWarmerGame.cs` (remove the leaderboard manager and its service registration)
- Modify: `MainMenuScreen.cs` (remove the `Level` background, the tutorial entry and tutorial state, and the `using System.Reflection.Emit` line)
- Modify: `CLAUDE.md` (describe the new modules, point to `user-stories.md` at the repo root, list `dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj`, and remove the Android, iOS and WindowsDX notes)

**Interfaces:** none new.

- [ ] **Step 1: Delete the files and remove the references** listed above.
- [ ] **Step 2: Run `dotnet build PCWarmer.DesktopGL/PCWarmer.DesktopGL.csproj`.** Expected: a successful build with no new warnings about removed types. Then run `dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj`. Expected: all tests pass.
- [ ] **Step 3: Run `dotnet run --project PCWarmer.DesktopGL`.** Expected: the app starts with no `ContentLoadException`, and the main menu, Settings, About and a full warm-up run all work.
- [ ] **Step 4: Commit** with `refactor: remove platformer template code and content`.
