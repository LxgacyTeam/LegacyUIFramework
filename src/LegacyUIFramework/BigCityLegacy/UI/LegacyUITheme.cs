namespace BigCityLegacy.UI
{
    public sealed class LegacyUITheme
    {
        private LegacyUITextures textures;
        private LegacyUIStyles styles;

        public LegacyUIPalette Palette { get; private set; }

        public LegacyUITextures Textures
        {
            get
            {
                if (textures == null)
                    textures = new LegacyUITextures(Palette);
                return textures;
            }
        }

        public LegacyUIStyles Styles
        {
            get
            {
                if (styles == null)
                    styles = new LegacyUIStyles(Palette, Textures);
                return styles;
            }
        }

        public LegacyUITheme()
            : this(new LegacyUIPalette())
        {
        }

        public LegacyUITheme(LegacyUIPalette palette)
        {
            Palette = palette ?? new LegacyUIPalette();
        }

        /// <summary>
        /// Drops cached textures and styles. Use this after changing Palette fields on an already used theme.
        /// The next draw call will rebuild the theme resources.
        /// </summary>
        public void Invalidate()
        {
            textures = null;
            styles = null;
        }

        /// <summary>
        /// Replaces the palette and drops cached resources.
        /// </summary>
        public void SetPalette(LegacyUIPalette palette)
        {
            Palette = palette ?? new LegacyUIPalette();
            Invalidate();
        }
    }
}
