$skillSource = 'D:\SourceKaravi\GitHub\Karavi.Skills\skills\karavi-folder'
$root = 'D:\SourceKaravi\GitHub'
$repos = Get-ChildItem $root -Directory | Where-Object { Test-Path (Join-Path $_.FullName 'karavi') }

$deployedCount = 0

foreach ($repo in $repos) {
    # 1. Copy command scripts to karavi/karavi.scripts.command/
    $cmdDir = Join-Path $repo.FullName 'karavi\karavi.scripts.command'
    if (Test-Path $cmdDir) {
        Copy-Item (Join-Path $skillSource 'scripts\karavi-folder.init.ps1') -Destination $cmdDir -Force
        Copy-Item (Join-Path $skillSource 'scripts\karavi-folder.create.ps1') -Destination $cmdDir -Force
        Copy-Item (Join-Path $skillSource 'scripts\karavi-folder.clean.ps1') -Destination $cmdDir -Force
    }

    # 2. Deploy to .cursor/skills/karavi-folder if .cursor exists
    if (Test-Path (Join-Path $repo.FullName '.cursor')) {
        $cursorSkills = Join-Path $repo.FullName '.cursor\skills\karavi-folder'
        if (-not (Test-Path $cursorSkills)) { New-Item -ItemType Directory -Force -Path $cursorSkills | Out-Null }
        Copy-Item -Path "$skillSource\*" -Destination $cursorSkills -Recurse -Force
    }

    # 3. Deploy to .agents/skills/karavi-folder if .agents exists
    if (Test-Path (Join-Path $repo.FullName '.agents')) {
        $agentsSkills = Join-Path $repo.FullName '.agents\skills\karavi-folder'
        if (-not (Test-Path $agentsSkills)) { New-Item -ItemType Directory -Force -Path $agentsSkills | Out-Null }
        Copy-Item -Path "$skillSource\*" -Destination $agentsSkills -Recurse -Force
    }

    # 4. Deploy to .claude/skills/karavi-folder if .claude exists
    if (Test-Path (Join-Path $repo.FullName '.claude')) {
        $claudeSkills = Join-Path $repo.FullName '.claude\skills\karavi-folder'
        if (-not (Test-Path $claudeSkills)) { New-Item -ItemType Directory -Force -Path $claudeSkills | Out-Null }
        Copy-Item -Path "$skillSource\*" -Destination $claudeSkills -Recurse -Force
    }

    $deployedCount++
    Write-Host "Deployed karavi-folder skill & scripts to: $($repo.Name)"
}

Write-Host "`n=== DEPLOYMENT COMPLETED ==="
Write-Host "Total Repositories Managed: $deployedCount"
