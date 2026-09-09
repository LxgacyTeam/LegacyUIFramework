namespace BigCityLegacy.UI
{
    /// <summary>
    /// Controls how a LegacyUIWindow blocks background input.
    ///
    /// This affects IMGUI events, Unity UI raycasts and the stock game MenuPress/nHelp mouse
    /// intersection checks when the framework is running inside BigCityLegacy.
    /// </summary>
    public enum LegacyUIInputBlockMode
    {
        /// <summary>
        /// Do not consume mouse events. Background UI can react through the window.
        /// </summary>
        None = 0,

        /// <summary>
        /// Consume mouse events only inside the window rectangle.
        /// </summary>
        Window = 1,

        /// <summary>
        /// Consume mouse events anywhere on the screen while the window is visible.
        /// </summary>
        Screen = 2
    }
}
