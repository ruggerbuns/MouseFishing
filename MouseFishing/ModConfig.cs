namespace MouseFishing
{
    /// <summary>Stores user settings.</summary>
    public sealed class ModConfig
    {
        // toggles Mouse Fishing mod
        public bool ModEnabled { get; set; }
        // controls bobber height as a percentage (100 = normal height)
        public int BobberHeight { get; set; }

        // constructor
        public ModConfig()
        {
            this.ModEnabled = true;

            // this.BobberHeight is a percentage (ex. 75 = 75%)
            this.BobberHeight = 75;
        }
    }
}
