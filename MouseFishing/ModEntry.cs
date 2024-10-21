using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace MouseFishing
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        /*********
        ** Private instance variables
        *********/
        // the user settings
        private ModConfig Config = new();

        // the currently open BobberBar menu
        private BobberBar? bobberBarMenu = null;

        // the user's cursor position when the BobberBar menu first opens
        private Point startingCursorPos;

        // the bobber bar position after the latest CursorMoved event
        private float lastBobberBarPos;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.Input.CursorMoved += this.OnCursorMoved;
            helper.Events.GameLoop.UpdateTicking += this.OnUpdateTicking;
            helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
        }

        /*********
        ** Private methods
        *********/
        /// <summary>
        /// Returns true if the player is in a state where Mouse Fishing should run, else returns false.
        /// Configurable via parameters.
        /// </summary>
        /// <param name="checkModEnabled">Check if mod is enabled</param>
        /// <param name="checkWorldReady">Check if the player has loaded a save</param>
        /// <param name="checkBobberMenu">Check if the player is in the bobber bar menu</param>
        private bool IsReadyToMouseFish(bool checkModEnabled = true, bool checkWorldReady = true, bool checkBobberMenu = true)
        {
            // return false if mod is disabled
            if (checkModEnabled && !this.Config.ModEnabled)
                return false;

            // return false if player hasn't loaded a save yet
            if (checkWorldReady && !Context.IsWorldReady)
                return false;

            // return false if player isn't in bobber bar menu
            if (checkBobberMenu && this.bobberBarMenu == null)
                return false;

            return true;
        }

        /// <summary>
        /// Raised after the game is launched, right before the first update tick.
        /// This happens once per game session (unrelated to loading saves).
        /// All mods are loaded and initialised at this point, so this is a good time to set up mod integrations.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // load config values from config.json
            this.Config = this.Helper.ReadConfig<ModConfig>();

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // add config options
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable Mouse Fishing",
                tooltip: () => "Toggles Mouse Fishing mod.",
                getValue: () => this.Config.ModEnabled,
                setValue: value => this.Config.ModEnabled = value
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "Bobber Height",
                tooltip: () => "Controls bobber height in fishing minigame. (100% - normal height)",
                getValue: () => this.Config.BobberHeight,
                setValue: value => this.Config.BobberHeight = value,
                min: 30,
                max: 100,
                formatValue: value => $"{value}%"
            );
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            // ignore if mod is disabled or player hasn't loaded a save yet
            if (!IsReadyToMouseFish(checkBobberMenu: false))
                return;

            // if the BobberBar menu was just opened
            if (e.NewMenu is BobberBar bobberBar)
            {
                // capture game state
                this.startingCursorPos = Game1.getMousePosition();
                this.bobberBarMenu = bobberBar;

                // set the bobber height according to user setting (this.Config.BobberHeight)
                this.bobberBarMenu.bobberBarHeight = (int)(this.bobberBarMenu.bobberBarHeight * ((float)this.Config.BobberHeight / 100));

                // set the user's cursor position to the top of the bobber
                int bobberStartingYPos = 568 - this.bobberBarMenu.bobberBarHeight;
                Game1.setMousePosition(this.startingCursorPos.X, bobberStartingYPos);
                this.lastBobberBarPos = bobberStartingYPos;
            }
            // if the BobberBar menu was just closed
            else if (e.OldMenu is BobberBar)
            {
                // clear stored BobberBar menu
                this.bobberBarMenu = null;

                // reset user's cursor position
                Game1.setMousePosition(this.startingCursorPos.X, this.startingCursorPos.Y);
            }
        }

        /// <summary>Raised after the player moves the in-game cursor.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnCursorMoved(object? sender, CursorMovedEventArgs e)
        {
            // ignore if player isn't ready to mouse fish
            if (!IsReadyToMouseFish())
                return;

            int bobberStartingYPos = 568 - this.bobberBarMenu.bobberBarHeight;
            // if the user's cursor goes below the bounds of the BobberBar
            if (e.NewPosition.ScreenPixels.Y > bobberStartingYPos)
            {
                // keep the user's cursor in bounds
                Game1.setMousePosition(this.startingCursorPos.X, bobberStartingYPos);
            }
            // if the user's cursor goes above the bounds of the BobberBar
            else if (e.NewPosition.ScreenPixels.Y < 0)
            {
                // keep the user's cursor in bounds
                Game1.setMousePosition(this.startingCursorPos.X, 0);
            }

            // set the in game bobber's position to match the user's cursor position
            this.bobberBarMenu.bobberBarPos = e.NewPosition.ScreenPixels.Y;
            this.lastBobberBarPos = e.NewPosition.ScreenPixels.Y;
        }

        /// <summary>Raised before the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicking(object? sender, UpdateTickingEventArgs e)
        {
            // ignore if player isn't ready to mouse fish
            if (!IsReadyToMouseFish())
                return;

            // reset acceleration and speed to stop the bobber from quickly falling
            this.bobberBarMenu.bobberAcceleration = 0;
            this.bobberBarMenu.bobberBarSpeed = 0;

            // stop bobber bar from slowly drifting downwards between mouse movements
            this.bobberBarMenu.bobberBarPos = this.lastBobberBarPos;
        }

        /// <summary>Raised after the player pressed/released any buttons on the keyboard, mouse, or controller.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            // ignore if player isn't ready to mouse fish
            if (!IsReadyToMouseFish())
                return;

            // loop through each button that was pressed
            foreach (SButton buttonPressed in e.Pressed)
            {
                // if D Pad Up, Left Thumbstick Up, or Right Thumbstick Up was pressed
                if (buttonPressed == SButton.DPadUp || buttonPressed == SButton.LeftThumbstickUp || buttonPressed == SButton.RightThumbstickUp)
                {
                    this.lastBobberBarPos = this.bobberBarMenu.bobberBarPos - 5;
                }
                // else if D Pad Down, Left Thumbstick Down, or Right Thumbstick Down was pressed
                else if (buttonPressed == SButton.DPadDown || buttonPressed == SButton.LeftThumbstickDown || buttonPressed == SButton.RightThumbstickDown)
                {
                    this.lastBobberBarPos = this.bobberBarMenu.bobberBarPos + 5;
                }
            }

            // loop through each button that is currently held down
            foreach (SButton buttonHeld in e.Held)
            {
                // if D Pad Up, Left Thumbstick Up, or Right Thumbstick Up is currently held down
                if (buttonHeld == SButton.DPadUp || buttonHeld == SButton.LeftThumbstickUp || buttonHeld == SButton.RightThumbstickUp)
                {
                    this.lastBobberBarPos = this.bobberBarMenu.bobberBarPos - 5;
                }
                // else if D Pad Down, Left Thumbstick Down, or Right Thumbstick Down is currently held down
                else if (buttonHeld == SButton.DPadDown || buttonHeld == SButton.LeftThumbstickDown || buttonHeld == SButton.RightThumbstickDown)
                {
                    this.lastBobberBarPos = this.bobberBarMenu.bobberBarPos + 5;
                }
            }
        }
    }
}