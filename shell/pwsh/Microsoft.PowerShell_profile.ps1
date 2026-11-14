# Bookstore terminal series: $PROFILE (PowerShell 7.6)

# --- History (post 02) ---
Set-PSReadLineOption -HistoryNoDuplicates -MaximumHistoryCount 20000
Set-PSReadLineOption -AddToHistoryHandler {
    param([string]$line)
    if ($line -match 'Authorization:|Bearer\s') {
        return $false
    }
    return [Microsoft.PowerShell.PSConsoleReadLine]::GetDefaultAddToHistoryOption($line)
}

# --- Line editing and predictions (post 02) ---
Set-PSReadLineOption -PredictionSource History
if (-not [Console]::IsOutputRedirected) {
    Set-PSReadLineOption -PredictionViewStyle ListView
}
Set-PSReadLineKeyHandler -Key Tab -Function MenuComplete
Set-PSReadLineKeyHandler -Key UpArrow -Function HistorySearchBackward
Set-PSReadLineKeyHandler -Key DownArrow -Function HistorySearchForward

# --- Completion (post 02) ---
if (Get-Command dotnet -ErrorAction Ignore) {
    dotnet completions script pwsh 2>$null | Out-String | Invoke-Expression
}

# --- Short commands (post 04) ---
function dt { dotnet test @args }
function db { dotnet build @args }
function gs { git status -sb @args }
function dtf {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(ValueFromRemainingArguments)][string[]]$Rest
    )
    dotnet test --filter "FullyQualifiedName~$Name" @Rest
}

# --- Modules and prompt (post 04) ---
if (Get-Module posh-git -ListAvailable) {
    Import-Module posh-git
}
if (Get-Command starship -ErrorAction Ignore) {
    Invoke-Expression (&starship init powershell)
}
