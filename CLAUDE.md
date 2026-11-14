# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

PCWarmer is a HackUMass XIV project: a deliberately silly app that heats a room by stressing the computer, with graphically intensive "screensavers" (with frame-rate options) to load the GPU and heavy calculations to load the CPU. User stories are in `docs/user-stories.md`.

**Current state:** the code is still the MonoGame 3.8 **Platformer starter-kit template** renamed to `PCWarmer` (player, enemies, gems, tile levels, leaderboard). Most of the gameplay code (`Game/`, `Levels/`, `GameplayScreen`) is placeholder to be replaced by the screensaver and stress-load features. The reusable infrastructure is the screen manager, menus, settings, input, localization and particles.

## Commands

Everything lives under `PCWarmer/PCWarmer/`. Run these from that directory. Use `PCWarmer.sln`, because `PCWarmer/PCWarmer.slnx` is empty.

```bash
dotnet build PCWarmer.DesktopGL/PCWarmer.DesktopGL.csproj     # cross-platform desktop (OpenGL)
dotnet run --project PCWarmer.DesktopGL                        # run the game
dotnet build PCWarmer.WindowsDX/PCWarmer.WindowsDX.csproj     # Windows-only DirectX build
dotnet tool restore && dotnet mgcb-editor                      # GUI editor for Content/PCWarmer.mgcb
```

- Target: .NET 8, MonoGame 3.8.x (`3.8.*` packages; MGCB tools pinned to 3.8.4 in `.config/dotnet-tools.json`). Platform projects run `dotnet tool restore` automatically before build.
- Android (`net8.0-android`) and iOS (`net8.0-ios`) projects need their .NET workloads installed. Build individual projects instead of the whole `.sln` unless those workloads are present.
- There are no test projects and no linter configured.

## Architecture

**Shared core plus thin platform heads.** All game code is in `PCWarmer.Core` (a class library that references `MonoGame.Framework.DesktopGL` with `PrivateAssets=All`). Each platform project (`DesktopGL`, `WindowsDX`, `Android`, `iOS`) only has an entry point that constructs `PCWarmerGame`. Each one builds the shared content through a linked `MonoGameContentReference` to `PCWarmer.Core/Content/PCWarmer.mgcb`.

**Content pipeline.** Every asset must be registered in `PCWarmer.Core/Content/PCWarmer.mgcb`, or `Content.Load` will fail at runtime. Edit it by hand or with `mgcb-editor`. Raw files that aren't processed (for example `Levels/*.txt`) use `/copy:` and are read as files from `Content/...` paths instead of through `Content.Load`. If you add custom `.fx` shaders, compiling them on Linux/macOS needs Wine.

**Game services.** `PCWarmer.Core/PCWarmerGame.cs` registers shared objects in `Game.Services`: `GraphicsDeviceManager`, `SettingsManager<PCWarmerSettings>`, `SettingsManager<PCWarmerLeaderboard>` and `ParticleManager`. Screens fetch them with `Game.Services.GetService<T>()`. Platform detection (`IsMobile` / `IsDesktop`) chooses the settings storage and full-screen behaviour.

**Screen stack.** `ScreenManagers/ScreenManager` (a `DrawableGameComponent`) owns a stack of `Screens/GameScreen` subclasses with transition on/off states, popup handling and input routing through `Inputs/InputState`. Add new modes (such as a screensaver screen) as `GameScreen` subclasses and push them with `ScreenManager.AddScreen`. Startup pushes `BackgroundScreen` and then `MainMenuScreen`. Menu screens derive from `MenuScreen`/`MenuEntry`.

**Settings persistence.** `Settings/SettingsManager<T>` serializes POCOs that implement `INotifyPropertyChanged` to JSON through an `ISettingsStorage`. Desktop storage writes under `%AppData%` (`Environment.SpecialFolder.ApplicationData`). Changing `SettingsFileName` on the storage switches files; the leaderboard uses one JSON file per level.

**Localization.** UI strings come from `Localization/Resources*.resx` (default, es-ES, fr-FR) through `LocalizationManager`. `PCWarmerSettings.Language` is an index into `LocalizationManager.GetSupportedCultures()`. When adding a UI string, add it to every `.resx` and to `Resources.Designer.cs`.
