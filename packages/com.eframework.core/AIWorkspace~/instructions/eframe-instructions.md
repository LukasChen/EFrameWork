---
name: "EFrame 规则"
description: "编辑 EFrame 项目中的 runtime、editor tooling、Procedure、QUI、UIController、启动流程、玩法功能代码、资源加载或生成工具时使用。"
applyTo: "{**/packages/com.eframework.core/Runtime/**/*.cs,**/packages/com.eframework.core/Editor/**/*.cs,**/Assets/App/Runtime/**/*.cs,**/Assets/Modules/**/*.cs,**/Assets/Scripts/**/*.cs,**/Assets/**/Editor/**/*.cs,**/Assets/Scripts/Editor/**/*.cs,**/Assets/**/*.prefab,**/Assets/**/*.unity,**/Assets/**/*.asset}"
---

# EFrame Framework 规则

本文件是使用 EFrame 的项目共享同步契约，只保留必须常驻的稳定边界。

## 协作边界

- 当用户是在提出问题、要求解释或分析方案时，先按讨论处理：可以做必要的只读检查来回答，但在编辑文件、执行初始化/安装/刷新动作或其他会改变项目状态的操作前，必须先确认方案并等待明确批准。
- 不要直接修改同步得到的 `eframe-*` instruction、skill、reference 或 managed block 来表达项目私有差异；项目私有规则必须写在项目自有 instruction 或其他项目拥有的位置。

## 启动与 Context

- 启动流程状态使用 EFrame 自有的 `EFrameProcedure` 和 `EFrameProcedureComponent`；不要绕过框架状态机手工驱动流程切换。
- 修改启动链路时必须保证 `EFrame.Initialize(...)` 先完成。
- 在 framework-aware 类型内部优先使用注入的 `Context`；只有启动、静态入口或非注入场景才使用 `EFrame.UI`、`EFrame.Assets`、`EFrame.Data` 等静态快捷入口。

## UI 常驻边界

- 业务 UI 推荐入口只暴露一条路径：通过 `EFrame.UI` 和 `UIControllerBase<TGeneratedView>` 管理页面、弹窗和提示。
- 普通业务 `UIControllerBase` 会默认绑定 `EFrame.Current`；不要在 Procedure 或父 controller 中重复手写 `BindContext(Context)`，除非是测试、框架内部或明确需要覆盖特殊 `EFrameContext`。
- 多个弹窗、引导、奖励提示需要顺序展示时，使用 `EFrame.UI.Queue` 或 `UIControllerBase.EnqueueToShow(...)` / `EnqueueToShowAsync(...)`；流程切换但希望保留剩余窗口时先 `EFrame.UI.Queue.Pause()`，回到可展示流程后 `Resume()`。
- `QUI`、`IUIService`、`UIViewHandle` 是框架内部或进阶扩展概念，不作为普通业务代码的直接入口。
- Controller 内访问生成 View 时使用 `CurrentView`；不要让业务逻辑直接保存或驱动 `UIViewHandle`。

## 资源常驻边界

- 运行时资源 id 应来自自动生成的 `ResPath`；不要在业务代码中手写 Addressables address 字符串或维护自定义路径中心类。
- `AssetReference` 可作为 Editor 配置字段，但进入运行时前应解析为 generated asset id。
- `Assets/App/Res`、`Assets/Scenes` 和 `Assets/Modules` 下的框架托管资源由 EFrame editor automation 维护 Addressables group、address、label 和 generated path；不要手动编辑这些托管条目。

## Editor 与 Unity 序列化

- Editor 代码只负责生成、导入、菜单命令、校验和 Inspector 扩展；Runtime 程序集不要引用 Editor 程序集，也不要把运行时业务逻辑塞进 `Editor` 程序集。
- 修改 prefab、scene object 或 `.asset` 时，业务 AI 优先直接编辑 Unity YAML；修改必须局部、透明、可审查，并保持 `guid`、`fileID`、`PrefabInstance`、`m_Modification`、组件顺序、层级和引用关系稳定。
- 禁止重写整个 Unity 资源文件、批量重排序列化块，或做不透明的大范围文本替换制造 Unity 序列化噪音。
- 只有直接 YAML 修改出现异常、无法完成、无法保持引用稳定，或验证后发现 Unity 序列化状态异常时，再调用 Unity Editor API 或其他 fallback 手段修复和保存，例如 `Undo`、`SerializedObject`、`PrefabUtility`、`EditorUtility.SetDirty`、`AssetDatabase.SaveAssets/Refresh`。
