### [Русский](./README_RU.md) | [English](./README.md) | [> 简体中文 <](./README_ZH.md)
---

# LegacyUIFramework

面向 Unity Mono/BepInEx 插件的小型独立 IMGUI 库。它可以轻松创建带有样式化 UI 元素的可拖动窗口，基于标准 Unity IMGUI 运行。

**作为 [BigCityLegacy](https://github.com/LxgacyTeam/BigCityLegacy) 项目的一部分开发，也可独立用于其他 BepInEx 插件和游戏。**

### 完整使用文档请参阅[简体中文文档](./LegacyUIFramework_docs_ZH.md)

![界面预览](https://github.com/LxgacyTeam/LegacyUIFramework/blob/main/docs/img/preview.png?raw=true)

---

## 功能与特性

* **自动或手动 UI 缩放**（默认 FHD 1×、UHD 2×），可针对插件、窗口和控件组单独设置
* 支持滚动的 **Dropdown** 下拉控件，以及步长和范围可配置的 **Counter** 计数器
* 所有 UI 元素均以可复用辅助方法实现，基于标准 Unity IMGUI
* 快速创建可拖动窗口，灵活配置大小、行为、不透明度等
* 创建样式化 UI 元素，例如按钮、复选框、滑块和输入框
* 轻松创建 ScrollView：内容大小自动计算的独立可滚动区域
* 支持主题，可全局应用，也可应用于单个元素或控件组
* `LegacyInput`：基于反射的 Legacy Input 包装器，使同一次插件构建能够兼容新旧 Unity 中不同的 `UnityEngine.Input` 程序集布局

![主题预览](https://github.com/LxgacyTeam/LegacyUIFramework/blob/main/docs/img/themes-preview.png?raw=true)

## 兼容性

#### 主项目（net472）
Unity 2017.2 — Unity 6，Mono，兼容 .NET 4.x 的运行时

#### 兼容构建（net35）
Unity 4.6 — 2017.1，Mono，.NET 3.5 运行时
*由独立的 `LegacyUIFramework.net35` 项目提供支持*

> [!NOTE]
> 当前实现仅支持 Mono，**不支持 IL2CPP**。

详细信息请参阅文档中的 [Unity 版本兼容性](./LegacyUIFramework_docs_ZH.md#unity-版本兼容性)章节。

## 使用方法

> [!WARNING]
> 克隆仓库后，请在开始使用前将 `LegacyUIFramework.csproj` 中的 `<GameDir>` 属性设置为游戏根目录的路径，因为框架依赖 Unity 和 BepInEx 库。

### 方法 1
1. 克隆仓库：
```powershell
git clone https://github.com/LxgacyTeam/LegacyUIFramework.git libs
```

2. 以 ProjectReference 的形式将框架项目添加到主项目：
```powershell
dotnet add yourplugin.csproj reference libs\LegacyUIFramework\src\LegacyUIFramework\LegacyUIFramework.csproj
```

### 方法 2
1. 克隆仓库并构建项目：
```powershell
git clone https://github.com/LxgacyTeam/LegacyUIFramework.git
cd LegacyUIFramework
dotnet build .\src\LegacyUIFramework\LegacyUIFramework.csproj
```

2. 在 `.csproj` 文件中以 Reference 的形式添加生成的 DLL：
```xml
<ItemGroup>
   <Reference Include="LegacyUIFramework">
      <HintPath>...\path\to\lib\LegacyUIFramework.dll</HintPath>
      <Private>true</Private>
   </Reference>
</ItemGroup>
```

## 示例插件

使用 `LegacyUIFramework.Example` BepInEx 插件，直观了解框架的功能和用法。
