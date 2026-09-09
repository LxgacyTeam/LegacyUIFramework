namespace BigCityLegacy.UI
{
    public sealed class LegacyUIWindowOptions
    {
        public bool Draggable = true;
        public bool ClampToScreen = true;
        public bool ShowCloseButton;
        public bool FadeWhileDragging = true;
        public float HeaderHeight = 38f;
        public float DragAlpha = 0.68f;
        public float? BackgroundAlpha;

        /// <summary>
        /// Optional theme used only while this window and its content are being drawn.
        /// If null, the current LegacyUI global/scoped theme is used.
        /// </summary>
        public LegacyUITheme Theme;

        /// <summary>
        /// Controls how visible windows consume mouse events after their own IMGUI controls are drawn.
        /// Default value blocks clicks only inside the window rectangle.
        /// </summary>
        public LegacyUIInputBlockMode InputBlockMode = LegacyUIInputBlockMode.Window;

        public float ScreenPadding = 8f;
        public string RightHint;
        public string PlayerPrefsKey;
    }
}
