using UnityEngine;

namespace BigCityLegacy.UI
{
    internal static class LegacyUIValueControls
    {
        private static int activeControlId;
        private static float dragOffset;

        public static float HorizontalSlider(Rect rect, float value, float min, float max)
        {
            LegacyUI.EnsureInitialized();
            if (max < min) Swap(ref min, ref max);
            value = Mathf.Clamp(value, min, max);

            int id = GUIUtility.GetControlID("BCL_HSlider".GetHashCode(), FocusType.Passive, rect);
            float thumbWidth = Mathf.Max(8f, LegacyUI.Styles.HorizontalSliderThumb.fixedWidth);
            float thumbHeight = Mathf.Max(8f, LegacyUI.Styles.HorizontalSliderThumb.fixedHeight);
            float usable = Mathf.Max(1f, rect.width - thumbWidth);
            float normalized = Mathf.Approximately(max, min) ? 0f : Mathf.InverseLerp(min, max, value);
            Rect track = new Rect(rect.x, rect.y + (rect.height - 4f) * 0.5f, rect.width, 4f);
            Rect thumb = new Rect(rect.x + usable * normalized, rect.y + (rect.height - thumbHeight) * 0.5f, thumbWidth, thumbHeight);

            Event ev = Event.current;
            if (ev != null)
            {
                switch (ev.GetTypeForControl(id))
                {
                    case EventType.MouseDown:
                        if (rect.Contains(ev.mousePosition) && ev.button == 0)
                        {
                            GUIUtility.hotControl = id;
                            activeControlId = id;
                            dragOffset = thumb.Contains(ev.mousePosition) ? ev.mousePosition.x - thumb.x : thumbWidth * 0.5f;
                            value = HorizontalValueFromMouse(rect, ev.mousePosition.x, dragOffset, thumbWidth, min, max);
                            ev.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == id && activeControlId == id)
                        {
                            value = HorizontalValueFromMouse(rect, ev.mousePosition.x, dragOffset, thumbWidth, min, max);
                            ev.Use();
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == id && activeControlId == id)
                        {
                            GUIUtility.hotControl = 0;
                            activeControlId = 0;
                            ev.Use();
                        }
                        break;
                }
            }

            if (Event.current == null || Event.current.type == EventType.Repaint)
            {
                GUI.Box(track, GUIContent.none, LegacyUI.Styles.HorizontalSlider);
                GUI.Box(thumb, GUIContent.none, LegacyUI.Styles.HorizontalSliderThumb);
            }

            return Mathf.Clamp(value, min, max);
        }

        public static float VerticalSlider(Rect rect, float value, float min, float max)
        {
            LegacyUI.EnsureInitialized();
            if (max < min) Swap(ref min, ref max);
            value = Mathf.Clamp(value, min, max);

            int id = GUIUtility.GetControlID("BCL_VSlider".GetHashCode(), FocusType.Passive, rect);
            float thumbWidth = Mathf.Max(8f, LegacyUI.Styles.VerticalSliderThumb.fixedWidth);
            float thumbHeight = Mathf.Max(8f, LegacyUI.Styles.VerticalSliderThumb.fixedHeight);
            float usable = Mathf.Max(1f, rect.height - thumbHeight);
            float normalized = Mathf.Approximately(max, min) ? 0f : Mathf.InverseLerp(min, max, value);
            Rect track = new Rect(rect.x + (rect.width - 4f) * 0.5f, rect.y, 4f, rect.height);
            Rect thumb = new Rect(rect.x + (rect.width - thumbWidth) * 0.5f, rect.y + usable * normalized, thumbWidth, thumbHeight);

            Event ev = Event.current;
            if (ev != null)
            {
                switch (ev.GetTypeForControl(id))
                {
                    case EventType.MouseDown:
                        if (rect.Contains(ev.mousePosition) && ev.button == 0)
                        {
                            GUIUtility.hotControl = id;
                            activeControlId = id;
                            dragOffset = thumb.Contains(ev.mousePosition) ? ev.mousePosition.y - thumb.y : thumbHeight * 0.5f;
                            value = VerticalValueFromMouse(rect, ev.mousePosition.y, dragOffset, thumbHeight, min, max);
                            ev.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == id && activeControlId == id)
                        {
                            value = VerticalValueFromMouse(rect, ev.mousePosition.y, dragOffset, thumbHeight, min, max);
                            ev.Use();
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == id && activeControlId == id)
                        {
                            GUIUtility.hotControl = 0;
                            activeControlId = 0;
                            ev.Use();
                        }
                        break;
                }
            }

            if (Event.current == null || Event.current.type == EventType.Repaint)
            {
                GUI.Box(track, GUIContent.none, LegacyUI.Styles.VerticalSlider);
                GUI.Box(thumb, GUIContent.none, LegacyUI.Styles.VerticalSliderThumb);
            }

            return Mathf.Clamp(value, min, max);
        }

        public static float HorizontalScrollbar(Rect rect, float value, float visibleSize, float leftValue, float rightValue)
        {
            LegacyUI.EnsureInitialized();
            if (rightValue < leftValue) Swap(ref leftValue, ref rightValue);
            visibleSize = Mathf.Max(0f, visibleSize);
            value = Mathf.Clamp(value, leftValue, rightValue);

            int id = GUIUtility.GetControlID("BCL_HScrollbar".GetHashCode(), FocusType.Passive, rect);
            float range = Mathf.Max(0f, rightValue - leftValue);
            float total = Mathf.Max(1f, range + visibleSize);
            float thumbWidth = range <= 0f ? rect.width : Mathf.Clamp(rect.width * (visibleSize / total), 18f, rect.width);
            float usable = Mathf.Max(1f, rect.width - thumbWidth);
            float normalized = range <= 0f ? 0f : Mathf.InverseLerp(leftValue, rightValue, value);
            Rect thumb = new Rect(rect.x + usable * normalized, rect.y, thumbWidth, rect.height);

            Event ev = Event.current;
            if (ev != null)
            {
                switch (ev.GetTypeForControl(id))
                {
                    case EventType.MouseDown:
                        if (rect.Contains(ev.mousePosition) && ev.button == 0)
                        {
                            GUIUtility.hotControl = id;
                            activeControlId = id;
                            dragOffset = thumb.Contains(ev.mousePosition) ? ev.mousePosition.x - thumb.x : thumbWidth * 0.5f;
                            value = HorizontalValueFromMouse(rect, ev.mousePosition.x, dragOffset, thumbWidth, leftValue, rightValue);
                            ev.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == id && activeControlId == id)
                        {
                            value = HorizontalValueFromMouse(rect, ev.mousePosition.x, dragOffset, thumbWidth, leftValue, rightValue);
                            ev.Use();
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == id && activeControlId == id)
                        {
                            GUIUtility.hotControl = 0;
                            activeControlId = 0;
                            ev.Use();
                        }
                        break;
                }
            }

            if (Event.current == null || Event.current.type == EventType.Repaint)
            {
                GUI.Box(rect, GUIContent.none, LegacyUI.Styles.HorizontalScrollbar);
                GUI.Box(thumb, GUIContent.none, LegacyUI.Styles.HorizontalScrollbarThumb);
                LegacyUI.DrawBorder(rect);
            }

            return Mathf.Clamp(value, leftValue, rightValue);
        }

        public static float VerticalScrollbar(Rect rect, float value, float visibleSize, float topValue, float bottomValue)
        {
            LegacyUI.EnsureInitialized();
            if (bottomValue < topValue) Swap(ref topValue, ref bottomValue);
            visibleSize = Mathf.Max(0f, visibleSize);
            value = Mathf.Clamp(value, topValue, bottomValue);

            int id = GUIUtility.GetControlID("BCL_VScrollbar".GetHashCode(), FocusType.Passive, rect);
            float range = Mathf.Max(0f, bottomValue - topValue);
            float total = Mathf.Max(1f, range + visibleSize);
            float thumbHeight = range <= 0f ? rect.height : Mathf.Clamp(rect.height * (visibleSize / total), 18f, rect.height);
            float usable = Mathf.Max(1f, rect.height - thumbHeight);
            float normalized = range <= 0f ? 0f : Mathf.InverseLerp(topValue, bottomValue, value);
            Rect thumb = new Rect(rect.x, rect.y + usable * normalized, rect.width, thumbHeight);

            Event ev = Event.current;
            if (ev != null)
            {
                switch (ev.GetTypeForControl(id))
                {
                    case EventType.MouseDown:
                        if (rect.Contains(ev.mousePosition) && ev.button == 0)
                        {
                            GUIUtility.hotControl = id;
                            activeControlId = id;
                            dragOffset = thumb.Contains(ev.mousePosition) ? ev.mousePosition.y - thumb.y : thumbHeight * 0.5f;
                            value = VerticalValueFromMouse(rect, ev.mousePosition.y, dragOffset, thumbHeight, topValue, bottomValue);
                            ev.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == id && activeControlId == id)
                        {
                            value = VerticalValueFromMouse(rect, ev.mousePosition.y, dragOffset, thumbHeight, topValue, bottomValue);
                            ev.Use();
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == id && activeControlId == id)
                        {
                            GUIUtility.hotControl = 0;
                            activeControlId = 0;
                            ev.Use();
                        }
                        break;
                }
            }

            if (Event.current == null || Event.current.type == EventType.Repaint)
            {
                GUI.Box(rect, GUIContent.none, LegacyUI.Styles.VerticalScrollbar);
                GUI.Box(thumb, GUIContent.none, LegacyUI.Styles.VerticalScrollbarThumb);
                LegacyUI.DrawBorder(rect);
            }

            return Mathf.Clamp(value, topValue, bottomValue);
        }

        private static float HorizontalValueFromMouse(Rect rect, float mouseX, float offset, float thumbWidth, float min, float max)
        {
            float usable = Mathf.Max(1f, rect.width - thumbWidth);
            float normalized = Mathf.Clamp01((mouseX - offset - rect.x) / usable);
            return Mathf.Lerp(min, max, normalized);
        }

        private static float VerticalValueFromMouse(Rect rect, float mouseY, float offset, float thumbHeight, float min, float max)
        {
            float usable = Mathf.Max(1f, rect.height - thumbHeight);
            float normalized = Mathf.Clamp01((mouseY - offset - rect.y) / usable);
            return Mathf.Lerp(min, max, normalized);
        }

        private static void Swap(ref float a, ref float b)
        {
            float tmp = a;
            a = b;
            b = tmp;
        }
    }
}
