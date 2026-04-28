# EFrame ResPath 规范

这份文档定义 EFrame 的资源地址使用规范，用于统一手写路径中心类、Addressables 自动生成代码以及运行时代码的调用边界。

## 1. 目标

- 不让业务代码直接散写 Addressables 地址字符串。
- 保留一层稳定、可阅读的运行时 API。
- 让编辑器可以基于 Addressables 配置自动生成强类型资源键。

## 2. 分层

`Assets/App/Runtime/Common/ResPath.cs`

- 手写入口层。
- 负责稳定 API、动态拼装辅助方法、少量通用别名。
- 允许被业务代码长期依赖，不应频繁破坏签名。

`Assets/App/Runtime/Generated/Res/ResPath.Generated.cs`

- 编辑器自动生成层。
- 由 Addressables 分组和地址扫描生成。
- 不手工修改；如果需要变化，应改 Addressables 配置或生成规则。

## 3. 使用顺序

优先级建议如下：

1. 编辑器可直接拖拽绑定的静态依赖，优先使用 `AssetReference`。
2. 代码显式加载或实例化的动态资源，优先使用 `ResPath.Generated.*` 常量。
3. 需要根据业务参数动态组合地址时，使用手写 `ResPath` 的辅助方法。
4. 不要在业务代码中直接调用 `AssetManager.LoadAsset("...")` 或 `AssetManager.Instantiate("...")` 并写裸字符串。

## 4. 地址规范

Addressables 地址由目录规则推导，约束如下：

- `Assets/App/Res/...` 生成地址如 `UI/Panels/HomeView`
- `Assets/App/Res/Bootstrap/...` 生成地址如 `Bootstrap/LoadingView`
- `Assets/App/Res/SceneAssets/...` 生成地址如 `SceneAssets/MainLighting`
- `Assets/Scenes/...` 生成地址如 `Scenes/StartUp`
- `Assets/Modules/<Name>/Res/...` 生成地址如 `Modules/<Name>/Res/UI/MainView`
- `Assets/Modules/<Name>/Scenes/...` 生成地址如 `Modules/<Name>/Scenes/Main`

## 5. 代码风格

- 手写 `ResPath.cs` 使用 `partial`，给生成层预留合并入口。
- 生成代码统一挂在 `ResPath.Generated` 下，避免和手写 API 混在一起。
- 模块内部若需要本地别名，可以提供模块级 `XxxResPath`，但底层地址仍应对齐框架规范。

## 6. 示例

```csharp
var panelPath = ResPath.Generated.UI.Panels.HomeView;
var startupScenePath = ResPath.Generated.Scenes.StartUp;
var shopMainView = ResPath.Generated.Modules.Shop.Res.UI.ShopMainView;

var dynamicPopup = ResPath.App.UI.Popup("RewardPopup");
var moduleScene = ResPath.Modules.Scene("Shop", "ShopMain");
```

## 7. 禁止事项

- 不要在业务逻辑里散写完整地址字符串。
- 不要手改 `ResPath.Generated.cs`。
- 不要让同一类资源同时走 `AssetReference` 和裸字符串两种入口。
- 不要把示例模块 `SampleModule` 当成长期业务占位不处理；确认结构后应替换为真实模块命名或清理无用示例。