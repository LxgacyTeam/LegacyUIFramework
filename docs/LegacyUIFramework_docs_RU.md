# LegacyUIFramework

`LegacyUIFramework.dll` — это небольшая отдельная IMGUI-библиотека для плагинов BepInEx 5.
Она содержит собственный визуальный стиль, а также переиспользуемые helpers для перетаскиваемых окон и базовых UI-элементов.

> [!NOTE]
> Фреймворк разрабатывается в рамках проекта BigCityLegacy и подразумевает использование как в нем самом, так и в дополнительных зависимых плагинах.
> Использование LegacyUIFramework для сторонних игр возможно, но не гарантирует полную совместимость.

Framework можно собрать и скопировать как отдельную DLL, а затем подключать из любого плагина, которому нужно рисовать интерфейс в едином стиле.

## Проекты

```text
src/LegacyUIFramework/LegacyUIFramework.csproj
src/LegacyUIFramework.Example/LegacyUIFramework.Example.csproj
```

Сборка framework:

```bash
dotnet build .\LegacyUIFramework.csproj -c Debug -p:GameDir="path\to\game\dir"
```

Сборка и копирование example-плагина в BepInEx:

```bash
dotnet build .\src\LegacyUIFramework.Example\LegacyUIFramework.Example.csproj -c Debug -p:GameDir="path\to\game\dir" -p:CopyToPlugins=true
```

Для своих плагинов скопируйте `LegacyUIFramework.dll` рядом с DLL плагина или в общую папку модулей BigCityLegacy, которую загружает BepInEx.

Для режима блокировки фонового ввода framework использует `0Harmony.dll` из `BepInEx/core` и `UnityEngine.UI.dll` + `UnityEngine.UIModule.dll`. 

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
LegacyUIGuiScope         сохраняет/восстанавливает глобальное состояние Unity IMGUI
LegacyUILayout           маленький helper для ручной fixed-layout вёрстки
```

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
HorizontalSlider
VerticalSlider
HorizontalScrollbar
VerticalScrollbar
Close button
Drag fade modifier
Input block mode switcher
Примеры themes для окна, группы и отдельного элемента
```

Этот плагин нужен только как пример использования. Для работы framework он не требуется.

## Правило интеграции для BigCityLegacy

Не переносить крупные необязательные UI helpers обратно в основной плагин BigCityLegacy. Новые плагины с большим количеством UI должны ссылаться на `LegacyUIFramework.dll` и хранить свою feature-логику в отдельных assemblies.
