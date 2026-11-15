# PCWarmer UI Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the template's text menus with one dark, red-accented start screen built on Myra. It has intensity presets and a fixed-size glowing ENGINE START button. The warming screen gets a fading gauge dashboard, and every run keeps the PC awake.

**Architecture:** Every decision a screen makes lives in plain, GPU-free classes that NUnit can test: presets (`RunPlan`), setting steps (`SettingChoices`), start-menu focus and values (`StartMenuModel`), confirm text, input rules, gauge maths, keep-awake calls and texture masks. Myra (with FontStashSharp fonts) only lays out and draws the start screen and the confirm box, and turns mouse clicks into calls on the model. Keyboard and gamepad keep going through `InputState`. The warming dashboard is drawn with `SpriteBatch` and the same fonts.

**Tech Stack:** .NET 8, MonoGame 3.8.x (DesktopGL and WindowsDX), Myra 1.6.8 (brings FontStashSharp), Inter font (SIL OFL 1.1), NUnit 4.

**Spec:** [docs/superpowers/specs/2026-11-15-ui-redesign-design.md](../specs/2026-11-15-ui-redesign-design.md)

## Global Constraints

- All commands run from `PCWarmer/PCWarmer/`. Verify with `dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj`. The dev container can't run the game; "Manual check" steps happen on a desktop machine.
- Target `net8.0`. MonoGame stays `3.8.*`. Myra is pinned to exactly `1.6.8`.
- Background colour `#141416`. Accent red `#C8102E`, used only on Start, the confirm box's "Yes, start", and the gauge arcs.
- Start button: round, 120 px across at the 800×480 reference size, "ENGINE" (small) over "START" (large), red radial gradient, dark ring, soft red glow, slow pulse. It never changes size or position whatever is picked.
- Secondary buttons (Exit, Cancel, Reset settings): rounded, dark grey fill, thin light border, normal-case text, never red. They must look different from the small uppercase, letter-spaced section labels.
- English only. New strings are hardcoded in English. Don't add translations.
- Every asset that's loaded must be in `PCWarmer.Core/Content/PCWarmer.mgcb`. Raw fonts use `/copy:` and are read from `Content/Fonts/...`.
- Only the screensaver goes full screen (`ScreensaverDisplay`). The menus stay windowed.
- Old settings files must load. Unknown values fall back to safe choices.
- Tests use NUnit 4, `Assert.That` style. Types the tests use are `public`.
- Out of scope: the ignition animation, measured CPU/GPU usage or temperatures, QA analysis tools, refresh-rate detection, removing the platformer template.

## Review Focus

1. **A settings file from before this change.** For example `"FpsCap": 0` (old 30 fps), `"FpsCap": 2` (old 144), no `ScreensaverId`, `"ScreensaverId": "gone"`, or `"HardwareLoad": 99`. It loads and runs at 60 fps, 120 fps, Particle Storm, Particle Storm and Medium respectively. Pinned in Tasks 1–2.
2. **A 1-, 2- or 3-core machine.** Every preset gets at least 1 thread, and Medium never uses all cores when there are 2 or more. Pinned in Task 3.
3. **The app is closed (window X or Alt+F4) mid-run, or `End` runs twice.** Keep-awake is released exactly once, and a second `Begin` doesn't stack. Pinned in Task 4.
4. **The Enter or click that started the run.** It doesn't immediately stop the run, and the mouse movement from clicking Start doesn't count as an intentional "show dashboard". Pinned by Task 5's new-presses-only rule and first-frame mouse skip, and by the manual check in Task 9.
5. **The confirm box is open over the start screen.** Clicks and keys reach only the box, never the start screen behind it. Pinned by a manual check in Task 8 and by `StartMenuModel` ignoring input while `Locked` (Task 7).

---

### Task 1: Frame-rate caps become 60 / 120 / Uncapped

**Files:**
- Modify: `PCWarmer.Core/Display/FpsCap.cs`, `PCWarmer.Core/Display/FrameRatePolicy.cs`, `PCWarmer.Core/Settings/SettingChoices.cs`
- Modify: `PCWarmer.Core/Screens/ScreensaverScreen.cs` (move `FpsCapText` out)
- Create: `PCWarmer.Core/Display/FpsCapText.cs`
- Test: `PCWarmer.Tests/Display/FrameRatePolicyTests.cs`, `PCWarmer.Tests/Display/FpsCapTextTests.cs`, `PCWarmer.Tests/Settings/SettingChoicesTests.cs`

**Interfaces:**
- Produces: `public enum FpsCap { Fps60 = 1, Fps120 = 2, Uncapped = 3 }`. The numbers are fixed so old JSON maps itself: old `Fps144` (2) reads as `Fps120`, and old `Fps30` (0) is undefined.
- Produces: `SettingChoices.FpsCapOrDefault(FpsCap cap) -> FpsCap`. A defined value is returned as is, and anything else gives `Fps60`.
- Produces: `public static class FpsCapText { public static string For(FpsCap cap); }`, giving `"60"`, `"120"` or `"Uncapped"`, and `"60"` for undefined values.

- [ ] **Step 1: Write the failing tests**

```csharp
// FrameRatePolicyTests
[TestCase(FpsCap.Fps60, 166_666L)]
[TestCase(FpsCap.Fps120, 83_333L)]
public void Capped_UsesFixedStepAtTheCapWithoutVSync(FpsCap cap, long ticks)
    => Assert.That(FrameRatePolicy.For(cap), Is.EqualTo(new FrameRateSettings(true, false, TimeSpan.FromTicks(ticks))));

[TestCase(0)]   // old Fps30
[TestCase(42)]
public void UndefinedCap_FallsBackTo60(int raw)
    => Assert.That(FrameRatePolicy.For((FpsCap)raw), Is.EqualTo(FrameRatePolicy.For(FpsCap.Fps60)));

// SettingChoicesTests
[TestCase(FpsCap.Fps60, ExpectedResult = FpsCap.Fps120)]
[TestCase(FpsCap.Fps120, ExpectedResult = FpsCap.Uncapped)]
[TestCase(FpsCap.Uncapped, ExpectedResult = FpsCap.Fps60)]
[TestCase((FpsCap)0, ExpectedResult = FpsCap.Fps60)]
public FpsCap NextFpsCap_Cycles(FpsCap cap) => SettingChoices.NextFpsCap(cap);

[TestCase(0, ExpectedResult = FpsCap.Fps60)]
[TestCase(2, ExpectedResult = FpsCap.Fps120)]   // old Fps144
[TestCase(42, ExpectedResult = FpsCap.Fps60)]
public FpsCap FpsCapOrDefault_MapsOldValues(int raw) => SettingChoices.FpsCapOrDefault((FpsCap)raw);

// FpsCapTextTests
[TestCase(FpsCap.Fps60, ExpectedResult = "60")]
[TestCase(FpsCap.Fps120, ExpectedResult = "120")]
[TestCase(FpsCap.Uncapped, ExpectedResult = "Uncapped")]
[TestCase((FpsCap)0, ExpectedResult = "60")]
public string For_ReadsLikeTheMenu(FpsCap cap) => FpsCapText.For(cap);
```

Delete the old `Fps30`/`Fps144` test cases.

- [ ] **Step 2: Run `dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj`.** Expected: compile errors (`Fps120`, `FpsCapText` and `FpsCapOrDefault` don't exist).
- [ ] **Step 3: Implement.**
  - Change the enum.
  - `FrameRatePolicy.For`: `Fps120 => Capped(120)`, with `_ => Capped(60)`.
  - `SettingChoices.FpsCapSteps = { Fps60, Fps120, Uncapped }`.
  - Add `FpsCapOrDefault`.
  - Move `ScreensaverScreen.FpsCapText` into `FpsCapText.For` and update its callers.
  - In `RunPlan` and `RunPlanTests`, temporarily swap `Fps30` → `Fps60` and `Fps144` → `Fps120` so the project compiles. Task 3 rewrites the presets.
- [ ] **Step 4: Run `dotnet test`.** Expected: all tests pass.
- [ ] **Step 5: Commit** with `feat: frame-rate caps 60/120/uncapped with fallback for old values`.

---

### Task 2: Settings for screensaver, screensaver detail and intensity order

**Files:**
- Modify: `PCWarmer.Core/Settings/PCWarmerSettings.cs`, `PCWarmer.Core/Settings/SettingChoices.cs`
- Test: `PCWarmer.Tests/Settings/PCWarmerSettingsTests.cs`, `PCWarmer.Tests/Settings/SettingChoicesTests.cs`

**Interfaces:**
- Consumes: `ScreensaverCatalog.DefaultId`, `ScreensaverCatalog.Entries`.
- Produces new `PCWarmerSettings` properties (`SetField` pattern):
  - `string ScreensaverId` (default `ScreensaverCatalog.DefaultId`)
  - `StressLevel ScreensaverLevel` (default `StressLevel.Medium`)
- `ResetToDefaults()` also resets `ScreensaverLevel`. It keeps `HardwareLoad`, `ScreensaverId` and `Language`.
- Produces in `SettingChoices`:
  - `IReadOnlyList<HardwareLoad> HardwareLoads` = None, Low, Medium, High, Custom (the segmented control's order)
  - `IReadOnlyList<StressLevel> Levels` = Low, Medium, High
  - `IReadOnlyList<FpsCap> FpsCaps`
  - `IReadOnlyList<int> Durations` = 0, 15, 30, 60, 120
  - `T Step<T>(IReadOnlyList<T> steps, T current, int direction)`: `direction` is +1 or −1, it wraps around at both ends, and an unknown `current` gives `steps[0]`
  - `int StepCpuThreads(int threads, int cores, int direction)`: 0 means all cores, then 1…`cores`, wrapping; out-of-range values give 0
  - `HardwareLoad HardwareLoadOrDefault(HardwareLoad load)`: unknown gives `Medium`
  - `StressLevel LevelOrDefault(StressLevel level)`: unknown gives `Medium`; it replaces `CpuLoadOrDefault`, so update its callers
  - `string ScreensaverIdOrDefault(string id)`: an id not in `ScreensaverCatalog.Entries` (or null) gives `ScreensaverCatalog.DefaultId`
  - Keep the existing `Next*` methods by implementing them with `Step(..., +1)`. Delete `NextHardwareLoad`'s old Custom-first order.

- [ ] **Step 1: Write the failing tests**

```csharp
[Test] public void NewSettings_PickParticleStormAtMediumDetail()
{
    var s = new PCWarmerSettings();
    Assert.That(s.ScreensaverId, Is.EqualTo("particle-storm"));
    Assert.That(s.ScreensaverLevel, Is.EqualTo(StressLevel.Medium));
    Assert.That(s.FpsCap, Is.EqualTo(FpsCap.Fps60));
}

[Test] public void FileWithoutNewFields_LoadsWithDefaults()
{
    var s = JsonSerializer.Deserialize<PCWarmerSettings>("""{"FpsCap":0,"HardwareLoad":2}""")!;
    Assert.That(s.ScreensaverId, Is.EqualTo("particle-storm"));
    Assert.That(SettingChoices.FpsCapOrDefault(s.FpsCap), Is.EqualTo(FpsCap.Fps60));
}

[Test] public void NewFields_SurviveSaveAndLoad()
{ /* ScreensaverId = "embers", ScreensaverLevel = High -> round-trip equal */ }

[Test] public void ResetToDefaults_ResetsDetailButKeepsScreensaverAndIntensity()
{ /* set ScreensaverLevel = High, ScreensaverId = "embers", HardwareLoad = High; reset ->
     ScreensaverLevel Medium, ScreensaverId "embers", HardwareLoad High */ }

[TestCase(HardwareLoad.None, 1, ExpectedResult = HardwareLoad.Low)]
[TestCase(HardwareLoad.Custom, 1, ExpectedResult = HardwareLoad.None)]
[TestCase(HardwareLoad.None, -1, ExpectedResult = HardwareLoad.Custom)]
[TestCase((HardwareLoad)99, 1, ExpectedResult = HardwareLoad.None)]
public HardwareLoad Step_WrapsBothWays(HardwareLoad load, int dir) => SettingChoices.Step(SettingChoices.HardwareLoads, load, dir);

[TestCase(0, 4, 1, ExpectedResult = 1)]
[TestCase(4, 4, 1, ExpectedResult = 0)]
[TestCase(0, 4, -1, ExpectedResult = 4)]
[TestCase(9, 4, -1, ExpectedResult = 0)]
public int StepCpuThreads_WrapsThroughAllCores(int t, int cores, int dir) => SettingChoices.StepCpuThreads(t, cores, dir);

[TestCase((HardwareLoad)99, ExpectedResult = HardwareLoad.Medium)]
public HardwareLoad HardwareLoadOrDefault_FallsBackToMedium(HardwareLoad l) => SettingChoices.HardwareLoadOrDefault(l);

[TestCase("embers", ExpectedResult = "embers")]
[TestCase("gone", ExpectedResult = "particle-storm")]
[TestCase(null, ExpectedResult = "particle-storm")]
public string ScreensaverIdOrDefault_FallsBack(string id) => SettingChoices.ScreensaverIdOrDefault(id);
```

Update any existing `NextHardwareLoad` cases to the new order.

- [ ] **Step 2: Run `dotnet test`.** Expected: compile errors for the new members.
- [ ] **Step 3: Implement** the properties, `ResetToDefaults` and the `SettingChoices` members above.
- [ ] **Step 4: Run `dotnet test`.** Expected: all tests pass.
- [ ] **Step 5: Commit** with `feat: save screensaver choice and detail; intensity order None..Custom`.

---

### Task 3: Presets and the confirm message

**Files:**
- Modify: `PCWarmer.Core/Stress/RunPlan.cs`
- Create: `PCWarmer.Core/Stress/RunConfirmation.cs`
- Test: `PCWarmer.Tests/Stress/RunPlanTests.cs` (rewrite), `PCWarmer.Tests/Stress/RunConfirmationTests.cs`

**Interfaces:**
- Consumes: `SettingChoices.LevelOrDefault`, `FpsCapOrDefault`, `HardwareLoadOrDefault`, `RunDuration` (Tasks 1–2).
- Produces: `public static RunPlan For(HardwareLoad load, PCWarmerSettings settings, int cores)`. The `RunPlan` record keeps its fields (`CpuDutyCycle`, `CpuThreads`, `FpsCap`, `ScreensaverLevel`, `Duration`, `FullScreen`) and `CreateCpuLoad()`.
  - **Presets:**

    | Load | Duty | Threads | FPS | Level |
    |---|---|---|---|---|
    | None | 0 | 0 | Fps60 | Low |
    | Low | 0.25 | max(1, cores / 2) | Fps60 | Low |
    | Medium | 0.60 | max(1, cores × 3 / 4) | Fps120 | Medium |
    | High | 1.00 | cores | Uncapped | High |

    Integer division rounds down, so on 2 or more cores Medium always leaves at least one core free.
  - **Every load**, presets included, takes `Duration` and `FullScreen` from `settings`.
  - **Custom:** duty from `LevelOrDefault(settings.CpuLoad)`, threads `settings.CpuThreads`, FPS `FpsCapOrDefault(settings.FpsCap)`, level `LevelOrDefault(settings.ScreensaverLevel)`.
  - **An unknown load** runs as Medium.
- Produces: `public static class RunConfirmation { public static (string Title, string Detail)? For(HardwareLoad load, string screensaverName); }`
  - `None` gives `null` (no box).
  - Otherwise the title is `$"Really warm your room with {screensaverName}?"` and the detail is `$"{load} intensity. Your CPU and GPU will run hard and your PC will get hot."`

- [ ] **Step 1: Write the failing tests**

```csharp
private static PCWarmerSettings Saved() => new()
{ CpuLoad = StressLevel.High, CpuThreads = 1, FpsCap = FpsCap.Uncapped, ScreensaverLevel = StressLevel.Low,
  DurationMinutes = 120, FullScreen = false };

[TestCase(HardwareLoad.None, 0.0, 0, FpsCap.Fps60, StressLevel.Low)]
[TestCase(HardwareLoad.Low, 0.25, 4, FpsCap.Fps60, StressLevel.Low)]
[TestCase(HardwareLoad.Medium, 0.60, 6, FpsCap.Fps120, StressLevel.Medium)]
[TestCase(HardwareLoad.High, 1.00, 8, FpsCap.Uncapped, StressLevel.High)]
public void Preset_OnEightCores(HardwareLoad load, double duty, int threads, FpsCap fps, StressLevel level)
{
    RunPlan plan = RunPlan.For(load, Saved(), cores: 8);
    Assert.That((plan.CpuDutyCycle, plan.CpuThreads, plan.FpsCap, plan.ScreensaverLevel),
                Is.EqualTo((duty, threads, fps, level)));
}

[TestCase(1, 1, 1, 1)]
[TestCase(2, 1, 1, 2)]
[TestCase(3, 1, 2, 3)]
[TestCase(4, 2, 3, 4)]
public void PresetThreads_NeverZeroAndMediumLeavesACoreFree(int cores, int low, int medium, int high)
{
    Assert.That(RunPlan.For(HardwareLoad.Low, Saved(), cores).CpuThreads, Is.EqualTo(low));
    Assert.That(RunPlan.For(HardwareLoad.Medium, Saved(), cores).CpuThreads, Is.EqualTo(medium));
    Assert.That(RunPlan.For(HardwareLoad.High, Saved(), cores).CpuThreads, Is.EqualTo(high));
}

[TestCase(HardwareLoad.None)] [TestCase(HardwareLoad.High)] [TestCase(HardwareLoad.Custom)]
public void EveryLoad_UsesSavedRunLengthAndDisplay(HardwareLoad load)
{
    RunPlan plan = RunPlan.For(load, Saved(), cores: 8);
    Assert.That(plan.Duration, Is.EqualTo(TimeSpan.FromMinutes(120)));
    Assert.That(plan.FullScreen, Is.False);
}

[Test] public void Custom_UsesItsOwnScreensaverLevel()
    => Assert.That(RunPlan.For(HardwareLoad.Custom, Saved(), 8),
                   Is.EqualTo(new RunPlan(1.0, 1, FpsCap.Uncapped, StressLevel.Low, TimeSpan.FromMinutes(120), false)));

[Test] public void UnknownLoad_RunsAsMedium()
    => Assert.That(RunPlan.For((HardwareLoad)99, Saved(), 8), Is.EqualTo(RunPlan.For(HardwareLoad.Medium, Saved(), 8)));

// RunConfirmationTests
[Test] public void None_HasNoConfirmation() => Assert.That(RunConfirmation.For(HardwareLoad.None, "Embers"), Is.Null);

[Test] public void High_AsksWithTheScreensaverAndIntensity()
    => Assert.That(RunConfirmation.For(HardwareLoad.High, "Embers"), Is.EqualTo((
        "Really warm your room with Embers?",
        "High intensity. Your CPU and GPU will run hard and your PC will get hot.")));
```

Keep the existing `CreateCpuLoad` tests (None gives `NoCpuLoad`).

- [ ] **Step 2: Run `dotnet test`.** Expected: compile errors (`cores` parameter, `RunConfirmation`).
- [ ] **Step 3: Implement** `RunPlan.For` and `RunConfirmation.For`. Update the one caller, `ScreensaverPickerScreen.StartWarming`, to pass `Environment.ProcessorCount`. Task 8 deletes that screen.
- [ ] **Step 4: Run `dotnet test`.** Expected: all tests pass.
- [ ] **Step 5: Commit** with `feat: intensity presets set threads and frame rate; confirm text`.

---

### Task 4: Keep the display and PC awake during every run

**Files:**
- Create: `PCWarmer.Core/Display/IKeepAwake.cs`, `PCWarmer.Core/Display/WindowsKeepAwake.cs`, `PCWarmer.Core/Display/SdlKeepAwake.cs`, `PCWarmer.Core/Display/KeepAwake.cs`
- Modify: `PCWarmer.Core/Screens/ScreensaverScreen.cs` (call it)
- Test: `PCWarmer.Tests/Display/WindowsKeepAwakeTests.cs`

**Interfaces:**
- Produces: `public interface IKeepAwake { void Begin(); void End(); }`. Both are idempotent.
- Produces: `public sealed class WindowsKeepAwake : IKeepAwake`
  - `WindowsKeepAwake(Func<uint, uint> setThreadExecutionState)`; production passes a P/Invoke to `kernel32!SetThreadExecutionState`
  - `public const uint AwakeFlags = 0x80000003;` (`ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED`)
  - `public const uint ReleaseFlags = 0x80000000;` (`ES_CONTINUOUS`)
- Produces: `public sealed class SdlKeepAwake : IKeepAwake`. It P/Invokes `SDL2!SDL_DisableScreenSaver` / `SDL_EnableScreenSaver`, and catches `DllNotFoundException` and `EntryPointNotFoundException` (becoming a no-op).
- Produces: `public static class KeepAwake { public static IKeepAwake ForCurrentPlatform(); }`. On `OperatingSystem.IsWindows()` it returns `WindowsKeepAwake`, otherwise `SdlKeepAwake`.
- `ScreensaverScreen` gets an `IKeepAwake keepAwake` constructor parameter, after `fullScreen`. It calls `Begin()` at the end of `LoadContent` and `End()` at the start of `UnloadContent`. Process exit releases Windows execution state on its own.

- [ ] **Step 1: Write the failing tests**

```csharp
[Test] public void Begin_AsksForDisplayAndSystemOnce()
{
    var calls = new List<uint>(); var k = new WindowsKeepAwake(f => { calls.Add(f); return 1; });
    k.Begin(); k.Begin();
    Assert.That(calls, Is.EqualTo(new[] { 0x80000003u }));
}

[Test] public void End_ReleasesOnceAndOnlyAfterBegin()
{
    var calls = new List<uint>(); var k = new WindowsKeepAwake(f => { calls.Add(f); return 1; });
    k.End(); k.Begin(); k.End(); k.End();
    Assert.That(calls, Is.EqualTo(new[] { 0x80000003u, 0x80000000u }));
}
```

- [ ] **Step 2: Run `dotnet test --filter WindowsKeepAwakeTests`.** Expected: compile error.
- [ ] **Step 3: Implement** the four types, and wire `KeepAwake.ForCurrentPlatform()` into the `ScreensaverScreen` construction in `ScreensaverPickerScreen`.
- [ ] **Step 4: Run `dotnet test`.** Expected: all tests pass.
- [ ] **Step 5: Manual check (Windows):** during a run, `powercfg /requests` lists PCWarmer under DISPLAY and SYSTEM. After stopping, it no longer does.
- [ ] **Step 6: Commit** with `feat: keep display and PC awake during every run`.

---

### Task 5: Dashboard fades, mouse brings it back, any press stops

**Files:**
- Modify: `PCWarmer.Core/Screens/ScreensaverInput.cs`, `PCWarmer.Core/Screens/ScreensaverScreen.cs`
- Test: `PCWarmer.Tests/Screens/ScreensaverInputTests.cs` (rewrite)

**Interfaces:**
- Produces: `public enum ScreensaverAction { None, ShowDashboard, Stop }`. `ToggleHud` is removed.
- Produces: `public static ScreensaverAction Decide(IReadOnlyCollection<Keys> newKeys, IReadOnlyCollection<Buttons> newButtons, bool newClick, Vector2 mouseDelta)`
  - Any new key, button or click gives `Stop`.
  - Otherwise, a mouse movement of more than `ScreensaverInput.MoveThreshold` pixels (`public const float MoveThreshold = 4f`, measured as vector length) gives `ShowDashboard`.
  - Anything else gives `None`.
  - `ToggleHudKey`, `ToggleHudButton` and the Hide UI button are deleted.
- `ScreensaverScreen`:
  - A `HintFade dashboardFade = new(hold: TimeSpan.FromSeconds(3), fade: TimeSpan.FromSeconds(1))`, with `Show()` in `LoadContent` and on `ShowDashboard`.
  - The dashboard is drawn at `dashboardFade.Opacity * TransitionAlpha`.
  - The mouse cursor is visible only while `Opacity > 0`.
  - `mouseDelta` is ignored on the first frame after `LoadContent`, so the click on Start doesn't count.
  - Remove the "Press any key to show UI" hint and the Hide UI button.

- [ ] **Step 1: Write the failing tests**

```csharp
private static readonly Keys[] NoKeys = Array.Empty<Keys>();
private static readonly Buttons[] NoButtons = Array.Empty<Buttons>();

[Test] public void Nothing_DoesNothing()
    => Assert.That(ScreensaverInput.Decide(NoKeys, NoButtons, false, Vector2.Zero), Is.EqualTo(ScreensaverAction.None));

[TestCase(Keys.H)] [TestCase(Keys.Space)] [TestCase(Keys.Escape)]
public void AnyKey_Stops(Keys key)
    => Assert.That(ScreensaverInput.Decide(new[] { key }, NoButtons, false, Vector2.Zero), Is.EqualTo(ScreensaverAction.Stop));

[Test] public void AnyGamepadButton_Stops()
    => Assert.That(ScreensaverInput.Decide(NoKeys, new[] { Buttons.Y }, false, Vector2.Zero), Is.EqualTo(ScreensaverAction.Stop));

[Test] public void Click_Stops()
    => Assert.That(ScreensaverInput.Decide(NoKeys, NoButtons, true, Vector2.Zero), Is.EqualTo(ScreensaverAction.Stop));

[Test] public void ClickWhileMoving_StillStops()
    => Assert.That(ScreensaverInput.Decide(NoKeys, NoButtons, true, new Vector2(50, 0)), Is.EqualTo(ScreensaverAction.Stop));

[Test] public void MouseMove_ShowsTheDashboard()
    => Assert.That(ScreensaverInput.Decide(NoKeys, NoButtons, false, new Vector2(3, 4.1f)), Is.EqualTo(ScreensaverAction.ShowDashboard));

[Test] public void TinyJitter_IsIgnored()
    => Assert.That(ScreensaverInput.Decide(NoKeys, NoButtons, false, new Vector2(2, 2)), Is.EqualTo(ScreensaverAction.None));
```

- [ ] **Step 2: Run `dotnet test --filter ScreensaverInputTests`.** Expected: compile errors.
- [ ] **Step 3: Implement** `Decide` and the `ScreensaverScreen` changes above. Keep `CollectNewPresses` (only keys that went down this frame) so the key that started the run doesn't stop it.
- [ ] **Step 4: Run `dotnet test`.** Expected: all tests pass.
- [ ] **Step 5: Commit** with `feat: warming dashboard fades and returns on mouse move`.

---

### Task 6: Myra, fonts, theme and generated shapes

**Files:**
- Modify: `PCWarmer.Core/PCWarmer.Core.csproj` (add `<PackageReference Include="Myra" Version="1.6.8" />`)
- Create: `PCWarmer.Core/Content/Fonts/Inter-Regular.ttf`, `Inter-SemiBold.ttf`, `OFL.txt`. Get them from the official Inter release on GitHub (rsms/inter, v4.x) and include its licence file.
- Modify: `PCWarmer.Core/Content/PCWarmer.mgcb`: `/copy:Fonts/Inter-Regular.ttf`, `/copy:Fonts/Inter-SemiBold.ttf`, `/copy:Fonts/OFL.txt`
- Create: `PCWarmer.Core/Ui/UiTheme.cs`, `PCWarmer.Core/Ui/UiFonts.cs`, `PCWarmer.Core/Ui/ShapeMasks.cs`, `PCWarmer.Core/Ui/UiTextures.cs`, `PCWarmer.Core/Ui/UiScale.cs`, `PCWarmer.Core/Screens/MyraScreen.cs`
- Modify: `PCWarmer.Core/PCWarmerGame.cs` (set `MyraEnvironment.Game = this` in `Initialize`, and register a `UiFonts` service after `LoadContent`), `PCWarmer.Core/Screens/BackgroundScreen.cs` (clear to `UiTheme.Background` and stop loading `Backgrounds/Layer0_2`)
- Test: `PCWarmer.Tests/Ui/ShapeMasksTests.cs`, `PCWarmer.Tests/Ui/UiScaleTests.cs`

**Interfaces:**
- Produces: `public static class UiTheme`, with `Color` fields:
  - `Background #141416`, `Surface #1D1E22`, `SurfaceSelected #2A2B30`
  - `Border #2E2F35`, `BorderFocused #8A8A90`
  - `Text #CFCFD4`, `TextStrong #F2F2F2`, `TextMuted #7C7C84`
  - `Accent #C8102E`, `AccentLight #FF5A4E`
  - `public const int CornerRadius = 10;`
- Produces: `public sealed class UiFonts { public UiFonts(Stream regular, Stream semiBold); public SpriteFontBase Regular(int px); public SpriteFontBase SemiBold(int px); }`, built on two FontStashSharp `FontSystem`s. Streams come from `TitleContainer.OpenStream("Content/Fonts/...")`.
- Produces: `public static class ShapeMasks`
  - `float[] RoundedRect(int width, int height, int radius)`: row-major alpha 0..1. Corners outside the radius are 0, the inside is 1, and the edges are anti-aliased over 1 px.
  - `float[] Disc(int size)`: a filled circle, 1 inside and 0 outside.
  - `float[] Glow(int size)`: radial falloff, 1 at the centre and 0 at the edge.
- Produces: `public static class UiTextures`. It turns each mask into a `Texture2D` (white with alpha) for a device and caches it. It also has `NinePatchRegion RoundedPanel(GraphicsDevice, int radius)` for Myra backgrounds. Its GPU code isn't unit-tested.
- Produces: `public static class UiScale { public static float For(Point windowSize); }`. That's `min(width / 800, height / 480)`, never below 0.5. Sizes in the spec are at scale 1.
- Produces: `abstract class MyraScreen : GameScreen`. It owns a Myra `Desktop` and calls `protected abstract Widget BuildRoot(float scale)` on `LoadContent` and whenever the window size changes. Each frame it sets the root's opacity to `TransitionAlpha` and calls `desktop.Render()` in `Draw`. It renders but ignores mouse input while `!IsActive` (another screen has focus). Disabled widgets must look the same as enabled ones.

- [ ] **Step 1: Write the failing tests**

```csharp
[Test] public void RoundedRect_CornersAreClearAndCentreIsSolid()
{
    float[] m = ShapeMasks.RoundedRect(40, 20, 10);
    Assert.That(m[0], Is.EqualTo(0f));              // top-left corner pixel
    Assert.That(m[10 * 40 + 20], Is.EqualTo(1f));   // centre
    Assert.That(m.Length, Is.EqualTo(800));
}

[Test] public void Disc_IsSolidInsideAndClearOutside()
{
    float[] m = ShapeMasks.Disc(32);
    Assert.That(m[16 * 32 + 16], Is.EqualTo(1f));
    Assert.That(m[0], Is.EqualTo(0f));
}

[Test] public void Glow_FadesFromCentreToEdge()
{
    float[] m = ShapeMasks.Glow(32);
    Assert.That(m[16 * 32 + 16], Is.GreaterThan(m[16 * 32 + 28]));
    Assert.That(m[16 * 32 + 31], Is.LessThan(0.1f));
}

[TestCase(800, 480, ExpectedResult = 1f)]
[TestCase(1600, 960, ExpectedResult = 2f)]
[TestCase(1920, 480, ExpectedResult = 1f)]
[TestCase(200, 100, ExpectedResult = 0.5f)]
public float For_FitsTheReferenceCanvas(int w, int h) => UiScale.For(new Point(w, h));
```

- [ ] **Step 2: Run `dotnet test --filter "ShapeMasksTests|UiScaleTests"`.** Expected: compile errors.
- [ ] **Step 3: Implement** the package reference, fonts and content entries, `UiTheme`, `UiFonts`, `ShapeMasks`, `UiTextures`, `UiScale`, `MyraScreen`, and the `PCWarmerGame` and `BackgroundScreen` changes.
- [ ] **Step 4: Run `dotnet test`.** Expected: all tests pass and Core builds with Myra.
- [ ] **Step 5: Commit** with `feat: add Myra, Inter fonts, dark theme and generated shapes`.

---

### Task 7: Start menu model

**Files:**
- Create: `PCWarmer.Core/Ui/StartMenuModel.cs`
- Test: `PCWarmer.Tests/Ui/StartMenuModelTests.cs`

**Interfaces:**
- Consumes: `PCWarmerSettings`, `SettingChoices` (Task 2), `ScreensaverCatalog.Entries`, `FpsCapText.For` (Task 1).
- Produces: `public enum StartMenuRow { Intensity, CpuLoad, ScreensaverDetail, CpuThreads, FrameRate, Screensaver, Start, RunLength, Display, ResetSettings, Exit }`
- Produces: `public enum StartMenuCommand { None, StartRun, Exit, ResetSettings }`
- Produces: `public sealed class StartMenuModel`
  - `StartMenuModel(PCWarmerSettings settings, int cores)`
  - `IReadOnlyList<StartMenuRow> VisibleRows`: in focus order Intensity, [CpuLoad, ScreensaverDetail, CpuThreads, FrameRate only when `HardwareLoad == Custom`], Screensaver, Start, RunLength, Display, ResetSettings, Exit
  - `StartMenuRow Focused`: `Start` when the model is created; if the focused row stops being visible, focus moves to `Intensity`
  - `bool Locked { get; set; }`: while true, every method below does nothing and returns `None`/`false`
  - `void MoveFocus(int delta)`: wraps around through `VisibleRows`
  - `void FocusRow(StartMenuRow row)`
  - `bool ChangeValue(int direction)`: steps the focused row's setting through `SettingChoices.Step` and `StepCpuThreads`
    - Intensity: `HardwareLoads`
    - CpuLoad: `Levels` → `CpuLoad`
    - ScreensaverDetail: `Levels` → `ScreensaverLevel`
    - CpuThreads: `StepCpuThreads`
    - FrameRate: `FpsCaps`
    - Screensaver: the ids of `ScreensaverCatalog.Entries` → `ScreensaverId`
    - RunLength: `Durations` → `DurationMinutes`
    - Display: toggles `FullScreen`
    - It returns true if a setting changed, so the caller saves. Start, ResetSettings and Exit return false.
  - `StartMenuCommand Activate()`
    - Start gives `StartRun`, Exit gives `Exit`, ResetSettings gives `ResetSettings`
    - on a value row it acts as `ChangeValue(+1)` and returns `None`
  - `void SetIntensity(HardwareLoad load)` and `void SetScreensaver(string id)`, for mouse clicks; each also focuses its row
  - `string ValueText(StartMenuRow row)`:
    - RunLength: `"30 min"` or `"Unlimited"`
    - Display: `"Full screen"` or `"Window"`
    - CpuThreads: `"All cores (8)"` or `"3"`
    - FrameRate: `FpsCapText`
    - CpuLoad and ScreensaverDetail: `"Low"`, `"Medium"` or `"High"`
    - Screensaver: the entry's `Name`
    - Intensity: the load name
    - It shows fallbacks for bad saved values.

- [ ] **Step 1: Write the failing tests**

```csharp
private static StartMenuModel Model(HardwareLoad load = HardwareLoad.Medium)
    => new(new PCWarmerSettings { HardwareLoad = load }, cores: 8);

[Test] public void Opens_FocusedOnStart() => Assert.That(Model().Focused, Is.EqualTo(StartMenuRow.Start));

[Test] public void Preset_HidesCustomRows()
    => Assert.That(Model().VisibleRows, Is.EqualTo(new[] { StartMenuRow.Intensity, StartMenuRow.Screensaver,
        StartMenuRow.Start, StartMenuRow.RunLength, StartMenuRow.Display, StartMenuRow.ResetSettings, StartMenuRow.Exit }));

[Test] public void Custom_ShowsCustomRowsAfterIntensity()
    => Assert.That(Model(HardwareLoad.Custom).VisibleRows.Take(5), Is.EqualTo(new[] { StartMenuRow.Intensity,
        StartMenuRow.CpuLoad, StartMenuRow.ScreensaverDetail, StartMenuRow.CpuThreads, StartMenuRow.FrameRate }));

[Test] public void LeavingCustom_MovesFocusOffAHiddenRow()
{
    var m = Model(HardwareLoad.Custom); m.FocusRow(StartMenuRow.CpuThreads);
    m.SetIntensity(HardwareLoad.High);
    Assert.That(m.Focused, Is.EqualTo(StartMenuRow.Intensity));
}

[Test] public void MoveFocus_Wraps()
{ var m = Model(); m.FocusRow(StartMenuRow.Exit); m.MoveFocus(1); Assert.That(m.Focused, Is.EqualTo(StartMenuRow.Intensity)); }

[Test] public void ChangeValue_OnIntensity_StepsAndReportsChange()
{
    var s = new PCWarmerSettings { HardwareLoad = HardwareLoad.High }; var m = new StartMenuModel(s, 8);
    m.FocusRow(StartMenuRow.Intensity);
    Assert.That(m.ChangeValue(1), Is.True);
    Assert.That(s.HardwareLoad, Is.EqualTo(HardwareLoad.Custom));
}

[Test] public void ChangeValue_OnStart_DoesNothing() => Assert.That(Model().ChangeValue(1), Is.False);

[Test] public void Activate_MapsCommands()
{
    var m = Model();
    Assert.That(m.Activate(), Is.EqualTo(StartMenuCommand.StartRun));
    m.FocusRow(StartMenuRow.Exit); Assert.That(m.Activate(), Is.EqualTo(StartMenuCommand.Exit));
    m.FocusRow(StartMenuRow.ResetSettings); Assert.That(m.Activate(), Is.EqualTo(StartMenuCommand.ResetSettings));
}

[Test] public void Locked_IgnoresEverything()
{
    var s = new PCWarmerSettings(); var m = new StartMenuModel(s, 8) { Locked = true };
    m.MoveFocus(1); m.SetIntensity(HardwareLoad.High);
    Assert.That(m.Focused, Is.EqualTo(StartMenuRow.Start));
    Assert.That(s.HardwareLoad, Is.EqualTo(HardwareLoad.Medium));
    Assert.That(m.Activate(), Is.EqualTo(StartMenuCommand.None));
}

[TestCase(StartMenuRow.RunLength, ExpectedResult = "30 min")]
[TestCase(StartMenuRow.Display, ExpectedResult = "Full screen")]
[TestCase(StartMenuRow.CpuThreads, ExpectedResult = "All cores (8)")]
[TestCase(StartMenuRow.FrameRate, ExpectedResult = "60")]
[TestCase(StartMenuRow.Screensaver, ExpectedResult = "Particle Storm")]
public string ValueText_ForDefaults(StartMenuRow row) => Model().ValueText(row);
```

- [ ] **Step 2: Run `dotnet test --filter StartMenuModelTests`.** Expected: compile error.
- [ ] **Step 3: Implement `StartMenuModel`.**
- [ ] **Step 4: Run `dotnet test`.** Expected: all tests pass.
- [ ] **Step 5: Commit** with `feat: start menu model for focus and values`.

---

### Task 8: New start screen and confirm box; remove the picker and settings screens

**Files:**
- Rewrite: `PCWarmer.Core/Screens/StartScreen.cs` (now a `MyraScreen`)
- Create: `PCWarmer.Core/Screens/ConfirmScreen.cs`, `PCWarmer.Core/Ui/EngineStartButton.cs`
- Delete: `PCWarmer.Core/Screens/ScreensaverPickerScreen.cs`, `SettingsScreen.cs`, `MenuHeading.cs`
- Modify: `PCWarmer.Core/Screens/MainMenuScreen.cs:292` (remove the Settings entry and its handler; template code, unreachable)
- Modify: `README.md` (Quick start steps 3–5), `CLAUDE.md` (Myra/Ui architecture paragraph; startup pushes `BackgroundScreen` then `StartScreen`), `docs/roadmap.md` (add "Ignition animation" as a later goal; mark the two UI items in progress)

**Interfaces:**
- Consumes: `StartMenuModel`, `RunPlan.For(load, settings, cores)`, `RunConfirmation.For`, `KeepAwake.ForCurrentPlatform()`, `UiTheme`, `UiFonts`, `UiTextures`, `MyraScreen`.
- Produces: `class ConfirmScreen : MyraScreen`
  - `ConfirmScreen(string title, string detail, string yesText, bool yesIsAccent)` with `event EventHandler<PlayerIndexEventArgs> Accepted` and `Cancelled`
  - it's a popup: a dimmed backdrop, then a rounded `Surface` panel
  - **Yes button:** `Accent`-filled when `yesIsAccent`, otherwise a secondary button
  - **Cancel:** always a secondary button
  - **Keys:** Enter means Yes, Esc means Cancel, and Left/Right move focus between the two buttons
- Produces: `class EngineStartButton : Widget`
  - a fixed `120 * scale` px square: glow, then a dark ring, then a red disc (`AccentLight` to `Accent`), with "ENGINE" in `SemiBold(12*scale)` over "START" in `SemiBold(21*scale)`
  - the pulse scales the glow's alpha between 0.55 and 0.85 over a 2.4-second sine
  - a thin `BorderFocused` ring when focused
  - a `Click` event
- `StartScreen`:
  - **Layout:** two columns at roughly 1.35 : 1.
    - Left: the project name "PCWarmer" at the very top, written exactly that way and never uppercased (`TextStrong`, `SemiBold(22*scale)`, never red), an INTENSITY label with a 5-segment control, the Custom rows (when visible), a SCREENSAVER label with a 2×2 tile grid (4×1 with short names "Storm", "Embers", "Starfield", "Orbs" when Custom is open), and an Exit secondary button.
    - Right: `EngineStartButton`, vertically centred and fixed in place, then a panel with the Run length and Display rows, then a "Reset settings" secondary button.
  - **Styling:**
    - section labels are `TextMuted`, uppercase, `Regular(10*scale)`; no letter-spacing (approved by the user)
    - rows are name on the left and "‹ value ›" on the right
    - the focused row or tile gets a `BorderFocused` outline
    - the picked segment or tile gets `SurfaceSelected`, with a faint `Accent` 50% edge on the picked tile
  - **Keyboard and gamepad** go through `InputState`: Up/Down → `MoveFocus`, Left/Right → `ChangeValue`, Enter/A → `Activate`, Esc/B → the exit question.
  - **Mouse:** clicks call `SetIntensity`, `SetScreensaver`, `FocusRow` + `ChangeValue(±1)` on the ‹ › arrows, or `Activate`.
  - **Saving:** after any change, `settingsManager.Save()` and rebuild the widget values.
  - **While a `ConfirmScreen` is open**, set `model.Locked = true`; set it back to false on Accepted or Cancelled.
  - **StartRun:**
    1. `RunPlan.For(SettingChoices.HardwareLoadOrDefault(settings.HardwareLoad), settings, Environment.ProcessorCount)` and `RunConfirmation.For(load, entry.Name)`.
    2. If the confirmation is null, start straight away. Otherwise show `ConfirmScreen(title, detail, "Yes, start", yesIsAccent: true)`.
    3. Starting builds a `StressSession` from `plan.CreateCpuLoad()`, then adds `new ScreensaverScreen(session, ScreensaverCatalog.Create(SettingChoices.ScreensaverIdOrDefault(settings.ScreensaverId), plan.ScreensaverLevel), plan.FpsCap, plan.FullScreen, KeepAwake.ForCurrentPlatform())`.
    4. `StartScreen` stays underneath, so stopping returns to it.
  - **Exit:** `ConfirmScreen(Resources.ExitQuestion, "", "Exit", yesIsAccent: false)`, and Accepted → `Game.Exit()`.
  - **ResetSettings:** `ConfirmScreen("Reset all settings to their defaults?", "", "Reset", false)`, and Accepted → `settings.ResetToDefaults()` and save.

- [ ] **Step 1: Implement `EngineStartButton`, `ConfirmScreen` and the new `StartScreen`;** delete the three old screens and fix `MainMenuScreen`.
- [ ] **Step 2: Run `dotnet test`.** Expected: everything builds and all tests pass.
- [ ] **Step 3: Update README Quick start:** "Pick an Intensity (None, Low, Medium, High or Custom) and a screensaver, then press ENGINE START and confirm (None starts straight away). Move the mouse to see the dashboard; press any key to stop." Also update the CLAUDE.md architecture paragraph and the roadmap.
- [ ] **Step 4: Manual check (desktop):**
  - [ ] The start screen matches mockup `start-v5-none.html`, with "PCWarmer" (not all caps) at the top.
  - [ ] Picking Custom opens its rows, and Start doesn't move or resize.
  - [ ] Exit is clearly a button, unlike the section labels.
  - [ ] Every row works with both the keyboard and the mouse, and choices are still there after a restart.
  - [ ] **None** starts without a box. **High** shows "Really warm your room with {name}?" with a red "Yes, start".
  - [ ] While the box is open, clicking behind it changes nothing.
  - [ ] Esc asks to exit, and Reset settings asks first.
  - [ ] Resizing the window scales everything evenly, and text stays sharp.
- [ ] **Step 5: Commit** with `feat: single start screen with Myra, intensity presets and engine start button`.

---

### Task 9: Gauge dashboard on the warming screen

**Files:**
- Create: `PCWarmer.Core/Display/GaugeScale.cs`, `PCWarmer.Core/Screens/Dashboard.cs`
- Modify: `PCWarmer.Core/Screens/ScreensaverScreen.cs` (replace `BuildHud`/`DrawHud` with `Dashboard.Draw`)
- Test: `PCWarmer.Tests/Display/GaugeScaleTests.cs`

**Interfaces:**
- Consumes: `UiFonts`, `UiTheme`, `UiTextures`, `UiScale` (Task 6), and `HintFade` opacity (Task 5).
- Produces: `public static class GaugeScale`
  - `public const float SweepDegrees = 270f;`
  - `float Fraction(double value, double full)`: clamped to 0..1, with 0 when `full <= 0`
  - `double FpsFull(FpsCap cap)`: 60 → 60, 120 → 120, Uncapped → 240, undefined → 60
- Produces: `sealed class Dashboard`
  - `Dashboard(UiFonts fonts, GraphicsDevice device)`
  - `void Draw(SpriteBatch batch, float opacity, float scale, double cpuDutyCycle, double fps, FpsCap cap, TimeSpan? remaining, int threads, int particles)`
  - **Layout:** a bottom-left `Surface` panel at 80% alpha with `CornerRadius`, holding two 62×62 gauges (×scale) and a readout. Each gauge has a `Border` track and an `Accent` arc of `Fraction × SweepDegrees`, built from small rotated segments of the blank texture, starting at 225°.
  - **Gauge labels:** "CPU {duty*100:0}%" and "{fps:0} FPS".
  - **Readout lines:** "{hh:mm:ss} left" or "Unlimited", "{threads} threads" (or "No CPU load" when the duty is 0), "{particles:N0} particles", and "Press any key to stop".

- [ ] **Step 1: Write the failing tests**

```csharp
[TestCase(0.6, 1.0, ExpectedResult = 0.6f)]
[TestCase(300, 240, ExpectedResult = 1f)]
[TestCase(-5, 60, ExpectedResult = 0f)]
[TestCase(10, 0, ExpectedResult = 0f)]
public float Fraction_IsClamped(double v, double full) => GaugeScale.Fraction(v, full);

[TestCase(FpsCap.Fps60, ExpectedResult = 60d)]
[TestCase(FpsCap.Fps120, ExpectedResult = 120d)]
[TestCase(FpsCap.Uncapped, ExpectedResult = 240d)]
[TestCase((FpsCap)0, ExpectedResult = 60d)]
public double FpsFull_MatchesTheCap(FpsCap cap) => GaugeScale.FpsFull(cap);
```

- [ ] **Step 2: Run `dotnet test --filter GaugeScaleTests`.** Expected: compile error.
- [ ] **Step 3: Implement `GaugeScale` and `Dashboard`,** and draw it from `ScreensaverScreen.Draw` when the dashboard opacity is above 0.
- [ ] **Step 4: Run `dotnet test`.** Expected: all tests pass.
- [ ] **Step 5: Manual check (desktop):**
  - [ ] The dashboard matches the right side of mockup `start-and-run.html`.
  - [ ] It shows for about 3 seconds, then fades over 1 second.
  - [ ] Moving the mouse brings it back.
  - [ ] Any key, click or gamepad button stops the run and returns to the start screen.
  - [ ] The CPU gauge reads 0% for None and 100% for High.
  - [ ] The FPS gauge fills at the cap.
- [ ] **Step 6: Commit** with `feat: gauge dashboard on the warming screen`.
