using System;
using System.IO;
using BepInEx;
using BigCityLegacy.UI;
using UnityEngine;

namespace BigCityLegacy.UI.Example
{
    [BepInPlugin("bigcitylegacy.legacyuiframework.example", "BigCityLegacy LegacyUIFramework Example", "1.0.0")]
    public sealed class LegacyUIFrameworkExamplePlugin : BaseUnityPlugin
    {
        private LegacyUIWindow window;
        private int tab;
        private bool toggleValue = true;
        private string textValue = "127.0.0.1";
        private string areaValue = "This is a styled text area.\nUse F9 to show/hide this example.";
        private float horizontalSliderValue = 0.35f;
        private float verticalSliderValue = 0.65f;
        private float horizontalScrollbarValue = 25f;
        private float verticalScrollbarValue = 45f;
        private string[] dataPathFiles = Array.Empty<string>();
        private float nextFileListRefresh;
        private LegacyUIScrollView scrollView;
        private string status = "Ready";
        private LegacyUITheme greenTheme;
        private LegacyUITheme whiteTheme;
        private LegacyUITheme amberTheme;

        private void Awake()
        {
            greenTheme = CreateGreenTheme();
            whiteTheme = CreateWhiteTheme();
            amberTheme = CreateAmberTheme();

            window = new LegacyUIWindow(
                "Legacy UI Framework Example",
                new Rect(120f, 90f, 455f, 500f),
                new LegacyUIWindowOptions
                {
                    ShowCloseButton = true,
                    RightHint = "[F9]",
                    PlayerPrefsKey = "LegacyUIFramework.Example.Window",
                    FadeWhileDragging = true,
                    DragAlpha = 0.62f,
                    InputBlockMode = LegacyUIInputBlockMode.Window
                });

            scrollView = new LegacyUIScrollView(new Rect(0f, 0f, 0f, 0f))
            {
                Padding = 8f,
                ShowHorizontalScrollbar = false,
                ShowVerticalScrollbar = true
            };

            RefreshDataPathFiles();
            nextFileListRefresh = Time.unscaledTime + 1f;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9) && window != null)
            {
                window.Visible = !window.Visible;
                window.SavePrefs();
            }

            if (window != null && window.Visible && tab == 1 && Time.unscaledTime >= nextFileListRefresh)
            {
                nextFileListRefresh = Time.unscaledTime + 1f;
                RefreshDataPathFiles();
            }
        }

        private void OnGUI()
        {
            if (window == null || !window.Visible) return;

            using (new LegacyUIGuiScope(-10000))
            {
                window.Draw(DrawWindowContent);
            }
        }

        private void DrawWindowContent(Rect content)
        {
            LegacyUILayout ui = new LegacyUILayout(content, 7f);

            Rect tabs = ui.Row(25f);
            float tabWidth = tabs.width / 5f;
            if (LegacyUI.TabButton(new Rect(tabs.x, tabs.y, tabWidth, tabs.height), "Controls", tab == 0)) tab = 0;
            if (LegacyUI.TabButton(new Rect(tabs.x + tabWidth, tabs.y, tabWidth, tabs.height), "Values", tab == 1)) tab = 1;
            if (LegacyUI.TabButton(new Rect(tabs.x + tabWidth * 2f, tabs.y, tabWidth + 1f, tabs.height), "ScrollView", tab == 2)) tab = 2;
            if (LegacyUI.TabButton(new Rect(tabs.x + tabWidth * 3f, tabs.y, tabWidth, tabs.height), "Text", tab == 3)) tab = 3;
            if (LegacyUI.TabButton(new Rect(tabs.x + tabWidth * 4f, tabs.y, tabWidth, tabs.height), "Themes", tab == 4)) tab = 4;

            ui.Space(4f);

            if (tab == 0)
                DrawControlsTab(ref ui);
            else if (tab == 1)
                DrawValuesTab(ref ui);
            else if (tab == 2)
                DrawScrollViewTab(ref ui);
            else if (tab == 3)
                DrawTextTab(ref ui);
            else
                DrawThemesTab(ref ui);

            Rect statusRect = new Rect(content.x, content.y + content.height - 24f, content.width, 20f);
            LegacyUI.Status(statusRect, status);
        }

        private void DrawControlsTab(ref LegacyUILayout ui)
        {
            Rect row = ui.Row(24f);
            LegacyUI.Label(new Rect(row.x, row.y, 90f, row.height), "Text Field");
            textValue = LegacyUI.TextField(new Rect(row.x + 95f, row.y, row.width - 95f, row.height), textValue);

            toggleValue = LegacyUI.Toggle(ui.Row(22f), toggleValue, "Styled checkbox / toggle");

            Rect buttons = ui.Row(27f);
            float btnW = (buttons.width - 14f) / 3f;
            if (LegacyUI.Button(new Rect(buttons.x, buttons.y, btnW, buttons.height), "Default")) status = "Default button clicked";
            if (LegacyUI.GreenButton(new Rect(buttons.x + btnW + 7f, buttons.y, btnW, buttons.height), "Primary")) status = "Primary button clicked";
            if (LegacyUI.DangerButton(new Rect(buttons.x + (btnW + 7f) * 2f, buttons.y, btnW, buttons.height), "Danger")) status = "Danger button clicked";

            ui.Space(8f);
            LegacyUI.HintBox(ui.Row(34f), "LegacyUIFramework version: " + LegacyUI.Version);
            ui.Space(4f);
            LegacyUI.Label(ui.Row(20f), "Drag the title bar: the whole window fades while dragging.");
            LegacyUI.Label(ui.Row(20f), "Close button is enabled in the window options.");
            LegacyUI.Label(ui.Row(20f), "Input block mode: " + window.Options.InputBlockMode);

            Rect blockButtons = ui.Row(25f);
            float blockW = (blockButtons.width - 14f) / 3f;
            if (LegacyUI.TabButton(new Rect(blockButtons.x, blockButtons.y, blockW, blockButtons.height), "None", window.Options.InputBlockMode == LegacyUIInputBlockMode.None))
                window.Options.InputBlockMode = LegacyUIInputBlockMode.None;
            if (LegacyUI.TabButton(new Rect(blockButtons.x + blockW + 7f, blockButtons.y, blockW, blockButtons.height), "Window", window.Options.InputBlockMode == LegacyUIInputBlockMode.Window))
                window.Options.InputBlockMode = LegacyUIInputBlockMode.Window;
            if (LegacyUI.TabButton(new Rect(blockButtons.x + (blockW + 7f) * 2f, blockButtons.y, blockW, blockButtons.height), "Screen", window.Options.InputBlockMode == LegacyUIInputBlockMode.Screen))
                window.Options.InputBlockMode = LegacyUIInputBlockMode.Screen;
        }

        private void DrawValuesTab(ref LegacyUILayout ui)
        {
            LegacyUI.Label(ui.Row(20f), "HorizontalSlider");
            horizontalSliderValue = LegacyUI.HorizontalSlider(ui.Row(24f), horizontalSliderValue, 0f, 1f);

            LegacyUI.Label(ui.Row(20f), "HorizontalScrollbar");
            horizontalScrollbarValue = LegacyUI.HorizontalScrollbar(ui.Row(14f), horizontalScrollbarValue, 20f, 0f, 100f);

            ui.Space(12f);
            Rect area = ui.Row(135f);
            Rect left = new Rect(area.x, area.y, area.width - 42f, area.height);
            Rect right = new Rect(area.x + area.width - 32f, area.y, 32f, area.height);

            LegacyUI.Panel(left);
            LegacyUI.Label(new Rect(left.x + 12f, left.y + 10f, left.width - 24f, 22f), "Vertical controls");
            LegacyUI.Label(new Rect(left.x + 12f, left.y + 36f, left.width - 24f, 22f), "Slider: " + verticalSliderValue.ToString("0.00"));
            LegacyUI.Label(new Rect(left.x + 12f, left.y + 62f, left.width - 24f, 22f), "Scrollbar: " + verticalScrollbarValue.ToString("0"));

            verticalSliderValue = LegacyUI.VerticalSlider(new Rect(right.x, right.y, 14f, right.height), verticalSliderValue, 0f, 1f);
            verticalScrollbarValue = LegacyUI.VerticalScrollbar(new Rect(right.x + 18f, right.y, 14f, right.height), verticalScrollbarValue, 35f, 0f, 100f);
        }

        private void DrawScrollViewTab(ref LegacyUILayout ui)
        {
            LegacyUI.Label(ui.Row(20f), "ScrollView: files in Application.dataPath");

            float availableHeight = Mathf.Max(0f, ui.Area.yMax - ui.Y - 30f);
            scrollView.ViewRect = new Rect(ui.X, ui.Y, ui.Width, availableHeight);
            scrollView.Draw(delegate(Rect content)
            {
                LegacyUILayout list = new LegacyUILayout(content, 5f);

                if (dataPathFiles.Length == 0)
                {
                    LegacyUI.MiniHint(list.Row(22f), "No files found in Application.dataPath");
                    return;
                }

                for (int i = 0; i < dataPathFiles.Length; i++)
                {
                    string file = dataPathFiles[i];
                    string fileName = Path.GetFileName(file);
                    if (LegacyUI.Button(list.Row(26f), fileName))
                        status = "Selected file: " + fileName;
                }
            });
        }

        private void RefreshDataPathFiles()
        {
            try
            {
                dataPathFiles = Directory.GetFiles(Application.dataPath, "*", SearchOption.TopDirectoryOnly);
                Array.Sort(dataPathFiles, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                dataPathFiles = Array.Empty<string>();
                Logger.LogWarning("Failed to enumerate Application.dataPath: " + ex.Message);
            }
        }

        private void DrawTextTab(ref LegacyUILayout ui)
        {
            LegacyUI.Label(ui.Row(20f), "TextArea");
            areaValue = LegacyUI.TextArea(ui.Row(105f), areaValue);

            ui.Space(8f);
            LegacyUI.Title(ui.Row(21f), "Title style");
            LegacyUI.Label(ui.Row(20f), "Regular label style");
            LegacyUI.MiniHint(ui.Row(18f), "Mini hint style");
            LegacyUI.Status(ui.Row(18f), "Status style");
        }

        private void DrawThemesTab(ref LegacyUILayout ui)
        {
            LegacyUI.Label(ui.Row(20f), "Per-window theme via <b>LegacyUIWindowOptions.Theme</b>");

            Rect themeButtons = ui.Row(22f);
            float blockW = (themeButtons.width - 14f) / 3f;
            if (LegacyUI.TabButton(new Rect(themeButtons.x, themeButtons.y, blockW, themeButtons.height), "Use Default theme", window.Options.Theme == null))
                window.Options.Theme = null;
            if (LegacyUI.TabButton(new Rect(themeButtons.x + blockW + 7f, themeButtons.y, blockW, themeButtons.height), "Use Green theme", window.Options.Theme == greenTheme))
                window.Options.Theme = greenTheme;
            if (LegacyUI.TabButton(new Rect(themeButtons.x + (blockW + 7f) * 2f, themeButtons.y, blockW, themeButtons.height), "Use White theme", window.Options.Theme == whiteTheme))
                window.Options.Theme = whiteTheme;

            ui.Space(8f);
            LegacyUI.Label(ui.Row(20f), "Group theme scope");
            Rect panel = ui.Row(86f);
            using (LegacyUI.WithTheme(amberTheme))
            {
                LegacyUI.Panel(panel, 0.78f);
                LegacyUI.Title(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, 20f), "Amber themed group");
                LegacyUI.Label(new Rect(panel.x + 12f, panel.y + 32f, panel.width - 24f, 20f), "Everything inside this block uses amberTheme.");
                if (LegacyUI.Button(new Rect(panel.x + 12f, panel.y + 57f, 140f, 22f), "Scoped Button"))
                    status = "Scoped themed button clicked";
            }

            ui.Space(8f);
            LegacyUI.Label(ui.Row(20f), "Single element theme overloads");
            Rect row = ui.Row(27f);
            float btnW = (row.width - 7f) * 0.5f;
            if (LegacyUI.Button(new Rect(row.x, row.y, btnW, row.height), "Used theme"))
                status = "Default themed button clicked";
            if (LegacyUI.Button(new Rect(row.x + btnW + 7f, row.y, btnW, row.height), "Green overload", greenTheme))
                status = "Blue overload button clicked";

            ui.Space(8f);
            LegacyUI.HintBox(ui.Row(48f), "Use <b>LegacyUI.WithTheme(theme)</b> for groups or pass theme\nto individual helpers like <b>LegacyUI.Button(rect, text, theme)</b>.");
        }

        private static LegacyUITheme CreateGreenTheme()
        {
            LegacyUIPalette palette = new LegacyUIPalette();
            palette.Active = new Color(0.15f, 0.7f, 0.37f, 1f);
            palette.Field = new Color(0.1f, 0.15f, 0.11f, 1f);
            palette.SliderTrack = new Color(0.1f, 0.15f, 0.11f, 1f);
            palette.SliderThumb = new Color(0.15f, 0.7f, 0.37f, 1f);
            palette.SliderThumbHover = new Color(0.24f, 0.95f, 0.57f, 1f);
            palette.Button = new Color(0.15f, 0.33f, 0.2f, 1f);
            palette.ButtonHover = new Color(0.19f, 0.75f, 0.39f, 1f);
            palette.TextButton = new Color(0.87f, 0.87f, 0.87f, 1f);
            palette.WindowBackground = new Color(0.12f, 0.18f, 0.14f, 0.88f);
            palette.Border = new Color(0.1f, 0.6f, 0.31f, 1f);
            palette.Status = new Color(0.2f, 1f, 0.55f, 1f);
            return new LegacyUITheme(palette);
        }

        private static LegacyUITheme CreateWhiteTheme()
        {
            LegacyUIPalette palette = new LegacyUIPalette();

            palette.WindowBackground = new Color(1f, 1f, 1f, 0.95f);
            palette.Border = new Color(0.62f, 0.62f, 0.62f, 1f);

            palette.Field = new Color(0.9f, 0.9f, 0.9f, 1f);

            palette.Button = new Color(0.84f, 0.84f, 0.84f, 1f);
            palette.ButtonHover = new Color(0.76f, 0.76f, 0.76f, 1f);
            palette.TextButton = new Color(0.16f, 0.16f, 0.16f, 1f);

            palette.GreenButton = new Color(0.78f, 0.78f, 0.78f, 1f);
            palette.GreenButtonHover = new Color(0.68f, 0.68f, 0.68f, 1f);
            palette.GreenButtonActive = new Color(0.58f, 0.58f, 0.58f, 1f);

            palette.Danger = new Color(0.87f, 0.3f, 0.3f, 1f);
            palette.DangerHover = new Color(0.93f, 0.4f, 0.4f, 1f);
            palette.DangerActive = new Color(0.85f, 0.1f, 0.1f, 1f);

            palette.Active = new Color(0.55f, 0.55f, 0.55f, 1f);

            palette.HintBackground = new Color(1.00f, 1.00f, 1.00f, 0.35f);
            palette.HintBackgroundAlt = new Color(0.88f, 0.88f, 0.88f, 0.92f);

            palette.CheckEmpty = new Color(0.92f, 0.92f, 0.92f, 1f);

            palette.Text = new Color(0.16f, 0.16f, 0.16f, 1f);
            palette.TextBright = new Color(0.04f, 0.04f, 0.04f, 1f);
            palette.TextMuted = new Color(0.42f, 0.42f, 0.42f, 1f);

            palette.Status = new Color(0.30f, 0.30f, 0.30f, 1f);

            palette.SliderTrack = new Color(0.78f, 0.78f, 0.78f, 1f);
            palette.SliderThumb = new Color(0.42f, 0.42f, 0.42f, 1f);
            palette.SliderThumbHover = new Color(0.26f, 0.26f, 0.26f, 1f);
            return new LegacyUITheme(palette);
        }

        private static LegacyUITheme CreateAmberTheme()
        {
            LegacyUIPalette palette = new LegacyUIPalette();
            palette.Active = new Color(0.80f, 0.44f, 0.12f, 1f);
            palette.SliderThumb = new Color(0.80f, 0.44f, 0.12f, 1f);
            palette.SliderThumbHover = new Color(1.00f, 0.58f, 0.18f, 1f);
            palette.Button = new Color(0.24f, 0.18f, 0.12f, 1f);
            palette.ButtonHover = new Color(0.36f, 0.24f, 0.14f, 1f);
            palette.WindowBackground = new Color(0.13f, 0.10f, 0.06f, 0.88f);
            palette.Status = new Color(1.00f, 0.72f, 0.34f, 1f);
            return new LegacyUITheme(palette);
        }

    }
}
