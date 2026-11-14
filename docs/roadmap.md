# Roadmap

Keeps a list of in-progress items and items that should be done. Finished items are under [Done](#done).

## In-Progress

### User items:

- Build a settings screen (separately)
	- Still to do: max CPU temperature and max GPU temperature.
		- They need temperature readings first. On Windows, GPU temperature on NVIDIA can come from `nvidia-smi` (no admin). CPU temperature needs a library such as LibreHardwareMonitorLib and running as administrator.
		- Decide what the limits do: only show the numbers, or also stop warming when a reading goes over its limit.
- Add a single option in the screensaver selection screen on hardware load: Default, None, Low, Medium, High.
	- Move the settings button to the screensaver select screen.
- Rename "Start warming" to "Start"
- Improve the UI with certain animated assets.
- Use a better button design.

### Claude items:

Engine work from [architecture-design.md](architecture-design.md) that the screens above build on. The screens themselves (start, settings and screensaver) stay in the user items.

- Remove the platformer template (Task 7)
	- Delete `Game/`, `GameplayScreen`, the leaderboard and the level content, then update `CLAUDE.md`.

## Done

### User items

- Build a starting screen
	- Add a button to start warming the room.
	- `StartScreen` replaces the template main menu at startup. Start Warming opens `ScreensaverPickerScreen` (Particle Storm, Embers, Starfield, Bouncing Orbs). Picking one asks "Really warm your room?", and yes starts the run with the saved settings.
- Build a settings screen (separately)
	- CPU load (Low / Medium / High: each core busy 25%, 60% or 100% of the time), frame rate (30 / 60 / 144 / uncapped), duration (unlimited / 15 / 30 / 60 / 120 min), CPU threads (all cores or 1 to the core count) and full screen. Each change is saved straight away, and bad saved values fall back to defaults (`SettingChoices`, with tests). The template's language and particle-effect options were removed.
	- Max CPU and GPU temperature are still open (see [User items](#user-items)).
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
