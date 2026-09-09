using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Color palette used by BigCityLegacy IMGUI themes.
    /// The default values are intentionally based on the current DirectConnectGui look.
    /// </summary>
    public sealed class LegacyUIPalette
    {
        public Color WindowBackground = new Color(0.05f, 0.05f, 0.06f, 0.85f);
        public Color Border = new Color(0.16f, 0.17f, 0.20f, 1f);
        public Color Field = new Color(0.10f, 0.11f, 0.13f, 1f);
        public Color Button = new Color(0.15f, 0.16f, 0.19f, 1f);
        public Color ButtonHover = new Color(0.20f, 0.22f, 0.26f, 1f);
        public Color GreenButton = new Color(0.12f, 0.38f, 0.20f, 1f);
        public Color GreenButtonHover = new Color(0.16f, 0.48f, 0.26f, 1f);
        public Color GreenButtonActive = new Color(0.10f, 0.38f, 0.2f, 1f);
        public Color Active = new Color(0.14f, 0.35f, 0.65f, 1f);
        public Color HintBackground = new Color(0.03f, 0.03f, 0.04f, 0.30f);
        public Color HintBackgroundAlt = new Color(0.03f, 0.03f, 0.04f, 0.90f);
        public Color CheckEmpty = new Color(0.12f, 0.13f, 0.15f, 1f);
        public Color Text = new Color(0.72f, 0.75f, 0.78f, 1f);
        public Color TextButton = new Color(0.72f, 0.75f, 0.78f, 1f);
        public Color TextBright = Color.white;
        public Color TextMuted = new Color(0.38f, 0.40f, 0.44f, 1f);
        public Color Status = new Color(0.92f, 0.76f, 0.35f, 1f);
        public Color Danger = new Color(0.70f, 0.18f, 0.18f, 1f);
        public Color DangerHover = new Color(0.88f, 0.24f, 0.24f, 1f);
        public Color DangerActive = new Color(0.6f, 0.15f, 0.15f, 1f);
        public Color SliderTrack = new Color(0.10f, 0.11f, 0.13f, 1f);
        public Color SliderThumb = new Color(0.14f, 0.35f, 0.65f, 1f);
        public Color SliderThumbHover = new Color(0.18f, 0.44f, 0.80f, 1f);

        public LegacyUIPalette()
        {
        }

        public LegacyUIPalette(LegacyUIPalette source)
        {
            if (source == null)
            {
                return;
            }

            WindowBackground = source.WindowBackground;
            Border = source.Border;
            Field = source.Field;
            Button = source.Button;
            ButtonHover = source.ButtonHover;
            GreenButton = source.GreenButton;
            GreenButtonHover = source.GreenButtonHover;
            GreenButtonActive = source.GreenButtonActive;
            Active = source.Active;
            HintBackground = source.HintBackground;
            HintBackgroundAlt = source.HintBackgroundAlt;
            CheckEmpty = source.CheckEmpty;
            Text = source.Text;
            TextButton = source.TextButton;
            TextBright = source.TextBright;
            TextMuted = source.TextMuted;
            Status = source.Status;
            Danger = source.Danger;
            DangerHover = source.DangerHover;
            DangerActive = source.DangerActive;
            SliderTrack = source.SliderTrack;
            SliderThumb = source.SliderThumb;
            SliderThumbHover = source.SliderThumbHover;
        }

        public LegacyUIPalette Clone()
        {
            return new LegacyUIPalette(this);
        }
    }
}
