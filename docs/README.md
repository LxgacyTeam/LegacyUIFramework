# LegacyUIFramework

### [README на Русском](https://github.com/LxgacyTeam/LegacyUIFramework/blob/main/docs/README_RU.md)
---
A small standalone IMGUI library for Unity Mono/BepInEx plugins. It makes it easy to create draggable windows with styled UI elements. It runs on top of standard Unity IMGUI.

**Developed as part of BigCityLegacy project, but usable independently by other BepInEx plugins and games.**

### Full usage documentation is available in the [Wiki section](https://github.com/LxgacyTeam/LegacyUIFramework/wiki/Documentation-on-English)


![preview image](https://github.com/LxgacyTeam/LegacyUIFramework/blob/main/docs/img/preview.png?raw=true)

---

## Features:
* All UI elements are implemented as reusable helpers on top of standard Unity IMGUI
* Quickly create draggable windows with flexible configuration for size, behavior, opacity, and more
* Create styled UI elements such as buttons, checkboxes, sliders, input fields, and more
* Easily create ScrollViews - standalone scrollable areas with auto content sizing
* Theme support, applied globally or to individual elements or groups of elements
* `LegacyInput` — a reflection-based Legacy Input wrapper that lets one plugin build work across old and new Unity layouts of `UnityEngine.Input`

![themes preview image](https://github.com/LxgacyTeam/LegacyUIFramework/blob/main/docs/img/themes-preview.png?raw=true)

## Compatibility:

#### Main project (net472):
Unity 2017.2 — Unity 6, Mono, .NET 4.x-compatible runtime

#### Compatibility build (net35):
Unity 4.6 — 2017.1, Mono, .NET 3.5 runtime
*supported by the separate `LegacyUIFramework.net35` project*

> [!NOTE]
> Only Mono, IL2CPP **is not supported** in the current implementation

For details on compatibility, see the [Compatibility section](https://github.com/LxgacyTeam/LegacyUIFramework/wiki/Documentation-on-English#unity-version-compatibility) of the Wiki.

## Usage:

> [!WARNING]
> After cloning the repository and before getting started, specify the path to your game's root directory in the `<GameDir>` attribute inside `LegacyUIFramework.csproj`, because the framework depends on Unity and BepInEx libraries.

### Method 1:
1. Clone the repository
```powershell
git clone https://github.com/LxgacyTeam/LegacyUIFramework.git libs
```

2. Add the framework project to your main project as a ProjectReference
```powershell
dotnet add yourplugin.csproj reference libs\LegacyUIFramework\src\LegacyUIFramework\LegacyUIFramework.csproj
```

### Method 2:
1. Clone the repository and build the project
```powershell
git clone https://github.com/LxgacyTeam/LegacyUIFramework.git
cd LegacyUIFramework
dotnet build -
```

2. Add the resulting DLL as a Reference in your `.csproj` file:
```xml
<ItemGroup>
   <Reference Include="LegacyUIFramework">
      <HintPath>...\path\to\lib\LegacyUIFramework.dll</HintPath>
      <Private>true</Private>
   </Reference>
</ItemGroup>
```

## Example Plugin:

Use the `LegacyUIFramework.Example` BepInEx plugin for a visual overview of the framework's features and usage.
