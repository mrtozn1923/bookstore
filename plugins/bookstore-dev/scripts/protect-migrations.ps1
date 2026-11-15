# PreToolUse (Edit|Write): blocks edits to committed migration files; new ones stay editable.
$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$path = [string]$payload.tool_input.file_path
if (-not $path) { exit 0 }

# Windows sends backslashes; compare with forward slashes.
$normalized = $path.Replace('\', '/')
if ($normalized -notmatch '/src/Bookstore\.Api/Data/Migrations/') { exit 0 }
if (-not (Test-Path -LiteralPath $path)) { exit 0 }

$name = Split-Path -Leaf $path
git -C (Split-Path -Parent $path) ls-files --error-unmatch -- $name *> $null
if ($LASTEXITCODE -ne 0) { exit 0 }

[ordered]@{
    hookSpecificOutput = [ordered]@{
        hookEventName            = 'PreToolUse'
        permissionDecision       = 'deny'
        permissionDecisionReason = "$name is a committed migration file. Never edit it; add a new migration with /bookstore-dev:add-migration."
    }
} | ConvertTo-Json -Depth 5 -Compress
exit 0
