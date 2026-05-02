# EFrame UI 使用范例

这份范例演示如何在业务项目里接入一个首页和一个确认弹窗。使用者只需要按下面这条路径工作：

```text
创建 UI Prefab
-> 配置 QUIBinding 和绑定项
-> 点击 Generate Class 生成 View 访问类
-> 编写 UIController
-> 在 Procedure 或业务入口里显示 Controller
```

日常业务代码只写 Controller，不手写 View 类，不直接创建 Binding，也不自己管理 UI 生命周期对象。

## 1. 示例目标

本例包含两个 UI：

- `HomeView`：首页面板，包含金币文本、开始按钮、商店按钮、退出按钮。
- `ConfirmExitPopup`：退出确认弹窗，包含提示文本、取消按钮、确认按钮。

推荐目录：

```text
Assets/
|- App/
|  |- Runtime/
|  |  |- Events/
|  |  |  |- CurrencyChangedEvent.cs
|  |  |- Procedure/
|  |  |  |- ProcedureHome.cs
|  |  |- UI/
|  |     |- Controllers/
|  |        |- HomeViewController.cs
|  |        |- ConfirmExitPopupController.cs
|  |- Res/
|     |- UI/
|        |- Panels/
|        |  |- Home/
|        |     |- HomeView.prefab
|        |- Popups/
|           |- ConfirmExit/
|              |- ConfirmExitPopup.prefab
```

资源同步后，代码里使用生成路径：

```csharp
ResPath.Generated.UI.Panels.Home.HomeView
ResPath.Generated.UI.Popups.ConfirmExit.ConfirmExitPopup
```

不要在业务代码里散写 Addressables 字符串。

## 2. 创建首页 Prefab

创建 `Assets/App/Res/UI/Panels/Home/HomeView.prefab`。

根节点添加 `QUIBinding`，配置：

- `DefaultLayer`：`QuiPanel`
- `IsViewRoot`：开启
- `UsingCache`：按页面复杂度选择，首页通常可以开启
- `AnimationRootName`：动画根节点名，例如 `Root`
- `AccessClassName`：`v_HomeView`
- `AccessClassNamespace`：`EFramework.Generated.UI`

推荐节点：

```text
HomeView
|- Root
|  |- Header
|  |  |- CoinText
|  |- Content
|     |- StartButton
|     |- ShopButton
|     |- ExitButton
```

在 `QUIBinding` 里添加绑定项：

```text
CoinText       TMP_Text   binding: CoinText
StartButton    Button     binding: StartButton
ShopButton     Button     binding: ShopButton
ExitButton     Button     binding: ExitButton
```

点击 `Generate Class` 后，工具会生成 `v_HomeView`。这个类由工具维护，业务不要手写或长期修改。

## 3. 创建弹窗 Prefab

创建 `Assets/App/Res/UI/Popups/ConfirmExit/ConfirmExitPopup.prefab`。

根节点添加 `QUIBinding`，配置：

- `DefaultLayer`：`QuiPopUp`
- `IsViewRoot`：开启
- `UsingCache`：通常关闭
- `AnimationRootName`：动画根节点名，例如 `Window`
- `AccessClassName`：`v_ConfirmExitPopup`
- `AccessClassNamespace`：`EFramework.Generated.UI`

推荐节点：

```text
ConfirmExitPopup
|- Mask
|- Window
|  |- MessageText
|  |- CancelButton
|  |- ConfirmButton
```

绑定项：

```text
MessageText       TMP_Text   binding: MessageText
CancelButton      Button     binding: CancelButton
ConfirmButton     Button     binding: ConfirmButton
```

同样点击 `Generate Class`，生成 `v_ConfirmExitPopup`。

## 4. 定义业务事件

如果页面需要响应业务状态变化，可以定义强类型事件。

```csharp
using EFramework.Runtime.Event;

namespace Game.App.Events
{
    public struct CurrencyChangedEvent : IEvent
    {
        public int Coin;
    }
}
```

## 5. 编写首页 Controller

业务页面逻辑写在 `UIControllerBase<TGeneratedView>` 子类里。这里的 `TGeneratedView` 是 UI Binding 工具生成的 View 访问类。

```csharp
using System;
using Cysharp.Threading.Tasks;
using Game.App.Common;
using Game.App.Events;
using EFramework.Generated.UI;
using EFramework.Runtime.UI;

namespace Game.App.UI.Controllers
{
    public sealed class HomeViewController : UIControllerBase<v_HomeView>
    {
        private IDisposable m_currencySubscription;

        public Action StartRequested { get; set; }
        public Action ShopRequested { get; set; }
        public Action ExitConfirmed { get; set; }

        protected override string AssetPath => ResPath.Generated.UI.Panels.Home.HomeView;

        protected override void OnViewCreated()
        {
            base.OnViewCreated();

            AddButtonClickListener(CurrentView.StartButton, OnStartClicked);
            AddButtonClickListener(CurrentView.ShopButton, OnShopClicked);
            AddButtonClickListener(CurrentView.ExitButton, OnExitClicked);
        }

        protected override void OnViewOpened()
        {
            base.OnViewOpened();

            RefreshCoin();
            m_currencySubscription = Context.Events.SubscribeScoped<CurrencyChangedEvent>(OnCurrencyChanged);
        }

        protected override void OnViewClosed()
        {
            m_currencySubscription?.Dispose();
            m_currencySubscription = null;

            base.OnViewClosed();
        }

        protected override void OnViewDestroyed()
        {
            StartRequested = null;
            ShopRequested = null;
            ExitConfirmed = null;

            base.OnViewDestroyed();
        }

        private void RefreshCoin()
        {
            int coin = 0;
            CurrentView.CoinText.text = coin.ToString();
        }

        private void OnCurrencyChanged(CurrencyChangedEvent evt)
        {
            CurrentView.CoinText.text = evt.Coin.ToString();
        }

        private void OnStartClicked()
        {
            StartRequested?.Invoke();
        }

        private void OnShopClicked()
        {
            ShopRequested?.Invoke();
        }

        private void OnExitClicked()
        {
            ShowExitConfirmAsync().Forget();
        }

        private async UniTaskVoid ShowExitConfirmAsync()
        {
            var popup = new ConfirmExitPopupController();
            var result = await popup.ShowAndWaitAsync();

            if (result == ConfirmExitResult.Confirm)
            {
                ExitConfirmed?.Invoke();
            }
        }
    }
}
```

使用规则：

- 用 `CurrentView` 访问绑定组件。
- 在 `OnViewCreated()` 里绑定按钮。
- 在 `OnViewOpened()` 里刷新每次打开都需要更新的数据。
- 在 `OnViewClosed()` 里释放本次打开期间的订阅或异步任务。
- 在 `OnViewDestroyed()` 里清理 Controller 持有的实例级引用。

## 6. 编写弹窗 Controller

有返回值的弹窗继承 `UIPopupController<TGeneratedView, TResult>`。

```csharp
using Game.App.Common;
using EFramework.Generated.UI;
using EFramework.Runtime.UI;

namespace Game.App.UI.Controllers
{
    public enum ConfirmExitResult
    {
        Cancel,
        Confirm
    }

    public sealed class ConfirmExitPopupController : UIPopupController<v_ConfirmExitPopup, ConfirmExitResult>
    {
        protected override string AssetPath => ResPath.Generated.UI.Popups.ConfirmExit.ConfirmExitPopup;

        protected override void OnViewCreated()
        {
            base.OnViewCreated();

            AddButtonClickListener(CurrentView.CancelButton, () => CloseWithResult(ConfirmExitResult.Cancel));
            AddButtonClickListener(CurrentView.ConfirmButton, () => CloseWithResult(ConfirmExitResult.Confirm));
        }

        protected override void OnViewOpened()
        {
            base.OnViewOpened();
            CurrentView.MessageText.text = "确定要离开当前页面吗？";
        }
    }
}
```

调用弹窗：

```csharp
var popup = new ConfirmExitPopupController();
var result = await popup.ShowAndWaitAsync();

if (result == ConfirmExitResult.Confirm)
{
    // Continue flow.
}
```

弹窗 prefab 已经配置了 `DefaultLayer = QuiPopUp` 时，调用时通常不需要再传层级。

## 7. 在 Procedure 中显示页面

`Procedure` 负责流程进入、退出、预加载和跳转；具体按钮和弹窗逻辑留在 Controller 里。

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using Game.App.Common;
using Game.App.UI.Controllers;
using EFramework.Runtime.Asset;
using EFramework.Runtime.Procedure;

namespace Game.App.Procedure
{
    public sealed class ProcedureHome : EFrameProcedure
    {
        private HomeViewController m_homeController;

        protected internal override async UniTask OnPreloadAsync(IAssetPreloadScope assets, ProcedureEnterContext context)
        {
            await assets.PreloadAsync<GameObject>(ResPath.Generated.UI.Panels.Home.HomeView);
            await assets.PreloadAsync<GameObject>(ResPath.Generated.UI.Popups.ConfirmExit.ConfirmExitPopup);
        }

        protected internal override void OnEnter(ProcedureEnterContext context)
        {
            base.OnEnter(context);

            m_homeController = new HomeViewController
            {
                StartRequested = OnStartRequested,
                ShopRequested = OnShopRequested,
                ExitConfirmed = OnExitConfirmed
            };

            m_homeController.Show();
        }

        protected internal override void OnLeave(bool isShutdown)
        {
            m_homeController?.Dispose();
            m_homeController = null;

            base.OnLeave(isShutdown);
        }

        private void OnStartRequested()
        {
            ChangeState<ProcedureBattle>();
        }

        private void OnShopRequested()
        {
            ChangeState<ProcedureShop>();
        }

        private void OnExitConfirmed()
        {
            ChangeState<ProcedureMainMenu>();
        }
    }
}
```

如果只是页面内部的页签切换、局部状态变化或临时弹窗，优先留在当前 Controller 内，不新增 `Procedure`。

## 8. 直接显示一个 Controller

普通业务入口可以直接创建 Controller 并显示：

```csharp
var controller = new HomeViewController();
controller.Show();
```

需要等待动画完成时：

```csharp
await controller.ShowAsync();
await controller.HideAsync();
```

如果业务确实需要统一管理页面实例，可以在项目自己的 UI 管理类里保存 Controller，而不是保存生成 View 或底层生命周期对象。

## 9. 自检清单

交付 UI 前检查：

- Prefab 位于 `Assets/App/Res/UI/Panels/...`、`Assets/App/Res/UI/Popups/...` 或模块对应目录。
- Prefab 根节点已经添加 `QUIBinding`。
- `DefaultLayer` 已在 prefab 上配置好：页面 `QuiPanel`，弹窗 `QuiPopUp`。
- 已点击 `Generate Class` 生成 View 访问类。
- 业务没有手写继承 `BindingViewBase` 的 View 类。
- Controller 继承 `UIControllerBase<TGeneratedView>` 或 `UIPopupController<TGeneratedView, TResult>`。
- Controller 通过 `CurrentView` 访问组件。
- 资源路径使用 `ResPath.Generated`。
- `Procedure` 退出时关闭或 `Dispose()` Controller。
- 场景里没有手工创建绕过框架的常驻顶层 Canvas 或 EventSystem。
