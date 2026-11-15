# Starts Claude Code in its own worktree with its own database and API port.
# Run it from the repository root, one terminal per worktree:
#   ./tools/start-worktree.ps1 -Name orders-csv-export -ApiPort 5081
param(
    [Parameter(Mandatory = $true)] [string]$Name,
    [int]$ApiPort = 5081,
    [string]$Server = 'localhost'
)

$database = 'BookstoreDb_' + ($Name -replace '[^A-Za-z0-9]', '_')

# Environment variables override user-secrets, so this terminal gets its own database.
$env:ConnectionStrings__BookstoreDb = "Server=$Server;Database=$database;Integrated Security=true;TrustServerCertificate=true"
$env:ASPNETCORE_URLS = "http://localhost:$ApiPort"

Write-Output "worktree: $Name  database: $database  api: $env:ASPNETCORE_URLS"
claude --worktree $Name
