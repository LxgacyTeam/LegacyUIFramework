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

`BackgroundAlpha` 设置窗口背景的基础不透明度。若为 `null`，在 `BackdropBlur = true` 时使用 `1f`，禁用模糊时使用当前主题的不透明度。显式设置的值（包括 `0f`）保持不变。`BlurTintAlpha` 默认为 `0.75f`，仅在成功绘制模糊图像时乘以背景不透明度；否则使用基础不透明度。默认值在绘制时计算，因此切换 `BackdropBlur` 不会覆盖 `BackgroundAlpha`。

隐式的 `1f` 和 `BlurTintAlpha` 乘数仅作用于窗口自身的背景。窗口未显式设置 `BackgroundAlpha` 时，子面板（`LegacyUI.Panel` 和 `LegacyUIScrollView` 背景）使用各自当前主题的不透明度。窗口显式设置的 `BackgroundAlpha` 会被继承，但不包含模糊乘数；面板或 ScrollView 自己的 `BackgroundAlpha` 优先。继承范围仅限于 `window.Draw`：嵌套窗口使用自己的上下文，返回或异常时恢复之前的上下文。打开及拖动动画仍作用于窗口的全部内容。

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

`LegacyUIScrollView` 背景使用 `Mathf.Clamp01(alpha) * 0.75f`，即将基础不透明度降低 25%。优先使用 ScrollView 自己的 `BackgroundAlpha`，其次使用所在窗口显式设置的 `BackgroundAlpha`，否则使用 ScrollView 当前主题的 `WindowBackground.a`。例如，`0.8f` 得到 `0.6f`，主题值 `0.85f` 得到 `0.6375f`。该乘数仅影响 ScrollView 背景，不影响内容、边框或滚动条；设置和调色板保持不变。

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

## 动画

动画默认启用：窗口和下拉菜单淡入淡出，拖动时平滑过渡到 `DragAlpha`，Toggle 状态和控件悬停效果也平滑变化。持续时间使用真实秒数，因此 `Time.timeScale = 0` 不会暂停动画。持续时间为零会禁用对应过渡，`Enabled = false` 会禁用该设置对象的全部动画。

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

`LegacyUI.WithAnimations(settings)` 为一组控件指定设置。`LegacyUIWindowOptions.Animations` 可覆盖插件设置；null 表示继承。`LegacyUIDropdown.Animations` 默认继承锚点的设置。离开作用域时会恢复 GUI 和动画设置，包括发生异常时。

`Visible` 表示请求的逻辑可见状态。每次 OnGUI 都应调用 `window.Draw(...)`，或使用 `window.IsRendered` 判断，以便淡出完成。不要在 `Closed` 回调中立即禁用 MonoBehaviour，应等待 `IsRendered == false`。`Closed` 仍立即触发，保存的可见性也是逻辑状态。淡出期间继续绘制内容，但禁用交互。`Opacity` 返回当前窗口透明度。下拉菜单保存锚点的变换、主题和透明度；在窗口前调用 `HandlePopupInput()`，在滚动裁剪外最后调用 `DrawPopup()`。

## 进度条

`LegacyUI.ProgressBar(rect, value, min, max, label)` 显示测量值。持久化的 `LegacyUIProgressBar` 实例可通过 `ValueProvider`（`Func<double>`）绑定数值；未提供回调时使用 `Value`。`Min` 和 `Max` 可以逐帧更新。回调在 GUI 线程执行。数值限制在范围内，支持反向范围；边界相等或数值无效时显示空条。Double 支持较大的下载字节数。控件不接收输入，并参与 ScrollView 自动内容尺寸计算。

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

主题中的 `LegacyUIPalette.ProgressFill` 和 `ProgressTrack` 分别设置填充色和背景色，与按钮及滑块颜色独立。默认标签显示百分比，`ShowPercentage = false` 隐藏标签，`Label` 可指定文本。动画及进度条同时支持主版本和 net35 版本。

## MadOut2 专用背景模糊模块

`src/LegacyUIFramework.Blur/LegacyUIFramework.Blur.csproj` 生成可选的 **LegacyUIFramework.Blur.dll**，仅支持使用 Built-in Render Pipeline 的 **MadOut2 9.4**，没有 net35 版本。基础库只提供 `ILegacyUIBackdrop` 接口，无需该模块也能使用。BigCityLegacy 只在受支持的客户端创建此模块，专用或无图形服务器不会创建它。

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

模块使用真实的 `Cam.me.unityCam`，并以 Camera.main 为备用。附加的 image effect 在 IMGUI 绘制前获取场景，同时原样传递相机输出。主场景相机输出到 `MainRenderTexture` 时也可用于捕获：GameUI 将该纹理铺满屏幕，因此窗口裁剪使用屏幕尺寸，不受场景渲染比例影响。其他离屏相机不参与选择。直接输出到屏幕时仍按相机 viewport 映射。它引用游戏原生 BlurBehind 着色器，在独立材质中使用 pass 0 与 `_Parameter`，不修改原生 Canvas BlurBehind 的启用状态、设置或全局着色器参数。恢复项目中的着色器源码是重建占位内容，不会替换游戏中的着色器。

所有窗口共享一张低分辨率纹理，默认宽高各缩小四倍。可配置 `Downsample`（1–8）、`Iterations`（0–4）和 `Radius`（0–8）。窗口裁剪考虑屏幕坐标、UI 缩放、相机 viewport 和 Y 方向。更改分辨率或相机时重建资源，空闲、禁用或销毁时释放资源。没有适用相机/着色器或窗口位于 viewport 外时，使用普通主题背景。`BlurTintAlpha` 只在模糊图像可用时乘以窗口背景透明度，窗口内容保持清晰。

### 拖动状态与相机输入

`window.isDragging` 与 `window.IsDragging` 是同一个只读状态。左键捕获标题栏后为 true，松开左键、关闭窗口、收到 Ignore 事件、禁用 `Draggable` 或检测到鼠标在 GUI 外松开时恢复 false。松开其他鼠标键不会取消左键拖动。此状态与 DragAlpha 动画独立，透明度恢复期间不会保持 true。

`window.IsDragHandleAtScreenPoint(point)` 使用最近绘制的标题栏区域进行命中检测，输入为实际屏幕像素坐标，原点与 Unity `Input.mousePosition` 一样位于左下角。它考虑 GUI/窗口缩放并排除关闭按钮。宿主可在 OnGUI 捕获拖动之前排除初始按下，再通过 `isDragging` 在整个拖动期间阻止相机旋转。查询不会创建窗口实例。

### BCL 与示例插件

BCL 的 About、Server Connection、玩家档案、更新器、VinylPort 及其确认窗口使用模糊背景和更透明的底色；性能叠加层也共享该背景。PointTool 明确禁用模糊。提供者不可用时保留普通背景。

Example 插件对 BCL 使用软依赖。检测到 BCL 时，从其对象获取 `ILegacyUIBackdrop` 组件，共享同一个捕获纹理，无需在编译时直接引用 BCL 或 Blur DLL。没有 BCL 时仍可独立运行。net35 示例也保留此可选集成，但通常不存在兼容的提供者。

### 模糊诊断

BCL 默认启用模糊诊断。消息前缀为 `[BCL Blur]`，以 Info 级别写入 `BepInEx/LogOutput.log`。配置位于 `BepInEx/config/com.bigcitylegacy.baseplugin.cfg`：

```ini
[UI]
BlurDiagnostics = true
```

启动记录包含实际加载的框架/模块 DLL 路径与版本、诊断修订号、游戏/Unity 版本及图形后端。后续记录显示窗口及 GUI 事件、提供者缺失、窗口禁用模糊、相机选择及渲染条件、着色器来源/支持状态/pass 数量、纹理尺寸、image effect 回调、viewport 越界和绘制成功状态。每五秒的摘要关联窗口 Repaint、背景请求、相机 pre-render、image 回调及完成的捕获数量。`repaint-consumed-before-backdrop` 专门表示 Repaint 在背景绘制前已被消费，而不是普通 Layout。

相同状态不会逐帧重复，状态变更也按阶段限流。诊断不会读取 GPU 像素或保存截图。设置 `BlurDiagnostics = false` 可关闭这些记录。其他宿主可使用 `LegacyBclBlur.Create(host, logCallback, diagnosticsEnabled)`、`DiagnosticsEnabled` 和 `DiagnosticLog`。基础库仅向实现 `ILegacyUIBackdropDiagnostics` 的提供者发送可选的 `LegacyUIBackdropDiagnosticInfo`，无需依赖特定日志系统。

收集报告时，保留从程序启动开始的日志，打开 About 或 VinylPort 并保持可见 10–15 秒后再复制日志。记录不会修改模糊设置或自动切换相机。

### HintBoxAlt 视觉选项

`LegacyUIHintBox` 提供持续保存的 Visible、IsRendered、Opacity、Rect、Options 状态。LegacyUIHintBoxOptions 包含 Theme、Animations（null 继承作用域/全局）、Backdrop、BackdropBlur、BlurTintAlpha、BackgroundAlpha、BackgroundColor、ScaleMultiplier、Padding、DrawBorder、InputBlockMode。默认颜色来自主题 HintBackgroundAlt；启用模糊时隐式 alpha 为 1，显式 BackgroundAlpha 或自定义 BackgroundColor 的 alpha 优先。BlurTintAlpha 仅在成功绘制模糊时作用于背景。内容保持清晰，fade 使用真实时间作用于整体，绘制后恢复 GUI 颜色、启用状态、主题与缩放。

原有 `LegacyUI.HintBoxAlt(rect, text)` 支持 fade-in 与可用的模糊提供者；`LegacyUI.StyledHintBoxAlt(rect, text, options)` 可显式传入设置。需要 fade-out 时保留实例，在 Visible=false 后继续 Draw，直到 IsRendered=false；静态即时调用停止后无法继续绘制淡出。按 virtual screen 定位时须考虑 ScaleMultiplier。核心实现也支持 net35，模糊模块仍为可选。

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
