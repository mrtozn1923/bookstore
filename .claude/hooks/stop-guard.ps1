# Stop: keeps Claude working while committed migrations are modified or the API tests are red.
$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$root = git -C $payload.cwd rev-parse --show-toplevel 2>$null
if ($LASTEXITCODE -ne 0) { exit 0 }

function Block([string]$reason) {
    if ($payload.stop_hook_active) {
        # Already continued once because of this hook: tell the user, let Claude stop.
        [ordered]@{ systemMessage = "stop-guard: $reason" } | ConvertTo-Json -Compress
    }
    else {
        [ordered]@{ decision = 'block'; reason = $reason } | ConvertTo-Json -Compress
    }
    exit 0
}

# 1. Committed migrations changed by any tool, shell commands included.
$migrations = @(git -C $root diff --name-only --diff-filter=MD HEAD -- 'src/Bookstore.Api/Data/Migrations' |
    Where-Object { $_ -notmatch 'ModelSnapshot\.cs$' })
if ($migrations.Count -gt 0) {
    Block ("Committed migrations were changed: " + ($migrations -join ', ') +
        ". Restore them with git restore and add a new migration instead.")
}

# 2. API tests, only when C# files changed since the last green run.
$changes = git -C $root status --porcelain -- '*.cs' '*.csproj'
if (-not $changes) { exit 0 }
$diff = (git -C $root diff HEAD -- '*.cs' '*.csproj') + ($changes -join "`n")
$sha = [System.Security.Cryptography.SHA256]::Create()
$hash = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($diff))) -replace '-', ''
$stamp = Join-Path ([IO.Path]::GetTempPath()) "bookstore-green-$($payload.session_id).txt"
if ((Test-Path -LiteralPath $stamp) -and ((Get-Content -LiteralPath $stamp -Raw).Trim() -eq $hash)) { exit 0 }

$output = dotnet test (Join-Path $root 'Bookstore.slnx') --nologo 2>&1
if ($LASTEXITCODE -eq 0) {
    Set-Content -LiteralPath $stamp -Value $hash
    exit 0
}
$failures = @($output | Where-Object { "$_" -match '\b(error|failed)\b' } | Select-Object -First 15)
Block ("dotnet test failed (exit $LASTEXITCODE). Fix these before finishing:`n" + ($failures -join "`n"))
