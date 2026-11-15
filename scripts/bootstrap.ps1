# Bootstrap a Windows machine for Bookstore. Safe to run more than once.
# Runs on Windows PowerShell 5.1, because PowerShell 7 is installed by this script.
[CmdletBinding()]
param(
    [switch] $AcceptConfigurationAgreements
)

$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    throw 'winget is missing. Install or update App Installer from the Microsoft Store.'
}

Write-Host '==> Machine tools (.config/configuration.winget)'
$wingetArgs = @('configure', '--file', '.config/configuration.winget', '--disable-interactivity')
if ($AcceptConfigurationAgreements) {
    $wingetArgs += '--accept-configuration-agreements'
}
winget @wingetArgs
if ($LASTEXITCODE -ne 0) {
    throw "winget configure failed with exit code $LASTEXITCODE"
}

# Tools installed above are on the PATH of new processes only; reload it for this session.
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' +
    [Environment]::GetEnvironmentVariable('Path', 'User')

Write-Host '==> Project runtimes (mise.toml, global.json)'
mise install

if ($env:DOTFILES_REPO) {
    Write-Host '==> Dotfiles'
    if (Test-Path (Join-Path $HOME '.local/share/chezmoi')) {
        chezmoi update
    } else {
        chezmoi init --apply $env:DOTFILES_REPO
    }
}

Write-Host '==> Project setup'
mise exec -- just setup

Write-Host 'Done. Start Bookstore with: mise exec -- just dev'
