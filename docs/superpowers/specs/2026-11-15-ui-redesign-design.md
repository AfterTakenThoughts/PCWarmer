# PCWarmer UI Redesign: Design

**Date:** 2026-11-15
**Status:** Approved by the user on 2026-11-15.
**Related:** [user-stories.md](../../user-stories.md), [roadmap.md](../../roadmap.md) ("Improve the UI with certain animated assets", "Use a better button design"), [architecture-design.md](../../architecture-design.md).

## Intent

PCWarmer's menus are still the MonoGame platformer template's: white and yellow text in a list, a black title, and a still image of the platformer's background. Nothing about them says "heater", and they don't look like a modern app.

The new interface should **feel like you're about to start an engine**. That feeling is meant abstractly, not as a specific car or rocket. The app should follow **modern app design standards**: dark, clean, rounded, with a modern font. **Red** is the accent colour, plus a few small aesthetic touches that carry the feeling. The big red Start button is the thing your eye goes to.

It has to keep working for all three user stories:

- **Max**, warming the house, wants one obvious way to make the PC work hard.
- **The QA tester** wants to fine-tune the load and see what it's doing.
- **The student** wants something nice on screen without the PC fighting the other apps they have open.

## Goals

1. Restyle the menus in a modern dark theme with red accents, a modern font and a plain dark background.
2. Put everything needed to start a run on **one start screen**: intensity, the Custom settings when picked, screensaver choice, run length, display mode, Start and Exit.
3. Make the **Start button** a large, round, glowing red button labelled "Start". It's the visual focus, and it **stays the same size and in the same place at all times**.
4. Make **Intensity** one control that sets the whole hardware load: CPU load, particle count, CPU threads and frame rate.
5. Replace the screensaver's text HUD with a compact **dashboard with gauges**.
6. Restyle the **"Really warm your room?"** confirm box to match.
7. **Keep the display and the PC awake during every run**, at every intensity including None (chosen by the user).

## Non-goals

- **The ignition animation** (Start flares and the gauges sweep up as the screensaver starts). It's a later goal for when there's spare hackathon time.
- **Real CPU or GPU usage and temperature readings.** The gauges show values the app already knows. Temperatures stay on the roadmap as separate work.
- **Extra analysis tools for the QA story**, such as logging, measured usage or hardware health reports. They're beyond this project's scope (chosen by the user).
- **Detecting the monitor's refresh rate.** The frame-rate baseline is a fixed 60 fps.
- **Changing the screensavers themselves**, beyond how many particles they draw.
- **Removing the platformer template** (architecture-design Task 7). That's separate work.
- **Mobile or other languages** (out of scope for the project).

## Anti-goals

These would make it a failure even if it technically worked:

- Start isn't the clear focus, or it moves or changes size when other options change.
- Any colour other than red competes for attention, or red is used on many things so it stops meaning "go".
- Changing a setting takes extra screens or steps compared with today.
- Medium makes the PC unusable for other apps.
- The None preset nags with a heat warning.

## Constraints

- **The existing structure stays:** `PCWarmer.Core` with thin DesktopGL and WindowsDX heads, `ScreenManager` and its `GameScreen` stack, `SettingsManager<PCWarmerSettings>` and the JSON settings file.
- **Only the screensaver goes full screen**, through `ScreensaverDisplay`. The menus stay windowed. The Display setting means "is the screensaver full screen".
- **The dev container can only run the tests**, so logic must be testable without a `GraphicsDevice`. That covers presets, layout maths and input decisions. Anything visual is checked by hand on a desktop machine.
- **English only.** New strings are hardcoded in English or go only into the default `Resources.resx`.
- **Every content asset is listed in `PCWarmer.Core/Content/PCWarmer.mgcb`.** A raw font file uses `/copy:` and is read from the `Content/...` path.
- **Old settings files must still load.** Unknown or removed values, such as the old `FpsCap.Fps30` and `Fps144`, fall back to safe choices.

## Approaches

| | Approach | Trade-off |
|---|---|---|
| A | Extend the existing screens and draw the new look with generated textures (like `GlowTexture`) and a content-pipeline font | No dependencies and fits the screen stack. More hand-written drawing code, and text is only sharp at the sizes the font was built at. |
| **B** | **Use a MonoGame UI library (Myra) for the menus** | **Ready-made panels, buttons, layout and focus handling, and fonts that are sharp at any size. Adds a dependency, and its input and layout handling has to sit alongside `ScreenManager`.** |
| C | Images made in an art tool, stretched to size | Looks exactly as designed, but needs artwork for every state, blurs when scaled, and still needs A's layout code. |

**Picked: B**, chosen by the user. The learning-curve cost doesn't really apply, because Claude writes the code.

**Library: Myra** (Claude's call). It's MIT-licensed, targets .NET Standard 2.0 and runs on MonoGame 3.8. Its widgets are built in code, and it renders `.ttf` fonts at any size through FontStashSharp. Gum was the alternative. Its main advantage is a visual editor, which matters less here, and its newest stable release is under two weeks old.

## Design

### Look

- **Background:** plain dark charcoal (about `#141416`) on every menu screen. It replaces the platformer image in `BackgroundScreen`.
- **Accent:** one red (about `#C8102E`), used only on the Start button, the confirm box's Yes button and the dashboard gauge arcs. The selected tile gets a faint red edge at most.
- **Everything else:**
  - surfaces are dark greys
  - text is light grey
  - borders are thin
  - corners are rounded (about 10 px)
  - section labels are small and uppercase, without letter-spacing (the user approved dropping the spacing on 2026-11-15)
- **Font:** a modern open-licence sans-serif. Inter (SIL OFL 1.1) is Claude's call. Its `.ttf` ships in `Content/Fonts/` with its licence file.
- **Start button:**
  - round, about 120 px across at the 800×480 reference size
  - a red radial gradient with a dark ring and a soft red glow
  - labelled "Start" (the user dropped the earlier "ENGINE" line on 2026-11-15)
  - it pulses slowly
  - it never changes size or position
- **Secondary buttons** (Exit, and Cancel in the confirm box), chosen by the user:
  - they must look like buttons, clearly different from the small uppercase section labels such as INTENSITY and SCREENSAVER
  - each is a rounded rectangle with a dark grey fill, a thin light border and normal-case text ("Exit")
  - when focused or hovered, the border brightens; they're never red, so they don't compete with Start

### Start screen

The **separate Settings screen and screensaver picker screen are removed**, and `StartScreen` becomes the only menu screen. It uses a two-column layout because the window is wide and short:

```
+--------------------------------------------------------------+
| PCWarmer                                                     |
|                                                              |
| INTENSITY                                    .-------.       |
| [None][Low][Medium][High][Custom]           /         \      |
|                                            |   Start   |     |
| (Custom only: CPU load / Screensaver       \           /     |
|  detail / CPU threads / Frame rate)          '-------'       |
|                                                              |
| SCREENSAVER                               Run length ‹30 min›|
| [Particle Storm][Embers]                  Display ‹Full scr.›|
| [Starfield][Bouncing Orbs]                                   |
|                                                              |
| ( Exit )                                                     |
+--------------------------------------------------------------+
```

- **Project name:** "PCWarmer" at the top of the screen, as the first line of the left column. It's written exactly that way, never in all caps (chosen by the user). It's in light, near-white text, semi-bold, about 22 px at the 800×480 reference size. It's the largest text on the screen, but it's never red.
- **Left column:**
  - the project name
  - **Intensity** as a five-part segmented control: None · Low · Medium · High · Custom
  - the **Custom panel**, only when Custom is picked
  - the **Screensaver** tiles
  - an **Exit** button (see "Secondary buttons" under Look)
- **Right column:** the **Start button**, and under it **Run length** and **Display**, which always show whatever preset is picked.
- **Custom panel**, one row each: CPU load (Low / Medium / High), Screensaver detail (Low / Medium / High), CPU threads (All cores, then 1 … core count), Frame rate (60 / 120 / Uncapped). It opens in the left column without moving the Start button. The screensaver tiles may get more compact (a 4-across row with short names) to make room.
- **Rows:** each row has its name on the left and its value on the right, with ‹ › arrows.
- **Screensaver detail:** sets the particle count for Particle Storm and Embers, the star count for Starfield and the orb count for Bouncing Orbs, all through `StressProfile`, as today.
- **Remembered choices:** intensity, the chosen screensaver and every setting are saved straight away and are still picked next time. On first run the screensaver is Particle Storm (`ScreensaverCatalog.DefaultId`). That's Claude's call.
- **Reset settings:** today's "Reset to Defaults" stays, as quiet text under Run length and Display. It still asks for confirmation first. That's Claude's call.
- **Keyboard:** Up and Down move between rows. Left and Right change the value in a row, or move between tiles and segments. Enter on Start begins the run, and Esc asks "Are you sure you want to exit?" (as today).
- **Mouse:** anything can be clicked directly.
- **Gamepad:** works like the keyboard, as today's menus do.
- **Focus:** the focused row or tile gets a thin outline. Start is focused when the screen opens.

### Presets

| Intensity | CPU load | Particles (screensaver level) | CPU threads | Frame rate | Confirm box |
|---|---|---|---|---|---|
| None | Off (no worker threads) | Low | – | 60 | **No**, starts straight away |
| Low | 25% | Low | Half the cores | 60 | Yes |
| Medium | 60% | Medium | ¾ of the cores | 120 | Yes |
| High | 100% | High | All cores | Uncapped | Yes |
| Custom | Custom panel | Custom panel | Custom panel | Custom panel | Yes |

- Thread counts round down and are never below 1. For example, 8 cores gives Low 4, Medium 6 and High 8.
- **Run length and Display always come from the saved settings**, for presets too. Today `RunPlan` uses the defaults for presets, and that changes.
- The frame-rate options become **60 / 120 / Uncapped**, replacing 30 / 60 / 144 / Uncapped. Old saved values fall back: `Fps30` → 60 and `Fps144` → 120. The fallback rule is Claude's call.
- **Custom uses its own screensaver level.** Today it always uses Medium.

### Confirm box

- It keeps today's wording and appears for every intensity except None:
  - **Title:** "Really warm your room with {screensaver}?"
  - **Detail:** "{Intensity} intensity. Your CPU and GPU will run hard and your PC will get hot."
- **Look:** a dark rounded panel over a dimmed screen, with a red **Yes, start** button and a grey **Cancel** button.
- **Keys:** Enter means Yes and Esc means Cancel.
- The exit question uses the same box, with neutral buttons.

### Dashboard (while warming)

- **Gauges:** a compact panel in a bottom corner, with two round gauges with red arcs:
  - **CPU:** the run's CPU load (0 / 25 / 60 / 100% or the Custom value). It rests at 0% for None.
  - **FPS:** the live frame rate.
- **Readout** next to them: time left (or "Unlimited"), threads, particle count, and a short hint line.
- **Behaviour (chosen by the user):**
  - The dashboard shows when the run starts, stays for a few seconds, then fades out so only the screensaver is on screen. Claude's call: about 3 seconds, then a 1-second fade, using the existing `HintFade`.
  - **Moving the mouse** brings it back and restarts the timer.
  - **Any key press, mouse click or gamepad button stops the run**, whether the dashboard is showing or not.
  - This replaces today's H / gamepad Y / Hide UI toggle and the "press any key to show UI" hint. `ScreensaverInput` changes to match, and its tests are updated.
- **Drawing:** it's drawn with the same font and colours as the menus.

### Keeping the PC awake

- From the moment a run starts until it ends (stopped, run length up, or the app closes), the app asks the operating system not to turn off the display or go to sleep. This applies to every intensity, None included. Once the run ends, normal power settings apply again.
- This must work in both the DesktopGL and the WindowsDX builds. On Windows that's `SetThreadExecutionState` with `ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED`, cleared with `ES_CONTINUOUS` when the run ends. On Linux and macOS (DesktopGL only), use SDL's screensaver inhibit or do nothing if it isn't available. That fallback is Claude's call.
- Keep it behind a small interface so `ScreensaverScreen` can be tested with a fake. That's Claude's call.

## What the builder decides

- **How Myra fits into `ScreenManager`**, for example one Myra `Desktop` for each menu screen or one shared one. Input has to reach either Myra or `InputState`, never both in the same frame.
- **How the screen-transition fades map onto Myra** (for example widget opacity).
- **How the round button, glow, pulse and gauges are drawn.** They're probably custom widgets or SpriteBatch drawing with generated textures, because Myra has no round glowing button.
- **How layout scales with window size.** Proportions should match the mockups, and Start must stay the same size relative to the window whatever is picked.
- **Code structure:** where preset logic lives (extend `RunPlan` and `SettingChoices`), new settings fields such as `ScreensaverId` and `ScreensaverLevel`, and how old fields migrate.
- **Pinning** the Myra version, which must be at least two weeks old.
- **Tests:** NUnit tests for the preset table, thread rounding, the FPS fallback, settings migration, skipping the None confirm, and any layout or focus logic that can be separated from drawing.

## User stories check

| Story | What the design gives it | Verdict |
|---|---|---|
| **Max:** "heat stress my computer, so I can warm up my house without relying on heaters" | **High** is one obvious choice that runs every core at 100%, high particles and an uncapped frame rate. Run length can be set to Unlimited, and it's always visible. | **Met.** |
| **QA:** "run intensive programs, so that I can test my computer's life under extreme stress" | **High** for maximum load. **Custom** to tune CPU load, threads, particles and frame rate separately. Unlimited run length. The dashboard shows CPU load, live FPS, threads, particles and time. | **Met within scope.** The CPU gauge shows the *chosen* load, not measured usage, and there are no temperatures. Extra analysis tools are out of scope for this project, so a tester uses their own monitoring tools alongside PCWarmer. |
| **Student:** "have something on my PC when idling, so that I can pay attention to class while keeping my computer on" | **None** runs only a calm, low-particle screensaver with no CPU load and no warning box. **Low** and **Medium** leave cores free for other apps. The dashboard fades away so only the screensaver shows. | **Met.** Every run keeps the display and PC awake (goal 7). When the run length is up (30 min by default) the app goes back to the menu, so a student who wants it on all class sets Run length to Unlimited, which is always visible on the start screen. |

## Mockups

Saved under `.superpowers/brainstorm/` (not committed): `visual-style.html`, `start-and-run.html`, `start-v4-two-column.html` and `start-v5-none.html` (the final start screen and preset table).
