using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>Visual settings for a HintBoxAlt, independent of its content.</summary>
    public sealed class LegacyUIHintBoxOptions
    {
        public LegacyUITheme Theme;
        public LegacyUIAnimationSettings Animations;
        public ILegacyUIBackdrop Backdrop;
        public bool BackdropBlur = true;
        public float BlurTintAlpha = .75f;
        public float? BackgroundAlpha;
        public Color? BackgroundColor;
        public float ScaleMultiplier = 1f;
        public bool DrawBorder = true;
        public float Padding = 12f;
        public LegacyUIInputBlockMode InputBlockMode = LegacyUIInputBlockMode.None;
    }
}
