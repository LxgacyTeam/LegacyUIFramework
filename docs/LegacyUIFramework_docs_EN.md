# LegacyUIFramework

`LegacyUIFramework.dll` is a small standalone IMGUI framework for BigCityLegacy plugins.
It contains its the visual style, also reusable helpers for draggable windows and common UI controls.

The framework can be built and copied as a separate DLL, then referenced by any plugin that wants to draw UI in the same style.

## Projects

```text
LegacyUIFramework.csproj
src/LegacyUIFramework.Example/LegacyUIFramework.Example.csproj
```

Build the framework:

```bash
dotnet build .\LegacyUIFramework.csproj -c Debug
```

Build and copy the example plugin into BepInEx:

```bash
dotnet build .\src\LegacyUIFramework.Example\LegacyUIFramework.Example.csproj -c Debug -p:GameDir="path\to\game\dir" -p:CopyToPlugins=true
```


For your plugins, copy `LegacyUIFramework.dll` next to the plugin DLL or to a shared BigCityLegacy modules folder loaded by BepInEx.

For background input blocking, the framework uses `0Harmony.dll` from `BepInEx/core` and `UnityEngine.UI.dll` + `UnityEngine.UIModule.dll` from `game_Data/Managed`. No extra DLLs need to be copied when the plugin runs in the usual BigCityLegacy BepInEx environment.

## Namespaces

```csharp
using BigCityLegacy.UI;
```

Main public classes:

```text
LegacyUI                 static facade for controls and styles
LegacyUITheme            theme object with palette, textures and styles
LegacyUIThemeScope       temporary scoped theme switch for groups of controls
LegacyUIPalette          editable color palette
LegacyUIWindow           draggable styled window
LegacyUIWindowOptions    window modifiers
LegacyUIScrollView       scrollable area with manual or automatic content canvas
LegacyUIGuiScope         saves/restores Unity IMGUI global state
LegacyUILayout           tiny manual layout helper
```

## Minimal window

```csharp
using BepInEx;
using BigCityLegacy.UI;
using UnityEngine;

[BepInPlugin("my.plugin.ui", "My UI Plugin", "1.0.0")]
public sealed class MyUiPlugin : BaseUnityPlugin
{
    private LegacyUIWindow window;
    private string serverHost = "127.0.0.1";

    private void Awake()
    {
        window = new LegacyUIWindow(
            "My Window",
            new Rect(120f, 90f, 360f, 220f),
            new LegacyUIWindowOptions
            {
                ShowCloseButton = true,
                RightHint = "[F9]",
                PlayerPrefsKey = "MyUiPlugin.Window",
                FadeWhileDragging = true,
                DragAlpha = 0.65f,
                InputBlockMode = LegacyUIInputBlockMode.Window
            });
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            window.Visible = !window.Visible;
            window.SavePrefs();
        }
    }

    private void OnGUI()
    {
        if (window == null || !window.Visible) return;

        using (new LegacyUIGuiScope(-10000))
        {
            window.Draw(DrawContent);
        }
    }

    private void DrawContent(Rect content)
    {
        LegacyUILayout ui = new LegacyUILayout(content);

        Rect row = ui.Row(24f);
        LegacyUI.Label(new Rect(row.x, row.y, 90f, row.height), "Host");
        serverHost = LegacyUI.TextField(new Rect(row.x + 95f, row.y, row.width - 95f, row.height), serverHost);

        if (LegacyUI.GreenButton(ui.Row(26f), "Apply"))
        {
            Debug.Log("Host selected: " + serverHost);
        }
    }
}
```

## Window modifiers

`LegacyUIWindowOptions` currently supports:

```csharp
new LegacyUIWindowOptions
{
    Draggable = true,
    ClampToScreen = true,
    ShowCloseButton = true,
    FadeWhileDragging = true,
    DragAlpha = 0.68f,
    BackgroundAlpha = 0.85f,
    Theme = customTheme,
    InputBlockMode = LegacyUIInputBlockMode.Window,
    HeaderHeight = 38f,
    ScreenPadding = 8f,
    RightHint = "[F8]",
    PlayerPrefsKey = "Some.Unique.Window.Key"
};
```

When `FadeWhileDragging` is enabled, the window and all child IMGUI elements are drawn with reduced alpha while the title bar is being dragged.

`BackgroundAlpha` changes only the window background opacity. If it is `null`, the opacity from the current theme is used.

`Theme` is optional. If it is set, only this window and its content are drawn with that theme. The global framework theme is not changed.

`InputBlockMode` controls how the window consumes mouse events after its own controls are drawn:

```text
None   = 0 — do not block background clicks
Window = 1 — block clicks only inside the window rectangle; this is the default
Screen = 2 — block mouse clicks anywhere on the screen while the window is visible
```

When `PlayerPrefsKey` is set, the framework stores window position, size and visibility:

```text
<key>.x
<key>.y
<key>.w
<key>.h
<key>.visible
```

## Controls

The framework includes wrappers for the common styled controls:

```csharp
LegacyUI.Panel(rect);
LegacyUI.DrawBorder(rect);
LegacyUI.Separator(rect);
LegacyUI.Label(rect, "Label");
LegacyUI.Title(rect, "Title");
LegacyUI.MiniHint(rect, "Hint");
LegacyUI.Status(rect, "Status");
LegacyUI.HintBox(rect, "Hint box");

string value = LegacyUI.TextField(rect, value);
string text = LegacyUI.TextArea(rect, text);

bool clicked = LegacyUI.Button(rect, "Default");
bool ok = LegacyUI.GreenButton(rect, "Primary");
bool danger = LegacyUI.DangerButton(rect, "Danger");
bool activeTabClicked = LegacyUI.TabButton(rect, "Tab", selected);
bool enabled = LegacyUI.Toggle(rect, enabled, "Toggle label");

float x = LegacyUI.HorizontalSlider(rect, x, 0f, 1f);
float y = LegacyUI.VerticalSlider(rect, y, 0f, 1f);
float sx = LegacyUI.HorizontalScrollbar(rect, sx, 20f, 0f, 100f);
float sy = LegacyUI.VerticalScrollbar(rect, sy, 20f, 0f, 100f);
```

The slider and scrollbar helpers are custom-drawn and do not depend on mutating `GUI.skin`, so several plugins can safely use the framework without fighting over global skin state.

`TextField` and `TextArea` automatically apply text-editing colors from the currently active theme: the caret uses `Palette.TextBright`, while the selection uses `Palette.Active` with reduced opacity. Unity stores these values in `GUI.skin.settings`, so the framework overrides them only for the duration of the individual text-control call and immediately restores the previous values. This works with the global theme, `LegacyUI.WithTheme(...)`, window themes and per-control theme overloads without permanently changing the global `GUI.skin`.

## ScrollView

`LegacyUIScrollView` is a standalone scrollable area. `ViewRect` defines the visible block including its scrollbars, while `ContentRect` is the virtual canvas inside the block.

The one-argument constructor enables automatic content sizing. The canvas is recalculated every draw from the `LegacyUI` controls rendered inside the callback plus `Padding`, which makes it suitable for dynamic lists:

```csharp
private LegacyUIScrollView scrollView = new LegacyUIScrollView(
    new Rect(20f, 20f, 320f, 220f))
{
    Padding = 8f,
    ShowHorizontalScrollbar = false,
    ShowVerticalScrollbar = true
};

private void DrawFiles(string[] files)
{
    scrollView.Draw(delegate(Rect content)
    {
        LegacyUILayout list = new LegacyUILayout(content, 5f);
        for (int i = 0; i < files.Length; i++)
        {
            if (LegacyUI.Button(list.Row(26f), files[i]))
                Debug.Log(files[i]);
        }
    });
}
```

In automatic mode `ContentRect` always has at least the size of the visible content viewport and grows or shrinks with the controls drawn during the current IMGUI pass. All standard `LegacyUI` helpers participate in measurement automatically. If you draw a control directly through `GUI.*`, register its rectangle manually inside the callback:

```csharp
Rect custom = new Rect(content.x, content.y + 40f, 500f, 24f);
GUI.Button(custom, "Raw IMGUI button");
scrollView.IncludeContentRect(custom);
```

To use a fixed virtual canvas, pass the inner rectangle to the second constructor or assign `ContentRect`. Assigning `ContentRect` switches `AutoContentSize` off:

```csharp
LegacyUIScrollView map = new LegacyUIScrollView(
    new Rect(20f, 20f, 320f, 220f),
    new Rect(0f, 0f, 900f, 700f));

map.Draw(delegate(Rect content)
{
    LegacyUI.Button(new Rect(content.x + 600f, content.y + 400f, 140f, 26f), "Far button");
});
```

Call `UseAutoContentSize()` to return to dynamic sizing and `ResetScroll()` to jump back to the top-left corner. `ShowHorizontalScrollbar` and `ShowVerticalScrollbar` control which axes reserve and draw framework-styled scrollbars. `ScrollbarSize`, `ScrollbarSpacing`, `DrawBackground`, `BackgroundAlpha`, `ScrollPosition`, and `Theme` can be adjusted per instance.

## Styles and theme

Access default styles directly when needed:

```csharp
GUI.Label(rect, "Custom label", LegacyUI.Styles.Label);
GUI.Button(rect, "Custom", LegacyUI.Styles.Button);
```

Available style properties include:

```text
Window
Label
Title
MiniHint
Hint
Status
TextField
TextArea
Button
GreenButton
DangerButton
Tab
CheckboxLabel
CloseButton
HorizontalSlider
HorizontalSliderThumb
VerticalSlider
VerticalSliderThumb
HorizontalScrollbar
HorizontalScrollbarThumb
VerticalScrollbar
VerticalScrollbarThumb
```

To customize the colors globally for one plugin:

```csharp
LegacyUIPalette palette = new LegacyUIPalette();
palette.Active = new Color(0.20f, 0.45f, 0.80f, 1f);
palette.WindowBackground = new Color(0.04f, 0.04f, 0.05f, 0.88f);
LegacyUI.SetTheme(new LegacyUITheme(palette));
```

The global theme is still useful as the default for one plugin, but you can also use temporary themes for individual windows, groups or single controls without changing global state.

Create reusable themes once, for example in `Awake()`:

```csharp
LegacyUIPalette bluePalette = new LegacyUIPalette();
bluePalette.Active = new Color(0.16f, 0.42f, 0.82f, 1f);
bluePalette.WindowBackground = new Color(0.04f, 0.05f, 0.08f, 0.88f);
LegacyUITheme blueTheme = new LegacyUITheme(bluePalette);
```

`LegacyUITheme` is safe to create in `Awake()` or other non-`OnGUI` initialization code. Textures and styles are created lazily when they are first needed by actual draw calls. If you mutate a palette after the theme has already been drawn, call `theme.Invalidate()` or replace it through `theme.SetPalette(...)`.

Use a theme for one whole window:

```csharp
window = new LegacyUIWindow(
    "Themed Window",
    new Rect(120f, 90f, 360f, 220f),
    new LegacyUIWindowOptions
    {
        Theme = blueTheme,
        ShowCloseButton = true
    });
```

Use a scoped theme for a group of controls:

```csharp
using (LegacyUI.WithTheme(blueTheme))
{
    LegacyUI.Panel(rect);
    LegacyUI.Title(titleRect, "Blue group");
    LegacyUI.Button(buttonRect, "Button");
}
```

Or pass the theme only to one control:

```csharp
LegacyUI.Button(rect, "Blue button", blueTheme);
LegacyUI.HorizontalSlider(rect, value, 0f, 1f, blueTheme);
LegacyUI.Panel(rect, 0.70f, blueTheme);
```

Scoped themes are stack-based, so nested themes are allowed. Dispose the scope inside the same `OnGUI` call where it was created.

## Example plugin

`LegacyUIFramework.Example` draws a window with:

```text
TextField
TextArea
Button
GreenButton
DangerButton
TabButton
Toggle
HintBox
ScrollView with an automatically sized dynamic file list
HorizontalScrollbar / VerticalScrollbar through ScrollView
Close button
Drag fade modifier
Input block mode switcher
Theme scope / per-window / per-control theme examples
```

This plugin is only a usage sample. It is not required by the framework.

## Integration rule for BigCityLegacy

Do not move large optional UI helpers back into the main BigCityLegacy plugin. New UI-heavy plugins should reference `LegacyUIFramework.dll` and keep their own feature logic in separate assemblies.
