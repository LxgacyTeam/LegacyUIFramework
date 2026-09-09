using UnityEngine;

namespace BigCityLegacy.UI
{
    public sealed class LegacyUITextures
    {
        public Texture2D WindowBackground { get; private set; }
        public Texture2D Border { get; private set; }
        public Texture2D Field { get; private set; }
        public Texture2D Button { get; private set; }
        public Texture2D ButtonHover { get; private set; }
        public Texture2D GreenButton { get; private set; }
        public Texture2D GreenButtonHover { get; private set; }
        public Texture2D GreenButtonActive { get; private set; }
        public Texture2D Active { get; private set; }
        public Texture2D HintBackground { get; private set; }
        public Texture2D HintBackgroundAlt { get; private set; }
        public Texture2D CheckEmpty { get; private set; }
        public Texture2D Danger { get; private set; }
        public Texture2D DangerHover { get; private set; }
        public Texture2D DangerActive { get; private set; }
        public Texture2D SliderTrack { get; private set; }
        public Texture2D SliderThumb { get; private set; }
        public Texture2D SliderThumbHover { get; private set; }

        internal LegacyUITextures(LegacyUIPalette palette)
        {
            WindowBackground = CreateSolidTexture(palette.WindowBackground, "BCL_UI_WindowBackground");
            Border = CreateSolidTexture(palette.Border, "BCL_UI_Border");
            Field = CreateSolidTexture(palette.Field, "BCL_UI_Field");
            Button = CreateSolidTexture(palette.Button, "BCL_UI_Button");
            ButtonHover = CreateSolidTexture(palette.ButtonHover, "BCL_UI_ButtonHover");
            GreenButton = CreateSolidTexture(palette.GreenButton, "BCL_UI_GreenButton");
            GreenButtonHover = CreateSolidTexture(palette.GreenButtonHover, "BCL_UI_GreenButtonHover");
            GreenButtonActive = CreateSolidTexture(palette.GreenButtonActive, "BCL_UI_GreenButtonActive");
            Active = CreateSolidTexture(palette.Active, "BCL_UI_Active");
            HintBackground = CreateSolidTexture(palette.HintBackground, "BCL_UI_HintBackground");
            HintBackgroundAlt = CreateSolidTexture(palette.HintBackgroundAlt, "BCL_UI_HintBackgroundAlt");
            CheckEmpty = CreateSolidTexture(palette.CheckEmpty, "BCL_UI_CheckEmpty");
            Danger = CreateSolidTexture(palette.Danger, "BCL_UI_Danger");
            DangerHover = CreateSolidTexture(palette.DangerHover, "BCL_UI_DangerHover");
            DangerActive = CreateSolidTexture(palette.DangerActive, "BCL_UI_DangerActive");
            SliderTrack = CreateSolidTexture(palette.SliderTrack, "BCL_UI_SliderTrack");
            SliderThumb = CreateSolidTexture(palette.SliderThumb, "BCL_UI_SliderThumb");
            SliderThumbHover = CreateSolidTexture(palette.SliderThumbHover, "BCL_UI_SliderThumbHover");
        }

        private static Texture2D CreateSolidTexture(Color color, string name)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            tex.name = name;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixel(0, 0, color);
            tex.Apply(false, true);
            return tex;
        }
    }
}
