# EFrame ResPath 规范

这份文档定义 EFrame 的资源地址使用规范，用于统一手写路径中心类、Addressables 自动生成代码以及运行时代码的调用边界。

## 1. 目标

- 不让业务代码直接散写 Addressables 地址字符串。
- 保留一层稳定、可阅读的运行时 API。
- 让编辑器可以基于 Addressables 配置自动生成强类型资源键。

## 2. 分层

`Assets/App/Runtime/Generated/Res/ResPath.Generated.cs`

- 编辑器自动生成层。
- 由 Addressables 分组和地址扫描生成。
- 不手工修改；如果需要变化，应改 Addressables 配置或生成规则。
- 命名空间固定为 `EFramework.Generated`，初始化时不需要填写项目 Root Namespace。

## 3. 使用顺序

优先级建议如下：

1. 编辑器可直接拖拽绑定的静态依赖，优先使用 `AssetReference`。
2. 代码显式加载或实例化的动态资源，优先使用 `ResPath.*` 常量。
3. 需要根据业务参数动态组合地址时，在业务代码中提供项目自有辅助方法，但底层仍应返回 EFrame 规范地址。
4. 不要在业务代码中直接调用 `AssetManager.LoadAsset("...")` 或 `AssetManager.Instantiate("...")` 并写裸字符串。

## 4. 地址规范

Addressables 地址由目录规则推导，约束如下：

- `Assets/App/Res/...` 生成地址如 `UI/Panels/Home/HomeView`
- `Assets/App/Res/Bootstrap/...` 生成地址如 `Bootstrap/LoadingView`
- `Assets/App/Res/SceneAssets/...` 生成地址如 `SceneAssets/MainLighting`
- `Assets/Scenes/...` 生成地址如 `Scenes/StartUp`
- `Assets/Modules/<Name>/Res/...` 生成地址如 `Modules/<Name>/UI/Panels/Main/MainView`
- `Assets/Modules/<Name>/Scenes/...` 生成地址如 `Modules/<Name>/Scenes/Main`

目录加细后，地址继续按资源相对路径生成，不额外折叠目录层级。例如：

- `Assets/App/Res/UI/Panels/Home/HomeView.prefab` 生成 `UI/Panels/Home/HomeView`
- `Assets/App/Res/UI/Panels/Home/Sprites/Banner.png` 生成 `UI/Panels/Home/Sprites/Banner`
- `Assets/App/Res/UI/Common/Atlases/CommonUI.spriteatlas` 生成 `UI/Common/Atlases/CommonUI`
- `Assets/App/Res/SceneAssets/BattleScene/Textures/Ground.png` 生成 `SceneAssets/BattleScene/Textures/Ground`
- `Assets/App/Res/SceneAssets/Common/Materials/SharedGround.mat` 生成 `SceneAssets/Common/Materials/SharedGround`
- `Assets/App/Res/SceneAssets/Common/FX/Fireball/Fireball.prefab` 生成 `SceneAssets/Common/FX/Fireball/Fireball`
- `Assets/App/Res/UI/Common/FX/ButtonClick/ButtonClick.prefab` 生成 `UI/Common/FX/ButtonClick/ButtonClick`
- `Assets/Modules/Shop/Res/UI/Panels/ShopMain/ShopMainView.prefab` 生成 `Modules/Shop/UI/Panels/ShopMain/ShopMainView`
- `Assets/Modules/Battle/Res/SceneAssets/Common/FX/Fireball/Fireball.prefab` 生成 `Modules/Battle/SceneAssets/Common/FX/Fireball/Fireball`

命名建议：

- 资源目录和文件名使用稳定、可读的英文 PascalCase 或 UpperCamelCase。
- 不要用 `New Folder`、`Temp`、`Test`、`Final2` 这类临时命名进入 Addressables 管理目录。
- 不要为了整理美术源文件频繁移动已被代码引用的运行时资源；移动会改变生成地址。
- 原始工程文件、PSD 源文件、参考图若不需要运行时加载，优先放在非 Addressables 扫描目录，或在项目中单独约定 `ArtSource` 类目录。

## 5. 代码风格

- 生成代码统一挂在 `ResPath` 下，避免和手写 API 混在一起。
- 模块内部若需要本地别名，可以提供模块级 `XxxResPath`，但底层地址仍应对齐框架规范。

## 6. 示例

```csharp
var panelPath = ResPath.UI.Panels.Home.HomeView;
var homeBannerPath = ResPath.UI.Panels.Home.Sprites.Banner;
var commonAtlasPath = ResPath.UI.Common.Atlases.CommonUI;
var battleGroundPath = ResPath.SceneAssets.BattleScene.Textures.Ground;
var sharedFireballFxPath = ResPath.SceneAssets.Common.FX.Fireball.Fireball;
var moduleFireballFxPath = ResPath.Modules.Battle.SceneAssets.Common.FX.Fireball.Fireball;
var startupScenePath = ResPath.Scenes.StartUp;
var shopMainView = ResPath.Modules.Shop.UI.Panels.ShopMain.ShopMainView;
```

## 7. 禁止事项

- 不要在业务逻辑里散写完整地址字符串。
- 不要手改 `ResPath.Generated.cs`。
- 不要让同一类资源同时走 `AssetReference` 和裸字符串两种入口。
- 不要把临时验证资源长期留在托管目录；确认结构后应替换为真实命名或清理无用资源。
