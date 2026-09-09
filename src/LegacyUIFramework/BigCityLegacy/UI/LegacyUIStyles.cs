using UnityEngine;

namespace BigCityLegacy.UI
{
    public sealed class LegacyUIStyles
    {
        public GUIStyle Window { get; private set; }
        public GUIStyle Label { get; private set; }
        public GUIStyle Title { get; private set; }
        public GUIStyle MiniHint { get; private set; }
        public GUIStyle Hint { get; private set; }
        public GUIStyle HintAlt { get; private set; }
        public GUIStyle Status { get; private set; }
        public GUIStyle TextField { get; private set; }
        public GUIStyle TextArea { get; private set; }
        public GUIStyle Button { get; private set; }
        public GUIStyle GreenButton { get; private set; }
        public GUIStyle DangerButton { get; private set; }
        public GUIStyle Tab { get; private set; }
        public GUIStyle CheckboxLabel { get; private set; }
        public GUIStyle CloseButton { get; private set; }
        public GUIStyle HorizontalSlider { get; private set; }
        public GUIStyle HorizontalSliderThumb { get; private set; }
        public GUIStyle VerticalSlider { get; private set; }
        public GUIStyle VerticalSliderThumb { get; private set; }
        public GUIStyle HorizontalScrollbar { get; private set; }
        public GUIStyle HorizontalScrollbarThumb { get; private set; }
        public GUIStyle VerticalScrollbar { get; private set; }
        public GUIStyle VerticalScrollbarThumb { get; private set; }

        internal LegacyUIStyles(LegacyUIPalette palette, LegacyUITextures textures)
        {
            Window = new GUIStyle();
            Window.normal.background = textures.WindowBackground;

            Label = new GUIStyle();
            Label.normal.textColor = palette.Text;
            Label.fontSize = 12;
            Label.alignment = TextAnchor.MiddleLeft;

            Title = new GUIStyle(Label);
            Title.fontSize = 13;
            Title.fontStyle = FontStyle.Bold;
            Title.normal.textColor = palette.TextBright;

            MiniHint = new GUIStyle(Label);
            MiniHint.fontSize = 10;
            MiniHint.alignment = TextAnchor.MiddleRight;
            MiniHint.normal.textColor = palette.TextMuted;

            Hint = new GUIStyle();
            Hint.normal.background = textures.HintBackground;
            Hint.normal.textColor = palette.Text;
            Hint.alignment = TextAnchor.MiddleCenter;
            Hint.fontSize = 12;
            Hint.richText = true;

            HintAlt = new GUIStyle(Hint);
            HintAlt.normal.background = textures.HintBackgroundAlt;
            HintAlt.normal.textColor = palette.TextMuted;

            Status = new GUIStyle(Label);
            Status.fontSize = 11;
            Status.normal.textColor = palette.Status;

            TextField = new GUIStyle();
            TextField.normal.background = textures.Field;
            TextField.normal.textColor = palette.TextBright;
            TextField.hover.background = textures.Field;
            TextField.hover.textColor = palette.TextBright;
            TextField.focused.background = textures.Field;
            TextField.focused.textColor = palette.TextBright;
            TextField.active.background = textures.Field;
            TextField.active.textColor = palette.TextBright;
            TextField.padding = new RectOffset(8, 8, 0, 0);
            TextField.alignment = TextAnchor.MiddleLeft;
            TextField.fontSize = 12;
            TextField.clipping = TextClipping.Clip;
            TextField.wordWrap = false;
            TextField.border = new RectOffset();

            TextArea = new GUIStyle(TextField);
            TextArea.alignment = TextAnchor.UpperLeft;
            TextArea.padding = new RectOffset(8, 8, 6, 6);
            TextArea.wordWrap = true;

            Button = new GUIStyle();
            Button.normal.background = textures.Button;
            Button.normal.textColor = palette.TextButton;
            Button.hover.background = textures.ButtonHover;
            Button.hover.textColor = palette.TextBright;
            Button.active.background = textures.Active;
            Button.active.textColor = palette.TextBright;
            Button.alignment = TextAnchor.MiddleCenter;
            Button.fontSize = 12;

            GreenButton = new GUIStyle(Button);
            GreenButton.normal.background = textures.GreenButton;
            GreenButton.hover.background = textures.GreenButtonHover;
            GreenButton.active.background = textures.GreenButtonActive;
            GreenButton.fontStyle = FontStyle.Bold;

            DangerButton = new GUIStyle(Button);
            DangerButton.normal.background = textures.Danger;
            DangerButton.hover.background = textures.DangerHover;
            DangerButton.active.background = textures.DangerActive;
            DangerButton.fontStyle = FontStyle.Bold;

            Tab = new GUIStyle(Button);
            Tab.fontSize = 11;

            CheckboxLabel = new GUIStyle(Label);
            CheckboxLabel.hover.textColor = palette.TextBright;

            CloseButton = new GUIStyle(Button);
            CloseButton.fontSize = 13;
            CloseButton.fontStyle = FontStyle.Bold;
            CloseButton.normal.background = null;
            CloseButton.hover.background = textures.Danger;
            CloseButton.active.background = textures.DangerHover;
            CloseButton.normal.textColor = palette.TextMuted;
            CloseButton.hover.textColor = palette.TextBright;
            CloseButton.active.textColor = palette.TextBright;

            HorizontalSlider = new GUIStyle();
            HorizontalSlider.normal.background = textures.SliderTrack;
            HorizontalSlider.fixedHeight = 4f;

            HorizontalSliderThumb = new GUIStyle();
            HorizontalSliderThumb.normal.background = textures.SliderThumb;
            HorizontalSliderThumb.hover.background = textures.SliderThumbHover;
            HorizontalSliderThumb.active.background = textures.SliderThumbHover;
            HorizontalSliderThumb.fixedWidth = 12f;
            HorizontalSliderThumb.fixedHeight = 18f;

            VerticalSlider = new GUIStyle();
            VerticalSlider.normal.background = textures.SliderTrack;
            VerticalSlider.fixedWidth = 4f;

            VerticalSliderThumb = new GUIStyle(HorizontalSliderThumb);
            VerticalSliderThumb.fixedWidth = 18f;
            VerticalSliderThumb.fixedHeight = 12f;

            HorizontalScrollbar = new GUIStyle();
            HorizontalScrollbar.normal.background = textures.Field;
            HorizontalScrollbar.fixedHeight = 14f;

            HorizontalScrollbarThumb = new GUIStyle();
            HorizontalScrollbarThumb.normal.background = textures.ButtonHover;
            HorizontalScrollbarThumb.hover.background = textures.SliderThumb;
            HorizontalScrollbarThumb.active.background = textures.SliderThumbHover;
            HorizontalScrollbarThumb.fixedHeight = 14f;

            VerticalScrollbar = new GUIStyle();
            VerticalScrollbar.normal.background = textures.Field;
            VerticalScrollbar.fixedWidth = 14f;

            VerticalScrollbarThumb = new GUIStyle(HorizontalScrollbarThumb);
            VerticalScrollbarThumb.fixedWidth = 14f;
            VerticalScrollbarThumb.fixedHeight = 0f;
        }
    }
}
