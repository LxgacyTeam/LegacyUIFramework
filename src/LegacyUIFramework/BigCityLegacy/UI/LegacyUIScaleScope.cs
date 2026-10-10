using System;
using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Temporary relative scale for a control or group. Dispose within the same OnGUI event.
    /// Coordinates supplied by the caller are still virtual coordinates.
    /// </summary>
    public sealed class LegacyUIScaleScope : IDisposable
    {
        private readonly Matrix4x4 previousMatrix;
        private readonly float previousScale;
        private bool disposed;

        internal LegacyUIScaleScope(float factor, Vector2 pivot)
        {
            previousMatrix = GUI.matrix;
            previousScale = LegacyUI.ActiveScale;
            if (float.IsNaN(factor) || float.IsInfinity(factor) || factor <= 0f)
                factor = 1f;
            LegacyUI.ActiveScale = previousScale * factor;
            GUI.matrix = previousMatrix * Matrix4x4.Translate(new Vector3(pivot.x, pivot.y, 0f))
                      * Matrix4x4.Scale(new Vector3(factor, factor, 1f))
                      * Matrix4x4.Translate(new Vector3(-pivot.x, -pivot.y, 0f));
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            GUI.matrix = previousMatrix;
            LegacyUI.ActiveScale = previousScale;
        }
    }
}
