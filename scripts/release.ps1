#!/usr/bin/env pwsh
<#
.SYNOPSIS
    One-command release: bump version, stage, commit, tag, push.
.PARAMETER Type
    Bump type: patch, minor, major
.PARAMETER Message
    Optional release message. Auto-generated if omitted.
.EXAMPLE
    ./scripts/release.ps1 -Type patch
    ./scripts/release.ps1 -Type minor -Message "Add dark mode support"
#>
param(
    [ValidateSet('patch', 'minor', 'major')]
    [string]$Type = 'patch',
    [string]$Message = ''
)

$ErrorActionPreference = 'Stop'

# 1. Bump version
Write-Host "=== Bumping version ($Type) ===" -ForegroundColor Cyan
& "$PSScriptRoot/update-version.ps1" -Type $Type

# 2. Read new version
$repoRoot = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $repoRoot "leishen" "VersionInfo.cs"
$content = Get-Content $versionFile -Raw
$newVersion = if ($content -match 'public const string Version = "(?<ver>[^"]+)";') { $Matches['ver'] } else { "?.?.?" }
$newTag = "v$newVersion"

# 3. Check git status
$status = git -C $repoRoot status --porcelain
if (-not $status) {
    Write-Warning "No changes to commit. Aborting."
    exit 1
}

# 4. Stage all
git -C $repoRoot add -A
Write-Host "  ✓ Staged all changes" -ForegroundColor Green

# 5. Commit
if ($Message) {
    $commitMsg = $Message
} else {
    $commitMsg = "chore: release $newTag"
}
git -C $repoRoot commit -m $commitMsg
Write-Host "  ✓ Committed: $commitMsg" -ForegroundColor Green

# 6. Tag
git -C $repoRoot tag $newTag
Write-Host "  ✓ Tagged: $newTag" -ForegroundColor Green

# 7. Push
Write-Host "`n=== Pushing to origin ===" -ForegroundColor Cyan
git -C $repoRoot push
git -C $repoRoot push --tags

Write-Host "`n✅ Release $newTag published!" -ForegroundColor Green
Write-Host "GitHub Actions will now build and create the release."
Write-Host "  → https://github.com/Bade-Gusi/leishen-pause/releases"
