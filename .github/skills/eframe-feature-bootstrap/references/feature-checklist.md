# EFrame Feature Checklist

## 1. 决策

- 这是新 `Procedure`、普通页面、弹窗还是独立 `MiniGame`？
- 是否真的发生了状态切换、相机切换、场景根对象切换？
- 是否只是当前流程里的一个 UI 子状态？

## 2. 目录映射

- 运行时代码：`Assets/App/Runtime/...`
- 主资源：`Assets/App/Res/...`
- 业务模块：`Assets/Modules/<Name>/{Runtime,Res,Editor,Scenes}`
- 生成代码：`Generated` 或固定生成目录
- 细分职责、场景目录和基础骨架以 `UNITY_DIRECTORY_STRUCTURE.md` 为准

## 3. 命名

- `ProcedureXxx`
- `XxxViewController`
- `XxxView.prefab`
- `ResPath` 或等价路径中心类

## 4. 生命周期模板

`Procedure`:

- `OnEnter`: 保存上下文、注册事件、创建对象、派发进入事件
- `OnLeave`: 退订事件、关闭 UI、销毁对象、清空引用

`UIController`:

- 创建时绑定按钮和订阅事件
- 销毁时解除监听并清空缓存引用
- 通过 `Show()` / `Hide()` 暴露显示语义

## 5. 资源与层级

- 资源路径集中管理，不散写字符串
- 主页面进入 `QuiPanel`
- 弹窗进入 `QuiPopUp`
- 提示层进入 `QuiTooltip` 或 `QuiTop`

## 6. 验证

- 能否在不改启动场景业务内容的前提下接入
- 是否存在成对清理
- 是否保留了可迁移到标准目录的空间