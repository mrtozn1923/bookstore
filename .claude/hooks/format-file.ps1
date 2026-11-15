# PostToolUse (Edit|Write): formats only the file Claude just changed.
$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$path = [string]$payload.tool_input.file_path
if (-not $path -or -not (Test-Path -LiteralPath $path)) { exit 0 }

$root = git -C (Split-Path -Parent $path) rev-parse --show-toplevel 2>$null
if ($LASTEXITCODE -ne 0) { exit 0 }

Push-Location $root
try {
    $relative = (Resolve-Path -LiteralPath $path -Relative) -replace '^\.[\\/]', ''
    if ($relative -match '\.cs$') {
        $output = dotnet format whitespace . --folder --include $relative 2>&1
    }
    elseif ($relative -match '^web[\\/]bookstore-web[\\/].*\.(ts|html|scss)$' -and
            (Test-Path -LiteralPath 'web/bookstore-web/node_modules/prettier')) {
        Push-Location 'web/bookstore-web'
        $output = npx prettier --write $path 2>&1
        Pop-Location
    }
    else {
        exit 0
    }
    if ($LASTEXITCODE -ne 0) {
        [Console]::Error.WriteLine("format failed for ${relative}: $output")
        exit 1
    }
}
finally {
    Pop-Location
}
exit 0
