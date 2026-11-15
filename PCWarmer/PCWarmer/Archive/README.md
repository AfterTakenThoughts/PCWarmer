# Archive

Code that the app no longer uses, kept for reference. Nothing in this folder is compiled: it sits next to the projects, not inside one. The folders mirror where each file used to live, so a file can be moved back to the same path.

- `PCWarmer.Core/Game/`, `Screens/GameplayScreen.cs`, `EndOfLevelMessageState.cs`, `PauseScreen.cs`, `MainMenuScreen.cs`, `AboutScreen.cs`, `LoadingScreen.cs`: the MonoGame platformer template.
- `Screens/MenuScreen.cs`, `MenuEntry.cs`, `MessageBoxScreen.cs`, `YesNoButtons.cs`: the template's text menus and message box, replaced by the Myra `StartScreen` and `ConfirmScreen`.
- `Inputs/TouchCollectionExtensions.cs`: touch input, only used by the template.
- `Settings/ConsoleSettingsStorage.cs`: a settings store the app never used (it always uses `DesktopSettingsStorage`).
- `Settings/PCWarmerLeaderboard.cs`: the template's per-level best times; the app no longer registers it.
- `Stress/DivergentSeries.cs`: a harmonic-series CPU workload that `CpuStressService` never used.
- `PCWarmer.Tests/...`: the tests for `YesNoButtons` and `DivergentSeries`.

- `PCWarmer.Core/Content/`: the template's content (backgrounds, levels, overlays, sounds, tiles, every sprite except `blank.png`, and the `Hud` sprite font with its `Roboto-Bold.ttf`). To use one again, move it back and add its `#begin` block to `PCWarmer.Core/Content/PCWarmer.mgcb`.
