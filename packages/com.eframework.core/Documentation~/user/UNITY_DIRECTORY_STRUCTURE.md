# EFrame Unity 目录结构规范

这份文档定义 EFrame 推荐的 Unity 项目基础目录结构，用于统一运行时代码、资源、编辑器工具、启动场景和业务模块的落点。

## 1. 目标

- 降低新项目冷启动时的目录分歧。
- 让 AI instruction、skills、初始化脚本和 Unity 代码生成同一套结构。
- 明确哪些目录属于主应用，哪些目录属于独立玩法或生成产物。

## 2. 基础目录

```text
Assets/
|- App/
|  |- Editor/
|  |- Res/
|  |  |- Bootstrap/
|  |  |- Audios/
|  |  |- Config/
|  |  |- Fonts/
|  |  |- FX/
|  |  |- Materials/
|  |  |- SceneAssets/
|  |  |- Shaders/
|  |  |- UI/
|  |     |- Common/
|  |     |- Panels/
|  |     |- Popups/
|  |     |- Widgets/
|  |- Runtime/
|     |- Common/
|     |- Config/
|     |- Data/
|     |- Events/
|     |- Generated/
|     |- Procedure/
|     |- Scene/
|     |- Services/
|     |- UI/
|        |- Controllers/
|        |- Views/
|        |- Widgets/
|- Modules/
|- Scenes/
|- Settings/
```

## 3. 主应用目录职责

`Assets/App/Runtime`

- 主应用运行时代码的统一入口。
- 适合放 Procedure、UIController、服务层、事件定义、配置读取、路径中心类。

`Assets/App/Res`

- 主应用业务资源入口。
- 适合放 UI 预制体、音频、配置资源、材质、特效、着色器、可复用场景资源。
- Addressables 推荐从这里收集和注册主应用资源。
- 不建议把 `.unity` 场景文件直接放进这里，避免与 `Assets/Scenes` 产生双重语义。

`Assets/App/Res/Bootstrap`

- 放启动期必须可用、且适合常驻的小体量资源。
- 例如首屏加载页、框架启动期配置、首页前置共享资源。
- 应保持克制，不要把普通业务模块资源塞进这里。

`Assets/App/Editor`

- 主应用专属编辑器工具。
- 仅存放 Editor 代码、菜单、导入器、生成器、校验器，不放运行时代码。

## 4. Runtime 细分约定

`Assets/App/Runtime/Common`

- 通用常量、`ResPath`、扩展方法、共享模型。
- `ResPath.cs` 作为手写稳定入口层，提供动态路径辅助方法和少量别名。

`Assets/App/Runtime/Config`

- 配置读取入口、配置服务、配置适配层。

`Assets/App/Runtime/Data`

- 本地数据结构、存档表、数据仓库、状态快照模型。

`Assets/App/Runtime/Events`

- 强类型事件定义和与业务相关的事件桥接。

`Assets/App/Runtime/Generated`

- 固定生成代码目录。
- 生成物不要散落到业务手写目录。
- Addressables 自动生成的 `ResPath.Generated.cs` 固定放在 `Assets/App/Runtime/Generated/Res/`，命名空间为 `EFramework.Generated`。
- App 级 UI binding 生成代码放在 `Assets/App/Runtime/Generated/UI/`；模块 UI binding 生成代码放在 `Assets/Modules/<Name>/Runtime/Generated/UI/`。

`Assets/App/Runtime/Procedure`

- `ProcedureXxx` 流程类。
- 只负责状态切换、进入退出编排、生命周期清理。

`Assets/App/Runtime/Scene`

- 场景入口组件、场景根对象协调代码、场景过渡辅助逻辑。

`Assets/App/Runtime/Services`

- 网络、账号、引导、任务等服务层。
- 服务负责长期存活的能力，不承担页面显示职责。

`Assets/App/Runtime/UI/Controllers`

- `XxxViewController`、弹窗控制器、UI 行为编排。

`Assets/App/Runtime/UI/Views`

- UI 视图脚本、绑定脚本、视图局部组件定义。

`Assets/App/Runtime/UI/Widgets`

- 可复用 UI 子组件逻辑，不单独承担主页面流程。

## 5. Res 细分约定

`Assets/App/Res/UI/Panels`

- 主页面、常驻页面、首页、设置页等面板预制体。
- 页面私有图片、动画、材质等资源可以跟随页面放在页面子目录中，不需要提升到 `Common`。
- 推荐结构：

```text
Assets/App/Res/UI/Panels/<PanelName>/
|- <PanelName>View.prefab
|- Sprites/
|- Animations/
|- Materials/
```

`Assets/App/Res/UI/Popups`

- 模态弹窗、确认框、奖励弹层等短生命周期预制体。
- 弹窗私有图片、动画、材质等资源可以跟随弹窗放在弹窗子目录中。
- 推荐结构：

```text
Assets/App/Res/UI/Popups/<PopupName>/
|- <PopupName>Popup.prefab
|- Sprites/
|- Animations/
|- Materials/
```

`Assets/App/Res/UI/Widgets`

- 列表项、通用条目、小型复用预制体。
- 仅被某个 Widget 使用的图片、动画、材质放在对应 Widget 子目录中。
- 推荐结构：

```text
Assets/App/Res/UI/Widgets/<WidgetName>/
|- <WidgetName>.prefab
|- Sprites/
|- Animations/
|- Materials/
```

`Assets/App/Res/UI/Common`

- UI 通用图集、共享节点、过渡资源。
- 只放跨页面、跨弹窗或跨 Widget 复用的 UI 资源。
- 推荐结构：

```text
Assets/App/Res/UI/Common/
|- Sprites/
|- Atlases/
|- Fonts/
|- Materials/
|- Transitions/
```

UI 资源归属判断：

- 只被一个页面使用：放 `Assets/App/Res/UI/Panels/<PanelName>/...`。
- 只被一个弹窗使用：放 `Assets/App/Res/UI/Popups/<PopupName>/...`。
- 只被一个复用组件使用：放 `Assets/App/Res/UI/Widgets/<WidgetName>/...`。
- 被多个 UI 复用：放 `Assets/App/Res/UI/Common/...`。
- UI 专用材质优先放 UI 目录内，不放到全局 `Assets/App/Res/Materials`。

`Assets/App/Res/SceneAssets`

- 放场景依赖资源、场景配置、Lighting 相关资产、可复用场景内容片段。
- 不放 `.unity` 场景文件本体，场景文件本身仍建议放在 `Assets/Scenes`。
- 推荐按场景或复用范围继续分层，避免 `SceneAssets` 变成混合资源池。
- 推荐结构：

```text
Assets/App/Res/SceneAssets/<SceneName>/
|- Prefabs/
|- Sprites/
|- Textures/
|- Materials/
|- Lighting/
|- Config/

Assets/App/Res/SceneAssets/Common/
|- Prefabs/
|- Textures/
|- Materials/
```

场景资源归属判断：

- `.unity` 场景文件本体：放 `Assets/Scenes/...`。
- 某个场景专属图片、贴图、材质、灯光、配置：放 `Assets/App/Res/SceneAssets/<SceneName>/...`。
- 多个场景复用的场景片段、贴图、材质：放 `Assets/App/Res/SceneAssets/Common/...`。
- 多个系统都复用、且不依附具体场景的材质：放 `Assets/App/Res/Materials/...`。

`Assets/App/Res/Materials`

- 放跨 UI、跨场景或跨系统复用的通用材质。
- 不放某个 UI 页面专用材质；这类资源应跟随 UI 页面、弹窗、Widget 或 `UI/Common`。
- 不放某个场景专用材质；这类资源应放 `Assets/App/Res/SceneAssets/<SceneName>/Materials`。

`Assets/App/Res/FX`

- 放主应用共享特效资源，不承担所有业务特效的集中收纳职责。
- 特效应按“业务拥有者”和“复用范围”归属：模块私有特效放模块内，跨模块共享特效才上移到这里。
- 一个特效通常是 Prefab、Materials、Textures、Shaders、Animations、VFXGraph 或粒子配置的组合，应以特效 Prefab 为中心组织私有依赖。
- 推荐结构：

```text
Assets/App/Res/FX/
|- Common/
|  |- Materials/
|  |- Textures/
|  |- Shaders/
|  |- Animations/
|  |- Prefabs/
|- UI/
|  |- Common/
|  |- Reward/
|  |- Transition/
|- Scene/
|  |- Common/
|  |- <SceneName>/
|- Gameplay/
|  |- Common/
|  |- Skill/
|  |- Hit/
|  |- Projectile/
|  |- Buff/
```

单个特效推荐结构：

```text
Assets/App/Res/FX/Gameplay/Skill/Fireball/
|- Fireball.prefab
|- Materials/
|- Textures/
|- Shaders/
|- Animations/
```

FX 资源归属判断：

- 只服务某个具体特效的材质、贴图、Shader、动画：跟随这个特效目录。
- 多个特效复用、但仍属于特效体系的资源：放 `Assets/App/Res/FX/Common/...`。
- 跨模块共享的 UI 特效：放 `Assets/App/Res/FX/UI/...`。
- 跨模块共享的场景表现特效：放 `Assets/App/Res/FX/Scene/...`。
- 跨模块共享的技能、受击、投射物、Buff 等玩法特效：放 `Assets/App/Res/FX/Gameplay/...`。
- 某个模块私有特效：放 `Assets/Modules/<ModuleName>/Res/FX/...`。
- 真正跨 UI、场景、FX、系统复用的材质或 Shader：放 `Assets/App/Res/Materials` 或 `Assets/App/Res/Shaders`。
- 只属于某个 UI 页面或弹窗的动效资源，如果不需要作为独立 FX 复用，可以跟随 UI 放 `Assets/App/Res/UI/.../Animations` 或 `Transitions`。
- 只属于某个场景的环境表现资源，如果不需要作为独立 FX 复用，可以跟随场景放 `Assets/App/Res/SceneAssets/<SceneName>/FX`。

## 6. 场景与设置

`Assets/Scenes`

- 放项目实际场景文件，启动场景命名保持 `StartUp.unity`。
- 启动场景只负责框架启动和流程切换，不承载常驻业务 UI。

`Assets/Settings`

- 放项目级 ScriptableObject 设置、Addressables 设置引用、全局配置资产。

## 7. Modules 结构

独立玩法、子系统、功能包或可拆分业务模块优先放到独立目录，不与主应用代码交叉堆放。

推荐结构：

```text
Assets/Modules/<Name>/
|- Editor/
|- Res/
|  |- UI/
|  |- SceneAssets/
|  |- FX/
|  |- ...
|- Runtime/
|  |- Common/
|  |- Procedure/
|  |- UI/
|  |- ...
|- Scenes/
```

Basic 初始化不再生成示例模块；后续新模块按同样结构直接创建到 `Assets/Modules/<Name>`，不依赖 `_Template` 目录。

约束：

- Module 自己的代码和资源尽量闭包在自己的目录里。
- 只有跨模块共享的内容才回收到 `Assets/App/...`。
- 如果模块还不成熟，也不要先扔到临时目录，优先在 `Assets/Modules/<Name>` 内演进。
- 如果业务项目确实是多玩法合集，可以在项目自有 instruction 中把 `Modules` 进一步细化为 `MiniGames`，但框架层默认保持抽象命名。
- 模块内资源可复用主应用的细分规则，例如 `Res/UI/Panels`、`Res/UI/Common`、`Res/SceneAssets/<SceneName>`、`Res/FX/Gameplay`。
- 模块私有图片、材质、特效不要提前放入 `Assets/App/Res`；确认跨模块共享后再上移。
- 模块私有 FX 以 `Assets/Modules/<Name>/Res/FX` 为入口，并按照 UI、Scene、Gameplay、Common 等复用范围继续细分。

### EFrame Extension Showcase

`Assets/Modules/EFrameExtensionShowcase/` 是框架提供的可选扩展示例模块安装位置。它通过 `EFrame Tools/项目初始化向导` 安装，不侵入 `Assets/App` 的 Basic 启动结构。

Showcase 模块安装后会自动设置为 StartUp 入口，用于查看 UI Virtual List 和 Debug Console 扩展演示入口；其入口 UI prefab 位于 `Assets/Modules/EFrameExtensionShowcase/Res/UI/Panels/EFrameExtensionShowcase/EFrameExtensionShowcaseView.prefab`；维护模板时直接打开 `test-fixtures/EFrameShowcaseUnity` 调试源模块，并通过 `tools/Sync-EFrameShowcaseTemplate.ps1` 同步回 package template。

详细安装方式见 [SAMPLES_AND_INITIALIZATION.md](SAMPLES_AND_INITIALIZATION.md)。

## 8. Addressables 目录与分组建议

为了支持按需加载，目录规划应至少体现“启动常驻内容”和“模块私有内容”的边界。

推荐分组思路：

- `Assets/App/Res/...` 对应主应用共享资源组，例如 `App-Shared`。
- 启动必须资源优先放在 `Assets/App/Res/Bootstrap/...`，对应 `App-Bootstrap`。
- `Assets/Modules/<Name>/Res/...` 对应模块私有资源组，例如 `Module-<Name>`。
- 场景文件建议单独成组，主场景来自 `Assets/Scenes/...`，模块场景来自 `Assets/Modules/<Name>/Scenes/...`。

目录规划原则：

- 不要把“可能按需卸载的模块资源”和“常驻共享资源”长期混放。
- 路径命名要稳定，便于 Addressables 地址和分组规则按目录推导。
- 如果后续要做远程更新，优先让组边界与目录边界保持一致，避免一个目录同时落到多个组。

ResPath 约束：

- 业务代码优先使用 `ResPath.Generated.*` 常量访问“代码主动加载”的 Addressables 地址。
- 手写 `ResPath.cs` 负责稳定 API，不承担整表手工维护。
- 详细规范见 [RESPATH_CONVENTION.md](RESPATH_CONVENTION.md)。

## 9. 自动 Addressables 与 ResPath

EFrame 的资源自动化建立在严格目录规范之上：资源放入约定目录后，Addressables 分组、地址和 `eframe-managed` 标签由框架工具统一管理，业务项目不再手动维护这些托管资源的 Addressables entry。

托管根目录：

- `Assets/App/Res/...`
- `Assets/Scenes/...`
- `Assets/Modules/<Name>/Res/...`
- `Assets/Modules/<Name>/Scenes/...`

自动映射规则：

- `Assets/App/Res/Bootstrap/...` -> `App Bootstrap Group`，地址相对 `Assets/App/Res` 生成，例如 `Bootstrap/LoadingView`
- `Assets/App/Res/SceneAssets/...` -> `App SceneAssets Group`，地址相对 `Assets/App/Res` 生成
- `Assets/App/Res/...` -> `App Shared Group`，地址相对 `Assets/App/Res` 生成
- `Assets/Scenes/...` -> `App Scenes Group`，地址相对 `Assets` 生成，例如 `Scenes/StartUp`
- `Assets/Modules/<Name>/Res/...` -> `Module <Name> Assets Group`，地址相对 `Assets` 生成
- `Assets/Modules/<Name>/Scenes/...` -> `Module <Name> Scenes Group`，地址相对 `Assets` 生成

生成物：

- `Assets/App/Runtime/Generated/Res/ResPath.Generated.cs` 由 Addressables 同步工具按“已选择的 ResPath 目录”生成，不要手动编辑，命名空间固定为 `EFramework.Generated`。
- UI binding 生成物按 prefab 归属落在 `Assets/App/Runtime/Generated/UI/` 或 `Assets/Modules/<Name>/Runtime/Generated/UI/`，命名空间固定为 `EFramework.Generated.UI`。
- 业务代码优先使用 `ResPath.Generated.*` 或 inspector-authored `AssetReference`，不要散写裸 Addressables 字符串。
- `ResPath.Generated` 是代码加载入口清单，不是完整资源总账。只被 prefab、材质、配置或其他 asset 引用的依赖资源可以继续由 Addressables 作为依赖收集，不必生成 ResPath 常量。

编辑器工具：

- `EFrame Tools/Addressables/Sync Groups And Generate ResPath` 打开托管资源窗口。
- 窗口可以选择哪些目录根下的直接文件参与 `ResPath.Generated`；子目录不会继承父目录选择，需要单独勾选。窗口也可查看当前 Addressables entry、期望目录映射、生成地址和 ResPath 成员之间的对应关系。
- 窗口会标记 `Synced`、`Needs Sync`、`Stale` 项，并显示目录结构检查结果。
- 导入、移动、删除托管目录资源时会自动同步；Player Build 前会执行同步与校验。

目录规范和自动化必须一起维护：如果项目把资源放到约定目录之外，框架不会把它纳入自动 Addressables 管线；如果项目确实需要额外目录，应在项目自有 instruction 中声明差异，并补充等价的路径中心和同步规则。是否进入 `ResPath.Generated` 则由同步工具中的目录选择决定。

## 10. 禁止事项

- 不要在 `Assets` 根下长期堆放零散脚本目录。
- 不要把 Editor 代码混入 Runtime 目录。
- 不要把 UI 预制体、音频、配置资源散写到多个随机目录。
- 不要把生成代码写进手写业务目录。
- 不要把完整 Simple Game Demo 放进 core package；该方向后续作为独立仓库处理。
## 11. 迁移原则

- 如果项目仍处于过渡结构，新增功能优先收敛到上述标准目录。
- 旧目录可以逐步迁移，但新代码不要继续扩散旧结构。
- 项目若确有特殊目录约束，应写在项目自己的 `project-*.instructions.md` 中，只记录差异。
