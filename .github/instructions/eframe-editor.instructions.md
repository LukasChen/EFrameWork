---
name: "EFrame Editor Rules"
description: "Use when editing Unity editor tooling, prefab generators, menus, importers, code generators, or any C# under Editor folders in an EFrame project."
applyTo:
  - "**/Assets/**/Editor/**/*.cs"
  - "**/Assets/Scripts/Editor/**/*.cs"
---

# EFrame Editor Rules

- 编辑器脚本只负责生成、导入、菜单命令和校验，不把运行时业务逻辑塞进 `Editor` 程序集。
- 自动生成的资源和代码路径必须稳定、可重复执行，避免同一个工具每次运行都制造无关差异。
- 生成物优先进入约定目录，例如 `Generated`、`App/Res/UI`、`Resources/UI` 等显式路径；不要把生成结果散落在临时目录。
- 工具脚本需要显式校验前置条件，例如目标目录是否存在、资源是否已经存在、是否需要覆盖。
- 如果工具会创建 UI 预制体、场景对象或绑定代码，命名必须与运行时规范对齐：`XxxView.prefab`、`XxxViewController`、`ProcedureXxx`。
- 优先让编辑器工具服务于规范化迁移，不要继续固化临时命名和过渡目录。