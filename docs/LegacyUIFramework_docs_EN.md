# LegacyUIFramework

`LegacyUIFramework.dll` is a small standalone IMGUI library for Unity Mono/BepInEx plugins.
It contains its own visual style, reusable helpers for draggable windows and common UI controls, and compatibility helpers for differences between Unity versions.

The framework is developed as part of BigCityLegacy, but it does not depend on the main mod's feature logic and can be used as a standalone DLL by other BepInEx plugins and compatible Unity Mono games.

## Projects

```text
src/LegacyUIFramework/LegacyUIFramework.csproj
src/LegacyUIFramework.net35/LegacyUIFramework.net35.csproj

src/LegacyUIFramework.Example/LegacyUIFramework.Example.csproj
src/LegacyUIFramework.Example.net35/LegacyUIFramework.Example.csproj
```

Build the framework:

```bash
dotnet build .\src\LegacyUIFramework\LegacyUIFramework.csproj -c Debug -p:GameDir="path\to\game\dir"
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
LegacyInput              compatibility wrapper for Unity Legacy Input API
```

## LegacyInput — compatible Legacy Input API

`LegacyInput` is intended for BepInEx plugins that need one build to work across different generations of Unity. In older Unity versions, `UnityEngine.Input` lived in the monolithic `UnityEngine.dll`; in newer versions the Legacy Input implementation is provided by the separate `UnityEngine.InputLegacyModule.dll`. A plugin compiled directly against `Input.GetKeyDown(...)` can therefore end up with an assembly reference tied to the layout of the selected Unity reference build.

`LegacyInput` has no compile-time reference to `UnityEngine.Input`. During `Init()` it searches the assemblies already loaded by Unity for the `UnityEngine.Input` type through reflection, creates delegates for the supported methods, and then uses those cached delegates without repeating reflection lookup every frame.

Initialize the wrapper explicitly once in `Awake()`:

```csharp
private void Awake()
{
    LegacyInput.Init();
}

private void Update()
{
    if (LegacyInput.GetKeyDown("f10") && window != null)
    {
        window.ToggleVisible();
        window.SavePrefs();
    }
}
```

Calling `Init()` more than once is safe: after the first initialization attempt the cached result is reused. Wrapper methods can also initialize lazily if `Init()` has not been called yet, but an explicit call in `Awake()` is recommended so delegate creation happens at a predictable point in the plugin lifecycle.

You can inspect wrapper state with:

```csharp
bool initialized = LegacyInput.Initialized;
bool available = LegacyInput.Available;
```

`Available` means that `UnityEngine.Input` was found and at least the string `GetKey(string)` overload could be bound to a delegate. Individual APIs may be unavailable in unusual Unity builds; the corresponding wrapper methods then return `false` or `0f`.

Supported methods and properties:

```csharp
// Keyboard
bool held = LegacyInput.GetKey("f10");
bool down = LegacyInput.GetKeyDown("f10");
bool up   = LegacyInput.GetKeyUp("f10");

// Mouse
bool mouseHeld = LegacyInput.GetMouseButton(0);
bool mouseDown = LegacyInput.GetMouseButtonDown(0);
bool mouseUp   = LegacyInput.GetMouseButtonUp(0);
Vector3 mousePosition = LegacyInput.MousePosition;

// Input Manager buttons
bool fire     = LegacyInput.GetButton("Fire1");
bool fireDown = LegacyInput.GetButtonDown("Fire1");
bool fireUp   = LegacyInput.GetButtonUp("Fire1");

// Input Manager axes
float horizontal = LegacyInput.GetAxis("Horizontal");
float raw         = LegacyInput.GetAxisRaw("Horizontal");

// Misc
bool anyKey = LegacyInput.AnyKey;
bool anyKeyDown = LegacyInput.AnyKeyDown;
```

Keyboard methods use the string key names supported by the regular `UnityEngine.Input.GetKey(string)` API. `GetButton*` and `GetAxis*` use names that must actually exist in the target game's Input Manager configuration.

> [!IMPORTANT]
> `LegacyInput` solves **assembly compatibility** between different locations of `UnityEngine.Input`; it is not an implementation of Unity's new Input System. It requires the Legacy Input Manager (`UnityEngine.Input`) to exist and be available in the game. In modern Unity projects built with only `Active Input Handling = Input System Package (New)`, Legacy Input may be disabled; the current wrapper does not replace that backend.

## Unity version compatibility

The lower bound of the current version is determined mainly by the **reference assembly layout and .NET target framework**.

Main `LegacyUIFramework`:

- the project targets `net472`; the resulting DLL is intended for a Unity Mono runtime with a compatible .NET 4.x API;
- the `.csproj` directly references modular `UnityEngine.CoreModule.dll`, `UnityEngine.IMGUIModule.dll`, `UnityEngine.TextRenderingModule.dll`, and `UnityEngine.UIModule.dll`; Unity officially split `UnityEngine.dll` into modules in Unity 2017.2;
- `LegacyUIInputBlocker` uses uGUI (`UnityEngine.UI.dll`, `Canvas`, `GraphicRaycaster`, `Image`), so Unity UI/uGUI is a runtime dependency of the framework;
- the framework targets **Mono** Unity/BepInEx. IL2CPP is not supported.

`LegacyUIFramework.net35` - is a separate build configuration for old Unity Mono games that use the .NET Framework 3.5 profile and the old monolithic Unity assembly layout.

- target framework: `net35`;
- Unity APIs are referenced from monolithic `UnityEngine.dll`;
- uGUI is referenced from `UnityEngine.UI.dll`;
- both builds compile the same framework source files, keeping their public API aligned.


A practical compatibility range is:

```text
Unity 2019.2 — Unity 6, Mono, .NET 4.x-compatible runtime
    primary recommended range for the current build

Unity 2017.2 — 2019.1
    conditionally compatible when the specific game uses .NET 4.x / .NET 4.6 Equivalent
    and ships the required UnityEngine modules and UnityEngine.UI

Unity 4.6 — 2017.1, Mono, legacy .NET 3.5 profile
    supported by the separate LegacyUIFramework.net35 project;
    uses monolithic UnityEngine.dll + UnityEngine.UI.dll
```

> [!NOTE]
> `src/LegacyUIFramework/LegacyUIFramework.csproj` remains the normal `net472` build.
> `src/LegacyUIFramework.net35/LegacyUIFramework.net35.csproj` is the alternative `net35` build for old monolithic Unity versions. 

For Unity 2017.2+ the normal LegacyUIFramework project is recommended because Unity moved to modular `UnityEngine.*Module.dll` assemblies. A `net35` build may still be useful on some newer games, but it should then use references matching that specific Unity version.


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
        LegacyInput.Init();

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
        if (LegacyInput.GetKeyDown("f9"))
        {
            window.Visible = !window.Visible;
            window.SavePrefs();
        }
    }

    private void OnGUI()
    {
        if (window == null || !window.IsRendered) return;

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

`BackgroundAlpha` sets the base window background opacity. If it is `null`, `BackdropBlur = true` uses `1f`; with blur disabled, the current theme's opacity is used. Explicit values, including `0f`, are preserved. `BlurTintAlpha` defaults to `0.75f` and multiplies the background opacity only when the blurred image is successfully drawn; otherwise the base opacity is used. The default is resolved at draw time, so toggling `BackdropBlur` does not overwrite `BackgroundAlpha`.

The implicit `1f` value and `BlurTintAlpha` multiplier apply only to the window background. When the window has no explicit `BackgroundAlpha`, child panels (`LegacyUI.Panel` and `LegacyUIScrollView` backgrounds) use the opacity from their current theme. An explicit window `BackgroundAlpha` is inherited without the blur multiplier; a panel or ScrollView can override it with its own `BackgroundAlpha`. Inheritance is scoped to `window.Draw`: nested windows establish their own context, and the previous context is restored on return or exception. Opening and dragging animations still affect the entire window content.

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

The `LegacyUIScrollView` background uses `Mathf.Clamp01(alpha) * 0.75f`, reducing the base opacity by 25%. It resolves its own `BackgroundAlpha` first, then the containing window's explicitly configured `BackgroundAlpha`, otherwise `WindowBackground.a` from the ScrollView's current theme. For example, `0.8f` produces `0.6f`, and a theme value of `0.85f` produces `0.6375f`. This multiplier affects only the ScrollView background, not its content, border or scrollbars; settings and palette remain unchanged.

> [!WARNING]
> **Warning — declaration order of IMGUI elements near a ScrollView**
>
> `LegacyUIScrollView`, like the other elements of `LegacyUIFramework`, operates on top of Unity IMGUI. In IMGUI, the state of interactive controls is tied to their internal `control ID`s, which Unity assigns based in part on the **order in which GUI elements are called inside `OnGUI()`**.
>
> For this reason, care should be taken if a `ScrollView` contains a **dynamic number of elements** and stateful controls are declared after it in the code, such as `TextField`, `TextArea`, or other elements that use keyboard/mouse focus.
>
> For example, a potentially problematic order:
>
> ```csharp
> scroll.Draw(content =>
> {
>     for (int i = 0; i < dynamicItems.Count; i++)
>         LegacyUI.Button(...);
> });
>
> text = LegacyUI.TextField(textRect, text);
> ```
>
> If the number of elements inside the `ScrollView` changes, the number of IMGUI controls created before the `TextField` changes as well. In some versions of Unity, this can cause the field's internal `control ID` to change between different GUI events (`Layout`, `MouseDown`, `KeyDown`, `Repaint`).
>
> As a result, a `TextField` that looks visually correct may behave incorrectly:
>
> * fail to receive or lose keyboard focus;
> * fail to display the caret;
> * fail to support text selection with the mouse;
> * lose the current cursor position;
> * respond incorrectly to clicks or keyboard input.
>
> **Recommended approach:** declare interactive elements that require stable IMGUI state **before the dynamic contents of the `ScrollView`**.
>
> ```csharp
> Rect scrollRect = ...;
> Rect textRect = ...;
>
> // Stateful controls first.
> text = LegacyUI.TextField(textRect, text);
>
> // Dynamic controls afterwards.
> scroll.ViewRect = scrollRect;
> scroll.Draw(content =>
> {
>     for (int i = 0; i < dynamicItems.Count; i++)
>         LegacyUI.Button(...);
> });
> ```
>
> The call order of IMGUI elements **does not have to match their visual layout**. An element's position is determined by the `Rect` passed to it, so a `TextField` can be called before the `ScrollView` even if it is visually located below it.
>
> This ordering is especially recommended for `TextField`, `TextArea`, and other controls whose state is preserved by Unity between GUI events.
>
> Also, unless necessary, avoid manually combining standard IMGUI input with `GUI.FocusControl`, custom reading of `Input.inputString`, or separate cursor/selection handling. `LegacyUI.TextField` and `LegacyUI.TextArea` are designed to use Unity IMGUI's standard focus mechanism.
>
> `LegacyUIScrollView` itself may contain any number of dynamic `LegacyUI` elements. The limitation applies specifically to the **stability of the IMGUI control order for controls placed after dynamically changing content**.


### IMGUI scaling

Scaling is **enabled by default** for UI drawn inside `LegacyUIGuiScope`. Keep using normal design-pixel `Rect` coordinates, window sizes, `LegacyUILayout` and `LegacyUIScrollView`; the framework applies the scaling transform via `GUI.matrix`. Do **not** multiply rectangle coordinates yourself.

Default mode is `Automatic`: `min(Screen.width / 1920, Screen.height / 1080) × BaseScale`, clamped to `MinScale..MaxScale` (defaults: 0.75–4). At `BaseScale=1`: 1920×1080 → 1×, 2560×1440 → 1.333×, 3840×2160 → 2×. For 3× at UHD set `BaseScale=1.5`.

Prefer a dedicated settings object **per plugin**, since multiple plugins can share a single framework assembly:

```csharp
private readonly LegacyUIScaleSettings uiScale = new LegacyUIScaleSettings();

private void OnGUI()
{
    using (new LegacyUIGuiScope(-10000, uiScale))
        window.Draw(DrawContent);
}

uiScale.Mode = LegacyUIScaleMode.Automatic;
uiScale.BaseScale = 1.5f;

// Alternatively, use a fixed multiplier on any screen:
uiScale.Mode = LegacyUIScaleMode.Manual;
uiScale.ManualScale = 1.25f;
```

Without an explicit settings object, `LegacyUIGuiScope(-10000)` uses shared `LegacyUI.ScaleSettings` (automatic by default). `LegacyUI.Scale` reports the active coefficient; `LegacyUI.VirtualScreenSize` provides dimensions in design pixels. `LegacyUI.MousePosition` handles custom mouse hit testing. Draggable windows and game input blocking account for the active transform.

`LegacyUIWindowOptions.ScaleMultiplier` scales an individual window (default 1×). For an individual element/group use:

```csharp
using (LegacyUI.WithScale(1.2f, new Vector2(buttonRect.x, buttonRect.y)))
    LegacyUI.Button(buttonRect, "Large button");
```

`WithScale` is **relative** to the current scale; the `pivot` overload preserves the chosen point. `Dispose()` always restores the prior matrix. Account for an enlarged control's visual bounds when laying out its neighbors.

Keep persistent themes and stateful controls in `Awake`/fields, **not** constructed inside `OnGUI`, to avoid allocations and native texture leaks.

### LegacyUIDropdown

A stateful dropdown using `LegacyUIScrollView`: `PopupWidth` controls the open popup width (0 = button width), `MaxHeight` caps its height, and excess items scroll vertically. Selection is exposed via `SelectedIndex` and `SelectionChanged`.

```csharp
// Awake:
dropdown = new LegacyUIDropdown(new[] { "Vanilla", "Sport", "Rally", "Drift" }, 0)
{
    PopupWidth = 260f,
    MaxHeight = 110f,
    ItemHeight = 27f
};

// OnGUI:
using (new LegacyUIGuiScope(-10000, uiScale))
{
    dropdown.HandlePopupInput(); // before underlying UI, prevents click-through
    window.Draw(delegate(Rect area)
    {
        dropdown.Draw(new Rect(area.x, area.y + 60f, 210f, 27f));
    });
    dropdown.DrawPopup(); // draw LAST and OUTSIDE GUI.BeginScrollView
}
```

The popup must be drawn in the same root GUI coordinate space as its anchor, **last**, and outside clipped `GUI.BeginGroup`/`GUI.BeginScrollView` areas. For dropdowns inside an existing scrollview you would need to convert the anchor's coordinates to root GUI coordinates. `HandlePopupInput` should run before other interactive controls if they could overlap the popup. Reuse the same dropdown instance across IMGUI events.

### Counter

**Stateless integer stepper:**

```csharp
quantity = LegacyUI.Counter(new Rect(15, 20, 200, 28), quantity,
                            step: 5, minValue: 0, maxValue: 100);
```

**Stateful editable floating-point stepper:**

```csharp
// Awake:
boost = new LegacyUICounter(1.5f, 0.25f, 0f, 10f);
boost.NumberFormat = "0.00";

// OnGUI:
float selectedBoost = boost.Draw(new Rect(15, 60, 210, 28));
```

`Step` determines the increment/decrement. `MinValue`/`MaxValue` clamp input and button presses. Typed numbers use invariant formatting (dot decimal separator); set `AllowTyping=false` to make the middle value read-only. Optional `Theme` changes the counter's appearance. Combine with `WithScale` to enlarge a group or control.

**IMGUI control-ID warning:** keep stateful controls in consistent call order across `Layout`, `Repaint`, and input events. Drawing a variable number of buttons in a scrollview *before* a text input can break focus and cursor handling. Draw `TextField` / `LegacyUICounter` before dynamically sized scrollviews; Rects determine visual placement independently of call order.


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

## Animations

Animations are enabled by default: windows and dropdown popups fade in/out, dragging transitions smoothly to/from `DragAlpha`, toggles fade between states, and buttons, tabs, fields and slider/scrollbar thumbs animate hover. Durations use real seconds and continue while `Time.timeScale` is zero. Zero duration disables one transition; `Enabled = false` disables all transitions in that settings object.

```csharp
private readonly LegacyUIAnimationSettings animations = new LegacyUIAnimationSettings
{
    Enabled = true,
    WindowDuration = 0.18f,
    DragDuration = 0.12f,
    DropdownDuration = 0.14f,
    ToggleDuration = 0.12f,
    HoverDuration = 0.12f
};

// OnGUI: plugin defaults are scoped; other plugins keep their own settings.
using (new LegacyUIGuiScope(-10000, scaleSettings, animations))
{
    window.Draw(DrawContent);
}

// Optional per-window override:
window.Options.Animations = new LegacyUIAnimationSettings { WindowDuration = 0.3f };
// Or change the shared defaults outside plugin-specific scopes:
LegacyUI.AnimationSettings.Enabled = false;
```

`LegacyUI.WithAnimations(settings)` scopes settings for a group. `LegacyUIWindowOptions.Animations` overrides plugin defaults; null inherits them. `LegacyUIDropdown.Animations` can override its anchor's settings. Settings and GUI state are restored when scopes end, including exceptions.

`Visible` is the requested logical visibility. Keep calling `window.Draw(...)` every OnGUI pass, or guard with `window.IsRendered`, so closing can finish fading. Do not disable the hosting MonoBehaviour from `Closed`; disable it once `IsRendered` is false. `Closed` still fires immediately and preferences store logical visibility. Content continues drawing during fade-out with input disabled. `Opacity` exposes the current window fade. Popups retain the anchor's transform, theme and opacity; keep calling `DrawPopup()` last outside scroll clips, and `HandlePopupInput()` before the window.

## Progress bars

`LegacyUI.ProgressBar(rect, value, min, max, label)` is a read-only measured-value control. `LegacyUIProgressBar` binds a persistent instance to a `Func<double>` value provider, or its `Value` field. `Min` and `Max` can change between frames. Providers run on the GUI thread. Values are clamped to the range; descending ranges work, equal endpoints and invalid numbers produce an empty bar. Double precision accommodates large byte counts. No input is consumed; the control participates in automatic ScrollView sizing.

```csharp
LegacyUI.ProgressBar(new Rect(20, 90, 260, 22), health, 0d, 100d, "HP");

// Keep this instance across GUI calls:
var download = new LegacyUIProgressBar
{
    Min = 0d,
    Max = totalBytes,
    ValueProvider = () => downloadedBytes,
    ShowPercentage = true
};
// OnGUI:
download.Draw(new Rect(20, 120, 260, 22));

var palette = new LegacyUIPalette { ProgressFill = Color.green, ProgressTrack = Color.black };
var theme = new LegacyUITheme(palette);
using (LegacyUI.WithTheme(theme)) LegacyUI.ProgressBar(rect, currentValue, minimum, maximum);
```

Progress colors are independent from buttons/sliders: `LegacyUIPalette.ProgressFill` and `ProgressTrack`. The default label is a percentage; set `ShowPercentage = false` or supply `Label` to replace it. Both normal and net35 framework builds include these controls and animations.

## Optional MadOut2 backdrop blur

`src/LegacyUIFramework.Blur/LegacyUIFramework.Blur.csproj` produces **LegacyUIFramework.Blur.dll**, an optional module for **MadOut2 9.4 only**, on its built-in render pipeline. It is not a universal Unity adapter and has no net35 build. The base library has only the `ILegacyUIBackdrop` interface and works without the module. BigCityLegacy references and initializes the module on supported clients; dedicated/headless servers do not create it.

```csharp
using BigCityLegacy.UI.Blur;

// MadOut2 9.4 client only; host owns the provider's lifetime:
LegacyBclBlur backdrop = LegacyBclBlur.Create(gameObject);
using (new LegacyUIGuiScope(-10000, scaleSettings, animations, backdrop))
    window.Draw(DrawContent);

window.Options.BackdropBlur = false; // disable for one window
window.Options.BlurTintAlpha = 0.75f;
// Or set window.Options.Backdrop to a specific ILegacyUIBackdrop provider.
```

The module follows the actual `Cam.me.unityCam` (with a main-camera fallback), captures before IMGUI via an appended image effect, and forwards the original scene unchanged. The scene camera also qualifies when GameUI renders it into `MainRenderTexture`: GameUI displays this target across the screen, so window crops use screen dimensions independently of the scene render scale. Other offscreen cameras are excluded. Direct-to-screen rendering retains the camera viewport mapping. It borrows the native BlurBehind shader reference, uses its pass 0 and `_Parameter` convention in a private material, and does not enable/disable native BlurBehind, change its settings or touch the Canvas blur globals. The recovered project's shader source is a reconstruction stub and is not bundled or used in place of the game's shader.

One quarter-resolution texture is reused by all requesting windows. `Downsample` (1–8), `Iterations` (0–4) and `Radius` (0–8) configure the provider. Window crops follow screen coordinates, scaling and camera viewport, with GUI/texture Y conversion. GPU resources are recreated on resize/camera changes, released when idle or disabled, and disposed with the host. Missing/unsupported shaders, a window outside the scene viewport or an unavailable camera fall back to the regular themed background. `BlurTintAlpha` multiplies the window tint only while a blurred image is available. Rendering of window content remains sharp.

### Drag state and host camera input

`window.isDragging` and `window.IsDragging` are read-only aliases for the same captured header-drag gesture. They become true when the left mouse button captures the header and return false on left release, closing, an ignored input event, disabling `Draggable`, or detecting a release outside the GUI. Releasing another mouse button does not cancel a left-button drag. The flag is independent of the DragAlpha fade; it does not stay true while opacity returns to normal.

`window.IsDragHandleAtScreenPoint(point)` tests the last drawn header in physical screen pixels, using the bottom-left origin of Unity `Input.mousePosition`. It accounts for GUI/window scaling and excludes the close button. This lets a host exclude the initial press from a polling-based camera before OnGUI starts the gesture; use `isDragging` to keep camera input blocked throughout the gesture. Querying these values does not create new windows.

### BCL and the Example plugin

BCL applies a blurred backdrop and a lighter background tint to About, Server Connection, player profile, updater, VinylPort and its confirmation window. The performance overlay also uses the shared backdrop. PointTool explicitly disables blur. If the capture provider is unavailable, each keeps its regular background.

The Example plugin has a soft dependency on BCL. When present, it discovers BCL's `ILegacyUIBackdrop` component and shares the same capture texture, without referencing the BCL or Blur DLL at compile time. Without BCL it remains a standalone framework example. This optional integration is also safe in the net35 example, where no compatible provider is normally present.

### Blur diagnostics

BCL enables blur diagnostics by default. Messages use `[BCL Blur]` and are written to `BepInEx/LogOutput.log` at Info level. The setting is in `BepInEx/config/com.bigcitylegacy.baseplugin.cfg`:

```ini
[UI]
BlurDiagnostics = true
```

Startup records the actual loaded framework/module paths and versions, diagnostic revision, game/Unity version and graphics backend. Further records identify the window/GUI event, missing provider, disabled window, camera selection and rendering eligibility, shader source/support/pass count, texture sizes, image-effect callbacks, viewport rejection and successful draws. A five-second health summary connects window repaint counts, draw requests, camera pre-render callbacks, received image callbacks and completed captures. In particular, `repaint-consumed-before-backdrop` distinguishes a Repaint event consumed before the background from an ordinary Layout pass.

Repeated identical states are suppressed; state changes are throttled per stage. The module does not read back pixels or save screenshots for diagnostics. Set `BlurDiagnostics = false` to disable these records. Other hosts may use `LegacyBclBlur.Create(host, logCallback, diagnosticsEnabled)`, `DiagnosticsEnabled` and `DiagnosticLog`. The framework itself only forwards optional `LegacyUIBackdropDiagnosticInfo` records to providers implementing `ILegacyUIBackdropDiagnostics`; it has no mandatory logger dependency.

To collect a report, keep the log from application startup, open About or VinylPort and leave it visible for 10–15 seconds before copying the log. The records describe the pipeline without changing blur settings or choosing a different camera automatically.

### HintBoxAlt visual options

`LegacyUIHintBox` provides persistent Visible/IsRendered/Opacity/Rect/Options state. LegacyUIHintBoxOptions exposes Theme, Animations (null inherits scoped/global settings), Backdrop, BackdropBlur, BlurTintAlpha, BackgroundAlpha, BackgroundColor, ScaleMultiplier, Padding, DrawBorder and InputBlockMode. Default color comes from the theme's HintBackgroundAlt. Blur uses implicit alpha 1; explicit BackgroundAlpha or a custom BackgroundColor alpha overrides it. BlurTintAlpha applies only after a successful backdrop draw. Content stays sharp; real-time fade affects the whole block. GUI color, enabled state, theme and scale are restored.

Existing `LegacyUI.HintBoxAlt(rect, text)` gains fade-in and the available backdrop. `LegacyUI.StyledHintBoxAlt(rect, text, options)` supplies explicit options. For fade-out, retain an instance and continue Draw after Visible=false until IsRendered=false. A static immediate call cannot fade out after calls stop. Account for ScaleMultiplier when positioning relative to virtual screen size. The core hint implementation also builds for net35; the blur module remains optional.

```csharp
var hint = new LegacyUIHintBox(new Rect(20, 20, 400, 90), new LegacyUIHintBoxOptions
{
    BackdropBlur = true,
    BlurTintAlpha = 0.75f,
    BackgroundAlpha = 1f,
    BackgroundColor = new Color(0.03f, 0.03f, 0.04f, 1f),
    ScaleMultiplier = 1.5f,
    Animations = new LegacyUIAnimationSettings { WindowDuration = 0.2f }
});
// In OnGUI, also after setting Visible = false:
if (hint.IsRendered) hint.Draw("Text");
// A callback allows custom content inside the padding:
hint.Draw("", area => LegacyUI.Label(area, "Custom content"));
```
