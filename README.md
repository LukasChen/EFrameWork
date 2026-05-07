# EFrame

中文 | [English](README.en.md)

EFrame 是一个轻量级 Unity 游戏框架，为业务项目提供标准启动模板、运行时基础设施、编辑器初始化工具和可同步的 AI 协作层。

框架维护流程见 [README.maintainer.md](README.maintainer.md)。

当前框架版本以 [packages/com.eframework.core/package.json](packages/com.eframework.core/package.json) 为准。

## 快速开始

通过 Unity Package Manager 安装 Core package：

```text
https://github.com/ethanhubin/EFrame.git?path=/packages/com.eframework.core
```

在 Unity Editor 中打开：

```text
EFrame Tools/项目初始化向导
-> Initialize / Repair Project
```

该入口会复制或修复 Basic 启动模板，同步 EFrame AI workspace，安装项目侧 AI updater，并修复 Addressables、UI sorting layers、Audio、fallback tween 和 Build Settings 等基础配置。

如果需要命令行冷启动：

```powershell
.\tools\Initialize-EFrameColdStart.ps1 -TargetRoot "D:\YourUnityProject" -Force
```

命令行冷启动默认只创建目录和 Basic 模板，不自动同步 AI workspace。需要一并同步 AI 时显式加上：

```powershell
.\tools\Initialize-EFrameColdStart.ps1 -TargetRoot "D:\YourUnityProject" -Force -IncludeAIWorkspace -AIClients all
```

仅通过 package 接入时，可从已安装 package 根目录运行等价脚本：

```powershell
.\Packages\com.eframework.core\Tools~\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -Force
```

同步完成后，可以在 Codex、GitHub Copilot 或 Claude Code 中直接用业务需求触发 EFrame AI workflow。AI 会从项目入口的 EFrame managed block 进入 `eframe-*` instruction、skill 和 API index，再按框架约定修改代码或给出审查结果。

常用提示词示例：

- `做一个 EFrame Settings 弹窗。`
  执行效果：生成弹窗 View、Controller 和绑定代码，并按 `QUI` 生命周期打开关闭。
- `接入 PlayerProfile 数据表。`
  执行效果：生成数据表接入代码，加载、变更标记和保存流程按 EFrame 约定处理。
- `把头像改成 EFrame 托管资源，用 ResPath 加载。`
  执行效果：整理资源目录、提示同步 Addressables / `ResPath.Generated`，并改为生成入口加载。
- `做一次 EFrame 规范审查。`
  执行效果：审查 UI、资源、数据表和 AI 同步常见问题，并给出需要修改的位置。

## 仓库结构

- [packages/com.eframework.core](packages/com.eframework.core)：Core runtime/editor package，包含 `EFrame`、`Procedure`、`QUI`、UI controller/handle、资源、音频、数据、事件和项目初始化工具。
- [packages/com.eframework.core/Tools~](packages/com.eframework.core/Tools~)：随 package 发布的项目初始化、AI 同步和健康检查脚本。
- [packages/com.eframework.core/Documentation~](packages/com.eframework.core/Documentation~)：给人看的使用文档、维护文档和 HTML 文档站点。
- [packages/com.eframework.ai-loop](packages/com.eframework.ai-loop)、[packages/com.eframework.ui.virtual-list](packages/com.eframework.ui.virtual-list)、[packages/com.eframework.ui-extras](packages/com.eframework.ui-extras)、[packages/com.eframework.effects](packages/com.eframework.effects)、[packages/com.eframework.debug-console](packages/com.eframework.debug-console)：可选扩展 package。

## Core 能力

EFrame Core 提供稳定的运行时基础设施和最小可运行启动骨架：

- `EFrameContext` 与 `EFrame.*` 快捷入口
- Procedure 启动流和 Basic `StartUp.unity`
- `QUI`、UI prefab binding、UIController、UIViewHandle、UI layer 和 transition
- Addressables 托管资源、`ResPath.Generated` 和构建前检查
- Audio 播放、mixer 音量、SFX pooling、debounce 和 music crossfade
- 数据表、事件、协程、资源加载和常用 utility service
- 项目初始化向导、Addressables 修复、UI binding、Scroller 等编辑器工具

常用运行时入口：

```csharp
EFrame.UI
EFrame.Audio
EFrame.Data
EFrame.Assets
```

框架创建的 view、controller 和 `EFrameBehaviour` 会自动获得 `Context`。业务启动点、静态入口或非注入代码可以使用 `EFrame.UI` / `EFrame.Audio` 等快捷入口；框架感知类型内部优先使用 `Context.UI` / `Context.Audio`。

## 项目约定

EFrame 将以下目录视为框架托管资源目录：

- `Assets/App/Res`
- `Assets/Scenes`
- `Assets/Modules/<ModuleName>/Res`
- `Assets/Modules/<ModuleName>/Scenes`

资源导入、移动或删除后，编辑器自动维护 Addressables group、address 和 `eframe-managed` label。运行时代码应优先使用生成的 `ResPath.Generated`，不要手写 Addressables 字符串或资源路径包装类。

手动检查或修复资源时打开：

```text
EFrame Tools/Addressables/Sync Groups And Generate ResPath
```

## Basic 与可选 Showcase

Basic 是 Core 维护的最小可运行项目模板，会创建或修复：

- `Assets/Scenes/StartUp.unity`
- `ProcedureLauncher` / `ProcedureHome`
- `HomeView` / `HomeViewController`
- `Assets/App/Res/UI/Panels/Home/HomeView.prefab`
- Addressables groups 和 `ResPath.Generated`
- UI sorting layers、基础音频资源和 fallback tween 准备状态

Extension Showcase 是可选业务模块，安装到 `Assets/Modules/EFrameExtensionShowcase/`，用于演示 UI Virtual List 和 Debug Console。

## AI 协作层

EFrame 的 AI 协作层会把框架约定同步到业务项目的 AI 客户端入口，让 Codex、GitHub Copilot、Claude Code 获得一致的 EFrame 编码指导。

只同步或检查 AI workspace：

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -StatusOnly
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -Force
```

安装项目侧 updater：

```powershell
.\tools\Install-EFrameAIProjectUpdater.ps1 -TargetRoot "D:\YourUnityProject"
```

同步后在业务项目内可继续执行：

```powershell
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -StatusOnly
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -Force
```

## 依赖与扩展

Core package 声明的 Unity 依赖包括：

- Addressables
- UGUI
- Input System
- Universal Render Pipeline
- Unity Newtonsoft.Json

`com.eframework.core` 需要 URP。`QUI` 会通过 URP overlay-camera API 将持久 UI camera 绑定到当前 `EFrameSceneCamera` stack，因此消费项目应配置 URP pipeline asset。

TextMeshPro 功能由 `com.unity.ugui` 提供，项目不应再引入旧的独立 `com.unity.textmeshpro` package 或框架本地 TMP 副本。

Core 默认使用 `EFrameTween` fallback backend。DOTween 是可选项目插件，不随 package 分发；需要启用时使用：

```text
EFrame Tools/项目初始化向导
-> Extensions
-> DOTween Adapter
-> Apply
```

启用 adapter 会添加 `EFRAME_USE_DOTWEEN` scripting define，并创建或打开 `Assets/Resources/DOTweenSettings.asset`。移除 DOTween 前先禁用 adapter。

## 文档入口

- 使用文档：[packages/com.eframework.core/Documentation~/user](packages/com.eframework.core/Documentation~/user)
- UI 框架指南：[UI_FRAMEWORK_GUIDE.md](packages/com.eframework.core/Documentation~/user/UI_FRAMEWORK_GUIDE.md)
- 目录结构约定：[UNITY_DIRECTORY_STRUCTURE.md](packages/com.eframework.core/Documentation~/user/UNITY_DIRECTORY_STRUCTURE.md)
- 样例与初始化：[SAMPLES_AND_INITIALIZATION.md](packages/com.eframework.core/Documentation~/user/SAMPLES_AND_INITIALIZATION.md)
- 资源路径约定：[RESPATH_CONVENTION.md](packages/com.eframework.core/Documentation~/user/RESPATH_CONVENTION.md)
- API HTML 索引：[Documentation~/api/index.html](packages/com.eframework.core/Documentation~/api/index.html)
- 文档站点入口：[Documentation~/index.html](packages/com.eframework.core/Documentation~/index.html)
- 维护者入口：[README.maintainer.md](README.maintainer.md)
