# LegacyUIFramework

`LegacyUIFramework.dll` 是一个面向 Unity Mono/BepInEx 插件的小型独立 IMGUI 库。
它提供独有的视觉样式、用于创建可拖动窗口和常用 UI 控件的可复用辅助工具，以及处理 Unity 版本差异的兼容性工具。

该框架作为 BigCityLegacy 的一部分开发，但不依赖主模组的功能逻辑，其他 BepInEx 插件和兼容的 Unity Mono 游戏也可以将其作为独立 DLL 使用。

## 项目

```text
src/LegacyUIFramework/LegacyUIFramework.csproj
src/LegacyUIFramework.net35/LegacyUIFramework.net35.csproj

src/LegacyUIFramework.Example/LegacyUIFramework.Example.csproj
src/LegacyUIFramework.Example.net35/LegacyUIFramework.Example.csproj
```

构建框架：

```bash
dotnet build .\src\LegacyUIFramework\LegacyUIFramework.csproj -c Debug -p:GameDir="path\to\game\dir"
```

构建示例插件并复制到 BepInEx：

```bash
dotnet build .\src\LegacyUIFramework.Example\LegacyUIFramework.Example.csproj -c Debug -p:GameDir="path\to\game\dir" -p:CopyToPlugins=true
```

对于自己的插件，请将 `LegacyUIFramework.dll` 放在插件 DLL 旁边，或放入由 BepInEx 加载的 BigCityLegacy 共享模块目录。

为了阻止输入传递到后台游戏，框架使用 `BepInEx/core` 中的 `0Harmony.dll`，以及 `game_Data/Managed` 中的 `UnityEngine.UI.dll` 和 `UnityEngine.UIModule.dll`。在常规 BigCityLegacy BepInEx 环境中运行插件时，无需复制额外的 DLL。

## 命名空间

```csharp
using BigCityLegacy.UI;
```

主要公共类：

```text
LegacyUI                 控件和样式的静态入口
LegacyUITheme            包含调色板、纹理和样式的主题对象
LegacyUIThemeScope       为控件组临时切换主题的作用域
LegacyUIPalette          可编辑调色板
LegacyUIWindow           可拖动的样式化窗口
LegacyUIWindowOptions    窗口选项
LegacyUIScrollView       手动或自动计算内容画布的可滚动区域
LegacyUIGuiScope         保存/恢复 Unity IMGUI 全局状态
LegacyUILayout           轻量级手动布局辅助工具
LegacyInput              Unity Legacy Input API 兼容包装器
```

## LegacyInput — 兼容的 Legacy Input API

`LegacyInput` 面向需要通过同一次构建兼容不同 Unity 版本的 BepInEx 插件。旧版 Unity 的 `UnityEngine.Input` 位于单体程序集 `UnityEngine.dll` 中；新版则由独立的 `UnityEngine.InputLegacyModule.dll` 提供 Legacy Input 实现。因此，直接针对 `Input.GetKeyDown(...)` 编译的插件，其程序集引用可能会绑定到编译时所选 Unity 参考程序集的布局。

`LegacyInput` 不在编译时引用 `UnityEngine.Input`。在 `Init()` 中，它通过反射在 Unity 已加载的程序集中查找 `UnityEngine.Input` 类型，为支持的方法创建委托，然后使用缓存的委托，避免每帧重复进行反射查找。

在 `Awake()` 中显式初始化一次：

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

可以安全地多次调用 `Init()`：第一次初始化尝试后，会复用缓存结果。如果尚未调用 `Init()`，包装方法也可以延迟初始化；不过，建议在 `Awake()` 中显式调用，以便在插件生命周期的确定阶段创建委托。

可以通过以下属性检查包装器状态：

```csharp
bool initialized = LegacyInput.Initialized;
bool available = LegacyInput.Available;
```

`Available` 表示已找到 `UnityEngine.Input`，且至少成功将字符串重载 `GetKey(string)` 绑定到委托。在某些特殊 Unity 构建中，个别 API 可能不可用；对应的包装方法会返回 `false` 或 `0f`。

支持的方法和属性：

```csharp
// 键盘
bool held = LegacyInput.GetKey("f10");
bool down = LegacyInput.GetKeyDown("f10");
bool up   = LegacyInput.GetKeyUp("f10");

// 鼠标
bool mouseHeld = LegacyInput.GetMouseButton(0);
bool mouseDown = LegacyInput.GetMouseButtonDown(0);
bool mouseUp   = LegacyInput.GetMouseButtonUp(0);
Vector3 mousePosition = LegacyInput.MousePosition;

// Input Manager 按钮
bool fire     = LegacyInput.GetButton("Fire1");
bool fireDown = LegacyInput.GetButtonDown("Fire1");
bool fireUp   = LegacyInput.GetButtonUp("Fire1");

// Input Manager 输入轴
float horizontal = LegacyInput.GetAxis("Horizontal");
float raw         = LegacyInput.GetAxisRaw("Horizontal");

// 其他
bool anyKey = LegacyInput.AnyKey;
bool anyKeyDown = LegacyInput.AnyKeyDown;
```

键盘方法使用标准 `UnityEngine.Input.GetKey(string)` API 支持的字符串按键名称。`GetButton*` 和 `GetAxis*` 使用的名称必须实际存在于目标游戏的 Input Manager 配置中。

> [!IMPORTANT]
> `LegacyInput` 解决的是 `UnityEngine.Input` 位于不同程序集时的**程序集兼容性**问题，并不是 Unity 新 Input System 的实现。它要求游戏中存在且可用的 Legacy Input Manager（`UnityEngine.Input`）。如果现代 Unity 项目仅使用 `Active Input Handling = Input System Package (New)` 构建，Legacy Input 可能被禁用；当前包装器无法替代该输入后端。

## Unity 版本兼容性

当前版本的最低兼容版本主要由**参考程序集布局和 .NET 目标框架**决定。

主项目 `LegacyUIFramework`：

- 目标框架为 `net472`；生成的 DLL 适用于具有兼容 .NET 4.x API 的 Unity Mono 运行时；
- `.csproj` 直接引用模块化程序集 `UnityEngine.CoreModule.dll`、`UnityEngine.IMGUIModule.dll`、`UnityEngine.TextRenderingModule.dll` 和 `UnityEngine.UIModule.dll`；Unity 从 2017.2 开始正式将 `UnityEngine.dll` 拆分为模块；
- `LegacyUIInputBlocker` 使用 uGUI（`UnityEngine.UI.dll`、`Canvas`、`GraphicRaycaster`、`Image`），因此框架在运行时依赖 Unity UI/uGUI；
- 框架面向 **Mono** Unity/BepInEx，不支持 IL2CPP。

`LegacyUIFramework.net35` 是独立的构建配置，适用于使用 .NET Framework 3.5 配置文件及旧式单体 Unity 程序集布局的 Unity Mono 游戏。

- 目标框架：`net35`；
- Unity API 引用自单体程序集 `UnityEngine.dll`；
- uGUI 引用自 `UnityEngine.UI.dll`；
- 两种构建编译相同的框架源文件，以保持公共 API 一致。

实际兼容范围如下：

```text
Unity 2019.2 — Unity 6, Mono, .NET 4.x-compatible runtime
    当前构建的主要推荐范围

Unity 2017.2 — 2019.1
    当具体游戏使用 .NET 4.x / .NET 4.6 Equivalent 时有条件兼容
    且游戏包含所需的 UnityEngine 模块和 UnityEngine.UI

Unity 4.6 — 2017.1, Mono, legacy .NET 3.5 profile
    由独立的 LegacyUIFramework.net35 项目提供支持；
    使用单体 UnityEngine.dll + UnityEngine.UI.dll
```

> [!NOTE]
> `src/LegacyUIFramework/LegacyUIFramework.csproj` 仍是标准的 `net472` 构建。
> `src/LegacyUIFramework.net35/LegacyUIFramework.net35.csproj` 是针对旧版单体 Unity 程序集的替代 `net35` 构建。

对于 Unity 2017.2 及更高版本，建议使用标准 LegacyUIFramework 项目，因为这些版本已采用模块化的 `UnityEngine.*Module.dll` 程序集。某些较新的游戏仍可能适合使用 `net35` 构建，但应使用与该游戏具体 Unity 版本匹配的引用。

## 最小窗口示例

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

## 窗口选项

`LegacyUIWindowOptions` 当前支持以下选项：

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

启用 `FadeWhileDragging` 后，拖动标题栏时，窗口及其所有子 IMGUI 元素都会以较低的不透明度绘制。

`BackgroundAlpha` 只改变窗口背景的不透明度。如果为 `null`，则使用当前主题中的不透明度。

`Theme` 为可选项。设置后，只有该窗口及其内容使用指定主题绘制，不会改变框架的全局主题。

`InputBlockMode` 控制窗口在绘制自身控件后如何拦截鼠标事件：

```text
None   = 0 — 不拦截后台点击
Window = 1 — 仅拦截窗口矩形内的点击；这是默认值
Screen = 2 — 窗口可见时拦截屏幕任意位置的鼠标点击
```

设置 `PlayerPrefsKey` 后，框架会保存窗口的位置、大小和可见状态：

```text
<key>.x
<key>.y
<key>.w
<key>.h
<key>.visible
```

## 控件

框架为常用的样式化控件提供了包装方法：

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

滑块和滚动条辅助方法采用自定义绘制，不依赖修改 `GUI.skin`，因此多个插件可以安全地使用框架，而不会争用全局皮肤状态。

`TextField` 和 `TextArea` 自动应用当前主题的文本编辑颜色：插入光标使用 `Palette.TextBright`，选区使用降低不透明度的 `Palette.Active`。Unity 将这些值存储在 `GUI.skin.settings` 中，因此框架只在调用单个文本控件期间临时覆盖它们，并立即恢复原值。该机制适用于全局主题、`LegacyUI.WithTheme(...)`、窗口主题和单个控件的主题重载，不会永久修改全局 `GUI.skin`。

## ScrollView

`LegacyUIScrollView` 是独立的可滚动区域。`ViewRect` 定义包含滚动条的可见区域，`ContentRect` 则定义区域内部的虚拟画布。

单参数构造函数启用自动内容大小计算。每次绘制时，框架会根据回调中绘制的 `LegacyUI` 控件及 `Padding` 重新计算画布大小，适合动态列表：

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

在自动模式下，`ContentRect` 至少与可见内容视口一样大，并根据当前 IMGUI 绘制过程中绘制的控件扩大或缩小。所有标准 `LegacyUI` 辅助方法都会自动参与测量。如果直接通过 `GUI.*` 绘制控件，请在回调中手动注册其矩形：

```csharp
Rect custom = new Rect(content.x, content.y + 40f, 500f, 24f);
GUI.Button(custom, "Raw IMGUI button");
scrollView.IncludeContentRect(custom);
```

要使用固定虚拟画布，请向第二个构造函数传入内部矩形，或为 `ContentRect` 赋值。给 `ContentRect` 赋值会关闭 `AutoContentSize`：

```csharp
LegacyUIScrollView map = new LegacyUIScrollView(
    new Rect(20f, 20f, 320f, 220f),
    new Rect(0f, 0f, 900f, 700f));

map.Draw(delegate(Rect content)
{
    LegacyUI.Button(new Rect(content.x + 600f, content.y + 400f, 140f, 26f), "Far button");
});
```

调用 `UseAutoContentSize()` 可恢复动态大小计算；调用 `ResetScroll()` 可返回左上角。`ShowHorizontalScrollbar` 和 `ShowVerticalScrollbar` 控制在哪些轴上预留空间并绘制框架样式的滚动条。每个实例都可以单独调整 `ScrollbarSize`、`ScrollbarSpacing`、`DrawBackground`、`BackgroundAlpha`、`ScrollPosition` 和 `Theme`。

> [!WARNING]
> **注意：ScrollView 附近 IMGUI 元素的调用顺序**
>
> 与 `LegacyUIFramework` 的其他元素一样，`LegacyUIScrollView` 基于 Unity IMGUI。IMGUI 交互控件的状态与内部 `control ID` 关联；Unity 分配这些 ID 时，部分依据是 **`OnGUI()` 中 GUI 元素的调用顺序**。
>
> 因此，如果 `ScrollView` 中的**元素数量会动态变化**，而代码在其后调用 `TextField`、`TextArea` 或其他使用键盘/鼠标焦点的有状态控件，就需要特别注意。
>
> 例如，以下顺序可能导致问题：
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
> 如果 `ScrollView` 中的元素数量发生变化，在 `TextField` 之前创建的 IMGUI 控件数量也会改变。在某些 Unity 版本中，这可能导致输入框的内部 `control ID` 在不同 GUI 事件（`Layout`、`MouseDown`、`KeyDown`、`Repaint`）之间发生变化。
>
> 因此，外观正常的 `TextField` 可能出现以下异常：
>
> * 无法获得键盘焦点，或失去焦点；
> * 不显示插入光标；
> * 无法用鼠标选择文本；
> * 丢失当前光标位置；
> * 无法正确响应点击或键盘输入。
>
> **建议：**在 **`ScrollView` 的动态内容之前**调用需要稳定 IMGUI 状态的交互元素。
>
> ```csharp
> Rect scrollRect = ...;
> Rect textRect = ...;
>
> // 先绘制有状态控件。
> text = LegacyUI.TextField(textRect, text);
>
> // 然后绘制动态控件。
> scroll.ViewRect = scrollRect;
> scroll.Draw(content =>
> {
>     for (int i = 0; i < dynamicItems.Count; i++)
>         LegacyUI.Button(...);
> });
> ```
>
> IMGUI 元素的调用顺序**不必与视觉布局一致**。元素的位置由传入的 `Rect` 决定，因此即使 `TextField` 在界面上位于 `ScrollView` 下方，也可以先调用它。
>
> 尤其建议对 `TextField`、`TextArea` 和其他由 Unity 在 GUI 事件之间保留状态的控件采用该顺序。
>
> 此外，除非确有必要，应避免将标准 IMGUI 输入与 `GUI.FocusControl`、自行读取 `Input.inputString` 或独立的光标/选区处理逻辑混合使用。`LegacyUI.TextField` 和 `LegacyUI.TextArea` 设计为使用 Unity IMGUI 的标准焦点机制。
>
> `LegacyUIScrollView` 本身可以包含任意数量的动态 `LegacyUI` 元素。这里的限制具体涉及**位于动态变化内容之后的控件，其 IMGUI 调用顺序必须保持稳定**。

## 样式与主题

需要时可以直接访问默认样式：

```csharp
GUI.Label(rect, "Custom label", LegacyUI.Styles.Label);
GUI.Button(rect, "Custom", LegacyUI.Styles.Button);
```

可用的样式属性包括：

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

为一个插件全局自定义颜色：

```csharp
LegacyUIPalette palette = new LegacyUIPalette();
palette.Active = new Color(0.20f, 0.45f, 0.80f, 1f);
palette.WindowBackground = new Color(0.04f, 0.04f, 0.05f, 0.88f);
LegacyUI.SetTheme(new LegacyUITheme(palette));
```

全局主题可以作为插件的默认主题；也可以为单个窗口、控件组或单个控件使用临时主题，而不改变全局状态。

只需创建一次可复用主题，例如在 `Awake()` 中：

```csharp
LegacyUIPalette bluePalette = new LegacyUIPalette();
bluePalette.Active = new Color(0.16f, 0.42f, 0.82f, 1f);
bluePalette.WindowBackground = new Color(0.04f, 0.05f, 0.08f, 0.88f);
LegacyUITheme blueTheme = new LegacyUITheme(bluePalette);
```

可以安全地在 `Awake()` 或其他非 `OnGUI` 的初始化代码中创建 `LegacyUITheme`。纹理和样式会在实际绘制首次需要它们时延迟创建。如果在主题已经绘制过之后修改调色板，请调用 `theme.Invalidate()`，或通过 `theme.SetPalette(...)` 替换调色板。

为整个窗口使用一个主题：

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

为一组控件使用限定作用域的主题：

```csharp
using (LegacyUI.WithTheme(blueTheme))
{
    LegacyUI.Panel(rect);
    LegacyUI.Title(titleRect, "Blue group");
    LegacyUI.Button(buttonRect, "Button");
}
```

也可以只向单个控件传入主题：

```csharp
LegacyUI.Button(rect, "Blue button", blueTheme);
LegacyUI.HorizontalSlider(rect, value, 0f, 1f, blueTheme);
LegacyUI.Panel(rect, 0.70f, blueTheme);
```

主题作用域基于栈，因此允许嵌套。请在创建作用域的同一次 `OnGUI` 调用中释放它。

## 示例插件

`LegacyUIFramework.Example` 绘制的窗口包含：

```text
TextField
TextArea
Button
GreenButton
DangerButton
TabButton
Toggle
HintBox
包含自动计算大小的动态文件列表的 ScrollView
通过 ScrollView 使用 HorizontalScrollbar / VerticalScrollbar
关闭按钮
拖动时降低不透明度
输入拦截模式切换
主题作用域、窗口主题和单个控件主题示例
```

该插件仅用于演示用法，框架本身不依赖它。

## BigCityLegacy 集成规则

不要将大型可选 UI 辅助工具移回 BigCityLegacy 主插件。包含大量 UI 的新插件应引用 `LegacyUIFramework.dll`，并将自身的功能逻辑保留在独立程序集中。

---

## 1.1.0 新增：缩放、Dropdown 和 Counter

### IMGUI 缩放

在 `LegacyUIGuiScope` 内绘制的 UI **默认启用缩放**。继续使用普通设计像素单位的 `Rect` 坐标、窗口大小、`LegacyUILayout` 和 `LegacyUIScrollView`；框架通过 `GUI.matrix` 应用缩放变换。**不要**自行将矩形坐标乘以缩放系数。

默认模式为 `Automatic`：`min(Screen.width / 1920, Screen.height / 1080) × BaseScale`，结果限制在 `MinScale..MaxScale` 范围内（默认 0.75–4）。当 `BaseScale=1` 时：1920×1080 → 1×，2560×1440 → 1.333×，3840×2160 → 2×。如需在 UHD 下使用 3×，请设置 `BaseScale=1.5`。

建议**每个插件**使用独立设置对象，因为多个插件可能共享同一个框架程序集：

```csharp
private readonly LegacyUIScaleSettings uiScale = new LegacyUIScaleSettings();

private void OnGUI()
{
    using (new LegacyUIGuiScope(-10000, uiScale))
        window.Draw(DrawContent);
}

uiScale.Mode = LegacyUIScaleMode.Automatic;
uiScale.BaseScale = 1.5f;

// 也可以在任何屏幕上使用固定缩放系数：
uiScale.Mode = LegacyUIScaleMode.Manual;
uiScale.ManualScale = 1.25f;
```

未显式指定设置对象时，`LegacyUIGuiScope(-10000)` 使用共享的 `LegacyUI.ScaleSettings`（默认自动缩放）。`LegacyUI.Scale` 返回当前缩放系数；`LegacyUI.VirtualScreenSize` 提供设计像素单位的屏幕尺寸。`LegacyUI.MousePosition` 用于自定义鼠标命中检测。可拖动窗口和游戏输入拦截会考虑当前变换。

`LegacyUIWindowOptions.ScaleMultiplier` 可缩放单个窗口（默认 1×）。对于单个元素或一组元素，使用：

```csharp
using (LegacyUI.WithScale(1.2f, new Vector2(buttonRect.x, buttonRect.y)))
    LegacyUI.Button(buttonRect, "Large button");
```

`WithScale` **相对于**当前缩放生效；带 `pivot` 的重载会保持所选点的位置。`Dispose()` 始终恢复此前的矩阵。布局相邻控件时，请考虑放大后控件的视觉边界。

请在 `Awake` 或字段中保留持久主题和有状态控件，**不要**在 `OnGUI` 内创建它们，以避免内存分配和原生纹理泄漏。

### LegacyUIDropdown

这是使用 `LegacyUIScrollView` 的有状态下拉控件：`PopupWidth` 控制展开弹出列表的宽度（0 表示与按钮同宽），`MaxHeight` 限制高度，超出的项目可以垂直滚动。通过 `SelectedIndex` 和 `SelectionChanged` 获取选择状态及变化。

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
    dropdown.HandlePopupInput(); // 在底层 UI 之前调用，防止点击穿透
    window.Draw(delegate(Rect area)
    {
        dropdown.Draw(new Rect(area.x, area.y + 60f, 210f, 27f));
    });
    dropdown.DrawPopup(); // 最后绘制，并放在 GUI.BeginScrollView 之外
}
```

弹出列表必须在与锚点相同的根 GUI 坐标空间中**最后绘制**，并位于带裁剪的 `GUI.BeginGroup`/`GUI.BeginScrollView` 区域之外。对于现有 ScrollView 内的下拉控件，需要将锚点坐标转换到根 GUI 坐标。如果其他交互控件可能与弹出列表重叠，应先调用 `HandlePopupInput`。在不同 IMGUI 事件之间复用同一个下拉控件实例。

### Counter

**无状态整数步进控件：**

```csharp
quantity = LegacyUI.Counter(new Rect(15, 20, 200, 28), quantity,
                            step: 5, minValue: 0, maxValue: 100);
```

**有状态、可编辑的浮点数步进控件：**

```csharp
// Awake:
boost = new LegacyUICounter(1.5f, 0.25f, 0f, 10f);
boost.NumberFormat = "0.00";

// OnGUI:
float selectedBoost = boost.Draw(new Rect(15, 60, 210, 28));
```

`Step` 决定递增/递减幅度。`MinValue`/`MaxValue` 对输入和按钮操作进行范围限制。输入数字使用固定区域性格式（小数点为句点）；设置 `AllowTyping=false` 可使中间的数值只读。可选的 `Theme` 改变计数器外观。结合 `WithScale` 可以放大控件或控件组。

**IMGUI 控件 ID 注意事项：**在 `Layout`、`Repaint` 和输入事件之间，保持有状态控件的调用顺序一致。在文本输入框*之前*绘制数量可变的 ScrollView 按钮，可能破坏焦点和光标处理。请先绘制 `TextField` / `LegacyUICounter`，再绘制内容数量动态变化的 ScrollView；`Rect` 独立决定视觉位置，与调用顺序无关。

## 1.1.1 更新 — 缩放鼠标输入与 Counter 文本编辑

- `LegacyUI.MousePosition` 现在直接返回当前 `Event.current.mousePosition`，不再重复应用 `GUI.matrix.inverse`。自定义 IMGUI 命中检测（窗口拖动、滑块、滚动条和下拉控件）必须使用 GUI 空间的事件坐标。此前的额外转换会在非 1× 缩放下导致命中区域偏移。原始 `LegacyInput.MousePosition` 仍是以左下角为原点的物理屏幕输入，不适合在裁剪区域内直接用于 GUI `Rect.Contains` 检测。
- `LegacyUICounter.Draw` 保留独立于已提交 `Value` 的**草稿字符串**。编辑时允许任意中间文本，包括 `-`、`1.` 和空字符串。**只有**在按 Enter、GUI 焦点变化或用户在输入框外单击鼠标左键时，才会解析数值并限制范围。`1.25` 和 `1,25` 均可接受。无效输入会恢复上次提交的值。
- Counter 先绘制文本输入框，再绘制按钮，以稳定 IMGUI 控件 ID；文本编辑器拥有焦点期间绝不重新格式化草稿，从而保留插入光标和选区。
- 示例插件的缩放滑块会在释放鼠标后应用更改，因此拖动过程中滑块自身的变换不会移动。`Reset 1x` 和 `F8` 可恢复手动 1× 缩放。

```csharp
// 只创建一次状态，不要在 OnGUI 内创建。
counter = new LegacyUICounter(1.5f, 0.25f, 0f, 10f);

// 草稿编辑在提交前不会改变 Value。
float confirmedValue = counter.Draw(new Rect(20f, 20f, 180f, 28f));
```

**注意：**Unity IMGUI 要求在 `Layout`、`Repaint`、`MouseDown` 和 `KeyDown` 之间保持 GUI 控件顺序一致；请保留 Counter 实例，并避免在编辑时动态改变此前控件的数量。
