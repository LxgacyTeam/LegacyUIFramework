# LegacyUIFramework

Небольшая отдельная IMGUI-библиотека для BepInEx-плагинов. Позволяет легко создавать перетаскиваемые окна со стилизованными UI элементами.
**Разрабатывается специально для проекта BigCityLegacy**.

### Полная документация по использованию доступна в разделе Wiki

---

## Возможности и особенности:
* Все UI элементы реализованы как переиспользуемые helpers поверх стандартного Unity IMGUI
* Позволяет быстро создавать перетаскиваемые окна с гибкой настройкой: размеры, поведение, прозрачность и т.д.
* Позволяет создавать стилизованные UI элементы: кнопки, чекбоксы, слайдеры, поля и т.д.
* Поддержка тем: применяется как глобально, так и для отдельных элементов или группы элементов

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