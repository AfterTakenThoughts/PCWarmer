# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

PCWarmer is a HackUMass XIV project: a deliberately silly app that heats a room by stressing the computer, with graphically intensive "screensavers" (with frame-rate options) to load the GPU and heavy calculations to load the CPU. User stories are in `docs/user-stories.md`.

**Current state:** PCWarmer started as the MonoGame 3.8 Platformer starter-kit template. The app now runs the stress engine (`Stress/`, `Screensavers/`, `Display/`) behind a Myra start screen (`Ui/`, `Screens/StartScreen`). The template's unused code and content (platformer `Game/`, `GameplayScreen`, the old text menus and message box, the leaderboard, and the levels, tiles, sprites, backgrounds and sounds) have been moved to `PCWarmer/PCWarmer/Archive/`, which isn't compiled or built. Don't add archived files back to a project or to `PCWarmer.mgcb` unless the app needs them again.

**Out of scope:** mobile versions and language options. The project is desktop only (DesktopGL and WindowsDX) and English only. The template's Android/iOS projects and mobile-only code (`IsMobile`, mobile settings storage, on-screen touch controls) have been removed. Don't reintroduce them. The localization code (es-ES/fr-FR `.resx` files, `PCWarmerSettings.Language`) is leftover; don't build on it or document it as a feature.

## Commands

Everything lives under `PCWarmer/PCWarmer/`. Run these from that directory. Use `PCWarmer.sln`, because `PCWarmer/PCWarmer.slnx` is empty.

```bash
dotnet build PCWarmer.DesktopGL/PCWarmer.DesktopGL.csproj     # cross-platform desktop (OpenGL)
dotnet run --project PCWarmer.DesktopGL                        # run the game
dotnet build PCWarmer.WindowsDX/PCWarmer.WindowsDX.csproj     # Windows-only DirectX build
dotnet tool restore && dotnet mgcb-editor                      # GUI editor for Content/PCWarmer.mgcb
```

- Target: .NET 8, MonoGame 3.8.x (`3.8.*` packages; MGCB tools pinned to 3.8.4 in `.config/dotnet-tools.json`). Platform projects run `dotnet tool restore` automatically before build.
- `dotnet build PCWarmer.sln` builds Core, DesktopGL, WindowsDX and Tests. WindowsDX targets `net8.0-windows` and only builds on Windows, so a full solution build fails on Linux (including this dev container).
- VS Code: the root `.vscode/launch.json` has **PCWarmer Debug** and **PCWarmer Release** configurations, which use the `build-desktopgl-debug` / `build-desktopgl-release` tasks in `.vscode/tasks.json`.
- Tests: `dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj` (NUnit 4, `Assert.That` constraint style). There is no linter configured.
  - `PCWarmer.Tests` is part of `PCWarmer.sln`. Keep every test project in the solution, because C# Dev Kit's Test Explorer only discovers tests in projects that belong to the loaded solution.
  - `dotnet build` only compiles the tests. It never runs them, and that is deliberate: don't hook test runs into the build (the IDE and the launch configurations build often). Run them with `dotnet test`, which builds first.
- **Dev container limitation:** this container can only run the tests, not the game itself. Verify changes with `dotnet test`; don't try to `dotnet run` the app or rely on the DesktopGL build here. Running the app has to happen on a desktop machine outside the container.

## Architecture

**Shared core plus thin platform heads.** All game code is in `PCWarmer.Core` (a class library that references `MonoGame.Framework.DesktopGL` with `PrivateAssets=All`). Each platform project (`DesktopGL`, `WindowsDX`) only has an entry point that constructs `PCWarmerGame`. Each one builds the shared content through a linked `MonoGameContentReference` to `PCWarmer.Core/Content/PCWarmer.mgcb`.

**Content pipeline.** Every asset must be registered in `PCWarmer.Core/Content/PCWarmer.mgcb`, or `Content.Load` will fail at runtime. Edit it by hand or with `mgcb-editor`. Raw files that aren't processed (for example the `Fonts/Inter-*.ttf` files) use `/copy:` and are read as files from `Content/...` paths instead of through `Content.Load`. If you add custom `.fx` shaders, compiling them on Linux/macOS needs Wine.

**Game services.** `PCWarmer.Core/PCWarmerGame.cs` registers shared objects in `Game.Services`: `GraphicsDeviceManager`, `SettingsManager<PCWarmerSettings>`, `ParticleManager` and `UiFonts` (the menus' Inter font). Screens fetch them with `Game.Services.GetService<T>()`. Settings always use `DesktopSettingsStorage`.

**Screen stack.** `ScreenManagers/ScreenManager` (a `DrawableGameComponent`) owns a stack of `Screens/GameScreen` subclasses with transition on/off states, popup handling and input routing through `Inputs/InputState`. Add new modes (such as a screensaver screen) as `GameScreen` subclasses and push them with `ScreenManager.AddScreen`. Startup pushes `BackgroundScreen` (a plain dark background) and then `StartScreen`, the only menu screen. `StartScreen` and `ConfirmScreen` derive from `Screens/MyraScreen`; the template's `MenuScreen`/`MenuEntry` text menus are archived.

**Menus (Myra).** Menus are laid out and drawn with Myra 1.6.8 (FontStashSharp fonts, Inter in `Content/Fonts/`). Every decision lives in GPU-free classes under `Ui/` (`StartMenuModel`, `ShapeMasks`, `UiScale`) and `Stress/` (`RunPlan`, `RunConfirmation`) so NUnit can test them. `MyraScreen` only lets the active screen read the mouse, and keyboard/gamepad always go through `InputState` (Myra never sees keys). Colours are in `Ui/UiTheme`; red is only for the Start button, "Yes, start" and the gauge arcs.

**Settings persistence.** `Settings/SettingsManager<T>` serializes POCOs that implement `INotifyPropertyChanged` to JSON through an `ISettingsStorage`. Desktop storage writes under `%AppData%` (`Environment.SpecialFolder.ApplicationData`). Changing `SettingsFileName` on the storage switches files.

**UI strings.** Existing menus read strings from `Localization/Resources.resx` through `LocalizationManager`, and `PCWarmerGame.Initialize` picks a culture from `PCWarmerSettings.Language`. Language selection is out of scope. Add new strings only to the default `Resources.resx` (and `Resources.Designer.cs`), or hardcode them in English. Don't add translations.
