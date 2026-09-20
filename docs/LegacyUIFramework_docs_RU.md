# LegacyUIFramework

`LegacyUIFramework.dll` — небольшая отдельная IMGUI-библиотека для Unity Mono/BepInEx-плагинов.
Она содержит собственный визуальный стиль, переиспользуемые helpers для перетаскиваемых окон и базовых UI-элементов, а также compatibility helpers для различий между версиями Unity.

Framework разрабатывается в рамках BigCityLegacy, но не зависит от feature-логики основного мода и может использоваться как отдельная DLL в других BepInEx-плагинах и играх с совместимым Unity Mono runtime.

## Проекты

```text
src/LegacyUIFramework/LegacyUIFramework.csproj
src/LegacyUIFramework.net35/LegacyUIFramework.net35.csproj

src/LegacyUIFramework.Example/LegacyUIFramework.Example.csproj
src/LegacyUIFramework.Example.net35/LegacyUIFramework.Example.csproj
```

Сборка framework:

```bash
dotnet build .\src\LegacyUIFramework\LegacyUIFramework.csproj -c Debug -p:GameDir="path\to\game\dir"
```

Сборка и копирование example-плагина в BepInEx:

```bash
dotnet build .\src\LegacyUIFramework.Example\LegacyUIFramework.Example.csproj -c Debug -p:GameDir="path\to\game\dir" -p:CopyToPlugins=true
```

Для своих плагинов скопируйте `LegacyUIFramework.dll` рядом с DLL плагина или в общую папку модулей BigCityLegacy, которую загружает BepInEx.

Для режима блокировки фонового ввода framework использует `0Harmony.dll` из `BepInEx/core` и `UnityEngine.UI.dll` + `UnityEngine.UIModule.dll` из `game_Data/Managed`. Дополнительные DLL копировать не нужно, если плагин работает внутри обычного BepInEx-окружения BigCityLegacy.

## Namespaces

```csharp
using BigCityLegacy.UI;
```

Основные публичные классы:

```text
LegacyUI                 static facade для controls и styles
LegacyUITheme            объект темы с palette, textures и styles
LegacyUIThemeScope       временное переключение темы для группы controls
LegacyUIPalette          редактируемая цветовая palette
LegacyUIWindow           перетаскиваемое стилизованное окно
LegacyUIWindowOptions    модификаторы окна
LegacyUIScrollView       scrollable area с ручным или автоматическим внутренним полотном
LegacyUIGuiScope         сохраняет/восстанавливает глобальное состояние Unity IMGUI
LegacyUILayout           маленький helper для ручной fixed-layout вёрстки
LegacyInput              compatibility wrapper для Unity Legacy Input API
```

## LegacyInput — совместимый Legacy Input API

`LegacyInput` предназначен для BepInEx-плагинов, одна сборка которых должна работать на разных поколениях Unity. В старых версиях движка класс `UnityEngine.Input` находился в монолитном `UnityEngine.dll`, а в более новых версиях Legacy Input вынесен в отдельный модуль `UnityEngine.InputLegacyModule.dll`. Если плагин напрямую компилируется с `Input.GetKeyDown(...)`, его assembly reference может оказаться привязан к конкретной структуре Unity assemblies reference-build-а.

`LegacyInput` не имеет compile-time ссылки на `UnityEngine.Input`. Во время `Init()` wrapper ищет тип `UnityEngine.Input` среди уже загруженных assemblies через reflection, создаёт delegates для поддерживаемых методов и затем использует их без повторного reflection lookup на каждом кадре.

Рекомендуется один раз явно инициализировать wrapper в `Awake()`:

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

`Init()` безопасно вызывать повторно: после первой попытки инициализации используется закэшированный результат. Методы wrapper-а также умеют лениво вызвать `Init()`, если он ещё не выполнялся, однако явный вызов в `Awake()` предпочтителен — delegates создаются в заранее определённый момент жизненного цикла плагина.

Состояние wrapper-а можно проверить через:

```csharp
bool initialized = LegacyInput.Initialized;
bool available = LegacyInput.Available;
```

`Available` означает, что `UnityEngine.Input` найден и как минимум строковый `GetKey(string)` удалось привязать к delegate. Отдельные API могут отсутствовать в необычной версии/сборке Unity; в таком случае соответствующие методы возвращают `false` или `0f`.

Поддерживаемые методы и свойства:

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

Для keyboard API используются строковые имена клавиш, поддерживаемые стандартным `UnityEngine.Input.GetKey(string)`. `GetButton*` и `GetAxis*` работают с именами, реально определёнными в Input Manager конкретной игры.

> [!IMPORTANT]
> `LegacyInput` решает **assembly compatibility** между вариантами размещения `UnityEngine.Input`, но не является реализацией нового Unity Input System. Он требует, чтобы Legacy Input Manager (`UnityEngine.Input`) присутствовал и был доступен в игре. В современных Unity-проектах, собранных только с `Active Input Handling = Input System Package (New)`, Legacy Input может быть отключён; такой backend текущий wrapper не подменяет.

## Совместимость с версиями Unity

Нижний предел текущей версии определяется **структурой reference assemblies и .NET target framework проекта**.

Основной `LegacyUIFramework`:

- проект собирается как `net472`; готовая DLL рассчитана на Unity Mono runtime с совместимым .NET 4.x API;
- `.csproj` напрямую ссылается на модульные `UnityEngine.CoreModule.dll`, `UnityEngine.IMGUIModule.dll`, `UnityEngine.TextRenderingModule.dll` и `UnityEngine.UIModule.dll`; такая модульная структура UnityEngine официально появилась в Unity 2017.2;
- `LegacyUIInputBlocker` использует uGUI (`UnityEngine.UI.dll`, `Canvas`, `GraphicRaycaster`, `Image`), поэтому наличие Unity UI/uGUI является runtime-зависимостью framework;
- framework рассчитан на **Mono**-вариант Unity/BepInEx. IL2CPP в текущем виде не поддерживается.


`LegacyUIFramework.net35`- отдельная сборочная конфигурация LegacyUIFramework для старых Unity Mono игр, использующих профиль .NET Framework 3.5 и старую монолитную структуру Unity assemblies.

- target framework: `net35`;
- Unity API берётся из монолитного `UnityEngine.dll`;
- uGUI берётся из `UnityEngine.UI.dll`;
- исходники UI общие с основной сборкой, поэтому API двух вариантов не расходится.


Практически диапазон удобно считать так:

```text
Unity 2019.2 — Unity 6, Mono, .NET 4.x-compatible runtime
    основной рекомендуемый диапазон для текущей сборки

Unity 2017.2 — 2019.1
    условно совместимо, если конкретная игра собрана на .NET 4.x

Unity 4.6 — 2017.1, Mono, .NET 3.5 runtime
    поддерживается отдельным проектом LegacyUIFramework.net35;
    используются monolithic UnityEngine.dll + UnityEngine.UI.dll
```

> [!NOTE]
> `src/LegacyUIFramework/LegacyUIFramework.csproj` остаётся основной `net472`-сборкой
> `src/LegacyUIFramework.net35/LegacyUIFramework.net35.csproj` — альтернативная `net35`-сборка для старых Unity с монолитным UnityEngine.

Для Unity 2017.2+ рекомендуется обычный проект LegacyUIFramework, поскольку начиная с этой ветки Unity перешла на модульные `UnityEngine.*Module.dll`. При необходимости `net35` всё ещё может использоваться и на более новых Unity, но для этого нужен отдельный набор reference assemblies под конкретную игру.

## Минимальное окно

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

## Модификаторы окна

`LegacyUIWindowOptions` сейчас поддерживает:

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

Если включён `FadeWhileDragging`, окно и все дочерние IMGUI-элементы рисуются с уменьшенной прозрачностью во время перетаскивания за заголовок.

`BackgroundAlpha` меняет только прозрачность фона окна. Если значение равно `null`, используется прозрачность из текущей темы.

`Theme` необязателен. Если он задан, только это окно и его содержимое рисуются в указанной теме. Глобальная тема framework не меняется.

`InputBlockMode` управляет тем, как окно поглощает события мыши после отрисовки собственных элементов:

```text
None   = 0 — не блокировать фоновые клики
Window = 1 — блокировать клики только внутри прямоугольника окна; режим по умолчанию
Screen = 2 — блокировать клики мыши по всему экрану, пока окно открыто
```

Если задан `PlayerPrefsKey`, framework сохраняет позицию, размер и видимость окна:

```text
<key>.x
<key>.y
<key>.w
<key>.h
<key>.visible
```

## Элементы управления

Framework содержит wrappers для часто используемых стилизованных controls:

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

Helpers для sliders и scrollbars отрисовываются вручную и не зависят от изменения `GUI.skin`, поэтому несколько плагинов могут безопасно использовать framework, не конфликтуя за глобальное состояние skin.

`TextField` и `TextArea` автоматически применяют цвета редактирования текста из текущей активной темы: caret использует `Palette.TextBright`, а выделение — `Palette.Active` с уменьшенной прозрачностью. Unity хранит эти параметры в `GUI.skin.settings`, поэтому framework изменяет их только на время конкретного вызова text control и сразу восстанавливает предыдущие значения. Это работает с глобальной темой, `LegacyUI.WithTheme(...)`, темой окна и overload-ами отдельных элементов без постоянного изменения глобального `GUI.skin`.

## ScrollView

`LegacyUIScrollView` — отдельная scrollable area. `ViewRect` задаёт внешний видимый блок вместе со scrollbars, а `ContentRect` — виртуальное внутреннее полотно.

Конструктор с одним `Rect` включает автоматический расчёт внутреннего полотна. На каждом draw-проходе его размер пересчитывается по `LegacyUI`-элементам, отрисованным внутри callback, с добавлением `Padding`. Это позволяет напрямую строить динамические списки:

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

В автоматическом режиме `ContentRect` всегда не меньше видимой области контента и динамически увеличивается или уменьшается по текущим controls. Все стандартные helpers из `LegacyUI` автоматически участвуют в измерении. Если элемент рисуется напрямую через `GUI.*`, его `Rect` нужно зарегистрировать внутри callback вручную:

```csharp
Rect custom = new Rect(content.x, content.y + 40f, 500f, 24f);
GUI.Button(custom, "Raw IMGUI button");
scrollView.IncludeContentRect(custom);
```

Для фиксированного внутреннего полотна передайте второй `Rect` в конструктор либо присвойте `ContentRect`. Присваивание `ContentRect` автоматически отключает `AutoContentSize`:

```csharp
LegacyUIScrollView map = new LegacyUIScrollView(
    new Rect(20f, 20f, 320f, 220f),
    new Rect(0f, 0f, 900f, 700f));

map.Draw(delegate(Rect content)
{
    LegacyUI.Button(new Rect(content.x + 600f, content.y + 400f, 140f, 26f), "Far button");
});
```

`UseAutoContentSize()` возвращает динамический расчёт, а `ResetScroll()` сбрасывает прокрутку в левый верхний угол. `ShowHorizontalScrollbar` и `ShowVerticalScrollbar` управляют тем, какие оси резервируют место и рисуют стилизованные scrollbars framework. Для отдельного экземпляра также доступны `ScrollbarSize`, `ScrollbarSpacing`, `DrawBackground`, `BackgroundAlpha`, `ScrollPosition` и `Theme`.

> [!WARNING]
> **Warning — порядок объявления IMGUI-элементов рядом со ScrollView**
>
> `LegacyUIScrollView`, как и остальные элементы `LegacyUIFramework`, работает поверх Unity IMGUI. В IMGUI состояние интерактивных контролов связано с их внутренними `control ID`, которые Unity назначает в том числе на основании **порядка вызовов GUI-элементов внутри `OnGUI()`**.
>
> По этой причине следует соблюдать осторожность, если `ScrollView` содержит **динамическое количество элементов**, а после него в коде объявляются stateful-контролы, например `TextField`, `TextArea` или другие элементы, использующие keyboard/mouse focus.
>
> Например, потенциально проблемный порядок:
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
> Если количество элементов внутри `ScrollView` изменяется, количество созданных перед `TextField` IMGUI-контролов также изменяется. В некоторых версиях Unity это может привести к изменению внутреннего `control ID` поля между различными GUI events (`Layout`, `MouseDown`, `KeyDown`, `Repaint`).
>
> В результате визуально исправный `TextField` может вести себя некорректно:
>
> * не получать или терять keyboard focus;
> * не отображать caret;
> * не поддерживать выделение текста мышью;
> * терять текущую позицию курсора;
> * некорректно реагировать на клики или клавиатурный ввод.
>
> **Рекомендуемый подход:** интерактивные элементы, которым требуется стабильное состояние IMGUI, объявляйте **до динамического содержимого `ScrollView`**.
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
> Порядок вызовов IMGUI-элементов **не обязан совпадать с их визуальным расположением**. Положение элемента определяется переданным ему `Rect`, поэтому `TextField` можно вызвать раньше `ScrollView`, даже если визуально он находится ниже него.
>
> Особенно желательно использовать такой порядок для `TextField`, `TextArea` и других controls, состояние которых сохраняется Unity между GUI events.
>
> Также без необходимости не следует вручную комбинировать стандартный ввод IMGUI с `GUI.FocusControl`, собственным чтением `Input.inputString` или отдельной обработкой курсора/выделения. `LegacyUI.TextField` и `LegacyUI.TextArea` рассчитаны на штатный механизм фокуса Unity IMGUI.
>
> Сам `LegacyUIScrollView` при этом может содержать любое количество динамических `LegacyUI`-элементов. Ограничение относится именно к **стабильности порядка IMGUI controls, расположенных после динамически изменяющегося содержимого**.


## Styles и тема

При необходимости можно обращаться к styles из framework напрямую:

```csharp
GUI.Label(rect, "Custom label", LegacyUI.Styles.Label);
GUI.Button(rect, "Custom", LegacyUI.Styles.Button);
```

Доступные свойства styles:

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

Чтобы глобально настроить цвета для одного плагина:

```csharp
LegacyUIPalette palette = new LegacyUIPalette();
palette.Active = new Color(0.20f, 0.45f, 0.80f, 1f);
palette.WindowBackground = new Color(0.04f, 0.04f, 0.05f, 0.88f);
LegacyUI.SetTheme(new LegacyUITheme(palette));
```

Глобальная тема по-прежнему удобна как тема по умолчанию для плагина, но также можно использовать и временные темы для отдельных окон, групп элементов или отдельных controls, не меняя глобальное состояние.

Создайте переиспользуемые темы один раз, например в `Awake()`:

```csharp
LegacyUIPalette bluePalette = new LegacyUIPalette();
bluePalette.Active = new Color(0.16f, 0.42f, 0.82f, 1f);
bluePalette.WindowBackground = new Color(0.04f, 0.05f, 0.08f, 0.88f);
LegacyUITheme blueTheme = new LegacyUITheme(bluePalette);
```

`LegacyUITheme` можно безопасно создавать в `Awake()` и другом коде инициализации вне `OnGUI`. Текстуры и стили создаются лениво при первом реальном рисовании. Если палитра изменяется уже после того, как тема была отрисована, вызовите `theme.Invalidate()` или замените палитру через `theme.SetPalette(...)`.

Использование темы для всего окна:

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

Использование темы для группы controls:

```csharp
using (LegacyUI.WithTheme(blueTheme))
{
    LegacyUI.Panel(rect);
    LegacyUI.Title(titleRect, "Blue group");
    LegacyUI.Button(buttonRect, "Button");
}
```

Или тема только для одного элемента:

```csharp
LegacyUI.Button(rect, "Blue button", blueTheme);
LegacyUI.HorizontalSlider(rect, value, 0f, 1f, blueTheme);
LegacyUI.Panel(rect, 0.70f, blueTheme);
```

Scoped-темы работают как stack, поэтому вложенные темы разрешены. Освобождайте scope внутри того же `OnGUI`, где он был создан.

## Example plugin

`LegacyUIFramework.Example` рисует окно со следующими элементами:

```text
TextField
TextArea
Button
GreenButton
DangerButton
TabButton
Toggle
HintBox
ScrollView с автоматически рассчитываемым динамическим списком файлов
HorizontalScrollbar / VerticalScrollbar внутри ScrollView
Close button
Drag fade modifier
Input block mode switcher
Примеры themes для окна, группы и отдельного элемента
```

Этот плагин нужен только как пример использования. Для работы framework он не требуется.

## Правило интеграции для BigCityLegacy

Не переносить крупные необязательные UI helpers обратно в основной плагин BigCityLegacy. Новые плагины с большим количеством UI должны ссылаться на `LegacyUIFramework.dll` и хранить свою feature-логику в отдельных assemblies.
