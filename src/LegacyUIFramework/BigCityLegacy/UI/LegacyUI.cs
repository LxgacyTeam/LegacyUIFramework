using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Main immediate-mode UI facade. Contains helpers for the default BigCityLegacy visual style.
    /// </summary>
    public static class LegacyUI
    {
        private static LegacyUITheme theme;
        private static readonly List<LegacyUITheme> themeStack = new List<LegacyUITheme>();
        private static readonly List<LegacyUIContentTracker> contentTrackerStack = new List<LegacyUIContentTracker>();

        public static string Version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "???";

        public static LegacyUITheme Theme
        {
            get
            {
                EnsureInitialized();
                if (themeStack.Count > 0)
                {
                    return themeStack[themeStack.Count - 1];
                }
                return theme;
            }
        }

        /// <summary>
        /// Base global theme used when no scoped theme is active.
        /// </summary>
        public static LegacyUITheme GlobalTheme
        {
            get
            {
                EnsureInitialized();
                return theme;
            }
        }

        public static LegacyUIStyles Styles
        {
            get { return Theme.Styles; }
        }

        public static LegacyUITextures Textures
        {
            get { return Theme.Textures; }
        }

        public static LegacyUIPalette Palette
        {
            get { return Theme.Palette; }
        }

        public static void SetTheme(LegacyUITheme newTheme)
        {
            theme = newTheme ?? new LegacyUITheme();
        }

        /// <summary>
        /// Temporarily uses a theme for a group of IMGUI calls without changing the global theme.
        /// </summary>
        public static LegacyUIThemeScope WithTheme(LegacyUITheme scopedTheme)
        {
            EnsureInitialized();
            return new LegacyUIThemeScope(scopedTheme);
        }

        /// <summary>
        /// Alias for WithTheme, useful when the call site reads better as a scope.
        /// </summary>
        public static LegacyUIThemeScope ThemeScope(LegacyUITheme scopedTheme)
        {
            return WithTheme(scopedTheme);
        }

        public static void EnsureInitialized()
        {
            if (theme == null)
                theme = new LegacyUITheme();
        }

        internal static void PushTheme(LegacyUITheme scopedTheme)
        {
            if (scopedTheme == null)
            {
                return;
            }
            EnsureInitialized();
            themeStack.Add(scopedTheme);
        }

        internal static void PopTheme(LegacyUITheme scopedTheme)
        {
            if (scopedTheme == null || themeStack.Count == 0)
            {
                return;
            }

            int last = themeStack.Count - 1;
            if (ReferenceEquals(themeStack[last], scopedTheme))
            {
                themeStack.RemoveAt(last);
                return;
            }

            for (int i = last; i >= 0; i--)
            {
                if (ReferenceEquals(themeStack[i], scopedTheme))
                {
                    themeStack.RemoveAt(i);
                    return;
                }
            }
        }

        internal static void RegisterElementRect(Rect rect)
        {
            if (contentTrackerStack.Count == 0)
                return;

            contentTrackerStack[contentTrackerStack.Count - 1].Include(rect);
        }

        internal static void PushContentTracker(LegacyUIContentTracker tracker)
        {
            if (tracker != null)
                contentTrackerStack.Add(tracker);
        }

        internal static void PopContentTracker(LegacyUIContentTracker tracker)
        {
            if (tracker == null || contentTrackerStack.Count == 0)
                return;

            int last = contentTrackerStack.Count - 1;
            if (ReferenceEquals(contentTrackerStack[last], tracker))
            {
                contentTrackerStack.RemoveAt(last);
                return;
            }

            for (int i = last; i >= 0; i--)
            {
                if (ReferenceEquals(contentTrackerStack[i], tracker))
                {
                    contentTrackerStack.RemoveAt(i);
                    return;
                }
            }
        }

        public static void Panel(Rect rect, float? backgroundAlpha = null)
        {
            RegisterElementRect(rect);
            if (backgroundAlpha.HasValue)
                DrawPanelBackground(rect, Mathf.Clamp01(backgroundAlpha.Value));
            else
                GUI.Box(rect, string.Empty, Styles.Window);

            DrawBorder(rect);
        }

        public static void Panel(Rect rect, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                Panel(rect);
            }
        }

        public static void Panel(Rect rect, float? backgroundAlpha, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                Panel(rect, backgroundAlpha);
            }
        }

        private static void DrawPanelBackground(Rect rect, float alpha)
        {
            Color oldColor = GUI.color;
            Color background = Palette.WindowBackground;
            background.a = alpha;

            GUI.color = new Color(
                oldColor.r * background.r,
                oldColor.g * background.g,
                oldColor.b * background.b,
                oldColor.a * background.a);

            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        public static void DrawBorder(Rect rect, int width = 1)
        {
            RegisterElementRect(rect);
            if (width <= 0) return;
            Texture2D tex = Textures.Border;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, width), tex);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - width, rect.width, width), tex);
            GUI.DrawTexture(new Rect(rect.x, rect.y, width, rect.height), tex);
            GUI.DrawTexture(new Rect(rect.x + rect.width - width, rect.y, width, rect.height), tex);
        }

        public static void DrawBorder(Rect rect, LegacyUITheme scopedTheme, int width = 1)
        {
            using (WithTheme(scopedTheme))
            {
                DrawBorder(rect, width);
            }
        }

        public static void Separator(Rect rect)
        {
            RegisterElementRect(rect);
            GUI.DrawTexture(rect, Textures.Border);
        }

        public static void Separator(Rect rect, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                Separator(rect);
            }
        }

        public static void Label(Rect rect, string text)
        {
            RegisterElementRect(rect);
            GUI.Label(rect, text ?? string.Empty, Styles.Label);
        }

        public static void Label(Rect rect, string text, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                Label(rect, text);
            }
        }

        public static void Title(Rect rect, string text)
        {
            RegisterElementRect(rect);
            GUI.Label(rect, text ?? string.Empty, Styles.Title);
        }

        public static void Title(Rect rect, string text, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                Title(rect, text);
            }
        }

        public static void MiniHint(Rect rect, string text)
        {
            RegisterElementRect(rect);
            GUI.Label(rect, text ?? string.Empty, Styles.MiniHint);
        }

        public static void MiniHint(Rect rect, string text, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                MiniHint(rect, text);
            }
        }

        public static void HintBox(Rect rect, string text)
        {
            RegisterElementRect(rect);
            GUI.Box(rect, string.Empty, Styles.Hint);
            GUI.Label(rect, text ?? string.Empty, Styles.Hint);
            DrawBorder(rect);
        }

        public static void HintBox(Rect rect, string text, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                HintBox(rect, text);
            }
        }

        public static void HintBoxAlt(Rect rect, string text)
        {
            RegisterElementRect(rect);
            GUI.Box(rect, string.Empty, Styles.HintAlt);
            GUI.Label(rect, text ?? string.Empty, Styles.HintAlt);
            DrawBorder(rect);
        }

        public static void HintBoxAlt(Rect rect, string text, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                HintBoxAlt(rect, text);
            }
        }

        public static void Status(Rect rect, string text)
        {
            RegisterElementRect(rect);
            GUI.Label(rect, text ?? string.Empty, Styles.Status);
        }

        public static void Status(Rect rect, string text, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                Status(rect, text);
            }
        }

        public static string TextField(Rect rect, string value)
        {
            RegisterElementRect(rect);
            return GUI.TextField(rect, value ?? string.Empty, Styles.TextField);
        }

        public static string TextField(Rect rect, string value, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                return TextField(rect, value);
            }
        }

        public static string TextArea(Rect rect, string value)
        {
            RegisterElementRect(rect);
            return GUI.TextArea(rect, value ?? string.Empty, Styles.TextArea);
        }

        public static string TextArea(Rect rect, string value, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                return TextArea(rect, value);
            }
        }

        public static bool Button(Rect rect, string text)
        {
            RegisterElementRect(rect);
            return GUI.Button(rect, text ?? string.Empty, Styles.Button);
        }

        public static bool Button(Rect rect, string text, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                return Button(rect, text);
            }
        }

        public static bool GreenButton(Rect rect, string text)
        {
            RegisterElementRect(rect);
            return GUI.Button(rect, text ?? string.Empty, Styles.GreenButton);
        }

        public static bool GreenButton(Rect rect, string text, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                return GreenButton(rect, text);
            }
        }

        public static bool DangerButton(Rect rect, string text)
        {
            RegisterElementRect(rect);
            return GUI.Button(rect, text ?? string.Empty, Styles.DangerButton);
        }

        public static bool DangerButton(Rect rect, string text, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                return DangerButton(rect, text);
            }
        }

        public static bool TabButton(Rect rect, string text, bool selected)
        {
            RegisterElementRect(rect);
            GUIStyle style = new GUIStyle(Styles.Tab);
            if (selected)
            {
                style.normal.background = Textures.Active;
                style.hover.background = Textures.Active;
                style.active.background = Textures.Active;
            }
            return GUI.Button(rect, text ?? string.Empty, style);
        }

        public static bool TabButton(Rect rect, string text, bool selected, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                return TabButton(rect, text, selected);
            }
        }

        public static bool Toggle(Rect rect, bool value, string label)
        {
            RegisterElementRect(rect);
            bool clicked = GUI.Button(rect, string.Empty, GUIStyle.none);

            Rect checkRect = new Rect(rect.x, rect.y + Mathf.Max(0f, (rect.height - 14f) * 0.5f), 14f, 14f);
            GUI.Box(checkRect, string.Empty, new GUIStyle { normal = { background = value ? Textures.Active : Textures.CheckEmpty } });
            DrawBorder(checkRect);

            Rect labelRect = new Rect(rect.x + 20f, rect.y, rect.width - 20f, rect.height);
            GUI.Label(labelRect, label ?? string.Empty, Styles.CheckboxLabel);

            return clicked ? !value : value;
        }

        public static bool Toggle(Rect rect, bool value, string label, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                return Toggle(rect, value, label);
            }
        }

        public static float HorizontalSlider(Rect rect, float value, float min, float max)
        {
            RegisterElementRect(rect);
            return LegacyUIValueControls.HorizontalSlider(rect, value, min, max);
        }

        public static float HorizontalSlider(Rect rect, float value, float min, float max, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                return HorizontalSlider(rect, value, min, max);
            }
        }

        public static float VerticalSlider(Rect rect, float value, float min, float max)
        {
            RegisterElementRect(rect);
            return LegacyUIValueControls.VerticalSlider(rect, value, min, max);
        }

        public static float VerticalSlider(Rect rect, float value, float min, float max, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                return VerticalSlider(rect, value, min, max);
            }
        }

        public static float HorizontalScrollbar(Rect rect, float value, float visibleSize, float leftValue, float rightValue)
        {
            RegisterElementRect(rect);
            return LegacyUIValueControls.HorizontalScrollbar(rect, value, visibleSize, leftValue, rightValue);
        }

        public static float HorizontalScrollbar(Rect rect, float value, float visibleSize, float leftValue, float rightValue, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                return HorizontalScrollbar(rect, value, visibleSize, leftValue, rightValue);
            }
        }

        public static float VerticalScrollbar(Rect rect, float value, float visibleSize, float topValue, float bottomValue)
        {
            RegisterElementRect(rect);
            return LegacyUIValueControls.VerticalScrollbar(rect, value, visibleSize, topValue, bottomValue);
        }

        public static float VerticalScrollbar(Rect rect, float value, float visibleSize, float topValue, float bottomValue, LegacyUITheme scopedTheme)
        {
            using (WithTheme(scopedTheme))
            {
                return VerticalScrollbar(rect, value, visibleSize, topValue, bottomValue);
            }
        }

        internal static Rect ClampRectToScreen(Rect rect, float padding)
        {
            if (rect.width < 1f || rect.height < 1f) return rect;
            float maxX = Mathf.Max(padding, Screen.width - rect.width - padding);
            float maxY = Mathf.Max(padding, Screen.height - rect.height - padding);
            rect.x = Mathf.Clamp(rect.x, padding, maxX);
            rect.y = Mathf.Clamp(rect.y, padding, maxY);
            return rect;
        }
    }
}
