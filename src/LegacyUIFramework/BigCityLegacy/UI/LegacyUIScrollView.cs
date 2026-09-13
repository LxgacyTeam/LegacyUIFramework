using System;
using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// A styled IMGUI scrollable area with a viewport rectangle and a virtual content canvas.
    /// The content rectangle can be supplied manually or calculated from LegacyUI controls drawn inside it.
    /// </summary>
    public sealed class LegacyUIScrollView
    {
        private Rect contentRect;
        private bool autoContentSize;
        private LegacyUIContentTracker activeTracker;

        public Rect ViewRect { get; set; }

        public Rect ContentRect
        {
            get { return contentRect; }
            set
            {
                contentRect = NormalizeRect(value);
                AutoContentSize = false;
                ClampScrollPosition(GetClientRect());
            }
        }

        public bool AutoContentSize
        {
            get { return autoContentSize; }
            set
            {
                autoContentSize = value;
                if (autoContentSize)
                {
                    contentRect.x = 0f;
                    contentRect.y = 0f;
                }
            }
        }

        public float Padding { get; set; }
        public Vector2 ScrollPosition { get; set; }
        public bool ShowHorizontalScrollbar { get; set; }
        public bool ShowVerticalScrollbar { get; set; }
        public float ScrollbarSize { get; set; }
        public float ScrollbarSpacing { get; set; }
        public bool DrawBackground { get; set; }
        public float? BackgroundAlpha { get; set; }
        public LegacyUITheme Theme { get; set; }

        public LegacyUIScrollView(Rect viewRect)
        {
            ViewRect = NormalizeRect(viewRect);
            AutoContentSize = true;
            Padding = 8f;
            ShowHorizontalScrollbar = true;
            ShowVerticalScrollbar = true;
            ScrollbarSize = 14f;
            ScrollbarSpacing = 4f;
            DrawBackground = true;
            contentRect = new Rect(0f, 0f, 0f, 0f);
        }

        public LegacyUIScrollView(Rect viewRect, Rect contentRect)
            : this(viewRect)
        {
            this.contentRect = NormalizeRect(contentRect);
            AutoContentSize = false;
        }

        public void UseAutoContentSize()
        {
            AutoContentSize = true;
        }

        public void ResetScroll()
        {
            Rect clientRect = GetClientRect();
            float minX;
            float maxX;
            float minY;
            float maxY;
            GetScrollRange(clientRect, out minX, out maxX, out minY, out maxY);
            ScrollPosition = new Vector2(minX, minY);
        }

        public void IncludeContentRect(Rect rect)
        {
            if (!AutoContentSize)
                return;

            if (activeTracker != null)
                activeTracker.Include(rect);
        }

        public void Draw(Action<Rect> drawContent)
        {
            using (LegacyUI.WithTheme(Theme))
            {
                DrawInternal(drawContent);
            }
        }

        private void DrawInternal(Action<Rect> drawContent)
        {
            LegacyUI.EnsureInitialized();

            ViewRect = NormalizeRect(ViewRect);
            LegacyUI.RegisterElementRect(ViewRect);

            if (DrawBackground)
                LegacyUI.Panel(ViewRect, BackgroundAlpha);

            Rect clientRect = GetClientRect();
            if (clientRect.width <= 0f || clientRect.height <= 0f)
                return;

            EnsureContentRect(clientRect);
            ClampScrollPosition(clientRect);

            LegacyUIContentTracker currentTracker = AutoContentSize ? new LegacyUIContentTracker() : null;
            if (currentTracker != null)
                LegacyUI.PushContentTracker(currentTracker);

            try
            {
                activeTracker = currentTracker;
                ScrollPosition = GUI.BeginScrollView(
                    clientRect,
                    ScrollPosition,
                    contentRect,
                    false,
                    false,
                    GUIStyle.none,
                    GUIStyle.none);

                try
                {
                    if (drawContent != null)
                        drawContent(GetContentArea(clientRect));
                }
                finally
                {
                    GUI.EndScrollView();
                }
            }
            finally
            {
                activeTracker = null;
                if (currentTracker != null)
                    LegacyUI.PopContentTracker(currentTracker);
            }

            if (AutoContentSize)
            {
                UpdateAutomaticContentRect(clientRect, currentTracker);
                ClampScrollPosition(clientRect);
            }

            DrawScrollbars(clientRect);
        }

        private Rect GetClientRect()
        {
            float width = Mathf.Max(0f, ViewRect.width);
            float height = Mathf.Max(0f, ViewRect.height);
            float size = Mathf.Max(0f, ScrollbarSize);
            float spacing = Mathf.Max(0f, ScrollbarSpacing);

            if (ShowVerticalScrollbar)
                width = Mathf.Max(0f, width - size - spacing);
            if (ShowHorizontalScrollbar)
                height = Mathf.Max(0f, height - size - spacing);

            return new Rect(ViewRect.x, ViewRect.y, width, height);
        }

        private Rect GetContentArea(Rect clientRect)
        {
            float padding = Mathf.Max(0f, Padding);
            if (AutoContentSize)
            {
                return new Rect(
                    padding,
                    padding,
                    Mathf.Max(0f, clientRect.width - padding * 2f),
                    Mathf.Max(0f, clientRect.height - padding * 2f));
            }

            return new Rect(
                contentRect.x + padding,
                contentRect.y + padding,
                Mathf.Max(0f, contentRect.width - padding * 2f),
                Mathf.Max(0f, contentRect.height - padding * 2f));
        }

        private void EnsureContentRect(Rect clientRect)
        {
            if (AutoContentSize)
            {
                if (contentRect.width <= 0f || contentRect.height <= 0f)
                {
                    contentRect = new Rect(0f, 0f, clientRect.width, clientRect.height);
                }
                else
                {
                    contentRect.width = Mathf.Max(clientRect.width, contentRect.width);
                    contentRect.height = Mathf.Max(clientRect.height, contentRect.height);
                }
            }
            else
            {
                contentRect = NormalizeRect(contentRect);
            }
        }

        private void UpdateAutomaticContentRect(Rect clientRect, LegacyUIContentTracker currentTracker)
        {
            float padding = Mathf.Max(0f, Padding);
            float xMin = 0f;
            float yMin = 0f;
            float xMax = clientRect.width;
            float yMax = clientRect.height;

            if (currentTracker != null && currentTracker.HasBounds)
            {
                Rect bounds = currentTracker.Bounds;
                xMin = Mathf.Min(0f, bounds.xMin - padding);
                yMin = Mathf.Min(0f, bounds.yMin - padding);
                xMax = Mathf.Max(xMax, bounds.xMax + padding);
                yMax = Mathf.Max(yMax, bounds.yMax + padding);
            }

            contentRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void DrawScrollbars(Rect clientRect)
        {
            float size = Mathf.Max(0f, ScrollbarSize);
            if (size <= 0f)
                return;

            float minX;
            float maxX;
            float minY;
            float maxY;
            GetScrollRange(clientRect, out minX, out maxX, out minY, out maxY);

            if (ShowHorizontalScrollbar)
            {
                Rect horizontal = new Rect(
                    ViewRect.x,
                    ViewRect.yMax - size,
                    clientRect.width,
                    size);

                ScrollPosition = new Vector2(
                    LegacyUI.HorizontalScrollbar(horizontal, ScrollPosition.x, clientRect.width, minX, maxX),
                    ScrollPosition.y);
            }

            if (ShowVerticalScrollbar)
            {
                Rect vertical = new Rect(
                    ViewRect.xMax - size,
                    ViewRect.y,
                    size,
                    clientRect.height);

                ScrollPosition = new Vector2(
                    ScrollPosition.x,
                    LegacyUI.VerticalScrollbar(vertical, ScrollPosition.y, clientRect.height, minY, maxY));
            }
        }

        private void ClampScrollPosition(Rect clientRect)
        {
            float minX;
            float maxX;
            float minY;
            float maxY;
            GetScrollRange(clientRect, out minX, out maxX, out minY, out maxY);
            ScrollPosition = new Vector2(
                Mathf.Clamp(ScrollPosition.x, minX, maxX),
                Mathf.Clamp(ScrollPosition.y, minY, maxY));
        }

        private void GetScrollRange(Rect clientRect, out float minX, out float maxX, out float minY, out float maxY)
        {
            float clientWidth = Mathf.Max(0f, clientRect.width);
            float clientHeight = Mathf.Max(0f, clientRect.height);

            minX = Mathf.Min(0f, contentRect.xMin);
            minY = Mathf.Min(0f, contentRect.yMin);
            maxX = Mathf.Max(minX, contentRect.xMax - clientWidth);
            maxY = Mathf.Max(minY, contentRect.yMax - clientHeight);
        }

        private static Rect NormalizeRect(Rect rect)
        {
            if (rect.width < 0f)
            {
                rect.x += rect.width;
                rect.width = -rect.width;
            }

            if (rect.height < 0f)
            {
                rect.y += rect.height;
                rect.height = -rect.height;
            }

            return rect;
        }
    }
}
