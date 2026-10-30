using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>Optional diagnostic sink; the base framework does not log by default.</summary>
    public interface ILegacyUIBackdropDiagnostics
    {
        void ReportWindow(LegacyUIBackdropDiagnosticInfo info);
    }

    public struct LegacyUIBackdropDiagnosticInfo
    {
        public string Title;
        public Rect Rect;
        public EventType EntryEvent;
        public EventType CurrentEvent;
        public EventType RawEvent;
        public bool Requested;
        public bool Drawn;
        public float BackgroundAlpha;
        public float TintMultiplier;
        public float Opacity;
    }
}
