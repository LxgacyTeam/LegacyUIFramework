using System;
using System.Globalization;
using UnityEngine;

namespace BigCityLegacy.UI
{
    /// <summary>
    /// Stateful, editable numeric stepper. Keep one instance across OnGUI calls.
    /// Text is deliberately separate from Value: typing never changes the numeric
    /// value until Enter, focus loss, or a click outside the text field.
    /// </summary>
    public sealed class LegacyUICounter
    {
        private static int nextId;
        private readonly string controlName;
        private string editText;
        private float value;
        private bool isEditing;

        public float Value
        {
            get { return value; }
            set
            {
                this.value = ClampValue(value);
                // Do not replace the text (or caret/selection) during an edit.
                if (!isEditing)
                    editText = FormatValue(this.value);
            }
        }

        public float Step { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public string NumberFormat { get; set; }
        public bool AllowTyping { get; set; }
        public LegacyUITheme Theme { get; set; }

        public LegacyUICounter(float initialValue, float step = 1f, float min = float.MinValue, float max = float.MaxValue)
        {
            nextId++;
            controlName = "LegacyUI.Counter." + nextId.ToString(CultureInfo.InvariantCulture);
            MinValue = min;
            MaxValue = max;
            Step = step;
            NumberFormat = "0.##";
            AllowTyping = true;
            value = ClampValue(initialValue);
            editText = FormatValue(value);
        }

        public float Draw(Rect rect)
        {
            using (LegacyUI.WithTheme(Theme))
            {
                float buttonWidth = Mathf.Min(rect.height, Mathf.Max(18f, rect.width * 0.25f));
                Rect minus = new Rect(rect.x, rect.y, buttonWidth, rect.height);
                Rect plus = new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height);
                Rect center = new Rect(minus.xMax + 3f, rect.y,
                                       Mathf.Max(0f, rect.width - buttonWidth * 2f - 6f), rect.height);

                Event e = Event.current;
                // Unity does not consistently clear keyboardControl when the user
                // clicks empty GUI space. Handle blur explicitly before TextField.
                if (isEditing && e != null)
                {
                    bool enter = e.type == EventType.KeyDown &&
                                 (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);
                    bool outsideClick = e.type == EventType.MouseDown && e.button == 0 &&
                                        !center.Contains(LegacyUI.MousePosition);
                    if (enter || outsideClick)
                    {
                        CommitEdit();
                        GUIUtility.keyboardControl = 0;
                        if (enter) e.Use();
                    }
                }

                // Draw the text input before the action buttons. Stable IMGUI
                // control ordering is important for its caret/selection state.
                if (AllowTyping)
                {
                    // One stable ID, one TextField call per GUI event. Avoid extra
                    // IMGUI controls before this field whose count can vary per event.
                    GUI.SetNextControlName(controlName);
                    string updated = LegacyUI.TextField(center, editText ?? FormatValue(value));
                    bool hasFocus = GUI.GetNameOfFocusedControl() == controlName;

                    if (hasFocus)
                    {
                        // Keep exactly what the text editor returns, including
                        // intermediate values such as '-', '1.', or invalid text.
                        isEditing = true;
                        editText = updated;
                    }
                    else if (isEditing)
                    {
                        // Focus can also move through Tab/another IMGUI control.
                        CommitEdit();
                    }
                    else
                    {
                        editText = FormatValue(value);
                    }
                }
                else
                {
                    if (isEditing)
                    {
                        CommitEdit();
                        GUIUtility.keyboardControl = 0;
                    }
                    LegacyUI.HintBox(center, FormatValue(value));
                }

                if (LegacyUI.Button(minus, "−")) Change(-1f);
                if (LegacyUI.Button(plus, "+")) Change(1f);
                LegacyUI.RegisterElementRect(rect);
                return value;
            }
        }

        private void CommitEdit()
        {
            float parsed;
            // Invariant output uses '.', but also accept ',' as a decimal
            // separator for users who type in locales that use comma.
            string normalized = (editText ?? string.Empty).Trim().Replace(',', '.');
            if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) &&
                !float.IsNaN(parsed) && !float.IsInfinity(parsed))
                value = ClampValue(parsed);
            // Invalid/empty text intentionally restores the last valid Value.
            isEditing = false;
            editText = FormatValue(value);
        }

        private void Change(float direction)
        {
            // Step buttons use the last confirmed value. Discard any unfinished
            // draft before stepping rather than partially parsing it.
            isEditing = false;
            GUIUtility.keyboardControl = 0;
            if (!float.IsNaN(Step) && !float.IsInfinity(Step) && Step > 0f)
            {
                double result = (double)value + (double)Step * direction;
                value = ClampValue((float)result);
            }
            editText = FormatValue(value);
        }

        private float ClampValue(float n)
        {
            if (float.IsNaN(n)) return value;
            float min = Mathf.Min(MinValue, MaxValue);
            float max = Mathf.Max(MinValue, MaxValue);
            return Mathf.Clamp(n, min, max);
        }

        private string FormatValue(float n)
        {
            return n.ToString(string.IsNullOrEmpty(NumberFormat) ? "0.##" : NumberFormat,
                              CultureInfo.InvariantCulture);
        }
    }
}
