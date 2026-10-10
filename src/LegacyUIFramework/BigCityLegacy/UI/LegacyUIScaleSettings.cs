using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Persistent configuration for one plugin's GUI scopes. Keep one instance in your plugin;
    /// do not create settings or themes inside OnGUI. Rects remain in virtual design pixels.
    /// </summary>
    public sealed class LegacyUIScaleSettings
    {
        public LegacyUIScaleMode Mode { get; set; }
        public float BaseScale { get; set; }
        public float ManualScale { get; set; }
        public float ReferenceWidth { get; set; }
        public float ReferenceHeight { get; set; }
        public float MinScale { get; set; }
        public float MaxScale { get; set; }

        public LegacyUIScaleSettings()
        {
            Mode = LegacyUIScaleMode.Automatic;
            BaseScale = 1f;
            ManualScale = 1f;
            ReferenceWidth = 1920f;
            ReferenceHeight = 1080f;
            MinScale = 0.75f;
            MaxScale = 4f;
        }

        public float ResolveScale()
        {
            float value = Mode == LegacyUIScaleMode.Manual
                ? ManualScale
                : BaseScale * Mathf.Min(Screen.width / Mathf.Max(1f, ReferenceWidth),
                                          Screen.height / Mathf.Max(1f, ReferenceHeight));
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                value = 1f;
            float lower = Mathf.Max(0.1f, MinScale);
            float upper = Mathf.Max(lower, MaxScale);
            return Mathf.Clamp(value, lower, upper);
        }
    }
}
