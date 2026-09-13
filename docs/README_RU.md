# LegacyUIFramework

Небольшая отдельная IMGUI-библиотека для BepInEx-плагинов. Позволяет легко создавать перетаскиваемые окна со стилизованными UI элементами.
**Разрабатывается специально для проекта BigCityLegacy**.

### Полная документация по использованию доступна в [разделе Wiki](https://github.com/LxgacyTeam/LegacyUIFramework/wiki/%D0%94%D0%BE%D0%BA%D1%83%D0%BC%D0%B5%D0%BD%D1%82%D0%B0%D1%86%D0%B8%D1%8F-%D0%BD%D0%B0-%D0%A0%D1%83%D1%81%D1%81%D0%BA%D0%BE%D0%BC)

![preview image](https://github.com/LxgacyTeam/LegacyUIFramework/blob/main/docs/img/preview.png?raw=true)

---

## Возможности и особенности:
* Все UI элементы реализованы как переиспользуемые helpers поверх стандартного Unity IMGUI
* Быстрое создание перетаскиваемых окон с гибкой настройкой: размеры, поведение, прозрачность и т.д.
* Создание стилизованных UI элементов: кнопки, чекбоксы, слайдеры, поля и т.д.
* Легкое создание ScrollView - отдельных scrollable areas с автоматическим расчётом внутреннего полотна.
* Поддержка тем: применяется как глобально, так и для отдельных элементов или группы элементов

![themes preview image](https://github.com/LxgacyTeam/LegacyUIFramework/blob/main/docs/img/themes-preview.png?raw=true)

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
cd LegacyUIFramework
dotnet build -
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
