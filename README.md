# EFrame

中文 | [English](README.en.md)

EFrame 是一个轻量级 Unity 游戏框架，同时内置可同步的 AI 协作层。它把运行时代码、编辑器工具、项目初始化模板、AI instruction/skill、同步脚本和维护检查放在同一个发布面里，目标是让新业务项目既能快速得到标准 Unity 工程结构，也能得到匹配 EFrame 约定的 AI 编码指导。

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

只同步或检查 AI workspace：

```powershell
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -StatusOnly
.\tools\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -Force
```

package-only 消费者可从已安装 package 根目录运行等价脚本：

```powershell
.\Packages\com.eframework.core\Tools~\Initialize-EFrameAI.ps1 -TargetRoot "D:\YourUnityProject" -Clients all -Force
```

## 仓库结构

- [packages/com.eframework.core](packages/com.eframework.core)：Core runtime/editor package，包含 `EFrame`、`Procedure`、`QUI`、UI controller/handle、资源、音频、数据、事件和项目初始化工具。
- [packages/com.eframework.core/AIWorkspace~](packages/com.eframework.core/AIWorkspace~)：随 package 发布的 AI 协作源，包含 `eframe-*` instructions/skills、managed blocks、manifest 和 AI 支持文档。
- [packages/com.eframework.core/Tools~](packages/com.eframework.core/Tools~)：随 package 发布的项目初始化、AI 同步和健康检查脚本。
- [packages/com.eframework.core/Documentation~](packages/com.eframework.core/Documentation~)：给人看的用户文档、维护者文档和 HTML 文档站点。
- [packages/com.eframework.ui.virtual-list](packages/com.eframework.ui.virtual-list)、[packages/com.eframework.ui-extras](packages/com.eframework.ui-extras)、[packages/com.eframework.effects](packages/com.eframework.effects)、[packages/com.eframework.debug-console](packages/com.eframework.debug-console)：可选扩展 package。
- [test-fixtures](test-fixtures)：用于 Basic 模板、Showcase 模块和 package 消费路径验证的 Unity fixture。
- [tools](tools)：框架仓库维护脚本和 package 脚本包装器。业务项目优先使用 package 内的 `Tools~/`。
- [AGENTS.md](AGENTS.md)、[CLAUDE.md](CLAUDE.md)、[.github/copilot-instructions.md](.github/copilot-instructions.md)：框架仓库内 AI 客户端入口，只做分流，不复制业务规则全文。

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

## Basic 与 Showcase

Basic 是 Core 维护的最小可运行项目模板，会创建或修复：

- `Assets/Scenes/StartUp.unity`
- `ProcedureLauncher` / `ProcedureHome`
- `HomeView` / `HomeViewController`
- `Assets/App/Res/UI/Panels/Home/HomeView.prefab`
- Addressables groups 和 `ResPath.Generated`
- UI sorting layers、基础音频资源和 fallback tween 准备状态

维护 Basic 模板时，编辑 [test-fixtures/EFrameBasicTemplate](test-fixtures/EFrameBasicTemplate)，再同步回 package template：

```powershell
.\tools\Sync-EFrameBasicTemplate.ps1
.\tools\Sync-EFrameBasicTemplate.ps1 -CheckOnly
```

Extension Showcase 是可选业务模块，安装到 `Assets/Modules/EFrameExtensionShowcase/`，用于演示 UI Virtual List 和 Debug Console。维护 Showcase 时编辑 [test-fixtures/EFrameShowcaseUnity](test-fixtures/EFrameShowcaseUnity)，再同步回 package template：

```powershell
.\tools\Sync-EFrameShowcaseTemplate.ps1
.\tools\Sync-EFrameShowcaseTemplate.ps1 -CheckOnly
```

## AI 协作层

EFrame 的 AI 协作层是框架发布契约的一部分，不是额外文档包。

- 业务项目同步源位于 [packages/com.eframework.core/AIWorkspace~](packages/com.eframework.core/AIWorkspace~)。
- 同步到业务项目的框架托管文件使用 `eframe-*` 前缀。
- Codex、GitHub Copilot、Claude Code 的项目入口文件由业务项目拥有；EFrame 只更新入口文件中的 EFrame managed block。
- AI-facing 支持文档位于 `AIWorkspace~/support-docs/`，当前 API 索引为 [EFRAME_AI_API_INDEX.md](packages/com.eframework.core/AIWorkspace~/support-docs/EFRAME_AI_API_INDEX.md)。
- 框架维护专用规则位于 [tools/MaintainerAIWorkspace](tools/MaintainerAIWorkspace)，不会同步到业务项目。

安装项目侧 updater：

```powershell
.\tools\Install-EFrameAIProjectUpdater.ps1 -TargetRoot "D:\YourUnityProject"
```

同步后在业务项目内可继续执行：

```powershell
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -StatusOnly
.\tools\Sync-EFrameAIFromFramework.ps1 -Clients all -Force
```

检查已同步项目：

```powershell
.\tools\Test-EFrameAIProject.ps1 -TargetRoot "D:\YourUnityProject" -FrameworkRoot "."
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

## 维护检查

发布前至少运行：

```powershell
.\tools\Test-EFrameAIRelease.ps1
```

涉及 Basic、Showcase、AI 同步、资源目录、启动流程、UI runtime contract 或 manifest 的变更，还应按影响面运行对应同步脚本和项目检查。

AI release 与 manifest 规则见 [tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md](tools/MaintainerAIWorkspace/EFRAME_AI_RELEASE_CHECKLIST.md)。

## 文档入口

- 用户文档：[packages/com.eframework.core/Documentation~/user](packages/com.eframework.core/Documentation~/user)
- UI 框架指南：[UI_FRAMEWORK_GUIDE.md](packages/com.eframework.core/Documentation~/user/UI_FRAMEWORK_GUIDE.md)
- 目录结构约定：[UNITY_DIRECTORY_STRUCTURE.md](packages/com.eframework.core/Documentation~/user/UNITY_DIRECTORY_STRUCTURE.md)
- 样例与初始化：[SAMPLES_AND_INITIALIZATION.md](packages/com.eframework.core/Documentation~/user/SAMPLES_AND_INITIALIZATION.md)
- 资源路径约定：[RESPATH_CONVENTION.md](packages/com.eframework.core/Documentation~/user/RESPATH_CONVENTION.md)
- AI 架构说明：[EFRAME_AI_ARCHITECTURE.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_ARCHITECTURE.md)
- AI setup 说明：[EFRAME_AI_SETUP.md](packages/com.eframework.core/Documentation~/maintainer/EFRAME_AI_SETUP.md)
- API HTML 索引：[Documentation~/api/index.html](packages/com.eframework.core/Documentation~/api/index.html)
- 文档站点入口：[Documentation~/index.html](packages/com.eframework.core/Documentation~/index.html)

## 版本规则

EFrame 使用语义化版本。正式起始版本为 `0.1.0`，当前发布版本记录在 [packages/com.eframework.core/package.json](packages/com.eframework.core/package.json)，仓库级发布记录在 [CHANGELOG.md](CHANGELOG.md)。

Unity code、AI collaboration rules、bootstrap tools、sync scripts 和 docs 视为同一个 EFrame release surface。`AIWorkspace~/eframe-ai.manifest.json` 只是业务项目检测同步文件漂移的内部标记，不是单独产品版本。
