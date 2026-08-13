#Requires -Version 5.1
<#
.SYNOPSIS
  Build BlazorGL KNIF effect XNBs with KNI MGCB and copy into BrowserClient wwwroot.

.NOTES
  Windows only (needs system d3dcompiler_47.dll). See README.md for Mac blocker.
#>
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Repo = Resolve-Path (Join-Path $Root "..\..")
$ClientEffects = Join-Path $Repo "PackTools\FSO.BrowserClient\wwwroot\Content\Effects"

Write-Host "Building FSO.BrowserEffects (KniPlatform=BlazorGL)…"
Push-Location $Root
try {
  dotnet build -c Release --nologo
  if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE)" }

  $built = Join-Path $Root "wwwroot\Content\Effects"
  if (-not (Test-Path (Join-Path $built "colorpoly2D.xnb"))) {
    throw "Expected XNB missing: $built\colorpoly2D.xnb"
  }

  New-Item -ItemType Directory -Force -Path $ClientEffects | Out-Null
  Copy-Item -Force (Join-Path $built "*.xnb") $ClientEffects
  Write-Host "Copied XNBs → $ClientEffects"
  Get-ChildItem $ClientEffects -Filter *.xnb | ForEach-Object { Write-Host "  $($_.Name) ($($_.Length) bytes)" }
}
finally {
  Pop-Location
}
