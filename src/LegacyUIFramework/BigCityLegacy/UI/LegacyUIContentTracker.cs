using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Tracks the bounds of controls drawn inside an automatic LegacyUIScrollView.
    /// </summary>
    internal sealed class LegacyUIContentTracker
    {
        private bool hasBounds;
        private Rect bounds;

        public bool HasBounds
        {
            get { return hasBounds; }
        }

        public Rect Bounds
        {
            get { return bounds; }
        }

        public void Include(Rect rect)
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

            if (!hasBounds)
            {
                bounds = rect;
                hasBounds = true;
                return;
            }

            float xMin = Mathf.Min(bounds.xMin, rect.xMin);
            float yMin = Mathf.Min(bounds.yMin, rect.yMin);
            float xMax = Mathf.Max(bounds.xMax, rect.xMax);
            float yMax = Mathf.Max(bounds.yMax, rect.yMax);
            bounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
