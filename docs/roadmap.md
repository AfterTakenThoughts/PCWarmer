## User items:

- Build a starting screen
	- Add a button to start warming the room.
- Build a settings screen (separately)
- Build the screensaver screen

## Claude items:

Engine work from [architecture-design.md](architecture-design.md) that the screens above build on. The screens themselves (start, settings and screensaver) stay in the user items.

- Set up the test project
	- Done: `PCWarmer.Tests` (NUnit) is in `PCWarmer.sln`, with tests for `DivergentSeries`.
- Add stress levels (Task 1)
	- `StressLevel` (Low / Medium / Max) and `StressProfile` with each level's CPU duty cycle, particle rate and particle cap.
- Build the CPU stress service (Task 2)
	- Background worker threads that burn CPU at the level's duty cycle, with a thread count clamped to 1 through the core count.
	- Every worker stops when the run ends or the app closes.
- Build the stress session (Task 3)
	- One warming run: start/stop, plus elapsed and remaining time measured on the wall clock so it keeps counting when the window is minimized.
- Add stress settings and the frame-rate policy (Task 4)
	- New `PCWarmerSettings` fields (screensaver, intensity, FPS cap, duration, CPU threads) that fall back to defaults on bad or missing values.
	- `FpsCap` (30 / 60 / 144 / uncapped) and `FrameRatePolicy` to apply it.
- Build the screensaver engine (Task 5, everything except `ScreensaverScreen`)
	- `IScreensaver`, `ScreensaverCatalog`, `EmissionBudget`, `FrameCounter` and the first screensaver, `ParticleStormScreensaver`.
- Remove the platformer template (Task 7)
	- Delete `Game/`, `GameplayScreen`, the leaderboard and the level content, then update `CLAUDE.md`.
