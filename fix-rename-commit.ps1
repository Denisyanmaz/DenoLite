# Fix "file already exists" when committing JiraLite -> DenoLite rename on Windows
# Run this in PowerShell from the repo root, with Cursor/VS Code and any Git UI closed.

# 1. Remove stale lock if present
$lock = Join-Path $PSScriptRoot ".git\index.lock"
if (Test-Path $lock) { Remove-Item -Force $lock; Write-Host "Removed index.lock" }

# 2. Remove old JiraLite paths from the index (keeps files on disk)
Set-Location $PSScriptRoot
git rm -r --cached JiraLite.Api 2>$null
git rm -r --cached JiraLite.Application 2>$null
git rm -r --cached JiraLite.Domain 2>$null
git rm -r --cached JiraLite.Infrastructure 2>$null
git rm -r --cached JiraLite.Tests 2>$null
git rm -r --cached JiraLite.Web 2>$null
git rm --cached JiraLite.slnx 2>$null

# 3. Add all DenoLite paths
git add DenoLite.Api/
git add DenoLite.Application/
git add DenoLite.Domain/
git add DenoLite.Infrastructure/
git add DenoLite.Tests/
git add DenoLite.Web/
git add DenoLite.slnx

Write-Host "`nDone. Check with: git status"
Write-Host "Then commit with: git commit -m `"Rename JiraLite to DenoLite`""
