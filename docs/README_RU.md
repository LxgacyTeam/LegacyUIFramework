# LegacyUIFramework

Небольшая отдельная IMGUI-библиотека для Unity Mono/BepInEx-плагинов. Позволяет легко создавать перетаскиваемые окна со стилизованными UI элементами. Работает поверх обычного Unity IMGUI.

**Разрабатывается в рамках проекта BigCityLegacy, но может использоваться независимо в других BepInEx-плагинах и играх.**

### Полная документация по использованию доступна в [разделе Wiki](https://github.com/LxgacyTeam/LegacyUIFramework/wiki/%D0%94%D0%BE%D0%BA%D1%83%D0%BC%D0%B5%D0%BD%D1%82%D0%B0%D1%86%D0%B8%D1%8F-%D0%BD%D0%B0-%D0%A0%D1%83%D1%81%D1%81%D0%BA%D0%BE%D0%BC)

![preview image](https://github.com/LxgacyTeam/LegacyUIFramework/blob/main/docs/img/preview.png?raw=true)

---

## Возможности и особенности:
* Все UI элементы реализованы как переиспользуемые helpers поверх стандартного Unity IMGUI
* Быстрое создание перетаскиваемых окон с гибкой настройкой: размеры, поведение, прозрачность и т.д.
* Создание стилизованных UI элементов: кнопки, чекбоксы, слайдеры, поля и т.д.
* Легкое создание ScrollView - отдельных scrollable areas с автоматическим расчётом внутреннего полотна.
* Поддержка тем: применяется как глобально, так и для отдельных элементов или группы элементов
* `LegacyInput` — reflection-wrapper для Legacy Input API, позволяющий одному плагину работать с разными вариантами размещения `UnityEngine.Input` в старых и новых Unity

![themes preview image](https://github.com/LxgacyTeam/LegacyUIFramework/blob/main/docs/img/themes-preview.png?raw=true)

## Совместимость:

#### Основной проект (net472):
Unity 2017.2 — Unity 6, Mono, .NET 4.x-compatible runtime

#### Compatibility build (net35):
Unity 4.6 — 2017.1, Mono, .NET 3.5 runtime
*поддерживается отдельным проектом `LegacyUIFramework.net35`*

> [!NOTE]
> Только Mono, IL2CPP **не поддерживается** в текущей реализации.

О совместимости см. подробно в соответсвующем разделе Wiki

## Использование:

> [!WARNING]
> Перед началом работы после клонирования, в `LegacyUIFramework.csproj` необходимо указать путь к корневой папке своей игры в атрибуте `<GameDir>`, т.к фреймворк использует библиотеки Unity и BepInEx

### Способ 1:
1. Клонируйте репозиторий
```powershell
git clone https://github.com/LxgacyTeam/LegacyUIFramework.git libs
```

2. Подключите проект фреймворка к своему основному проекту как ProjectReference
```powershell
dotnet add yourplugin.csproj reference libs\LegacyUIFramework\src\LegacyUIFramework\LegacyUIFramework.csproj
```

### Способ 2:
1. Клонируйте репозиторий и соберите проект
```powershell
git clone https://github.com/LxgacyTeam/LegacyUIFramework.git
cd LegacyUIFramework\src\LegacyUIFramework\LegacyUIFramework.csproj
dotnet build
```

2. Подключите полученный DLL как Reference в своем .csproj файле:
```xml
<ItemGroup>
   <Reference Include="LegacyUIFramework">
      <HintPath>...\path\to\lib\LegacyUIFramework.dll</HintPath>
      <Private>true</Private>
   </Reference>
</ItemGroup>
```

## Example Plugin:

Используйте BepInEx-плагин `LegacyUIFramework.Example` для наглядного ознакомления с возможностями и применением фреймворка
