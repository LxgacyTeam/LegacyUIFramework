using System;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Temporarily switches the active LegacyUI theme for a group of IMGUI calls.
    /// Dispose the scope at the end of the same OnGUI block.
    /// </summary>
    public sealed class LegacyUIThemeScope : IDisposable
    {
        private readonly LegacyUITheme theme;
        private readonly bool active;
        private bool disposed;

        internal LegacyUIThemeScope(LegacyUITheme theme)
        {
            this.theme = theme;
            active = theme != null;
            if (active)
            {
                LegacyUI.PushTheme(theme);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            if (active)
            {
                LegacyUI.PopTheme(theme);
            }
        }
    }
}
