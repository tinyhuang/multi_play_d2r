$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$errors = @()
$expectedName = 'tinyhuang'
$expectedEmail = 'tinyhuang@163.com'

function Add-CheckError {
    param([string]$Message)
    $script:errors += $Message
}

function Read-GitValue {
    param([string[]]$GitArgs)

    $value = & git -C $repoRoot @GitArgs
    if ($LASTEXITCODE -ne 0 -or $null -eq $value) {
        return ''
    }

    return ($value | Out-String).Trim()
}

# 1) WinForms project should not enable Native AOT / full trimming.
$appCsproj = Join-Path $repoRoot 'src/D2RMultiPlay.App/D2RMultiPlay.App.csproj'
$appText = Get-Content -Path $appCsproj -Raw
if ($appText -match '<PublishAot>\s*true\s*</PublishAot>') {
    Add-CheckError 'D2RMultiPlay.App.csproj: WinForms should not enable <PublishAot>true</PublishAot>.'
}
if ($appText -match '<TrimMode>\s*full\s*</TrimMode>') {
    Add-CheckError 'D2RMultiPlay.App.csproj: WinForms should not use <TrimMode>full</TrimMode>.'
}
if ($appText -notmatch '<ApplicationHighDpiMode>\s*PerMonitorV2\s*</ApplicationHighDpiMode>') {
    Add-CheckError 'D2RMultiPlay.App.csproj: expected <ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>.'
}

$appManifest = Join-Path $repoRoot 'src/D2RMultiPlay.App/app.manifest'
$appManifestText = Get-Content -Path $appManifest -Raw
if ($appManifestText -match '<dpiAwareness' -or $appManifestText -match '<dpiAware') {
    Add-CheckError 'app.manifest: remove DPI declarations (<dpiAwareness>/<dpiAware>) and use ApplicationHighDpiMode instead.'
}

# 2) Reject duplicate EmbeddedResource Include items for Strings.resx.
if ($appText -match '<EmbeddedResource\s+Include="Resources\\Strings\.resx"') {
    Add-CheckError 'D2RMultiPlay.App.csproj: Use EmbeddedResource Update for Strings.resx, not Include.'
}
if ($appText -match '<EmbeddedResource\s+Include="Resources\\Strings\.zh-CN\.resx"') {
    Add-CheckError 'D2RMultiPlay.App.csproj: Remove explicit Include for Strings.zh-CN.resx (implicit SDK item already exists).'
}

# 3) Block unsupported source-generated P/Invoke signatures.
$nativeMethodsPath = Join-Path $repoRoot 'src/D2RMultiPlay.Core/Interop/NativeMethods.cs'
$nativeText = Get-Content -Path $nativeMethodsPath -Raw

$badStructs = @('MONITORINFOEX', 'DEVMODE', 'STARTUPINFO')
foreach ($struct in $badStructs) {
    $pattern = "(?s)\[LibraryImport\([^\]]*\)\]\s*(?:\[[^\]]*\]\s*)*public\s+static\s+partial\s+\w+\s+\w+\([^;]*WinStructs\\.$struct[^;]*\);"
    if ([regex]::IsMatch($nativeText, $pattern)) {
        Add-CheckError "NativeMethods.cs: WinStructs.$struct must not be used in [LibraryImport] signatures; use [DllImport]."
    }
}

# 4) Ensure xUnit is globally imported in test project to avoid missing [Fact] namespace issues.
$globalUsingsPath = Join-Path $repoRoot 'tests/D2RMultiPlay.Core.Tests/GlobalUsings.cs'
if (-not (Test-Path $globalUsingsPath)) {
    Add-CheckError 'tests/D2RMultiPlay.Core.Tests/GlobalUsings.cs is missing; expected global using Xunit;'
} else {
    $globalUsingsText = Get-Content -Path $globalUsingsPath -Raw
    if ($globalUsingsText -notmatch 'global\s+using\s+Xunit\s*;') {
        Add-CheckError 'tests/D2RMultiPlay.Core.Tests/GlobalUsings.cs must contain: global using Xunit;'
    }
}

# 5) Ensure workflow actions stay on v5+ (Node24-ready).
$workflowPath = Join-Path $repoRoot '.github/workflows/build.yml'
$workflowText = Get-Content -Path $workflowPath -Raw
$requiredActions = @{
    'actions/checkout' = 5
    'actions/setup-dotnet' = 5
    'actions/upload-artifact' = 5
}

foreach ($action in $requiredActions.Keys) {
    $matches = [regex]::Matches($workflowText, "$action@v(?<ver>\d+)")
    if ($matches.Count -eq 0) {
        Add-CheckError "build.yml: missing $action@v$($requiredActions[$action]) or higher."
        continue
    }

    foreach ($m in $matches) {
        $ver = [int]$m.Groups['ver'].Value
        if ($ver -lt $requiredActions[$action]) {
            Add-CheckError "build.yml: $action must be v$($requiredActions[$action]) or higher, found v$ver."
        }
    }
}

# 6) Enforce commit identity policy on checked out HEAD.
$headAuthorName = Read-GitValue @('show', '-s', '--format=%an', 'HEAD')
$headAuthorEmail = Read-GitValue @('show', '-s', '--format=%ae', 'HEAD')
$headCommitterName = Read-GitValue @('show', '-s', '--format=%cn', 'HEAD')
$headCommitterEmail = Read-GitValue @('show', '-s', '--format=%ce', 'HEAD')

if ($headAuthorName -ne $expectedName -or $headAuthorEmail -ne $expectedEmail) {
    Add-CheckError "HEAD author must be $expectedName <$expectedEmail>, found $headAuthorName <$headAuthorEmail>."
}
if ($headCommitterName -ne $expectedName -or $headCommitterEmail -ne $expectedEmail) {
    Add-CheckError "HEAD committer must be $expectedName <$expectedEmail>, found $headCommitterName <$headCommitterEmail>."
}

if ($errors.Count -gt 0) {
    Write-Host 'CI precheck failed with the following issues:' -ForegroundColor Red
    foreach ($e in $errors) {
        Write-Host " - $e" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'CI precheck passed.' -ForegroundColor Green
