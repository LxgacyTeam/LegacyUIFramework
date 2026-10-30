namespace BigCityLegacy.UI
{
    public sealed class LegacyUIWindowOptions
    {
        /// <summary>Null inherits the current plugin/global animation settings.</summary>
        public LegacyUIAnimationSettings Animations;
        /// <summary>Null inherits the provider supplied by LegacyUIGuiScope.</summary>
        public ILegacyUIBackdrop Backdrop;
        public bool BackdropBlur = true;
        /// <summary>Window tint multiplier while a blurred backdrop is available.</summary>
        public float BlurTintAlpha = 0.75f;
        public bool Draggable = true;
        public bool ClampToScreen = true;
        public bool ShowCloseButton;
        public bool FadeWhileDragging = true;
        public float HeaderHeight = 38f;
        public float DragAlpha = 0.68f;
        /// <summary>
        /// Explicit values are inherited by child panels unless overridden there.
        /// Null leaves child panels themed; only the window uses 1 with BackdropBlur enabled.
        /// </summary>
        public float? BackgroundAlpha;

        /// <summary>
        /// Optional theme used only while this window and its content are being drawn.
        /// If null, the current LegacyUI global/scoped theme is used.
        /// </summary>
        public LegacyUITheme Theme;

        /// <summary>Window-local scale multiplier. Default 1; relative to current GUI scope.</summary>
        public float ScaleMultiplier = 1f;

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
