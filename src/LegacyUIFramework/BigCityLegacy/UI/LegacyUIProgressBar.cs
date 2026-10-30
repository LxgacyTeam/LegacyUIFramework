using System;
using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>Keep an instance to bind a measured value; providers execute on the GUI thread.</summary>
    public sealed class LegacyUIProgressBar
    {
        public double Value;
        public double Min;
        public double Max = 1d;
        public Func<double> ValueProvider;
        public string Label;
        public bool ShowPercentage = true;
        public LegacyUITheme Theme;

        public void Draw(Rect rect)
        {
            double value = ValueProvider != null ? ValueProvider() : Value;
            string text = Label;
            if (text == null && ShowPercentage) text = (Normalize(value, Min, Max) * 100d).ToString("0") + "%";
            using (LegacyUI.WithTheme(Theme)) LegacyUI.ProgressBar(rect, value, Min, Max, text);
        }

        /// <summary>Supports ascending/descending ranges and 64-bit download sizes.</summary>
        public static double Normalize(double value, double min, double max)
        {
            if (double.IsNaN(value) || double.IsNaN(min) || double.IsNaN(max) ||
                double.IsInfinity(min) || double.IsInfinity(max) || min == max) return 0d;
            if (min < max) { if (value <= min) return 0d; if (value >= max) return 1d; }
            else { if (value >= min) return 0d; if (value <= max) return 1d; }
            double range = max - min;
            if (double.IsInfinity(range)) return (value * 0.5d - min * 0.5d) / (max * 0.5d - min * 0.5d);
            return Math.Max(0d, Math.Min(1d, (value - min) / range));
        }
    }

    /// <summary>Optional host integration. No camera/shader dependency in the base library.</summary>
    public interface ILegacyUIBackdrop
    {
        /// <summary>Draw into the current GUI rect; return false if no scene texture is available.</summary>
        bool Draw(Rect rect);
    }
}
