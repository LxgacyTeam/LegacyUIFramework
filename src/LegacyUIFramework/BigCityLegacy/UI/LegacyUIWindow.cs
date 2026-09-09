using System;
using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Draggable IMGUI window. It does not use GUI.Window.
    /// </summary>
    public sealed class LegacyUIWindow
    {
        private Rect rect;
        private Vector2 dragOffset;
        private bool isDragging;
        private bool prefsLoaded;
        private readonly int inputBlockOwnerId = LegacyUIInputBlocker.AllocateOwnerId();

        public string Title { get; set; }
        public bool Visible { get; set; }
        public LegacyUIWindowOptions Options { get; private set; }
        public Rect Rect
        {
            get { return rect; }
            set { rect = value; }
        }

        public bool IsDragging
        {
            get { return isDragging; }
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
            Visible = true;
            Options = options ?? new LegacyUIWindowOptions();
        }

        public void Draw(Action<Rect> drawContent)
        {
            using (LegacyUI.WithTheme(Options != null ? Options.Theme : null))
            {
                DrawInternal(drawContent);
            }
        }

        private void DrawInternal(Action<Rect> drawContent)
        {
            LegacyUI.EnsureInitialized();
            if (!Visible)
            {
                LegacyUIInputBlocker.ClearWindow(inputBlockOwnerId);
                return;
            }

            LoadPrefsOnce();
            if (!Visible)
            {
                LegacyUIInputBlocker.ClearWindow(inputBlockOwnerId);
                return;
            }
            FixRect();
            HandleDragAndClose();
            if (!Visible)
            {
                LegacyUIInputBlocker.ClearWindow(inputBlockOwnerId);
                return;
            }

            FixRect();
            LegacyUIInputBlocker.ReportWindow(inputBlockOwnerId, rect, Options.InputBlockMode);

            Color oldColor = GUI.color;
            if (isDragging && Options.FadeWhileDragging)
            {
                Color c = GUI.color;
                c.a *= Mathf.Clamp01(Options.DragAlpha);
                GUI.color = c;
            }

            LegacyUI.Panel(rect, Options.BackgroundAlpha);
            if (DrawHeader())
            {
                GUI.color = oldColor;
                LegacyUIInputBlocker.ClearWindow(inputBlockOwnerId);
                return;
            }

            Rect content = new Rect(
                rect.x + 15f,
                rect.y + Options.HeaderHeight + 12f,
                rect.width - 30f,
                Mathf.Max(0f, rect.height - Options.HeaderHeight - 24f));

            if (drawContent != null)
                drawContent(content);

            GUI.color = oldColor;
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

            if (Options.ShowCloseButton && GUI.Button(GetCloseButtonRect(), "×", LegacyUI.Styles.CloseButton))
            {
                Close();
                return true;
            }

            LegacyUI.Separator(new Rect(rect.x + 12f, rect.y + Options.HeaderHeight, rect.width - 24f, 1f));
            return false;
        }

        private void HandleDragAndClose()
        {
            Event cur = Event.current;
            if (cur == null) return;

            if (!Options.Draggable) return;

            Rect titleArea = new Rect(rect.x, rect.y, rect.width, Options.HeaderHeight);
            if (Options.ShowCloseButton)
                titleArea.width -= 34f;

            if (cur.type == EventType.MouseDown && cur.button == 0 && titleArea.Contains(cur.mousePosition))
            {
                isDragging = true;
                dragOffset = cur.mousePosition - rect.position;
                cur.Use();
            }
            else if (cur.type == EventType.MouseDrag && isDragging)
            {
                rect.position = cur.mousePosition - dragOffset;
                FixRect();
                cur.Use();
            }
            else if (cur.type == EventType.MouseUp && isDragging)
            {
                isDragging = false;
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
                shouldBlock = new Rect(0f, 0f, Screen.width, Screen.height).Contains(cur.mousePosition);
            }
            else
            {
                shouldBlock = rect.Contains(cur.mousePosition);
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
