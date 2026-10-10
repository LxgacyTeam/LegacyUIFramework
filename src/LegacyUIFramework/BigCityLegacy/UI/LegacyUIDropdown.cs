using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Stateful dropdown with a fixed-width popup and a bounded-height styled scrollview.
    /// Call Draw() in normal control order, and DrawPopup() LAST in an unclipped GUI scope.
    /// For overlapping controls, call HandlePopupInput() BEFORE the window's other controls.
    /// Keep this instance across OnGUI events (never instantiate inside OnGUI).
    /// </summary>
    public sealed class LegacyUIDropdown
    {
        private readonly LegacyUIScrollView list;
        private Rect anchorRect;
        private bool anchorKnown;
        private bool scrollbarDragging;
        private Matrix4x4 anchorMatrix;
        private float anchorScale = 1f;
        private readonly int inputBlockId = LegacyUIInputBlocker.AllocateOwnerId();
        private int selectedIndex;

        public IList<string> Items { get; set; }
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; }
        }
        public bool IsOpen { get; set; }
        /// <summary>Popup width in design pixels; <= 0 uses anchor width.</summary>
        public float PopupWidth { get; set; }
        public float MaxHeight { get; set; }
        public float ItemHeight { get; set; }
        public float Gap { get; set; }
        public LegacyUITheme Theme { get; set; }
        public event Action<int> SelectionChanged;

        public LegacyUIDropdown(IList<string> items, int selectedIndex = 0)
        {
            Items = items ?? new string[0];
            this.selectedIndex = selectedIndex;
            PopupWidth = 0f;
            MaxHeight = 180f;
            ItemHeight = 27f;
            Gap = 3f;
            list = new LegacyUIScrollView(new Rect(0, 0, 1, 1), new Rect(0, 0, 1, 1));
            list.Padding = 0f;
            list.ShowHorizontalScrollbar = false;
            list.ShowVerticalScrollbar = false;
            list.DrawBackground = true;
        }

        public int Draw(Rect rect)
        {
            anchorRect = rect;
            anchorKnown = true;
            anchorMatrix = GUI.matrix;
            anchorScale = LegacyUI.ActiveScale;
            using (LegacyUI.WithTheme(Theme))
            {
                int count = Items == null ? 0 : Items.Count;
                string text = selectedIndex >= 0 && selectedIndex < count ? Items[selectedIndex] : "Select...";
                if (LegacyUI.Button(rect, text + (IsOpen ? "  ▲" : "  ▼")))
                {
                    IsOpen = !IsOpen;
                    if (IsOpen) list.ResetScroll();
                }
            }
            return selectedIndex;
        }

        /// <summary>
        /// Optional early input handling. Call inside LegacyUIGuiScope before the window
        /// is drawn, to prevent click-through. Uses the cached transform of the anchor.
        /// </summary>
        public void HandlePopupInput()
        {
            if (!IsOpen || !anchorKnown || Items == null) return;
            Event e = Event.current;
            if (e == null) return;
            Matrix4x4 beforeMatrix = GUI.matrix;
            float beforeScale = LegacyUI.ActiveScale;
            GUI.matrix = anchorMatrix;
            LegacyUI.ActiveScale = anchorScale;
            try
            {
            Rect popup = GetPopupRect();
            Vector2 mouse = LegacyUI.MousePosition;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (!popup.Contains(mouse) && !anchorRect.Contains(mouse))
                {
                    scrollbarDragging = false;
                    IsOpen = false;
                    return; // Do not swallow an intentional click outside the popup.
                }
                if (!popup.Contains(mouse)) return;
                float localY = mouse.y - popup.y + list.ScrollPosition.y;
                int index = Mathf.FloorToInt(localY / Mathf.Max(1f, ItemHeight));
                float scrollbarWidth = list.ShowVerticalScrollbar ? list.ScrollbarSize + list.ScrollbarSpacing : 0f;
                if (mouse.x >= popup.x + popup.width - scrollbarWidth && scrollbarWidth > 0f)
                {
                    scrollbarDragging = true;
                    ScrollToPointer(popup, mouse.y);
                    e.Use();
                    return;
                }
                if (index >= 0 && index < Items.Count)
                {
                    SetSelectedIndex(index);
                    IsOpen = false;
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseDrag && scrollbarDragging)
            {
                ScrollToPointer(popup, mouse.y);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && scrollbarDragging)
            {
                scrollbarDragging = false;
                e.Use();
            }
            else if (e.type == EventType.ScrollWheel && popup.Contains(mouse))
            {
                float maxScroll = Mathf.Max(0f, Items.Count * ItemHeight - popup.height);
                Vector2 scroll = list.ScrollPosition;
                scroll.y = Mathf.Clamp(scroll.y + e.delta.y * ItemHeight, 0f, maxScroll);
                list.ScrollPosition = scroll;
                e.Use();
            }
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                IsOpen = false;
                e.Use();
            }
            }
            finally
            {
                GUI.matrix = beforeMatrix;
                LegacyUI.ActiveScale = beforeScale;
            }
        }

        /// <summary>Draw after other controls; do not call inside GUI.BeginScrollView.</summary>
        public void DrawPopup()
        {
            if (!IsOpen || !anchorKnown || Items == null || Items.Count == 0)
            {
                LegacyUIInputBlocker.ClearWindow(inputBlockId);
                return;
            }
            Matrix4x4 beforeMatrix = GUI.matrix;
            float beforeScale = LegacyUI.ActiveScale;
            GUI.matrix = anchorMatrix;
            LegacyUI.ActiveScale = anchorScale;
            try
            {
            using (LegacyUI.WithTheme(Theme))
            {
                Rect popup = GetPopupRect();
                // Block the game's polling-based mouse input under the popup as well.
                LegacyUIInputBlocker.ReportWindow(inputBlockId,
                    LegacyUI.ToPhysicalRect(popup), LegacyUIInputBlockMode.Window);
                float fullHeight = Items.Count * Mathf.Max(1f, ItemHeight);
                list.ViewRect = popup;
                list.ContentRect = new Rect(0f, 0f, popup.width, fullHeight);
                list.ShowVerticalScrollbar = fullHeight > popup.height + 0.5f;
                list.Draw(delegate(Rect area)
                {
                    // Draw elements in the same control order for Layout/Repaint/Mouse events.
                    float width = Mathf.Max(0f, area.width);
                    for (int i = 0; i < Items.Count; i++)
                    {
                        if (LegacyUI.TabButton(new Rect(area.x, area.y + i * ItemHeight, width, ItemHeight),
                                               Items[i] ?? string.Empty, i == selectedIndex))
                        {
                            SetSelectedIndex(i);
                            IsOpen = false;
                        }
                    }
                });
            }
            }
            finally
            {
                GUI.matrix = beforeMatrix;
                LegacyUI.ActiveScale = beforeScale;
            }
        }

        private void ScrollToPointer(Rect popup, float mouseY)
        {
            if (Items == null) return;
            float total = Items.Count * Mathf.Max(1f, ItemHeight);
            float maxScroll = Mathf.Max(0f, total - popup.height);
            float fraction = Mathf.Clamp01((mouseY - popup.y) / Mathf.Max(1f, popup.height));
            Vector2 scroll = list.ScrollPosition;
            scroll.y = fraction * maxScroll;
            list.ScrollPosition = scroll;
        }

        private Rect GetPopupRect()
        {
            float width = PopupWidth > 0f ? PopupWidth : anchorRect.width;
            float height = Mathf.Min(Mathf.Max(1f, MaxHeight), Math.Max(1, Items == null ? 0 : Items.Count) * Mathf.Max(1f, ItemHeight));
            Vector2 screen = LegacyUI.VirtualScreenSize;
            width = Mathf.Min(width, screen.x);
            height = Mathf.Min(height, screen.y);
            float x = Mathf.Clamp(anchorRect.x, 0f, Mathf.Max(0f, screen.x - width));
            float y = anchorRect.yMax + Gap;
            if (y + height > screen.y && anchorRect.y - Gap >= height)
                y = anchorRect.y - Gap - height;
            else
                y = Mathf.Clamp(y, 0f, Mathf.Max(0f, screen.y - height));
            return new Rect(x, y, width, height);
        }

        private void SetSelectedIndex(int index)
        {
            if (index == selectedIndex) return;
            selectedIndex = index;
            if (SelectionChanged != null) SelectionChanged(index);
        }
    }
}
