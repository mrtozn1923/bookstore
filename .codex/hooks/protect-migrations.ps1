# PreToolUse (apply_patch): blocks patches that change or delete a committed migration file.
# New migration files ("*** Add File:") and migrations that are not committed yet stay editable.
# Written for Windows PowerShell 5.1 and PowerShell 7.
$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$patch = [string]$payload.tool_input.command
if (-not $patch) { exit 0 }

$root = git -C $payload.cwd rev-parse --show-toplevel 2>$null
if (-not $root) { exit 0 }
$root = [System.IO.Path]::GetFullPath($root).TrimEnd('\', '/')

$blocked = @()
foreach ($line in $patch -split "`r?`n") {
    if ($line -notmatch '^\*\*\* (Update|Delete) File: (.+)$') { continue }

    # The patch may use a path relative to the session directory or an absolute path.
    $path = $Matches[2].Trim()
    if (-not [System.IO.Path]::IsPathRooted($path)) { $path = Join-Path $payload.cwd $path }
    $full = [System.IO.Path]::GetFullPath($path)
    if (-not $full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) { continue }

    $relative = $full.Substring($root.Length).TrimStart('\', '/').Replace('\', '/')
    if (-not $relative.StartsWith('src/Bookstore.Api/Data/Migrations/', [System.StringComparison]::OrdinalIgnoreCase)) { continue }

    git -C $root ls-files --error-unmatch -- $relative *> $null
    if ($LASTEXITCODE -eq 0) { $blocked += (Split-Path -Leaf $full) }
}

if ($blocked.Count -eq 0) { exit 0 }

[ordered]@{
    hookSpecificOutput = [ordered]@{
        hookEventName            = 'PreToolUse'
        permissionDecision       = 'deny'
        permissionDecisionReason = "Committed migration files cannot be changed: $($blocked -join ', '). Add a new migration with the add-migration skill."
    }
} | ConvertTo-Json -Depth 5 -Compress
exit 0
