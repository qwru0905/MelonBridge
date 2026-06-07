#requires -version 5.1
<#
.SYNOPSIS
    Builds MelonBridge in Release and stages a distributable bundle under dist/.

.DESCRIPTION
    Produces dist/MelonBridge/ (the folder a user drops into UnityModManager/)
    and zips it as dist/MelonBridge-<version>.zip, with <version> read from Info.json.
#>
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $root "MelonBridge"
$outputDir = Join-Path $projectDir "bin\$Configuration\net48"
$distRoot = Join-Path $root "dist"
$stageDir = Join-Path $distRoot "MelonBridge"

Write-Host "Building MelonBridge ($Configuration)..."
& dotnet build (Join-Path $projectDir "MelonBridge.csproj") -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
New-Item -ItemType Directory -Path $stageDir | Out-Null

# Files that make up the shipped MelonBridge bundle (kept in sync with the
# CopyToGame target in MelonBridge.csproj).
$files = @(
    "MelonBridge.dll",
    "MelonLoader.dll",
    "Info.json",
    "Tomlet.dll",
    "0Harmony.Melon.dll",
    "MonoMod.Utils.Melon.dll",
    "MonoMod.RuntimeDetour.Melon.dll",
    "Mono.Cecil.dll"
)

foreach ($file in $files) {
    $source = Join-Path $outputDir $file
    if (-not (Test-Path $source)) { throw "Expected build output not found: $source" }
    Copy-Item $source -Destination $stageDir
}

$info = Get-Content (Join-Path $projectDir "Info.json") -Raw | ConvertFrom-Json
$version = $info.Version
$zipPath = Join-Path $distRoot "MelonBridge-$version.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Compress-Archive -Path "$stageDir\*" -DestinationPath $zipPath
Write-Host "Staged bundle: $stageDir"
Write-Host "Release zip:   $zipPath"
