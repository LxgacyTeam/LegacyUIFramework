using System;
using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Saves Unity IMGUI global state and restores it at the end of the frame section.
    /// Use this before drawing mod UI to avoid inheriting game GUI state.
    /// </summary>
    public sealed class LegacyUIGuiScope : IDisposable
    {
        private readonly Matrix4x4 matrix;
        private readonly Color color;
        private readonly Color backgroundColor;
        private readonly bool enabled;
        private readonly int depth;
        private bool disposed;

        public LegacyUIGuiScope(int depth = -10000)
        {
            matrix = GUI.matrix;
            color = GUI.color;
            backgroundColor = GUI.backgroundColor;
            enabled = GUI.enabled;
            this.depth = GUI.depth;

            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
            GUI.depth = depth;

            LegacyUI.EnsureInitialized();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            GUI.depth = depth;
            GUI.enabled = enabled;
            GUI.backgroundColor = backgroundColor;
            GUI.color = color;
            GUI.matrix = matrix;
        }
    }
}
