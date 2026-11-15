# Roadmap

Keeps a list of in-progress items and items that should be done. Finished items are under [Done](#done).

## In-Progress

### User items:

- Build a settings screen (separately)
	- The UI redesign folded the settings screen into the start screen: run length and display sit under the Start button, and the other options open under Intensity when Custom is picked. The temperature limits below would go in those Custom options.
	- Still to do: max CPU temperature and max GPU temperature.
		- They need temperature readings first. On Windows, GPU temperature on NVIDIA can come from `nvidia-smi` (no admin). CPU temperature needs a library such as LibreHardwareMonitorLib and running as administrator.
		- Decide what the limits do: only show the numbers, or also stop warming when a reading goes over its limit.
- Add the ignition animation (later goal, when there's spare hackathon time)
	- When a run starts, the Start button's glow flares and the dashboard gauges sweep up from zero, like an engine catching. Left out of the [UI redesign](superpowers/specs/2026-11-15-ui-redesign-design.md) on purpose.

### Claude items:

Engine work from [architecture-design.md](architecture-design.md) that the screens above build on. The screens themselves (start, settings and screensaver) stay in the user items.

- Nothing open right now.

## Done

### User items

- Build a starting screen
	- Add a button to start warming the room.
	- `StartScreen` replaces the template main menu at startup. Start opens `ScreensaverPickerScreen` (Particle Storm, Embers, Starfield, Bouncing Orbs). Picking one asks "Really warm your room?", and yes starts the run with the saved settings.
- Rename "Start warming" to "Start"
	- The start screen's button now reads "Start", so it fits the student user story as well as the room-warming one.
- Build a settings screen (separately)
	- CPU load (Low / Medium / High: each core busy 25%, 60% or 100% of the time), frame rate (30 / 60 / 144 / uncapped), duration (unlimited / 15 / 30 / 60 / 120 min), CPU threads (all cores or 1 to the core count) and full screen. Each change is saved straight away, and bad saved values fall back to defaults (`SettingChoices`, with tests). The template's language and particle-effect options were removed.
	- Max CPU and GPU temperature are still open (see [User items](#user-items)).
- Add a hardware load option to the screensaver selection screen: Custom, None, Low, Medium, High
	- None, Low, Medium and High are presets for CPU load, frame rate and screensaver particles (`RunPlan`, with tests); Custom uses the settings screen.
	- The settings button moved from the start screen to the screensaver select screen, and only shows when "Custom" is selected.
- Redesign the UI ([design](superpowers/specs/2026-11-15-ui-redesign-design.md), [plan](superpowers/plans/2026-11-15-ui-redesign.md))
	- Covers the two roadmap items "Improve the UI with certain animated assets" and "Use a better button design".
	- One start screen replaces the start, screensaver picker and settings screens. On the left: the "PCWarmer" title, Intensity (None, Low, Medium, High, Custom), the Custom options when picked, the screensaver tiles, and an Exit button. On the right: a big, round, glowing red Start button that pulses slowly and never moves or changes size, with Run length, Display and Reset settings under it.
	- Dark charcoal background, one red accent (Start, "Yes, start" and the gauge arcs), rounded dark buttons and the Inter font. Built with Myra.
	- Presets: None runs only the screensaver (no CPU load, no confirm box); Low is 25% CPU on half the cores at 60 fps; Medium is 60% on three-quarters of the cores at 120 fps; High is 100% on all cores, uncapped. Frame-rate choices are now 60 / 120 / Uncapped.
	- The "Really warm your room?" box is a dark rounded panel with a red "Yes, start" button.
	- While warming, a gauge dashboard (CPU load and FPS) shows for a few seconds, then fades; moving the mouse brings it back, and any key, click or gamepad button stops the run.
	- Every run keeps the display and PC awake.
- Build the screensaver screen
	- `ScreensaverScreen` hosts `ParticleStormScreensaver` with a HUD (FPS, level, CPU load and threads, particles, time warming). Any key, click or gamepad button stops the run. It keeps rendering when the window is unfocused.
	- Uses the saved CPU load, threads, duration and frame-rate cap. It ends on its own when the duration is up.
	- Four screensavers in `ScreensaverCatalog`. Their motion (`EmberField`, `Starfield`, `OrbField`) is tested without a GPU.

### Claude items

- Set up the test project
	- `PCWarmer.Tests` (NUnit) is in `PCWarmer.sln`, with tests for `DivergentSeries`.
- Add stress levels (Task 1)
	- `StressLevel` (Low / Medium / High) and `StressProfile` with each level's CPU duty cycle, particle rate and particle cap.
	- Tests in `StressProfileTests`.
- Build the CPU stress service (Task 2)
	- Background worker threads that burn CPU at the level's duty cycle, with a thread count clamped to 1 through the core count.
	- Every worker stops when the run ends or the app closes.
	- Tests in `CpuStressServiceTests`.
- Build the stress session (Task 3)
	- One warming run: start/stop, plus elapsed and remaining time measured on the wall clock so it keeps counting when the window is minimized.
	- Tests in `StressSessionTests`. It takes a CPU duty cycle (from the CPU load level's `StressProfile`).
- Add stress settings and the frame-rate policy (Task 4)
	- New `PCWarmerSettings` fields (screensaver, intensity, FPS cap, duration, CPU threads) that fall back to defaults on bad or missing values.
	- `FpsCap` (30 / 60 / 144 / uncapped) and `FrameRatePolicy` to apply it.
	- Done for CPU load, FPS cap, duration and CPU threads, with tests in `PCWarmerSettingsTests` and `FrameRatePolicyTests`. There's no screensaver-id setting: `ScreensaverPickerScreen` asks which of the four screensavers to run each time you start warming.
- Build the screensaver engine (Task 5, everything except `ScreensaverScreen`)
	- `IScreensaver`, `ScreensaverCatalog`, `EmissionBudget`, `FrameCounter` and the first screensaver, `ParticleStormScreensaver`.
	- Plus Embers, Starfield and Bouncing Orbs. Tests are in `EmissionBudgetTests`, `FrameCounterTests`, `ScreensaverCatalogTests`, `EmberFieldTests`, `StarfieldTests` and `OrbFieldTests`.
- Build the UI redesign's engine pieces
	- `RunPlan` presets and `RunConfirmation`, `StartMenuModel` (focus and values for the start screen), keep-awake (`WindowsKeepAwake`, `SdlKeepAwake`), `GaugeScale`, `ShapeMasks`, `UiScale`, `MouseGate` and `DeferredActions`, all tested without a GPU.
	- Old settings files still load: the old 30 fps becomes 60 and the old 144 becomes 120.
- Remove the platformer template (Task 7)
	- The template's code, its tests, the leaderboard and the unused content (levels, tiles, sprites, backgrounds, sounds) were moved to `PCWarmer/PCWarmer/Archive/`, which isn't compiled or built. `PCWarmer.mgcb` now lists only the Inter fonts and `Sprites/blank.png`.
