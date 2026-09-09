using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Tiny manual layout helper for fixed IMGUI windows.
    /// It keeps code readable without depending on GUILayout.
    /// </summary>
    public struct LegacyUILayout
    {
        public Rect Area;
        public float X;
        public float Y;
        public float Width;
        public float Spacing;

        public LegacyUILayout(Rect area, float spacing = 6f)
        {
            Area = area;
            X = area.x;
            Y = area.y;
            Width = area.width;
            Spacing = spacing;
        }

        public Rect Row(float height)
        {
            Rect r = new Rect(X, Y, Width, height);
            Y += height + Spacing;
            return r;
        }

        public Rect Row(float width, float height)
        {
            Rect r = new Rect(X, Y, width, height);
            Y += height + Spacing;
            return r;
        }

        public Rect Take(float width, float height)
        {
            Rect r = new Rect(X, Y, width, height);
            X += width + Spacing;
            return r;
        }

        public void NewLine(float height = 0f)
        {
            X = Area.x;
            Y += height + Spacing;
        }

        public void Space(float pixels)
        {
            Y += pixels;
        }

        public Rect SplitLeft(Rect row, float width)
        {
            return new Rect(row.x, row.y, width, row.height);
        }

        public Rect SplitRight(Rect row, float leftWidth, float gap = 8f)
        {
            return new Rect(row.x + leftWidth + gap, row.y, Mathf.Max(0f, row.width - leftWidth - gap), row.height);
        }
    }
}
