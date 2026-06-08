#!/usr/bin/env pwsh
<#
.SYNOPSIS
    手动打包并发布所有 Nacos.NET NuGet 包
.PARAMETER Version
    包版本号，如 1.0.0。不指定则读取 csproj 中的 Version。
.PARAMETER ApiKey
    NuGet.org API Key。不指定则从环境变量 NUGET_API_KEY 读取。
.PARAMETER NuGetSource
    发布目标。默认 https://api.nuget.org/v3/index.json。
.PARAMETER OutputDir
    nupkg 输出目录。默认 ./nupkgs。
.EXAMPLE
    ./publish.ps1 -Version 1.0.1
    ./publish.ps1 -Version 1.0.1 -ApiKey "oy2abc..."
    ./publish.ps1 -NuGetSource "http://my-nexus/nuget"
#>
param(
    [string]$Version,
    [string]$ApiKey = $env:NUGET_API_KEY,
    [string]$NuGetSource = "https://api.nuget.org/v3/index.json",
    [string]$OutputDir = "./nupkgs"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projects = @(
    "src/Nacos.NET/Nacos.NET.csproj",
    "src/Nacos.NET.Extensions.Configuration/Nacos.NET.Extensions.Configuration.csproj",
    "src/Nacos.NET.Config.Encryption/Nacos.NET.Config.Encryption.csproj",
    "src/Nacos.NET.AspNetCore/Nacos.NET.AspNetCore.csproj"
)

# ── 清理输出目录 ──────────────────────────────────────────────
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDir | Out-Null

# ── 构建版本参数 ──────────────────────────────────────────────
$packArgs = @("--configuration", "Release", "--output", $OutputDir)
if ($Version) { $packArgs += @("-p:Version=$Version") }

# ── 打包 ──────────────────────────────────────────────────────
foreach ($proj in $projects) {
    Write-Host "Packing $proj ..." -ForegroundColor Cyan
    dotnet pack $proj @packArgs
    if ($LASTEXITCODE -ne 0) { throw "Pack failed for $proj" }
}

# ── 显示产物 ──────────────────────────────────────────────────
$packages = Get-ChildItem $OutputDir -Filter "*.nupkg"
Write-Host "`nPackages built:" -ForegroundColor Green
$packages | ForEach-Object { Write-Host "  $($_.Name)" }

# ── 发布 ──────────────────────────────────────────────────────
if (-not $ApiKey) {
    Write-Warning "No API key provided. Set -ApiKey or NUGET_API_KEY env var to publish."
    Write-Host "To publish manually, run:"
    Write-Host "  dotnet nuget push $OutputDir/*.nupkg --api-key <KEY> --source $NuGetSource --skip-duplicate"
    exit 0
}

foreach ($pkg in $packages) {
    Write-Host "Pushing $($pkg.Name) ..." -ForegroundColor Cyan
    dotnet nuget push $pkg.FullName `
        --api-key $ApiKey `
        --source $NuGetSource `
        --skip-duplicate
    if ($LASTEXITCODE -ne 0) { throw "Push failed for $($pkg.Name)" }
}

Write-Host "`nAll packages published successfully!" -ForegroundColor Green
