using System.Collections.Generic;
using UnityEngine;

namespace BigCityLegacy.UI
{
    internal static class LegacyUIAnimatedControls
    {
        private sealed class State
        {
            internal readonly LegacyUIFade Hover = new LegacyUIFade();
            internal readonly LegacyUIFade Value = new LegacyUIFade();
            internal GUIStyle Source, Flat;
            internal int Frame;
        }
        private static readonly Dictionary<long, State> states = new Dictionary<long, State>();
        private static readonly List<long> expired = new List<long>();
        private static int lastPrune;

        private static State Get(int id)
        {
            int frame = Time.frameCount;
            if (frame - lastPrune > 120)
            {
                lastPrune = frame; expired.Clear();
                foreach (KeyValuePair<long, State> item in states)
                    if (frame - item.Value.Frame > 300) expired.Add(item.Key);
                foreach (long key in expired) states.Remove(key);
            }
            long identity = ((long)LegacyUI.AnimationOwner << 32) ^ (uint)id;
            State state;
            if (!states.TryGetValue(identity, out state))
            { state = new State(); state.Hover.Reset(0f, Time.realtimeSinceStartup); states.Add(identity, state); }
            state.Frame = frame;
            return state;
        }

        private static float Hover(State state, Rect rect)
        {
            LegacyUIAnimationSettings settings = LegacyUI.CurrentAnimations;
            bool hover = GUI.enabled && Event.current != null && rect.Contains(LegacyUI.MousePosition);
            return state.Hover.Sample(hover ? 1f : 0f, settings.HoverDuration, settings.Enabled, Time.realtimeSinceStartup);
        }

        internal static bool Button(Rect rect, string text, GUIStyle style, bool selected = false)
        {
            int id = GUIUtility.GetControlID(173719, FocusType.Passive, rect);
            State state = Get(id);
            float hover = Hover(state, rect);
            if (state.Source != style || state.Flat == null)
            {
                state.Source = style; state.Flat = new GUIStyle(style);
                state.Flat.normal.background = null;
                state.Flat.hover.background = null;
            }
            Color textColor = Color.Lerp(style.normal.textColor, style.hover.textColor, hover);
            state.Flat.normal.textColor = state.Flat.hover.textColor = textColor;
            if (IsRepaint)
            {
                Texture normal = selected ? LegacyUI.Textures.Active : style.normal.background;
                Texture over = selected ? normal : style.hover.background;
                if (normal != null) GUI.DrawTexture(rect, normal);
                if (over != null) DrawAlpha(rect, over, hover);
            }
            return GUI.Button(rect, text ?? string.Empty, state.Flat);
        }

        internal static bool Toggle(Rect rect, bool value, string label)
        {
            int id = GUIUtility.GetControlID(173720, FocusType.Passive, rect);
            State state = Get(id);
            bool result = GUI.Button(rect, string.Empty, GUIStyle.none) ? !value : value;
            LegacyUIAnimationSettings settings = LegacyUI.CurrentAnimations;
            float selected = state.Value.Sample(result ? 1f : 0f, settings.ToggleDuration, settings.Enabled, Time.realtimeSinceStartup);
            float hover = Hover(state, rect);
            Rect box = new Rect(rect.x, rect.y + Mathf.Max(0f, (rect.height - 14f) * 0.5f), 14f, 14f);
            if (IsRepaint)
            {
                GUI.DrawTexture(box, LegacyUI.Textures.CheckEmpty);
                DrawAlpha(box, LegacyUI.Textures.Active, selected);
                DrawAlpha(box, LegacyUI.Textures.ButtonHover, hover * 0.35f);
            }
            LegacyUI.DrawBorder(box);
            GUI.Label(new Rect(rect.x + 20f, rect.y, Mathf.Max(0f, rect.width - 20f), rect.height), label ?? string.Empty, LegacyUI.Styles.CheckboxLabel);
            return result;
        }

        internal static void Thumb(int id, Rect rect, GUIStyle style, Texture hoverTexture)
        {
            float hover = Hover(Get(id), rect);
            if (!IsRepaint) return;
            GUI.Box(rect, GUIContent.none, style);
            if (hoverTexture != null) DrawAlpha(rect, hoverTexture, hover);
        }

        internal static void FieldHover(Rect rect)
        {
            int id = GUIUtility.GetControlID(173721, FocusType.Passive, rect);
            float hover = Hover(Get(id), rect);
            if (!IsRepaint || hover <= 0f) return;
            Color old = GUI.color;
            try
            {
                GUI.color = new Color(old.r, old.g, old.b, old.a * hover * 0.7f);
                Texture texture = LegacyUI.Textures.Active;
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), texture);
                GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), texture);
                GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), texture);
                GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), texture);
            }
            finally { GUI.color = old; }
        }

        private static bool IsRepaint { get { return Event.current != null && Event.current.type == EventType.Repaint; } }
        private static void DrawAlpha(Rect rect, Texture texture, float alpha)
        {
            Color old = GUI.color;
            try { GUI.color = new Color(old.r, old.g, old.b, old.a * alpha); GUI.DrawTexture(rect, texture); }
            finally { GUI.color = old; }
        }
    }
}
