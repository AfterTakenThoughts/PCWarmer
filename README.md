# PCWarmer | HackUMass XIV

A very stupid solution to a very practical problem.

Imagine you need to warm up your house. Your heaters aren't working well and the only way you can think of to heat up your house is to use your personal computer.

PCWarmer holds a series of graphically intensive screensavers, with frame rate options to warm up your GPU, and a series of intensive calculations to warm up your CPU.

> **Status:** in development for HackUMass XIV. The app currently builds and runs from the MonoGame starter template; the features below are the plan.

## Before using this software

PCWarmer is specifically designed to push a computer to its absolute limits. Before running it:

- Make sure your computer has good airflow.
- Keep an eye on temperatures and make sure your computer can handle running hot.
- Don't leave it running unattended for long periods. By default a run stops on its own after 30 minutes; you can change this in Settings, including setting it to unlimited.
- Plug your laptop charger in; laptops on battery will drain quickly.

The screensaver opens full-screen by default. Press any key to stop the run and return to the menu.

This software comes with no warranty (see [LICENSE](LICENSE)). If you actually need to heat a room, use a real heater instead. This project is the funnier option.

## Quick start

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Clone this repo, then build and run the desktop app:

   ```bash
   cd PCWarmer/PCWarmer
   dotnet run --project PCWarmer.DesktopGL
   ```

   The first build downloads the MonoGame content tools, so it can take a minute.
3. *(Optional)* Open **Settings** to change the CPU load, frame-rate cap, run length, CPU threads or display mode. The defaults are Medium load, 60 fps, a 30-minute run, all CPU cores and full screen.
4. Choose **Start**, pick a screensaver (Particle Storm, Embers, Starfield or Bouncing Orbs), and confirm.
5. Press any key to stop. The run also stops on its own when the run length is up.

See [Running the project](#running-the-project) for VS Code launch configurations and Release builds.

## What it will do

### GPU warmers: screensavers
A collection of full-screen, graphically heavy screensavers designed to keep your graphics card busy (and warm).

- Several visual modes to choose from
- **Frame rate options:** uncap the frame rate for maximum heat, or cap it for a gentler warm-up
- Runs full-screen so it works as an idle screensaver

### CPU warmers: heavy calculations
Computationally intensive workloads that put your processor to work, so the GPU isn't doing all the heating alone.

### Controls and settings
- Pick which warmers run: GPU, CPU, or both
- Settings are saved between sessions

## Who it's for

- **People with a cold room and a warm PC.** Heat your space without relying on a heater.
- **Hardware QA testers.** Run sustained intensive workloads to see how a machine holds up under extreme stress.
- **Students and idle PCs.** Give your computer something to do while it sits on, so you can focus on class.

More detail is in [docs/user-stories.md](docs/user-stories.md).

## Built with

- [MonoGame](https://monogame.net/) 3.8
- .NET 8 / C#
- Desktop platforms: Windows, Linux and macOS (DesktopGL), plus a Windows DirectX build

## Running the project

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
cd PCWarmer/PCWarmer
dotnet run --project PCWarmer.DesktopGL
```

The first build restores the MonoGame content tools automatically, which may take a minute.

In VS Code, open the repo root and use the **PCWarmer Debug** or **PCWarmer Release** launch configurations (F5). Use Release when you want maximum heat.

## Running the tests

Unit tests live in `PCWarmer/PCWarmer/PCWarmer.Tests` and use NUnit 4. Run them with:

```bash
cd PCWarmer/PCWarmer
dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj
```

`dotnet test` builds the project first. A plain `dotnet build` only compiles the tests and never runs them.

To run a subset, add a filter, for example `dotnet test PCWarmer.Tests/PCWarmer.Tests.csproj --filter "FullyQualifiedName~DivergentSeries"`.

In VS Code with the C# Dev Kit, the tests also show up in the Testing panel (Test Explorer). It only finds tests in projects that are part of `PCWarmer.sln`, so add any new test project to the solution.

## License

This project is under MIT. See [LICENSE](LICENSE) for details.
