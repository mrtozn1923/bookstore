# PostToolUse (dotnet test that exited 0): keeps warnings, errors and the last lines.
$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$response = $payload.tool_response
if (-not $response -or -not ($response.PSObject.Properties.Name -contains 'stdout')) { exit 0 }

$lines = @(([string]$response.stdout) -split "\r?\n")
if ($lines.Count -le 60) { exit 0 }

$important = @($lines | Where-Object { $_ -match '\b(error|warning|failed)\b' } | Select-Object -First 40)
$tail = @($lines | Select-Object -Last 15)
$kept = $important + $tail
$header = "[output shortened by .claude/hooks/short-test-output.ps1: kept $($kept.Count) of $($lines.Count) lines]"
$response.stdout = (@($header) + $kept) -join "`n"

[ordered]@{
    hookSpecificOutput = [ordered]@{
        hookEventName     = 'PostToolUse'
        updatedToolOutput = $response
    }
} | ConvertTo-Json -Depth 10 -Compress
exit 0
