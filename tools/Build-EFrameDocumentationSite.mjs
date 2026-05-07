import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const docsRoot = join(root, "packages/com.eframework.core/Documentation~");

const navGroups = [
  {
    title: "开始",
    links: [
      ["首页", "index.html"],
      ["快速开始", "quickstart/index.html"],
      ["API 速查", "api/index.html"],
    ],
  },
  {
    title: "核心模块",
    links: [
      ["Core", "runtime/core/index.html"],
      ["Assets", "runtime/assets/index.html"],
      ["UI", "runtime/ui/index.html"],
      ["Data", "runtime/data/index.html"],
      ["Audio", "runtime/audio/index.html"],
      ["Events", "runtime/events/index.html"],
      ["Utils", "runtime/utils/index.html"],
    ],
  },
  {
    title: "扩展包",
    links: [
      ["扩展包总览", "extensions/index.html"],
      ["UI Extras", "extensions/ui-extras/index.html"],
      ["Effects", "extensions/effects/index.html"],
      ["Debug Console", "extensions/debug-console/index.html"],
      ["AI Loop", "extensions/ai-loop/index.html"],
    ],
  },
  {
    title: "示例",
    links: [
      ["UI 页面", "examples/ui-page/index.html"],
      ["Popup", "examples/popup/index.html"],
      ["DataTable", "examples/data-table/index.html"],
      ["资源预加载", "examples/resource-preload/index.html"],
      ["虚拟列表", "examples/virtual-list/index.html"],
    ],
  },
  {
    title: "编辑器与 AI",
    links: [
      ["Bootstrap", "editor/bootstrap/index.html"],
      ["UI 工具", "editor/ui-tools/index.html"],
      ["音频工具", "editor/audio-tools/index.html"],
      ["AI 协作层", "ai/index.html"],
    ],
  },
];

function rel(from, to) {
  const base = dirname(join(docsRoot, from));
  return relative(base, join(docsRoot, to)).replaceAll("\\", "/") || ".";
}

function esc(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function inlineCode(text) {
  return esc(text).replace(/`([^`]+)`/g, "<code>$1</code>");
}

function p(text) {
  return `<p>${inlineCode(text)}</p>`;
}

function list(items) {
  return `<ul>${items.map((item) => `<li>${inlineCode(item)}</li>`).join("")}</ul>`;
}

function code(lang, body) {
  return `<div class="api-signature"><pre><code class="language-${lang}">${esc(body.trim())}</code></pre></div>`;
}

function table(headers, rows) {
  return `<table><thead><tr>${headers.map((h) => `<th>${h}</th>`).join("")}</tr></thead><tbody>${rows
    .map((row) => `<tr>${row.map((cell) => `<td>${inlineCode(cell)}</td>`).join("")}</tr>`)
    .join("")}</tbody></table>`;
}

function cards(items, className = "article-card") {
  return `<div class="card-grid">${items
    .map(
      (item) => `<article class="${className}">
        <div class="eyebrow">${item.kicker || ""}</div>
        <h3>${item.href ? `<a href="${item.href}">${item.title}</a>` : item.title}</h3>
        ${p(item.body)}
        ${item.list ? list(item.list) : ""}
      </article>`,
    )
    .join("")}</div>`;
}

function rows(items) {
  return `<div class="module-list">${items
    .map((item) => `<div class="module-row"><strong>${inlineCode(item.title)}</strong>${p(item.body)}</div>`)
    .join("")}</div>`;
}

function section(title, body, eyebrow = "") {
  return `<section class="section">
    <div class="section-header"><div>${eyebrow ? `<div class="eyebrow">${eyebrow}</div>` : ""}<h2>${title}</h2></div></div>
    ${body}
  </section>`;
}

function apiSummary(rowsData) {
  return `<div class="panel">${table(["入口", "用途", "最小记忆点"], rowsData)}</div>`;
}

function quickTemplate(title, steps, snippet) {
  return `<div class="panel">
    <h3>${title}</h3>
    ${list(steps)}
    ${snippet ? code("csharp", snippet) : ""}
  </div>`;
}

function renderPage(page) {
  const stylePath = rel(page.path, "assets/style.css");
  const scriptPath = rel(page.path, "assets/site.js");
  const nav = navGroups
    .map(
      (group) => `<section class="nav-section">
        <h2>${group.title}</h2>
        <nav class="nav-links">
          ${group.links
            .map(([label, target]) => {
              const href = rel(page.path, target);
              const active = target === page.path ? ' class="is-active"' : "";
              return `<a${active} href="${href}">${label}</a>`;
            })
            .join("")}
        </nav>
      </section>`,
    )
    .join("");

  const crumbs = ["文档", ...(page.crumbs || [])]
    .map((crumb, index) => (index === 0 ? `<span><a href="${rel(page.path, "index.html")}">${crumb}</a></span>` : `<span>${crumb}</span>`))
    .join("");

  const meta = page.meta
    ? `<div class="meta-list">${page.meta.map(([k, v]) => `<div><dt>${k}</dt><dd>${inlineCode(v)}</dd></div>`).join("")}</div>`
    : "";

  return `<!DOCTYPE html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>${page.title} - EFrame 文档</title>
  <link rel="stylesheet" href="${stylePath}">
  <script defer src="${scriptPath}"></script>
</head>
<body>
  <div class="site-shell">
    <aside class="site-nav" data-open="false">
      <a class="brand" href="${rel(page.path, "index.html")}">
        <small>EFrame</small>
        <strong>框架文档</strong>
        <span>${page.navTitle || page.title}</span>
      </a>
      <button class="nav-toggle" type="button">展开文档导航</button>
      <div class="nav-sections">${nav}</div>
    </aside>
    <main class="content-area">
      <div class="page">
        <header class="page-header">
          <div class="breadcrumbs">${crumbs}</div>
          <div class="eyebrow">${page.eyebrow || "EFrame"}</div>
          <h1>${page.h1}</h1>
          <p class="lead">${inlineCode(page.lead)}</p>
          ${meta}
        </header>
        ${page.body}
        <footer class="site-footer">文档站由 <code>tools/Build-EFrameDocumentationSite.mjs</code> 生成。API 细节保持精简，完整行为以源码、package README 和同步 AI 契约为准。</footer>
      </div>
    </main>
  </div>
</body>
</html>
`.replace(/[ \t]+$/gm, "");
}

const coreModules = [
  ["Core", "启动、`EFrameContext`、Procedure、协程服务和上下文注入。"],
  ["Assets", "托管 Addressables、`ResPath`、异步句柄和资源释放责任。"],
  ["UI", "handle-first UI 主链：Controller -> Handle -> QUI -> BindingViewBase。"],
  ["Data", "`DataTable<T>`、稳定 `StorageKey`、dirty、迁移和结构化结果。"],
  ["Audio", "Music/SFX、Mixer、`AudioClipAsset`、池化、防抖和音乐 crossfade。"],
  ["Events", "结构体事件总线、Scoped 订阅和 App 生命周期广播。"],
  ["Utils", "输入、对象池、时间、HTTP、集合和低层辅助工具。"],
];

const packages = [
  ["com.eframework.core", "0.7.8", "必装核心", "Runtime、Editor bootstrap、Basic 模板、AI 同步源。"],
  ["com.eframework.ui-extras", "0.3.1", "可选 UI 扩展", "CheckableButton、Tabbar、交互反馈、图形辅助、虚拟列表/网格。"],
  ["com.eframework.effects", "0.1.1", "可选表现扩展", "飞行动画、图标弹跳、CameraShake 和配套 Editor。"],
  ["com.eframework.debug-console", "0.1.0", "可选调试扩展", "运行时日志面板和命令控制台。"],
  ["com.eframework.ai-loop", "0.1.0", "可选 Editor 自动化", "截图、UI 元素标注、输入模拟、录制和回放。"],
];

const pages = [
  {
    path: "index.html",
    title: "首页",
    navTitle: "文档首页",
    eyebrow: "Unity 轻量框架 + 可同步 AI 协作层",
    h1: "EFrame 文档入口",
    lead:
      "这里汇总 Core 模块、可选扩展包、初始化样板、常用示例和 AI 协作层。模块页提供用途、最小样板和精简 API 表。",
    meta: [
      ["Core", "com.eframework.core 0.7.8"],
      ["Unity", "6000.0 + URP + Addressables"],
      ["文档原则", "模块先行，API 精简"],
    ],
    body:
      section(
        "先选你的任务",
        cards([
          { kicker: "新项目", title: "初始化一个 EFrame 项目", href: "quickstart/index.html", body: "用项目初始化向导复制 Basic 模板，同步 AI 工作区，补齐 Addressables、UI 层级、音频和 build settings。" },
          { kicker: "UI", title: "创建一个 UI 页面", href: "examples/ui-page/index.html", body: "按资源路径、生成 View、Controller 和 Show/Hide 生命周期落地，不直接操作 BindingViewBase 生命周期。" },
          { kicker: "Data", title: "接入一张 DataTable", href: "examples/data-table/index.html", body: "用稳定 StorageKey、受控修改和 LastLoadResult/LastSaveResult 组织持久化数据。" },
          { kicker: "Resource", title: "梳理资源预加载", href: "examples/resource-preload/index.html", body: "资源放进托管目录，通过 ResPath 和显式 handle 管理加载与释放。" },
        ]),
        "Start Here",
      ) +
      section("核心模块", rows(coreModules.map(([title, body]) => ({ title, body }))), "Core Modules") +
      section(
        "可选扩展包",
        cards([
          { kicker: "UI", title: "UI Extras", href: "extensions/ui-extras/index.html", body: "把复用控件、交互反馈、图形辅助和虚拟列表从 Core 主链中拆出。" },
          { kicker: "FX", title: "Effects", href: "extensions/effects/index.html", body: "承载飞行动画、图标弹跳和轻量 CameraShake，业务项目按需安装。" },
          { kicker: "Debug", title: "Debug Console", href: "extensions/debug-console/index.html", body: "运行时日志与命令控制台，适合开发包和测试环境。" },
          { kicker: "AI", title: "AI Loop", href: "extensions/ai-loop/index.html", body: "Editor-only 截图、标注、输入模拟、录制和回放，服务 AI 辅助验收。" },
        ]),
        "Extensions",
      ) +
      section("包版本速览", `<div class="panel">${table(["Package", "Version", "定位", "说明"], packages)}</div>`, "Coverage"),
  },
  {
    path: "quickstart/index.html",
    title: "快速开始",
    crumbs: ["快速开始"],
    eyebrow: "Minimum Path",
    h1: "Basic 项目初始化",
    lead:
      "EFrame 项目初始化由项目初始化向导负责复制 Basic 模板并修复框架依赖。项目启动后，再按模块页面和示例补充业务能力。",
    meta: [
      ["入口", "EFrame Tools/项目初始化向导"],
      ["模板", "Basic"],
      ["关键产物", "StartUp.unity / ProcedureHome / HomeView.prefab"],
    ],
    body:
      section(
        "最短接入链路",
        `<div class="sequence-board">
          ${["安装 com.eframework.core", "打开项目初始化向导", "Initialize / Repair Project", "运行 StartUp 场景", "按模块添加业务能力"].map((t, i) => `<div class="sequence-step"><div class="sequence-step-meta">${i + 1}</div><div class="sequence-step-body"><strong>${t}</strong><p>${[
            "通过 UPM Git URL 或本地 file package 接入 Core。",
            "模板复制应通过初始化向导完成，以保证 Addressables 和 AI 契约同步。",
            "向导会复制 Basic、同步 AI、修复资源、UI layers、Audio、Tween 和 build settings。",
            "确认 `EFrameComponent` 初始化成功，Procedure 系统进入 `ProcedureHome`。",
            "UI、Data、Assets、Extensions 分别走对应文档和示例。"][i]}</p></div></div>`).join("")}
        </div>`,
      ) +
      section(
        "启动样板",
        quickTemplate(
          "在启动链之外，优先通过注入的 Context 访问服务",
          [
            "`EFrameComponent.Awake()` 暂停 Procedure，等待 `EFrame.Initialize(this)` 完成。",
            "框架创建 `EFrameContext`，再启动 Procedure 系统。",
            "业务对象拿到 `Context` 后使用 `Context.UI`、`Context.Data`、`Context.Assets`。",
          ],
          `public sealed class ProcedureHome : EFrameProcedure
{
    protected override void OnEnter(ProcedureEnterContext context)
    {
        base.OnEnter(context);
        Context.Audio.PlaySfx("Audio/UI/Open");
    }
}`,
        ),
        "Template",
      ) +
      section(
        "下一步按任务进入",
        cards([
          { kicker: "UI", title: "创建页面", href: "../examples/ui-page/index.html", body: "适合首页、设置页、主面板等常规页面。" },
          { kicker: "Popup", title: "创建弹窗", href: "../examples/popup/index.html", body: "适合确认框、奖励弹窗、异步结果弹窗。" },
          { kicker: "Data", title: "创建数据表", href: "../examples/data-table/index.html", body: "适合设置、进度、背包、任务状态等持久化数据。" },
          { kicker: "Extension", title: "安装扩展示例", href: "../extensions/index.html", body: "按需安装 UI Extras、Effects、Debug Console 或 AI Loop。" },
        ]),
      ),
  },
  {
    path: "api/index.html",
    title: "API 速查",
    crumbs: ["API 速查"],
    eyebrow: "Compact API Map",
    h1: "API 速查",
    lead:
      "本页汇总常用入口、用途和使用边界。完整参数和实现细节以模块页、源码和 package README 为准。",
    body:
      section(
        "Runtime 服务入口",
        apiSummary([
          ["`EFrame.UI` / `Context.UI`", "创建 UI binding、view handle、导航栈和缓存", "业务页面通常走 `UIControllerBase<TView>`，直接使用 `UIViewHandle` 仅适合框架或高级扩展。"],
          ["`EFrame.Assets` / `Context.Assets`", "异步加载、实例化和释放资源", "资源 id 来自 `ResPath`，返回 handle 必须释放。"],
          ["`EFrame.Data` / `Context.Data`", "注册、获取、批量加载保存 DataTable", "表负责 `StorageKey`、dirty、迁移和结果解释。"],
          ["`EFrame.Audio` / `Context.Audio`", "Music/SFX、Mixer、AudioClipAsset", "事件音频和触觉不在 Core，放扩展或业务层。"],
          ["`EFrame.Events` / `Context.Events`", "结构体事件发布订阅", "短生命周期对象使用 scoped 订阅。"],
        ]),
      ) +
      section(
        "Editor 入口",
        apiSummary([
          ["项目初始化向导", "Basic、Extensions、Showcase、DOTween Adapter", "新项目和修复都从这里走。"],
          ["Addressables 同步窗口", "托管目录、ResPath、build preflight", "托管目录下的 Addressables entries 由框架工具维护。"],
          ["UIAutoBindingEditor", "QUIBinding、ViewConfig、生成 View 访问类", "生成类围绕 `OnBindingSet()` 和 `CurrentView` 使用。"],
          ["AudioClipAssetEditor", "音频片段资产化和基础播放配置", "运行时通过 `EFrame.Audio` 消费。"],
        ]),
      ) +
      section("扩展包入口", `<div class="panel">${table(["Package", "入口", "用途"], [
        ["UI Extras", "`CheckableButton` / `Tabbar` / `QVirtualListView`", "复用 UI 控件、交互反馈、虚拟列表/网格。"],
        ["Effects", "`FlyAnimationSystem` / `IconBounceEffect` / `CameraShakeFx`", "表现动画和轻量镜头震动。"],
        ["Debug Console", "`EFrameDebugConsole.Show()`", "运行时日志与命令面板。"],
        ["AI Loop", "`EFrameAiLoop.*Async`", "Editor PlayMode 截图、标注、输入模拟和回放。"],
      ])}</div>`),
  },
];

const runtimePages = [
  {
    path: "runtime/core/index.html",
    title: "运行时 / Core",
    crumbs: ["运行时", "Core"],
    eyebrow: "Startup & Context",
    h1: "Core 启动与上下文",
    lead: "Core 负责框架初始化、`EFrameContext` 创建、服务聚合、Procedure 生命周期和上下文注入。",
    meta: [["主入口", "`EFrameComponent`"], ["上下文", "`EFrameContext`"], ["流程", "`EFrameProcedureManager`"]],
    body: section("特点和能力", rows([
      { title: "单一启动入口", body: "`EFrameComponent` 在启动场景中完成框架初始化。" },
      { title: "上下文聚合", body: "`EFrameContext` 持有 UI、Assets、Data、Audio、Events 和 Coroutine 服务。" },
      { title: "Procedure 生命周期", body: "`EFrameProcedure` 提供 preload、enter、leave 等流程阶段。" },
      { title: "自动注入", body: "框架创建的对象可通过 `IEFrameContextAware` 接收 Context。" },
    ])) + section("范例", quickTemplate("Procedure 中使用 Context", ["让 `EFrameComponent` 负责初始化。", "Procedure 里直接使用 `Context`。", "外层静态入口才使用 `EFrame.Current`。"], `protected override void OnEnter(ProcedureEnterContext context)
{
    base.OnEnter(context);
    Context.Events.Dispatch(new AppStartedEvent());
}`)) +
      section("API 细节", apiSummary([
        ["`EFrame.Initialize(component)`", "创建上下文并初始化服务", "启动链调用一次。"],
        ["`EFrame.Current`", "当前上下文", "只在外层边界使用。"],
        ["`EFrameContext.InjectInto(target)`", "给对象注入 Context", "框架创建对象会自动做。"],
        ["`EFrameProcedure`", "流程状态基类", "`OnPreloadAsync`、`OnEnter`、`OnLeave` 分工明确。"],
      ])),
  },
  {
    path: "runtime/assets/index.html",
    title: "运行时 / Assets",
    crumbs: ["运行时", "Assets"],
    eyebrow: "Addressables + Handles",
    h1: "Assets 资源加载",
    lead: "EFrame 托管 `Assets/App/Res`、`Assets/Scenes` 和 `Assets/Modules`，Editor 自动同步 Addressables，业务运行时通过 `ResPath` 和显式 handle 访问。",
    meta: [["服务", "`IAssetService`"], ["资源 id", "`ResPath`"], ["约束", "handle 必须释放"]],
    body: section("特点和能力", rows([
      { title: "托管目录", body: "`Assets/App/Res`、`Assets/Scenes`、`Assets/Modules` 由 EFrame 同步 Addressables。" },
      { title: "生成路径", body: "`ResPath` 只暴露被选择的资源入口。" },
      { title: "显式句柄", body: "`AssetHandle<T>` 和 `InstanceHandle` 表达释放责任。" },
      { title: "构建预检", body: "Player build 前会检查托管 entries 和生成路径是否漂移。" },
    ])) + section("范例", quickTemplate("Procedure 预加载 + Controller 使用", ["资源放在托管目录。", "通过 Addressables 同步窗口选择生成 ResPath 的目录。", "Procedure 热路径使用 preload scope。"], `protected override async UniTask OnPreloadAsync(IAssetPreloadScope assets, ProcedureEnterContext context)
{
    await assets.PreloadAsync(ResPath.UI.HomeView);
}`)) +
      section("API 细节", apiSummary([
        ["`LoadAsync<T>(assetId)`", "加载资源并返回 `AssetHandle<T>`", "读取 `handle.Asset`，结束时 `Dispose()`。"],
        ["`InstantiateAsync(assetId, parent)`", "实例化 Addressables 对象", "返回 `InstanceHandle`。"],
        ["`GetFromPool(assetId, parent)`", "复用型实例化", "由句柄归还。"],
        ["`EFrame Tools/Addressables/Sync Groups And Generate ResPath`", "同步托管目录并生成 ResPath", "手工修复和审查入口。"],
      ])),
  },
  {
    path: "runtime/ui/index.html",
    title: "运行时 / UI",
    crumbs: ["运行时", "UI"],
    eyebrow: "Handle-First UI",
    h1: "UI 主链与生命周期",
    lead: "页面、弹窗和提示都应通过 `UIControllerBase<TView>` 组织。View 是 binding 的强类型访问层，`QUI` 是宿主和缓存，`UIViewHandle<TView>` 管状态。",
    meta: [["主链", "Controller -> Handle -> QUI -> View"], ["Prefab", "`QUIBinding`"], ["相机", "`EFrameSceneCamera` + URP Overlay"]],
    body:
      section("特点和能力", rows([
        { title: "Controller 编排", body: "`UIControllerBase<TView>` 负责显示时机、事件绑定和业务响应。" },
        { title: "Handle 状态", body: "`UIViewHandle<TView>` 管理 Created、Open、Closed、Released 等状态。" },
        { title: "QUI 宿主", body: "`QUI` 负责实例化、缓存、层级、transition、导航栈和 UI camera。" },
        { title: "View 访问", body: "`BindingViewBase` 只承接 binding 和组件访问。" },
      ])) +
      section("按职责继续下钻", cards([
        { kicker: "Host", title: "QUI", href: "qui/index.html", body: "实例化、缓存、层级容器、默认 transition、导航栈和 UI Camera。" },
        { kicker: "View", title: "BindingViewBase", href: "binding-view/index.html", body: "binding 注入、组件映射和底层 open/close 原语。" },
        { kicker: "Handle", title: "UIViewHandle", href: "view-handle/index.html", body: "Created/Open/Closed/Released 状态与缓存语义。" },
        { kicker: "Controller", title: "UIControllerBase", href: "controller/index.html", body: "Show/Hide 生命周期、Hook 分工和按钮监听清理。" },
        { kicker: "Sequence", title: "Lifecycle", href: "lifecycle/index.html", body: "ShowAsync/HideAsync 真实调用顺序。" },
      ])) +
      section("范例", quickTemplate("页面 Controller 最小模式", ["Prefab 根节点挂 `QUIBinding`。", "生成 View 访问类或保持 View wrapper 只做组件访问。", "事件绑定放 `OnViewCreated()`，刷新放 `OnViewOpened()`。"], `public sealed class HomeController : UIControllerBase<HomeView>
{
    protected override string AssetPath => ResPath.UI.HomeView;

    protected override void OnViewCreated()
    {
        AddButtonClickListener(CurrentView.StartButton, OnStartClicked);
    }
}`)) +
      section("API 细节", apiSummary([
        ["`UIControllerBase<TView>.ShowAsync()`", "显示页面并按需创建 handle", "业务首选入口。"],
        ["`CurrentView`", "当前活跃 View 实例", "替代旧 `controller.View` facade。"],
        ["`OnViewCreated/Destroyed`", "实例级绑定和释放", "缓存关闭不会触发 Destroyed。"],
        ["`OnViewOpened/Closed`", "每次打开/关闭级刷新和暂停", "适合刷新 UI 状态。"],
      ])),
  },
  {
    path: "runtime/data/index.html",
    title: "运行时 / Data",
    crumbs: ["运行时", "Data"],
    eyebrow: "Persistence",
    h1: "Data 持久化数据",
    lead: "`DataTable<T>` 统一数据模型、StorageKey、dirty、迁移和结果对象。业务修改通过表的公开属性或方法进入。",
    meta: [["服务", "`IDataService`"], ["表", "`DataTable<T>`"], ["结果", "`LastLoadResult` / `LastSaveResult`"]],
    body: section("特点和能力", rows([
      { title: "稳定 StorageKey", body: "存档身份由表声明，不依赖可变类名或命名空间。" },
      { title: "受控修改", body: "`SetValue` / `Mutate` 只在值变化时标记 dirty。" },
      { title: "版本迁移", body: "`CurrentVersion` 和 `Migrate` 支持旧存档升级。" },
      { title: "结构化结果", body: "`LastLoadResult` / `LastSaveResult` 供 UI、遥测和恢复逻辑使用。" },
    ])) + section("子页面", cards([
      { kicker: "Service", title: "Data Service", href: "service/index.html", body: "注册、获取、批量加载保存和 App 生命周期接线。" },
      { kicker: "Table", title: "DataTable", href: "table/index.html", body: "StorageKey、dirty、迁移、默认数据和受控修改。" },
      { kicker: "Storage", title: "Storage & Results", href: "storage/index.html", body: "JsonFileStorage、备份回退和结构化诊断结果。" },
    ])) + section("范例", quickTemplate("一张设置表", ["模型可序列化。", "表声明稳定 `StorageKey`。", "所有修改走表的属性或方法。"], `[Serializable]
public sealed class PlayerSettingsData
{
    public bool MusicOn = true;
}

public sealed class PlayerSettingsTable : DataTable<PlayerSettingsData>
{
    public override string StorageKey => "player.settings";
    public bool MusicOn { get => Data.MusicOn; set => SetValue(ref Data.MusicOn, value); }
}`)) + section("API 细节", apiSummary([
      ["`RegisterTable<T>()`", "创建、注入 storage、立即加载", "启动流程集中注册。"],
      ["`GetTable<T>()`", "获取已注册表", "同一状态只保留 DataService 注册表实例。"],
      ["`SaveAll(forceFlush)`", "批量保存", "pause/quit 会自动接线。"],
      ["`LastLoadResult/LastSaveResult`", "可读诊断", "状态判断使用结构化结果对象。"],
    ])),
  },
  {
    path: "runtime/audio/index.html",
    title: "运行时 / Audio",
    crumbs: ["运行时", "Audio"],
    eyebrow: "Music / SFX / Mixer",
    h1: "Audio 基础播放",
    lead: "事件式音频映射和触觉反馈不再属于 Core。Core 提供 `AudioManager`、`AudioClipAsset`、Music/SFX、Mixer 音量、防抖和音乐 crossfade。",
    meta: [["服务", "`IAudioService`"], ["资产", "`AudioClipAsset`"], ["资源", "`AudioResourcePaths`"]],
    body: section("特点和能力", rows([
      { title: "Music / SFX", body: "统一播放入口和基础路由。" },
      { title: "Mixer 音量", body: "通过 Audio Bootstrap 准备 mixer 后统一控制音量。" },
      { title: "SFX 池化与防抖", body: "短音效播放带基础对象池和最小间隔控制。" },
      { title: "AudioClipAsset", body: "把多个音频片段和播放参数包装成资产。" },
    ])) + section("范例", quickTemplate("播放一个 UI 音效", ["用 Audio Bootstrap 准备 mixer 和基础资源。", "短音效走 `PlaySfx`。", "复杂音频集合可包装成 `AudioClipAsset`。"], `Context.Audio.PlaySfx("Audio/UI/Click");
await Context.Audio.PlayMusicAsync("Audio/Music/Home", fadeDuration: 0.35f);`)) + section("API 细节", apiSummary([
      ["`PlaySfx(assetPath)`", "播放短音效", "内部带基础池化和防抖。"],
      ["`PlayMusicAsync(assetPath, fadeDuration)`", "播放/切换 BGM", "双 AudioSource crossfade。"],
      ["`SetMusicVolume/SetSfxVolume`", "Mixer 音量控制", "依赖 Audio Bootstrap。"],
      ["`PlayAudioClipAsset(...)`", "播放包装后的音频资产", "适合多 clip 或参数化音频。"],
    ])),
  },
  {
    path: "runtime/events/index.html",
    title: "运行时 / Events",
    crumbs: ["运行时", "Events"],
    eyebrow: "Struct Event Bus",
    h1: "Events 事件总线",
    lead: "`EventBus` 适合 App 状态广播、UI 行为通知和轻量模块通信。事件 payload 建议保持轻量。",
    body: section("特点和能力", rows([
      { title: "结构体事件", body: "事件类型以结构体为主，适合轻量广播。" },
      { title: "Scoped 订阅", body: "短生命周期对象可以持有可释放订阅。" },
      { title: "内置 App 事件", body: "框架提供 started、paused、resumed、quit、update 等生命周期事件。" },
    ])) + section("范例", quickTemplate("Scoped 订阅", ["短生命周期对象使用 scoped 订阅。", "在销毁或关闭时释放 scope。", "事件 payload 用结构体。"], `m_subscription = Context.Events.SubscribeScoped<CurrencyChangedEvent>(OnCurrencyChanged);`)) + section("API 细节", apiSummary([
      ["`Dispatch<T>(eventData)`", "广播结构体事件", "payload 保持轻量。"],
      ["`Subscribe<T>(handler)`", "普通订阅", "调用方负责取消。"],
      ["`SubscribeScoped<T>(handler)`", "返回可释放订阅", "UI/Controller 首选。"],
      ["`AppStarted/Paused/Resumed/Quit/UpdateEvent`", "框架内置事件", "用于应用生命周期感知。"],
    ])),
  },
  {
    path: "runtime/utils/index.html",
    title: "运行时 / Utils",
    crumbs: ["运行时", "Utils"],
    eyebrow: "Utility Catalog",
    h1: "Utils 工具集",
    lead: "输入、对象池、时间、HTTP、集合、UI 辅助等都在这里。资源、事件、UI、Data 这类能力应优先走服务层。",
    body: section("特点和能力", rows([
      { title: "扩展辅助", body: "`UnityExtensions`、`StringExtensions`。" },
      { title: "集合与池化", body: "`ObjectPool<T>`、`PooledObject<T>`、`ListUtils`。" },
      { title: "时间与通用计算", body: "`EFrame.Time`、`GameTimeManager`、`DateUtils`、`RandomHelper`、`BezierHelper`。" },
      { title: "工程辅助", body: "`HttpHelper`、`IslandDetector`、`DynamicRenderScale`。" },
    ])) + section("范例", quickTemplate("对象池用法", ["高频临时对象使用 `ObjectPool<T>`。", "资源、UI、事件仍优先走服务层。", "释放时回到池中，避免跨帧散落临时分配。"], `var pool = new ObjectPool<Foo>();
var item = pool.Get();
pool.Release(item);`)) + section("API 细节", apiSummary([
      ["`ObjectPool<T>`", "对象复用", "适合高频临时对象。"],
      ["`RandomHelper` / `ListUtils`", "随机与集合辅助", "统一使用框架随机源。"],
      ["`EFrame.Time`", "框架时间入口", "默认本地时间，可显式同步服务器时间。"],
    ])),
  },
];

const uiSubPages = [
  ["runtime/ui/qui/index.html", "QUI", "UI 宿主、缓存池、层级容器和 transition 调度者。", [["`CreateBinding(assetPath)`", "实例化 prefab 并注入 Context", "根节点必须有 QUIBinding。"], ["`CreateViewHandle<TView>()`", "创建 View + Handle", "业务通常不直接调用。"], ["`PushView/PopView`", "导航栈", "handle-first。"], ["`RegisterSceneCamera`", "绑定 URP overlay UI camera", "场景相机挂 EFrameSceneCamera。"]]],
  ["runtime/ui/binding-view/index.html", "BindingViewBase", "Prefab binding 的强类型包装层，公开生命周期由 Controller/Handle 承担。", [["`SetBinding(binding)`", "延迟注入 binding", "触发 `OnBindingSet()`。"], ["`OnBindingSet()`", "缓存组件引用", "生成 View 访问类围绕它工作。"], ["`PrepareForOpen/Close`", "底层显隐原语", "由 handle 调用。"]]],
  ["runtime/ui/view-handle/index.html", "UIViewHandle", "把 View 变成有状态生命周期单元。", [["`Open/OpenAsync`", "进入 Opening/Open", "会处理 transition 竞态。"], ["`Close/CloseAsync`", "进入 Closed 或 Released", "取决于 UsingCache。"], ["`CloseAndDestroy`", "强制释放", "不保留缓存。"], ["`State/IsShowing/IsAlive`", "状态查询", "以 handle state 为准。"]]],
  ["runtime/ui/controller/index.html", "UIControllerBase", "Controller 负责编排，不承担 prefab 细节。", [["`Show/ShowAsync`", "显示入口", "按需创建 handle。"], ["`Hide/HideAsync`", "关闭入口", "按缓存决定释放。"], ["`CurrentView`", "活跃 view", "替代旧 View facade。"], ["`AddButtonClickListener`", "绑定并自动清理", "放在 OnViewCreated。"]]],
  ["runtime/ui/lifecycle/index.html", "生命周期", "一次 ShowAsync/HideAsync 的真实顺序。", [["`OnBindingSet`", "View 拿到 binding 后", "早于 Controller OnViewCreated。"], ["`OnViewCreated`", "实例首次创建", "绑定实例级事件。"], ["`OnViewOpened`", "每次打开成功后", "刷新显示状态。"], ["`OnViewClosed`", "每次关闭开始", "暂停 per-open 工作。"], ["`OnViewDestroyed`", "实例释放", "缓存关闭不会触发。"]]],
];

for (const [path, name, lead, api] of uiSubPages) {
  pages.push({
    path,
    title: `运行时 / UI / ${name}`,
    crumbs: ["运行时", "UI", name],
    eyebrow: "UI Detail",
    h1: `UI / ${name}`,
    lead,
    body: section("特点和能力", rows([
      { title: "职责", body: lead },
      { title: "业务使用", body: "常规业务页面优先通过 `UIControllerBase<TView>` 间接使用。" },
      { title: "高级使用", body: "自定义宿主、transition、缓存或导航时才直接接触该层类型。" },
    ])) + section("范例", quickTemplate(`${name} 使用位置`, ["页面 prefab 先配置 `QUIBinding`。", "Controller 通过 `CurrentView` 消费 View。", "生命周期操作由 Controller/Handle 触发。"], "")) + section("API 细节", apiSummary(api)),
  });
}

const dataSubPages = [
  ["runtime/data/service/index.html", "Data Service", "`IDataService` / `DataManager` 负责注册、批量加载保存和 App 生命周期接线。", [["`RegisterTable<T>()`", "创建并加载表", "校验 Key 和 StorageKey 唯一性。"], ["`GetTable<T>()`", "按类型获取表", "未注册会记录错误。"], ["`LoadAll/SaveAll`", "批量编排", "不理解单张表业务语义。"], ["`Dispose()`", "保存并释放表", "应用退出路径。"]]],
  ["runtime/data/table/index.html", "DataTable", "`DataTable<T>` 把 dirty、版本迁移、默认数据和结果对象统一收口。", [["`StorageKey`", "稳定存档身份", "使用显式稳定字符串。"], ["`SetValue/Mutate`", "受控修改", "值变化时才 dirty。"], ["`CurrentVersion/Migrate`", "版本迁移", "兼容旧存档。"], ["`LastLoadResult/LastSaveResult`", "结果诊断", "给 UI、遥测和恢复逻辑用。"]]],
  ["runtime/data/storage/index.html", "Storage & Results", "`JsonFileStorage` 负责文件写入、备份回退和结构化诊断。", [["`JsonFileStorage`", "默认文件存储", "tmp -> replace -> bak。"], ["`DataLoadResult`", "加载来源与状态", "区分主文件、备份、默认回退。"], ["`DataSaveResult`", "保存状态", "区分 dirty、force flush、跳过和失败。"], ["`DataEnvelope<T>`", "版本化包裹", "承载 version/time/data。"]]],
];

for (const [path, name, lead, api] of dataSubPages) {
  pages.push({
    path,
    title: `运行时 / Data / ${name}`,
    crumbs: ["运行时", "Data", name],
    eyebrow: "Data Detail",
    h1: `Data / ${name}`,
    lead,
    body: section("特点和能力", rows([
      { title: "Service", body: "只做注册、查找、批量加载保存和生命周期接线。" },
      { title: "Table", body: "拥有业务字段、修改入口、迁移、默认值和结果解释。" },
      { title: "Storage", body: "只关心字节、文件、备份、序列化和底层失败。" },
    ])) + section("范例", quickTemplate(`${name} 使用位置`, ["启动流程注册表。", "业务逻辑通过表的公开 API 修改数据。", "UI 和遥测读取结果对象，不依赖日志文本。"], "")) + section("API 细节", apiSummary(api)),
  });
}

const extensionPages = [
  {
    path: "extensions/index.html",
    title: "扩展包总览",
    crumbs: ["扩展包"],
    eyebrow: "Optional Packages",
    h1: "扩展包总览",
    lead: "UI Extras、Effects、Debug Console 和 AI Loop 都是按需安装的 package。初始化向导可以批量写入依赖，也可以安装 Showcase 模块演示重点能力。",
    body: section("特点和能力", `<div class="panel">${table(["Package", "适用场景", "能力边界"], [
      ["UI Extras", "复用控件、交互反馈、图形辅助、虚拟列表/网格", "页面生命周期仍由 Core 的 QUI/UIController 主链负责。"],
      ["Effects", "飞行动画、图标弹跳、CameraShake", "资源、音频和释放策略仍按 EFrame 资源流管理。"],
      ["Debug Console", "运行时日志面板和命令控制台", "面向开发、QA 和内部构建。"],
      ["AI Loop", "AI 辅助截图验收、输入模拟、录制回放", "Editor-only，用于编辑器自动化流程。"],
    ])}</div>`) + section("范例", quickTemplate("通过初始化向导安装", ["打开 `EFrame Tools/项目初始化向导`。", "在 Extensions 勾选需要的包。", "点击 Apply 写入 `Packages/manifest.json`。", "需要示例时再安装 Showcase。"], "")) + section("API 细节", apiSummary([
      ["`com.eframework.ui-extras`", "UI 控件和交互扩展", "运行时依赖 Core UI 主链。"],
      ["`com.eframework.effects`", "表现效果扩展", "按表现需求安装。"],
      ["`com.eframework.debug-console`", "运行时调试控制台", "开发、QA、内部构建使用。"],
      ["`com.eframework.ai-loop`", "Editor-only 验收自动化", "用于截图、输入和报告。"],
    ])),
  },
  {
    path: "extensions/ui-extras/index.html",
    title: "扩展包 / UI Extras",
    crumbs: ["扩展包", "UI Extras"],
    eyebrow: "Controls / Interaction / Virtual List",
    h1: "UI Extras 扩展包",
    lead: "这里的组件用于 prefab authoring 和局部交互增强。页面打开关闭仍然走 `UIControllerBase<TView>`。",
    body: section("特点和能力", rows([
      { title: "Components", body: "`CheckableButton`、`Tabbar`、`EmptyRayCasterGraphic`。" },
      { title: "Interaction", body: "`UIInteractionGuard`、`UIInteractionReporter`、`UIInteractionFeedback`。" },
      { title: "Graphics", body: "`UISmoothFill`、`UIRoundedRectImage`。" },
      { title: "VirtualList", body: "`QVirtualListView`、`QVirtualGridView`、`QVirtualListItem`、adapter contracts。" },
    ])) + section("范例", quickTemplate("虚拟列表接入", ["模板 item 可以 inactive。", "adapter 只绑定可见项。", "外部改变 viewport 尺寸后调用 `RefreshLayout()`。"], `using EFramework.Extensions.UI.VirtualList;

public sealed class InventoryAdapter : IQVirtualListAdapter
{
    public int Count => m_items.Count;
    public void BindItem(QVirtualListItem item, int index) { }
}`)) + section("API 细节", apiSummary([
      ["`CheckableButton`", "带 checked/toggle 语义的按钮", "适合 tab、开关型按钮。"],
      ["`Tabbar`", "基于 CheckableButton 的标签选择器", "适合少量固定页签。"],
      ["`UIInteractionFeedback`", "按钮/Toggle 状态反馈", "支持 scale、offset、color、material 等动作。"],
      ["`QVirtualListView`", "单轴虚拟列表", "适合可变尺寸列表。"],
      ["`QVirtualGridView`", "固定尺寸虚拟网格", "支持 cross-axis auto wrap。"],
    ])),
  },
  {
    path: "extensions/effects/index.html",
    title: "扩展包 / Effects",
    crumbs: ["扩展包", "Effects"],
    eyebrow: "Presentation Effects",
    h1: "Effects 扩展包",
    lead: "飞行动画、图标弹跳和 CameraShake 都适合做表现层能力。资源、音频和释放策略仍需按 EFrame 资源流管理。",
    body: section("特点和能力", rows([
      { title: "FlyAnimationSystem", body: "池化飞行体序列，支持路径、可视数量、音频和释放策略。" },
      { title: "IconBounceEffect", body: "配置化图标回弹表现，支持 config 或组件内联参数。" },
      { title: "CameraShakeFx", body: "轻量相机震动辅助。" },
      { title: "Editor", body: "配置资产和 debug runner 的 Inspector 支持。" },
    ])) + section("范例", quickTemplate("播放一次飞行动画", ["安装 `com.eframework.effects`。", "准备 `FlyAnimationConfig` 和 Addressables prefab。", "业务只发起表现请求，不直接管理池对象。"], `var handle = FlyAnimationSystem.Play(new FlySequenceRequest
{
    Config = coinFlyConfig,
    StartWorldPosition = from,
    EndWorldPosition = to,
    Count = 10
});`)) + section("API 细节", apiSummary([
      ["`FlyAnimationSystem.Play(request)`", "播放飞行序列", "返回 handle 供跟踪。"],
      ["`FlyAnimationConfig`", "飞行动画配置资产", "定义 prefab、路径、数量和音频。"],
      ["`IconBounceEffect`", "图标回弹组件", "可使用 config 或内联参数。"],
      ["`CameraShakeFx`", "轻量相机震动", "表现层辅助能力。"],
    ])),
  },
  {
    path: "extensions/debug-console/index.html",
    title: "扩展包 / Debug Console",
    crumbs: ["扩展包", "Debug Console"],
    eyebrow: "Runtime Debug",
    h1: "Debug Console 扩展包",
    lead: "该 package vendor Unity Ingame Debug Console，适合开发、QA 和内部构建。正式发布是否启用由项目配置决定。",
    body: section("特点和能力", rows([
      { title: "日志面板", body: "运行时查看 Unity log 输出。" },
      { title: "命令控制台", body: "支持上游 Ingame Debug Console 的命令能力。" },
      { title: "按需显示", body: "业务调试入口可调用 `EFrameDebugConsole.Show()`。" },
    ])) + section("范例", quickTemplate("按需打开控制台", ["安装 `com.eframework.debug-console`。", "在调试入口或隐藏手势中调用 Show。", "业务日志策略仍由项目单独定义。"], `using EFramework.Extensions.DebugConsole;

EFrameDebugConsole.Show();`)) + section("API 细节", apiSummary([
      ["`EFrameDebugConsole.Show()`", "实例化并显示控制台", "运行时代码按需调用。"],
      ["`IngameDebugConsole.Runtime`", "上游控制台运行时代码", "版本 1.8.2。"],
    ])),
  },
  {
    path: "extensions/ai-loop/index.html",
    title: "扩展包 / AI Loop",
    crumbs: ["扩展包", "AI Loop"],
    eyebrow: "Editor-only AI Validation",
    h1: "AI Loop 扩展包",
    lead: "AI Loop 是 Editor-only 工具包，用于 AI/测试流程产出截图、JSON artifact 和 Markdown report。",
    body: section("特点和能力", rows([
      { title: "截图与标注", body: "捕获 Game View / Editor Window，并可标注 EFrame UI 元素。" },
      { title: "输入模拟", body: "通过 uGUI ExecuteEvents 和 Input System 注入鼠标、键盘输入。" },
      { title: "录制回放", body: "将输入序列保存为 JSON，并在 PlayMode 中回放。" },
      { title: "报告产物", body: "输出 Markdown report、截图和 JSON artifact，便于 AI/测试审查。" },
    ])) + section("范例", quickTemplate("截图并点击 UI", ["进入 PlayMode 并确保 Game View 可渲染。", "截图可选择标注可交互 UI。", "鼠标模拟走 uGUI event handlers，不移动 OS cursor。"], `using EFramework.Editor.AILoop;

var shot = await EFrameAiLoop.CaptureGameViewAsync(new EFrameScreenshotOptions
{
    AnnotateElements = true
});

await EFrameAiLoop.ClickUiAsync(640, 360);`)) + section("API 细节", apiSummary([
      ["`CaptureGameViewAsync(options)`", "Game View 截图和标注", "结果含 `Path` / `ReportPath`。"],
      ["`ClickUiAsync(x, y)`", "uGUI 鼠标事件模拟", "不移动 OS cursor。"],
      ["`PressKeyAsync(key, duration)`", "Input System 键盘模拟", "不影响 legacy Input。"],
      ["`Start/StopRecordingInputAsync`", "录制 JSON 输入", "可用于 replay。"],
      ["`StartReplayInputAsync(path)`", "回放输入", "稳定性依赖场景和帧时序。"],
    ])),
  },
];

const editorPages = [
  {
    path: "editor/bootstrap/index.html",
    title: "编辑器 / Bootstrap",
    crumbs: ["编辑器", "Bootstrap"],
    eyebrow: "Project Bootstrap",
    h1: "Bootstrap 初始化工具",
    lead: "项目初始化向导负责 Basic、扩展包、Showcase、Addressables、Audio、DOTween Adapter、StartUp 和 build settings。模板复制由该工具统一执行。",
    body: section("特点和能力", rows([
      { title: "Basic 模板", body: "创建 StartUp 场景、Procedure、Home UI、音频资源和基础目录。" },
      { title: "资源同步", body: "修复托管 Addressables entries，并生成 `ResPath`。" },
      { title: "扩展安装", body: "按 Core 来源写入本地 file 或 git package 依赖。" },
      { title: "Showcase 模块", body: "安装可选扩展示范模块并设置 StartUp 入口。" },
    ])) + section("范例", quickTemplate("新项目 / 修复项目", ["打开 `EFrame Tools/项目初始化向导`。", "先执行 `Initialize / Repair Project`。", "再按需勾选 Extensions 并 Apply。", "需要扩展示范时安装 Showcase。"], "")) + section("API 细节", apiSummary([
      ["Initialize / Repair Project", "复制 Basic 并修复项目结构", "首选冷启动入口。"],
      ["Extensions -> Apply", "写入可选 package 依赖和 DOTween define", "取消勾选不删除已安装包。"],
      ["Showcase -> Install Showcase", "安装扩展示范模块", "依赖导入完成后可重复点击修复。"],
      ["Addressables Sync", "托管目录和 ResPath", "build 前会 preflight。"],
    ])),
  },
  {
    path: "editor/ui-tools/index.html",
    title: "编辑器 / UI 工具",
    crumbs: ["编辑器", "UI 工具"],
    eyebrow: "UI Authoring",
    h1: "UI 编辑器工具",
    lead: "日常 UI 制作应在 prefab 侧维护 `QUIBinding` 和组件绑定，生成访问类后由 Controller 通过 `CurrentView` 消费。",
    body: section("特点和能力", rows([
      { title: "QUIBinding 配置", body: "维护默认层级、缓存、动画根和动画时长。" },
      { title: "组件扫描", body: "扫描 RectTransform 树并维护可绑定组件。" },
      { title: "访问类生成", body: "生成围绕 `OnBindingSet()` 的强类型 View 访问类。" },
      { title: "QScroller 配置", body: "补充框架滚动组件字段和 UnityEvent。" },
    ])) + section("范例", quickTemplate("从 prefab 到 Controller", ["Prefab 根挂 `QUIBinding`。", "配置 DefaultLayer、UsingCache、AnimationRootName。", "扫描组件并 Generate Class。", "Controller 在 `OnViewCreated()` 绑定行为。"], "")) + section("API 细节", apiSummary([
      ["`UIAutoBindingEditor`", "维护 QUIBinding 和生成访问类", "减少手写 transform.Find。"],
      ["`ViewConfig`", "默认层级、缓存、动画根和时长", "运行时由 QUI/Handle 消费。"],
      ["`QScrollerEditor`", "补充框架滚动字段", "ScrollRect 仍负责基础滚动行为。"],
    ])),
  },
  {
    path: "editor/audio-tools/index.html",
    title: "编辑器 / 音频工具",
    crumbs: ["编辑器", "音频工具"],
    eyebrow: "Audio Authoring",
    h1: "音频编辑器工具",
    lead: "`AudioClipAssetEditor` 和 Audio Bootstrap 负责资产包装、Mixer 准备和基础资源落地。运行时仍通过 `EFrame.Audio` 消费。",
    body: section("特点和能力", rows([
      { title: "AudioClipAsset", body: "把一个或多个 clip 和播放参数包装为运行时入口。" },
      { title: "Mixer 初始化", body: "Audio Bootstrap 准备基础 mixer 和资源路径。" },
      { title: "运行时消费", body: "业务通过 `EFrame.Audio` 播放，不直接依赖 Editor 配置。" },
    ])) + section("范例", quickTemplate("创建一个 AudioClipAsset", ["完成 Audio Bootstrap。", "创建或选择 `AudioClipAsset`。", "配置 clips、volume、pitch、min interval。", "运行时用 `PlayAudioClipAsset` 或 `PlaySfx`。"], "")) + section("API 细节", apiSummary([
      ["`AudioClipAssetEditor`", "维护音频片段集合和播放参数", "适合 UI 音效、随机短音频。"],
      ["Audio Bootstrap", "Mixer 和基础资源准备", "项目初始化会处理。"],
      ["`AudioResourcePaths`", "框架音频资源路径", "Resources 侧入口。"],
    ])),
  },
  {
    path: "ai/index.html",
    title: "AI 协作层",
    crumbs: ["AI 协作层"],
    eyebrow: "Synced AI Contract",
    h1: "AI 协作层契约",
    lead: "AIWorkspace 同步业务项目需要的 instructions、skills、managed blocks 和 API support docs；maintainer-only release 与 manifest 规则留在外层工具目录。",
    body: section("同步边界", rows([
      { title: "`eframe-*`", body: "框架托管并同步到业务项目，项目不应手工 fork。" },
      { title: "`project-*`", body: "业务项目本地 overlay，只写项目差异。" },
      { title: "`maintainer-*`", body: "只服务框架仓库维护，不同步到业务项目。" },
      { title: "`EFRAME_AI_API_INDEX.md`", body: "面向 AI 的稳定 API 查询入口，同步到 `.github/eframe/`。" },
    ])) + section("何时更新 AI 层", list([
      "启动流程、目录结构、资源规则、UI 主链、DataTable 契约、bootstrap 工具发生变化。",
      "变更会影响 Copilot、Codex 或 Claude Code 在业务项目里的生成、重构或审查方式。",
      "发布或同步托管 AI 文件前按 maintainer release checklist 校验。",
    ])),
  },
];

const examplePages = [
  {
    path: "examples/ui-page/index.html",
    title: "示例 / UI 页面",
    crumbs: ["示例", "UI 页面"],
    eyebrow: "UI Workflow",
    h1: "UI 页面示例",
    lead: "示例覆盖资源路径、View 访问类、Controller、按钮绑定和显示调用。页面生命周期通过 Controller/Handle 进入。",
    body: section("范例", quickTemplate("Home 页面", ["Prefab 放 `Assets/App/Res/UI/Panels/Home/HomeView.prefab`。", "根节点挂 `QUIBinding`，默认层级 `QuiPanel`。", "生成 View 访问类。", "Controller 通过 `CurrentView` 绑定按钮。"], `public sealed class HomeController : UIControllerBase<HomeView>
{
    protected override string AssetPath => ResPath.UI.Panels.Home.HomeView;

    protected override void OnViewCreated()
    {
        AddButtonClickListener(CurrentView.StartButton, OnStartClicked);
    }
}`)) + section("常见扩展", cards([
      { kicker: "Refresh", title: "每次打开刷新状态", body: "把金币、红点、按钮状态放进 `OnViewOpened()`，构造函数保持无业务刷新。" },
      { kicker: "Cleanup", title: "实例释放清理", body: "实例级订阅、回调和持有资源在 `OnViewDestroyed()` 释放。" },
      { kicker: "Safe Area", title: "安全区", body: "移动端使用 prefab-authored `SafeAreaFitter`，业务代码保持布局无关。" },
    ])),
  },
  {
    path: "examples/popup/index.html",
    title: "示例 / Popup",
    crumbs: ["示例", "Popup"],
    eyebrow: "Popup Workflow",
    h1: "Popup 示例",
    lead: "`UIPopupController<TView,TResult>` 保持 handle-first 生命周期，并把用户选择变成 awaitable result。",
    body: section("范例", quickTemplate("确认弹窗", ["Prefab 默认层级 `QuiPopUp`。", "按钮在 `OnViewCreated()` 绑定。", "用 `CloseWithResult` 结束弹窗。"], `public enum ConfirmResult { Cancel, Confirm }

public sealed class ConfirmPopupController : UIPopupController<ConfirmPopupView, ConfirmResult>
{
    protected override string AssetPath => ResPath.UI.Popups.ConfirmPopup;

    protected override void OnViewCreated()
    {
        AddButtonClickListener(CurrentView.CancelButton, () => CloseWithResult(ConfirmResult.Cancel));
        AddButtonClickListener(CurrentView.ConfirmButton, () => CloseWithResult(ConfirmResult.Confirm));
    }
}`)) + section("调用方式", code("csharp", `var popup = new ConfirmPopupController();
var result = await popup.ShowAndWaitAsync(UILayer.QuiPopUp);
if (result == ConfirmResult.Confirm)
{
    SaveAndExit();
}`)),
  },
  {
    path: "examples/data-table/index.html",
    title: "示例 / DataTable",
    crumbs: ["示例", "DataTable"],
    eyebrow: "Persistence Workflow",
    h1: "DataTable 示例",
    lead: "示例展示稳定 StorageKey、受控修改入口、注册、保存和结果读取。业务修改回到表内部，让 dirty、迁移和保存结果保持一致。",
    body: section("范例", quickTemplate("玩家设置表", ["声明 serializable data。", "表给稳定 StorageKey。", "注册后读取并保存。"], `[Serializable]
public sealed class PlayerSettingsData
{
    public bool MusicOn = true;
    public bool SfxOn = true;
}

public sealed class PlayerSettingsTable : DataTable<PlayerSettingsData>
{
    public override string StorageKey => "player.settings";
    public bool MusicOn { get => Data.MusicOn; set => SetValue(ref Data.MusicOn, value); }
}`)) + section("使用方式", code("csharp", `Context.Data.RegisterTable<PlayerSettingsTable>();
var table = Context.Data.GetTable<PlayerSettingsTable>();
table.MusicOn = false;
table.Save();
Debug.Log(table.LastSaveResult.Message);`)),
  },
  {
    path: "examples/resource-preload/index.html",
    title: "示例 / 资源预加载",
    crumbs: ["示例", "资源预加载"],
    eyebrow: "Resource Workflow",
    h1: "资源预加载示例",
    lead: "资源流的核心是目录托管、ResPath、显式 handle 和清晰释放时机。",
    body: section("范例", quickTemplate("进入战斗前预加载 UI 和特效", ["资源放托管目录。", "生成 ResPath。", "Procedure preload scope 负责热路径。", "临时实例由 handle 释放。"], `protected override async UniTask OnPreloadAsync(IAssetPreloadScope assets, ProcedureEnterContext context)
{
    await assets.PreloadAsync(ResPath.UI.BattleHud);
    await assets.PreloadAsync(ResPath.Effects.CoinFly);
}

var fx = await Context.Assets.InstantiateAsync(ResPath.Effects.CoinFly, parent);
fx.Dispose();`)),
  },
  {
    path: "examples/virtual-list/index.html",
    title: "示例 / 虚拟列表",
    crumbs: ["示例", "虚拟列表"],
    eyebrow: "UI Extras Example",
    h1: "虚拟列表示例",
    lead: "虚拟列表把 item 创建限制在可见范围内。模板可以 inactive，adapter 只绑定当前 index 的数据。",
    body: section("范例", quickTemplate("背包列表 adapter", ["安装 `com.eframework.ui-extras`。", "Prefab 上配置 `QVirtualListView` 和 item template。", "adapter 只负责 Count 和 BindItem。"], `using EFramework.Extensions.UI.VirtualList;

public sealed class InventoryAdapter : IQVirtualListAdapter
{
    public int Count => Items.Count;

    public void BindItem(QVirtualListItem item, int index)
    {
        item.GetComponent<InventoryItemView>().SetData(Items[index]);
    }
}`)),
  },
];

pages.push(...runtimePages, ...extensionPages, ...editorPages, ...examplePages);

pages.push({
  path: "runtime/effects/index.html",
  title: "运行时 / Effects",
  crumbs: ["运行时", "Effects"],
  eyebrow: "Moved To Extension",
  h1: "Effects 模块迁移",
  lead: "Core 页保留迁移提示。飞行动画、IconBounce 和 CameraShake 的实际使用见 Effects 扩展包页面。",
  body: section("特点和能力", rows([
    { title: "Core 状态", body: "Core 不再承载表现效果实现，仅保留迁移入口。" },
    { title: "扩展包", body: "`com.eframework.effects` 提供 FlyAnimationSystem、IconBounceEffect、CameraShakeFx。" },
  ])) + section("范例", quickTemplate("迁移到 Effects 扩展包", ["在初始化向导的 Extensions 中勾选 Effects。", "执行 Apply 写入 `Packages/manifest.json`。", "业务表现调用迁移到 `EFramework.Extensions.Effects` 命名空间。"], "")) + section("API 细节", apiSummary([
    ["`com.eframework.effects`", "表现效果 package", "按需安装。"],
    ["`FlyAnimationSystem`", "飞行动画序列", "替代 Core 内旧表现入口。"],
    ["`IconBounceEffect` / `CameraShakeFx`", "局部表现辅助", "由扩展包维护。"],
  ])),
});

pages.push({
  path: "404.html",
  title: "页面未找到",
  h1: "页面不存在",
  lead: "回到首页或模块索引重新选择阅读路径。",
  body: section("常用入口", cards([
    { kicker: "Home", title: "文档首页", href: "index.html", body: "从任务入口重新开始。" },
    { kicker: "UI", title: "运行时 / UI", href: "runtime/ui/index.html", body: "查看当前最常用的模块页面。" },
  ])),
});

for (const page of pages) {
  const target = join(docsRoot, page.path);
  mkdirSync(dirname(target), { recursive: true });
  writeFileSync(target, renderPage(page), "utf8");
}

console.log(`Generated ${pages.length} documentation pages.`);
