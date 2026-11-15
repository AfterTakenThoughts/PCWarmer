using System;
using System.Collections.Generic;
using System.Globalization;
using PCWarmer.Core.Effects;
using PCWarmer.Core.Localization;
using PCWarmer.Core.Settings;
using PCWarmer.Core.Ui;
using Myra;
using PCWarmer.ScreenManagers;
using PCWarmer.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PCWarmer.Core
{
    /// <summary>
    /// The main class for the game, responsible for managing game components, settings, 
    /// and platform-specific configurations.
    /// </summary>
    /// <remarks>
    /// This class is the entry point for the game and handles initialization, content loading,
    /// and screen management.
    /// </remarks>}
    public class PCWarmerGame : Game
    {
        // Resources for drawing.
        private GraphicsDeviceManager graphicsDeviceManager;

        // Manages the game's screen transitions and screens.
        private ScreenManager screenManager;

        // Manages game settings, such as preferences and configurations.
        private SettingsManager<PCWarmerSettings> settingsManager;


        // Texture for rendering particles.
        private Texture2D particleTexture;

        // Manages particle effects in the game.
        private ParticleManager particleManager;

        /// <summary>
        /// Initializes a new instance of the game. Configures desktop settings,
        /// initializes the settings manager service, and sets up the
        /// screen manager for screen transitions.
        /// </summary>
        public PCWarmerGame()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            // Share GraphicsDeviceManager as a service.
            Services.AddService(typeof(GraphicsDeviceManager), graphicsDeviceManager);

            // PCWarmer is desktop only.
            ISettingsStorage storage = new DesktopSettingsStorage();
            IsMouseVisible = true;

            // Initialize the settings manager.
            settingsManager = new SettingsManager<PCWarmerSettings>(storage);

            // The menus always run in a window; only the screensaver goes full screen (see ScreensaverDisplay).
            // Never switch the monitor's resolution: that resizes the user's other windows.
            graphicsDeviceManager.HardwareModeSwitch = false;
            graphicsDeviceManager.IsFullScreen = false;
            Services.AddService(typeof(SettingsManager<PCWarmerSettings>), settingsManager);

            Content.RootDirectory = "Content";

            // Configure screen orientations.
            graphicsDeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;

            // Initialize the screen manager.
            screenManager = new ScreenManager(this);
            Components.Add(screenManager);
        }

        /// <summary>
        /// Initializes the game, including setting up localization and adding the 
        /// initial screens to the ScreenManager.
        /// </summary>
        protected override void Initialize()
        {
            // Myra lays out and draws the menus and reads the mouse. Keyboard and gamepad go through InputState,
            // so Myra is told no keys are ever down.
            MyraEnvironment.Game = this;
            MyraEnvironment.DownKeysGetter = keys => System.Array.Clear(keys, 0, keys.Length);

            base.Initialize();

            // Load supported languages and set the default language.
            List<CultureInfo> cultures = LocalizationManager.GetSupportedCultures();
            var languages = new List<CultureInfo>();
            for (int i = 0; i < cultures.Count; i++)
            {
                languages.Add(cultures[i]);
            }
            var selectedLanguage = languages[settingsManager.Settings.Language].Name;
            LocalizationManager.SetCulture(selectedLanguage);

            // Add the background and the start screen.
            screenManager.AddScreen(new BackgroundScreen(), null);
            screenManager.AddScreen(new StartScreen(), null);
        }

        /// <summary>
        /// Loads game content, such as textures and particle systems.
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();

            // Load a texture for particles and initialize the particle manager.
            particleTexture = Content.Load<Texture2D>("Sprites/blank");
            particleManager = new ParticleManager(particleTexture, new Vector2(400, 200));

            // Share the particle manager as a service.
            Services.AddService(typeof(ParticleManager), particleManager);

            // The menus' font, shared by every Myra screen and the warming dashboard.
            using (var regular = TitleContainer.OpenStream("Content/Fonts/Inter-Regular.ttf"))
            using (var semiBold = TitleContainer.OpenStream("Content/Fonts/Inter-SemiBold.ttf"))
                Services.AddService(typeof(UiFonts), new UiFonts(regular, semiBold));
        }
    }
}