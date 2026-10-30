using System;
using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>Durations in real seconds; zero disables an individual transition.</summary>
    public sealed class LegacyUIAnimationSettings
    {
        public bool Enabled = true;
        public float WindowDuration = 0.18f;
        public float DragDuration = 0.12f;
        public float DropdownDuration = 0.14f;
        public float ToggleDuration = 0.12f;
        public float HoverDuration = 0.12f;
    }

    public sealed class LegacyUIAnimationScope : IDisposable
    {
        private readonly LegacyUIAnimationSettings previous;
        private readonly int previousOwner;
        private bool disposed;

        internal LegacyUIAnimationScope(LegacyUIAnimationSettings settings, int? owner)
        {
            previous = LegacyUI.ActiveAnimations;
            previousOwner = LegacyUI.AnimationOwner;
            if (settings != null) LegacyUI.ActiveAnimations = settings;
            if (owner.HasValue) LegacyUI.AnimationOwner = owner.Value;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            LegacyUI.ActiveAnimations = previous;
            LegacyUI.AnimationOwner = previousOwner;
        }
    }

    // Sampling uses absolute real time, never deltaTime: OnGUI may run several
    // times per frame, and games may pause by setting timeScale to zero.
    internal sealed class LegacyUIFade
    {
        private float from, target, started;
        private bool initialized;

        internal void Reset(float value, float now)
        {
            from = target = value; started = now; initialized = true;
        }

        internal float Sample(float value, float duration, bool enabled, float now)
        {
            if (!initialized) Reset(value, now);
            if (!enabled || float.IsNaN(duration) || duration <= 0f || float.IsInfinity(duration))
            { Reset(value, now); return value; }
            float current = Evaluate(duration, now);
            if (value != target)
            { from = current; target = value; started = now; }
            return Evaluate(duration, now);
        }

        private float Evaluate(float duration, float now)
        {
            float t = Mathf.Clamp01((now - started) / duration);
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(from, target, t);
        }
    }
}
