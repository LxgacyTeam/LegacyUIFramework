# LegacyUIFramework

`LegacyUIFramework.dll` is a small standalone IMGUI library for BepInEx 5 plugins.
It provides its own visual style as well as reusable helpers for draggable windows and basic UI elements.

> [!NOTE]
> The framework is developed as part of the BigCityLegacy project and is intended for use both by the project itself and by additional dependent plugins.
> Using LegacyUIFramework with third-party games is possible, but full compatibility is not guaranteed.

The framework can be built and copied as a standalone DLL, then referenced from any plugin that needs to render its interface in a consistent style.

## Projects

```text
src/LegacyUIFramework/LegacyUIFramework.csproj
src/LegacyUIFramework.Example/LegacyUIFramework.Example.csproj
```

Build the framework:

```bash
dotnet build .\LegacyUIFramework.csproj -c Debug -p:GameDir="path\to\game\dir"
```

Build the example plugin and copy it to BepInEx:

```bash
dotnet build .\src\LegacyUIFramework.Example\LegacyUIFramework.Example.csproj -c Debug -p:GameDir="path\to\game\dir" -p:CopyToPlugins=true
```

For your own plugins, copy `LegacyUIFramework.dll` next to the plugin DLL or into the shared BigCityLegacy modules folder loaded by BepInEx.

For background input blocking, the framework uses `0Harmony.dll` from `BepInEx/core` as well as `UnityEngine.UI.dll` and `UnityEngine.UIModule.dll`.

## Namespaces

```csharp
using BigCityLegacy.UI;
```

Main public classes:

```text
LegacyUI                 static facade for controls and styles
LegacyUITheme            theme object containing palette, textures, and styles
LegacyUIThemeScope       temporarily switches the theme for a group of controls
LegacyUIPalette          editable color palette
LegacyUIWindow           draggable styled window
LegacyUIWindowOptions    window modifiers
LegacyUIGuiScope         saves/restores the global Unity IMGUI state
LegacyUILayout           small helper for manual fixed-layout positioning
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

When `FadeWhileDragging` is enabled, the window and all child IMGUI elements are rendered with reduced opacity while the window is being dragged by its header.

`BackgroundAlpha` changes only the opacity of the window background. If the value is `null`, the opacity from the current theme is used.

`Theme` is optional. When specified, only this window and its contents are rendered using that theme. The framework's global theme is not changed.

`InputBlockMode` controls how the window consumes mouse events after rendering its own elements:

```text
None   = 0 — do not block background clicks
Window = 1 — block clicks only inside the window rectangle; default mode
Screen = 2 — block mouse clicks across the entire screen while the window is open
```

If `PlayerPrefsKey` is specified, the framework saves the window position, size, and visibility:

```text
<key>.x
<key>.y
<key>.w
<key>.h
<key>.visible
```

## Controls

The framework provides wrappers for commonly used styled controls:

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

The slider and scrollbar helpers are rendered manually and do not depend on changes to `GUI.skin`, allowing multiple plugins to safely use the framework without conflicting over the global skin state.

## Styles and theme

If needed, you can access framework styles directly:

```csharp
GUI.Label(rect, "Custom label", LegacyUI.Styles.Label);
GUI.Button(rect, "Custom", LegacyUI.Styles.Button);
```

Available style properties:

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

To configure colors globally for a single plugin:

```csharp
LegacyUIPalette palette = new LegacyUIPalette();
palette.Active = new Color(0.20f, 0.45f, 0.80f, 1f);
palette.WindowBackground = new Color(0.04f, 0.04f, 0.05f, 0.88f);
LegacyUI.SetTheme(new LegacyUITheme(palette));
```

The global theme is still convenient as a plugin-wide default, but temporary themes can also be used for individual windows, groups of elements, or single controls without changing global state.

Create reusable themes once, for example in `Awake()`:

```csharp
LegacyUIPalette bluePalette = new LegacyUIPalette();
bluePalette.Active = new Color(0.16f, 0.42f, 0.82f, 1f);
bluePalette.WindowBackground = new Color(0.04f, 0.05f, 0.08f, 0.88f);
LegacyUITheme blueTheme = new LegacyUITheme(bluePalette);
```

`LegacyUITheme` can safely be created in `Awake()` or other initialization code outside `OnGUI`. Textures and styles are created lazily on first actual rendering. If the palette is changed after the theme has already been rendered, call `theme.Invalidate()` or replace the palette using `theme.SetPalette(...)`.

Using a theme for an entire window:

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

Using a theme for a group of controls:

```csharp
using (LegacyUI.WithTheme(blueTheme))
{
    LegacyUI.Panel(rect);
    LegacyUI.Title(titleRect, "Blue group");
    LegacyUI.Button(buttonRect, "Button");
}
```

Or applying a theme to a single element only:

```csharp
LegacyUI.Button(rect, "Blue button", blueTheme);
LegacyUI.HorizontalSlider(rect, value, 0f, 1f, blueTheme);
LegacyUI.Panel(rect, 0.70f, blueTheme);
```

Scoped themes work as a stack, so nested themes are supported. Dispose the scope within the same `OnGUI` call in which it was created.

## Example plugin

`LegacyUIFramework.Example` renders a window containing the following elements:

```text
TextField
TextArea
Button
GreenButton
DangerButton
TabButton
Toggle
HintBox
HorizontalSlider
VerticalSlider
HorizontalScrollbar
VerticalScrollbar
Close button
Drag fade modifier
Input block mode switcher
Examples of themes for a window, a group, and a single element
```

This plugin exists only as a usage example. It is not required for the framework to function.

## Integration rule for BigCityLegacy

Do not move large optional UI helpers back into the main BigCityLegacy plugin. New plugins with substantial UI should reference `LegacyUIFramework.dll` and keep their feature logic in separate assemblies.
