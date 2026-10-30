using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>HintBoxAlt with persistent fade state. Continue Draw while IsRendered, including fade-out.</summary>
    public sealed class LegacyUIHintBox
    {
        private readonly LegacyUIFade fade = new LegacyUIFade();
        private readonly int owner = LegacyUIInputBlocker.AllocateOwnerId();
        private LegacyUIAnimationSettings lastAnimations;
        private bool visible = true;
        private GUIStyle labelSource, labelStyle;
        public Rect Rect { get; set; }
        public LegacyUIHintBoxOptions Options { get; private set; }
        public bool Visible { get { return visible; } set { visible = value; if (!value) LegacyUIInputBlocker.ClearWindow(owner); } }
        public float Opacity
        {
            get
            {
                var settings = Options.Animations ?? lastAnimations ?? LegacyUI.CurrentAnimations;
                return fade.Sample(visible ? 1f : 0f, settings.WindowDuration, settings.Enabled, Time.realtimeSinceStartup);
            }
        }
        public bool IsRendered { get { return visible || Opacity > .001f; } }
        public LegacyUIHintBox(Rect rect, LegacyUIHintBoxOptions options = null)
        { Rect = rect; Options = options ?? new LegacyUIHintBoxOptions(); fade.Reset(0f, Time.realtimeSinceStartup); }

        public void Draw(string text) { Draw(text, null); }
        public void Draw(string text, Action<Rect> content)
        {
            using (new LegacyUIAnimationScope(Options.Animations, owner))
            using (LegacyUI.WithTheme(Options.Theme))
            using (LegacyUI.WithScale(Options.ScaleMultiplier))
            {
                lastAnimations = LegacyUI.CurrentAnimations;
                float opacity = Opacity;
                if (!Visible && opacity <= .001f) { LegacyUIInputBlocker.ClearWindow(owner); return; }
                LegacyUI.RegisterElementRect(Rect);
                if (Visible) LegacyUIInputBlocker.ReportWindow(owner, LegacyUI.ToPhysicalRect(Rect), Options.InputBlockMode);
                Color before = GUI.color; bool beforeEnabled = GUI.enabled;
                try
                {
                    GUI.color = new Color(before.r, before.g, before.b, before.a * opacity);
                    GUI.enabled = beforeEnabled && Visible;
                    var provider = Options.Backdrop ?? LegacyUI.ActiveBackdrop;
                    bool drawn = Options.BackdropBlur && Event.current != null && Event.current.type == EventType.Repaint && provider != null && provider.Draw(Rect);
                    Color background = Options.BackgroundColor ?? LegacyUI.Palette.HintBackgroundAlt;
                    background.a = Mathf.Clamp01(Options.BackgroundAlpha ?? (Options.BackgroundColor.HasValue ? background.a : Options.BackdropBlur ? 1f : background.a));
                    if (drawn) background.a *= Mathf.Clamp01(Options.BlurTintAlpha);
                    GUI.color = new Color(before.r * background.r, before.g * background.g, before.b * background.b, before.a * opacity * background.a);
                    GUI.DrawTexture(Rect, Texture2D.whiteTexture);
                    GUI.color = new Color(before.r, before.g, before.b, before.a * opacity);
                    var diagnostic = provider as ILegacyUIBackdropDiagnostics;
                    if (diagnostic != null) diagnostic.ReportWindow(new LegacyUIBackdropDiagnosticInfo {
                        Title = "HintBoxAlt", Rect = Rect, Requested = Options.BackdropBlur, Drawn = drawn,
                        EntryEvent = Event.current != null ? Event.current.type : EventType.Ignore,
                        CurrentEvent = Event.current != null ? Event.current.type : EventType.Ignore,
                        RawEvent = Event.current != null ? Event.current.rawType : EventType.Ignore,
                        BackgroundAlpha = background.a, TintMultiplier = Options.BlurTintAlpha, Opacity = GUI.color.a });
                    if (!string.IsNullOrEmpty(text))
                    {
                        // A label with the original HintAlt style would paint its background
                        // a second time and cover both the blur and the configured tint.
                        if (labelSource != LegacyUI.Styles.HintAlt || labelStyle == null)
                        {
                            labelSource = LegacyUI.Styles.HintAlt; labelStyle = new GUIStyle(labelSource);
                            labelStyle.normal.background = labelStyle.hover.background = labelStyle.active.background = labelStyle.focused.background = null;
                        }
                        GUI.Label(Rect, text, labelStyle);
                    }
                    if (Options.DrawBorder) LegacyUI.DrawBorder(Rect);
                    float padding = Mathf.Max(0f, Options.Padding);
                    if (content != null) content(new Rect(Rect.x + padding, Rect.y + padding, Mathf.Max(0f, Rect.width - 2f * padding), Mathf.Max(0f, Rect.height - 2f * padding)));
                }
                finally { GUI.color = before; GUI.enabled = beforeEnabled; }
            }
        }

        private sealed class Cached { internal LegacyUIHintBox Box; internal int Frame; }
        private static readonly Dictionary<long, Cached> cache = new Dictionary<long, Cached>();
        private static int lastPrune;
        internal static void DrawImmediate(Rect rect, string text, LegacyUIHintBoxOptions options)
        {
            int frame = Time.frameCount;
            if (frame - lastPrune > 120)
            {
                lastPrune = frame; var expired = new List<long>();
                foreach (var pair in cache) if (frame - pair.Value.Frame > 300) expired.Add(pair.Key);
                foreach (long key in expired) cache.Remove(key);
            }
            int id = GUIUtility.GetControlID(173727, FocusType.Passive, rect);
            long identity = ((long)LegacyUI.AnimationOwner << 32) ^ (uint)id; Cached state;
            if (!cache.TryGetValue(identity, out state) || frame - state.Frame > 2)
            { state = new Cached { Box = new LegacyUIHintBox(rect, options) }; cache[identity] = state; }
            state.Frame = frame; state.Box.Rect = rect;
            if (options != null) state.Box.Options = options;
            state.Box.Draw(text);
        }
    }
}
