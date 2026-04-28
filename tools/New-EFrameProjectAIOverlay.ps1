param(
    [string]$TargetRoot = (Get-Location).Path,
    [string]$FileName = "project-local.instructions.md",
    [switch]$Force
)

$resolvedTargetRoot = (Resolve-Path $TargetRoot).Path
$instructionsDirectory = Join-Path $resolvedTargetRoot ".github\instructions"
$overlayPath = Join-Path $instructionsDirectory $FileName

if (-not (Test-Path $instructionsDirectory)) {
    New-Item -ItemType Directory -Path $instructionsDirectory -Force | Out-Null
}

if ((Test-Path $overlayPath) -and -not $Force) {
    Write-Warning "Overlay instruction already exists: $overlayPath (use -Force to overwrite)"
    return
}

$content = @'
---
name: "Project Local Rules"
description: "Use when editing this specific game project's Unity runtime, UI, Procedure, scenes, resources, or editor tooling. Adds project-specific rules on top of synced EFrame instructions."
applyTo:
  - "**/Assets/**/*.cs"
  - "**/Assets/**/*.unity"
  - "**/Assets/**/*.prefab"
---

# Project Local Rules

这份文件是项目自己的 AI overlay 规则。

使用方式：

- 不要复制整份 EFrame 框架规则到这里；框架规则会通过 `eframe-*.instructions.md` 同步进当前项目并同时生效。
- 这里只写本项目独有的约束，例如命名例外、资源路径约定、流程拆分习惯、特定第三方插件接入方式。
- 如果本项目需要比框架默认值更严格或更具体的规则，直接在这里明确写出。

建议填写内容：

- 本项目的启动场景名、入口 `Procedure` 名称
- 本项目实际使用的目录结构和过渡目录
- 本项目 UI 命名或资源路径约定
- 本项目必须遵守的插件、表格、网络、Addressables 规则
'@

Set-Content -Path $overlayPath -Value $content -Encoding UTF8
Write-Host "Project overlay instruction created at $overlayPath"