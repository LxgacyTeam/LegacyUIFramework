using System;
using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Draggable IMGUI window. It does not use GUI.Window.
    /// </summary>
    public sealed class LegacyUIWindow
    {
        private static readonly System.Collections.Generic.List<WeakReference> drawnWindows = new System.Collections.Generic.List<WeakReference>();
        private int lastDrawFrame = -100;
        /// <summary>True while any recently drawn framework window is open or fading out.</summary>
        public static bool AnyWindowOpen
        {
            get
            {
                bool open = false;
                for (int i = drawnWindows.Count - 1; i >= 0; i--)
                {
                    var window = drawnWindows[i].Target as LegacyUIWindow;
                    if (window == null) { drawnWindows.RemoveAt(i); continue; }
                    if (Time.frameCount - window.lastDrawFrame <= 2 && window.IsRendered) open = true;
                }
                return open;
            }
        }
        private Rect rect;
        private Vector2 dragOffset;
        private bool dragging;
        private int dragStartedFrame;
        private Rect physicalDragHandle;
        private bool dragHandleKnown;
        private bool prefsLoaded;
        private readonly int inputBlockOwnerId = LegacyUIInputBlocker.AllocateOwnerId();

        public string Title { get; set; }
        private bool visible = true;
        private float opacity;
        private readonly LegacyUIFade visibilityFade = new LegacyUIFade();
        private readonly LegacyUIFade dragFade = new LegacyUIFade();
        private LegacyUIAnimationSettings lastAnimations;
        public bool Visible
        {
            get { return visible; }
            set
            {
                if (visible == value) return;
                visible = value;
                if (!value) { dragging = false; LegacyUIInputBlocker.ClearWindow(inputBlockOwnerId); }
                UpdateOpacity();
            }
        }
        /// <summary>Keep calling Draw while this is true, including after Visible becomes false.</summary>
        public bool IsRendered { get { UpdateOpacity(); return visible || opacity > 0.001f; } }
        public float Opacity { get { UpdateOpacity(); return opacity; } }
        private void UpdateOpacity()
        {
            LegacyUIAnimationSettings settings = (Options != null ? Options.Animations : null) ?? lastAnimations ?? LegacyUI.CurrentAnimations;
            opacity = visibilityFade.Sample(visible ? 1f : 0f, settings.WindowDuration, settings.Enabled, Time.realtimeSinceStartup);
        }
        public LegacyUIWindowOptions Options { get; private set; }
        public Rect Rect
        {
            get { return rect; }
            set { rect = value; }
        }

        public bool IsDragging
        {
            get { RefreshDragState(); return dragging; }
        }

        /// <summary>Read-only alias: true only while a header drag gesture is captured.</summary>
        public bool isDragging { get { return IsDragging; } }

        /// <summary>Input.mousePosition-style physical screen coordinates (origin at bottom left).
        /// Uses the last drawn header transform, including plugin/window scaling; excludes close.</summary>
        public bool IsDragHandleAtScreenPoint(Vector2 point)
        {
            return Visible && Options.Draggable && dragHandleKnown &&
                physicalDragHandle.Contains(new Vector2(point.x, Screen.height - point.y));
        }

        private void RefreshDragState()
        {
            if (!dragging) return;
            if (!Visible || !Options.Draggable) { dragging = false; return; }
            // MouseUp can be lost when the pointer is released outside the game.
            // Do not poll on the capture frame: native input may precede IMGUI.
            if (Time.frameCount != dragStartedFrame && LegacyInput.Init() && !LegacyInput.GetMouseButton(0))
            {
                dragging = false;
                SavePrefs();
            }
        }

        private Rect GetDragHandleRect()
        {
            return new Rect(rect.x, rect.y, Mathf.Max(0f, rect.width - (Options.ShowCloseButton ? 34f : 0f)),
                Mathf.Max(0f, Options.HeaderHeight));
        }

        public event Action Closed;

        public LegacyUIWindow(string title, Rect startRect)
            : this(title, startRect, null)
        {
        }

        public LegacyUIWindow(string title, Rect startRect, LegacyUIWindowOptions options)
        {
            Title = title ?? string.Empty;
            rect = startRect;
            Options = options ?? new LegacyUIWindowOptions();
            visibilityFade.Reset(0f, Time.realtimeSinceStartup);
            dragFade.Reset(1f, Time.realtimeSinceStartup);
        }

        public void Draw(Action<Rect> drawContent)
        {
            float? previousPanelAlpha = LegacyUI.ActivePanelBackgroundAlpha;
            LegacyUI.ActivePanelBackgroundAlpha = Options.BackgroundAlpha;
            try
            {
                using (new LegacyUIAnimationScope(Options.Animations, inputBlockOwnerId))
                using (LegacyUI.WithTheme(Options != null ? Options.Theme : null))
                using (LegacyUI.WithScale(Options != null ? Options.ScaleMultiplier : 1f))
                {
                    DrawInternal(drawContent);
                }
            }
            finally { LegacyUI.ActivePanelBackgroundAlpha = previousPanelAlpha; }
        }

        private void DrawInternal(Action<Rect> drawContent)
        {
            Event entry = Event.current;
            EventType entryEvent = entry != null ? entry.type : EventType.Ignore;
            EventType rawEvent = entry != null ? entry.rawType : EventType.Ignore;
            LegacyUI.EnsureInitialized();
            lastAnimations = LegacyUI.CurrentAnimations;
            LoadPrefsOnce();
            UpdateOpacity();
            if (!IsRendered)
            { dragHandleKnown = false; LegacyUIInputBlocker.ClearWindow(inputBlockOwnerId); return; }
            if (lastDrawFrame == -100) drawnWindows.Add(new WeakReference(this));
            lastDrawFrame = Time.frameCount;
            FixRect();
            if (Visible) HandleDragAndClose();
            if (lastDrawFrame == -100) drawnWindows.Add(new WeakReference(this));
            lastDrawFrame = Time.frameCount;
            FixRect();
            dragHandleKnown = Visible && Options.Draggable && GUI.enabled;
            if (dragHandleKnown) physicalDragHandle = LegacyUI.ToPhysicalRect(GetDragHandleRect());
            LegacyUIInputBlocker.ReportWindow(inputBlockOwnerId, LegacyUI.ToPhysicalRect(rect), Options.InputBlockMode);

            Color oldColor = GUI.color;
            bool oldEnabled = GUI.enabled;
            try
            {
                LegacyUIAnimationSettings settings = LegacyUI.CurrentAnimations;
                float drag = dragFade.Sample(dragging && Options.FadeWhileDragging ? Mathf.Clamp01(Options.DragAlpha) : 1f,
                    settings.DragDuration, settings.Enabled, Time.realtimeSinceStartup);
                GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, oldColor.a * opacity * drag);
                GUI.enabled = oldEnabled && Visible;
                ILegacyUIBackdrop backdrop = Options.Backdrop ?? LegacyUI.ActiveBackdrop;
                EventType backdropEvent = Event.current != null ? Event.current.type : EventType.Ignore;
                bool blurred = Event.current != null && Event.current.type == EventType.Repaint &&
                    Options.BackdropBlur && backdrop != null && backdrop.Draw(rect);
                float? alpha = Options.BackgroundAlpha;
                if (!alpha.HasValue && Options.BackdropBlur) alpha = 1f;
                if (blurred) alpha = (alpha ?? LegacyUI.Palette.WindowBackground.a) * Mathf.Clamp01(Options.BlurTintAlpha);
                ILegacyUIBackdropDiagnostics diagnostic = backdrop as ILegacyUIBackdropDiagnostics;
                if (diagnostic != null)
                    diagnostic.ReportWindow(new LegacyUIBackdropDiagnosticInfo
                    {
                        Title = Title, Rect = rect, EntryEvent = entryEvent, CurrentEvent = backdropEvent,
                        RawEvent = rawEvent, Requested = Options.BackdropBlur, Drawn = blurred,
                        BackgroundAlpha = alpha ?? LegacyUI.Palette.WindowBackground.a,
                        TintMultiplier = Options.BlurTintAlpha, Opacity = GUI.color.a
                    });
                LegacyUI.Panel(rect, alpha);
                if (DrawHeader()) GUI.enabled = false;
                Rect content = new Rect(rect.x + 15f, rect.y + Options.HeaderHeight + 12f,
                    rect.width - 30f, Mathf.Max(0f, rect.height - Options.HeaderHeight - 24f));
                if (drawContent != null) drawContent(content);
            }
            finally { GUI.color = oldColor; GUI.enabled = oldEnabled; }
            BlockBackgroundInputIfNeeded();
        }

        public void ToggleVisible()
        {
            Visible = !Visible;
            if (!Visible)
            {
                LegacyUIInputBlocker.ClearWindow(inputBlockOwnerId);
                SavePrefs();
            }
        }

        public void Close()
        {
            if (!Visible) return;
            Visible = false;
            LegacyUIInputBlocker.ClearWindow(inputBlockOwnerId);
            SavePrefs();
            if (Closed != null) Closed();
        }

        public void SavePrefs()
        {
            string key = Options.PlayerPrefsKey;
            if (string.IsNullOrEmpty(key)) return;
            PlayerPrefs.SetFloat(key + ".x", rect.x);
            PlayerPrefs.SetFloat(key + ".y", rect.y);
            PlayerPrefs.SetFloat(key + ".w", rect.width);
            PlayerPrefs.SetFloat(key + ".h", rect.height);
            PlayerPrefs.SetInt(key + ".visible", Visible ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadPrefsOnce()
        {
            if (prefsLoaded) return;
            prefsLoaded = true;

            string key = Options.PlayerPrefsKey;
            if (string.IsNullOrEmpty(key)) return;

            if (PlayerPrefs.HasKey(key + ".x") && PlayerPrefs.HasKey(key + ".y"))
            {
                rect.x = PlayerPrefs.GetFloat(key + ".x", rect.x);
                rect.y = PlayerPrefs.GetFloat(key + ".y", rect.y);
            }

            if (PlayerPrefs.HasKey(key + ".w") && PlayerPrefs.HasKey(key + ".h"))
            {
                rect.width = Mathf.Max(120f, PlayerPrefs.GetFloat(key + ".w", rect.width));
                rect.height = Mathf.Max(90f, PlayerPrefs.GetFloat(key + ".h", rect.height));
            }

            if (PlayerPrefs.HasKey(key + ".visible"))
                Visible = PlayerPrefs.GetInt(key + ".visible", Visible ? 1 : 0) == 1;
        }

        private void FixRect()
        {
            if (rect.width < 120f) rect.width = 120f;
            if (rect.height < 90f) rect.height = 90f;
            if (Options.ClampToScreen)
                rect = LegacyUI.ClampRectToScreen(rect, Options.ScreenPadding);
        }

        private bool DrawHeader()
        {
            Rect titleRect = new Rect(rect.x + 16f, rect.y + 11f, rect.width - 72f, 20f);
            LegacyUI.Title(titleRect, Title);

            if (!string.IsNullOrEmpty(Options.RightHint) && !Options.ShowCloseButton)
                LegacyUI.MiniHint(new Rect(rect.x + rect.width - 116f, rect.y + 12f, 100f, 18f), Options.RightHint);
            else if (!string.IsNullOrEmpty(Options.RightHint) && Options.ShowCloseButton)
                LegacyUI.MiniHint(new Rect(rect.x + rect.width - 138f, rect.y + 12f, 100f, 18f), Options.RightHint);

            if (Options.ShowCloseButton && LegacyUI.StyledButton(GetCloseButtonRect(), "×", LegacyUI.Styles.CloseButton))
            {
                Close();
                return true;
            }

            LegacyUI.Separator(new Rect(rect.x + 12f, rect.y + Options.HeaderHeight, rect.width - 24f, 1f));
            return false;
        }

        private void HandleDragAndClose()
        {
            RefreshDragState();
            Event cur = Event.current;
            if (cur == null) return;
            if (!Options.Draggable || !GUI.enabled || cur.type == EventType.Ignore)
            { dragging = false; return; }
            Rect titleArea = GetDragHandleRect();

            if (cur.type == EventType.MouseDown && cur.button == 0 && titleArea.Contains(LegacyUI.MousePosition))
            {
                dragging = true;
                dragStartedFrame = Time.frameCount;
                dragOffset = LegacyUI.MousePosition - rect.position;
                cur.Use();
            }
            else if (cur.type == EventType.MouseDrag && dragging)
            {
                rect.position = LegacyUI.MousePosition - dragOffset;
                if (lastDrawFrame == -100) drawnWindows.Add(new WeakReference(this));
            lastDrawFrame = Time.frameCount;
            FixRect();
                cur.Use();
            }
            else if (cur.type == EventType.MouseUp && cur.button == 0 && dragging)
            {
                dragging = false;
                SavePrefs();
                cur.Use();
            }
        }

        private void BlockBackgroundInputIfNeeded()
        {
            if (Options.InputBlockMode == LegacyUIInputBlockMode.None)
                return;

            Event cur = Event.current;
            if (!IsBlockableMouseEvent(cur))
                return;

            bool shouldBlock;
            if (Options.InputBlockMode == LegacyUIInputBlockMode.Screen)
            {
                shouldBlock = new Rect(0f, 0f, LegacyUI.VirtualScreenSize.x, LegacyUI.VirtualScreenSize.y).Contains(LegacyUI.MousePosition);
            }
            else
            {
                shouldBlock = rect.Contains(LegacyUI.MousePosition);
            }

            if (shouldBlock)
                cur.Use();
        }

        private static bool IsBlockableMouseEvent(Event cur)
        {
            if (cur == null)
                return false;

            return cur.type == EventType.MouseDown ||
                   cur.type == EventType.MouseUp ||
                   cur.type == EventType.MouseDrag ||
                   cur.type == EventType.ScrollWheel;
        }

        private Rect GetCloseButtonRect()
        {
            return new Rect(rect.x + rect.width - 31f, rect.y + 9f, 20f, 20f);
        }
    }
}
