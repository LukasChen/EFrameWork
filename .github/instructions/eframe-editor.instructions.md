---
name: "EFrame Editor Rules"
description: "Use when editing Unity editor tooling, prefab generators, menus, importers, code generators, or any C# under Editor folders in an EFrame project."
applyTo: "{**/packages/com.eframework.core/Editor/**/*.cs,**/Assets/**/Editor/**/*.cs,**/Assets/Scripts/Editor/**/*.cs}"
---

# EFrame Editor Rules

This file is the synced editor contract for projects that use EFrameWork. Keep it focused on stable editor-side rules that should still apply after AI cold-start into a business project.

Project-local `project-*.instructions.md` overlays may add more specific editor rules. When they conflict with this framework contract, follow the more specific project rule while preserving generated paths and runtime naming expected by EFrame tools.

- Editor 代码只负责生成、导入、菜单命令、校验和 Inspector 扩展；Runtime 程序集不要引用 Editor 程序集，也不要把运行时业务逻辑塞进 `Editor` 程序集。
- 生成器必须稳定、可重复执行，并有明确覆盖策略；默认避免无提示覆盖用户文件，生成物进入 `Generated`、`App/Res/UI`、`Resources/UI` 等约定路径。
- 工具脚本先显式校验前置条件，例如目标目录、已有资源、覆盖意图和必需配置；失败时给出可修复的错误信息。
- 修改 prefab、scene object 或 `.asset` 时使用 Unity Editor 正规路径，例如 `Undo`、`PrefabUtility`、`EditorUtility.SetDirty`、`AssetDatabase.SaveAssets/Refresh`；不要用不透明的文本替换制造 Unity 序列化噪音。
- UI 预制体、场景对象或绑定代码必须与运行时命名对齐：`XxxView.prefab`、`XxxViewController`、`ProcedureXxx`。
- UI 基础配置（例如 `QUI` 依赖的 SortingLayer）优先进入 `ProjectBootstrap` 初始化链路；目录按职责分层到 `Editor/UI`、`Editor/ProjectBootstrap`、`Editor/Tools`。
- 编辑器工具如果改变业务项目落盘结构或生成代码样式，必须让 `eframe-feature-bootstrap` 和 `eframe-guideline-audit` 的规则继续匹配新的生成结果。
