using System;
using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Temporarily applies theme-aware IMGUI text editing colors.
    /// Unity stores caret and selection colors in GUI.skin.settings rather than GUIStyle,
    /// so text controls need a short-lived skin override while they are drawn.
    /// </summary>
    internal sealed class LegacyUITextInputSkinScope : IDisposable
    {
        private const float SelectionAlpha = 0.55f;

        private readonly GUISettings settings;
        private readonly Color previousCursorColor;
        private readonly Color previousSelectionColor;
        private readonly bool active;
        private bool disposed;

        internal LegacyUITextInputSkinScope(LegacyUIPalette palette)
        {
            GUISkin skin = GUI.skin;
            if (skin == null || skin.settings == null || palette == null)
            {
                return;
            }

            settings = skin.settings;
            previousCursorColor = settings.cursorColor;
            previousSelectionColor = settings.selectionColor;

            Color selectionColor = palette.Active;
            selectionColor.a = Mathf.Clamp01(selectionColor.a * SelectionAlpha);

            settings.cursorColor = palette.TextBright;
            settings.selectionColor = selectionColor;
            active = true;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            if (!active || settings == null)
            {
                return;
            }

            settings.cursorColor = previousCursorColor;
            settings.selectionColor = previousSelectionColor;
        }
    }
}
