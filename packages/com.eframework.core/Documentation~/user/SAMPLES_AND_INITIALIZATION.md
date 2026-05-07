# EFrame 示例与初始化

EFrame 使用三层结构：

- Core：`com.eframework.core`，稳定的 runtime/editor 基础设施和最小启动骨架。
- Extension：可选 package，例如 `com.eframework.ui-extras`、`com.eframework.effects`、`com.eframework.debug-console` 和 `com.eframework.ai-loop`。
- Samples：在有用时安装到消费端 Unity 项目的项目骨架或模块。

## Basic

Basic 是 Core 拥有的最小可运行项目骨架。它是当前初始化模板的维护版本，不引用可选 extension package。

从 Unity 安装：

```text
EFrame Tools/项目初始化向导
-> Initialize / Repair Project
```

初始化器会复制 package Basic 模板，为所有受支持客户端同步 EFrame AI 工作区，在可用时安装项目 AI updater，然后修复 Addressables、UI sorting layers、音频、fallback tween 准备状态和 build settings。

Basic 会创建或准备：

- `Assets/Scenes/StartUp.unity`
- `Assets/App/Runtime/Procedure/ProcedureLauncher.cs`
- `Assets/App/Runtime/Procedure/ProcedureHome.cs`
- `Assets/App/Runtime/UI/Views/HomeView.cs`
- `Assets/App/Runtime/UI/Controllers/HomeViewController.cs`
- `Assets/App/Res/UI/Panels/Home/HomeView.prefab`
- Addressables groups 和生成的 `ResPath`
- UI sorting layers 配置
- 基础音频资源和 fallback tween 后端准备状态

可编辑的 Basic 源是 `test-fixtures/EFrameBasicTemplate` 下的完整 Unity fixture；维护者通过 `tools/Sync-EFrameBasicTemplate.ps1` 将其 `Assets` 文件夹同步到 `Editor/Templates/Basic`。

## Extension Showcase 模块

Extension Showcase 是可选 Project Module，安装到：

```text
Assets/Modules/EFrameExtensionShowcase/
```

从 Unity 安装：

```text
EFrame Tools/项目初始化向导
-> Extensions -> Apply
-> Showcase -> Install Showcase
```

`Extensions` 会展开并自动扫描当前项目 package 状态，写入 UI Extras、Effects、Debug Console 和 AI Loop 的复选框。`Select All` 会选择所有 extension package，便于新项目一次性安装全部 extensions。`Apply` 会把已勾选的 extension packages 写入 `Packages/manifest.json`；在本地框架 checkout 中使用同级 `file:` package 引用，当 Core 通过带 `?path=/packages/com.eframework.core` 的 git URL 安装时，会为每个选中的 extension package 推导匹配的 git dependency。取消勾选已安装 package 不会移除它。同一列表还包含 `DOTween Adapter`，它会应用或移除 `EFRAME_USE_DOTWEEN` scripting define，而不是添加 package dependency。

`AI Loop` 安装 `com.eframework.ai-loop`，这是一个 editor-only PlayMode 验证 package，用于 Game View 截图、uGUI 输入模拟、键盘模拟和输入录制/回放。它不是 Basic 启动骨架的一部分，只应在项目需要 AI 辅助验证时安装，例如 `UI 自动验收`、`截图验收` 或 `录制回放验收`。

`Install Showcase` 会先安装 `com.eframework.ui-extras` 和 `com.eframework.debug-console`。如果这些 package 刚被加入 manifest 或仍在解析，请等待 Unity Package Manager 导入和脚本编译完成，然后再次点击 `Install Showcase`。当所需 package 可用后，该动作会在模块模板缺失时复制模板，使用复制出的 `VirtualListShowcaseWindow.cs.meta` 修复已安装 virtual-list prefab 的脚本引用，刷新 assets，同步托管 Addressables，并将 StartUp 入口设置为 `GameApp.Modules.EFrameExtensionShowcase.Procedure.ProcedureEFrameExtensionShowcaseEntry`。在已有模块上重复运行 `Install Showcase` 会保留本地文件，同时仍执行 prefab 脚本引用修复。

维护框架模板时，直接用 Unity 打开 `test-fixtures/EFrameShowcaseUnity` 并编辑其中的 `Assets/Modules/EFrameExtensionShowcase/`。Release 前运行 `tools/Sync-EFrameShowcaseTemplate.ps1` 生成 package 模板；该脚本会把 `.cs` 转换为 `.cs.txt`，同时保留模块资源和 `.meta` 文件。

当前 showcase UI 由 `Assets/Modules/EFrameExtensionShowcase/Res/UI/Panels/EFrameExtensionShowcase/EFrameExtensionShowcaseView.prefab` 支撑，并且只列出 UI Virtual List 和 Debug Console 入口。UI Virtual List 由 `com.eframework.ui-extras` 提供，会打开模块自有的 `QVirtualListShowcaseWindow.prefab` 示例窗口，覆盖可变尺寸列表、网格、滚动、重新加载、刷新、池化和可见范围报告。Debug Console 会打开运行时控制台面板。
