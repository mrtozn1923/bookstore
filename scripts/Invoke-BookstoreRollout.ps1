#!/usr/bin/env pwsh
#Requires -Version 7.6
#Requires -Modules Microsoft.PowerShell.SecretManagement

<#
.SYNOPSIS
    Deploys Bookstore.Api to several servers one by one over SSH remoting and stops at the first failure.

.DESCRIPTION
    For each server: opens an SSH session, copies the scripts, the module and the package
    with Copy-Item -ToSession, runs Deploy-BookstoreApi.ps1 there and checks its exit code.
    The migration bundle runs only on the first server. The database connection string
    comes from the SecretManagement vault (secret BookstoreDbConnection), never from the command line.

.PARAMETER ComputerName
    Servers in rollout order.

.PARAMETER UserName
    SSH user on the servers. Authentication uses SSH keys.

.PARAMETER SitePath
    Physical path of the site on the servers.

.PARAMETER HealthUrl
    Base address of the API as seen from the server itself.

.EXAMPLE
    ./Invoke-BookstoreRollout.ps1 -ComputerName web1, web2 -UserName deploy -SitePath C:\inetpub\bookstore-api -HealthUrl http://localhost

.OUTPUTS
    Progress lines prefixed with the server name. Exit codes: 0 all servers deployed, 1 rollout stopped.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string[]] $ComputerName,

    [Parameter(Mandatory)]
    [string] $UserName,

    [Parameter(Mandatory)]
    [string] $SitePath,

    [ValidatePattern('^https?://')]
    [string] $HealthUrl = 'http://127.0.0.1:5080',

    [string] $PackagePath = (Join-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -ChildPath 'artifacts', 'api'),

    [string] $MigrationBundle
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Path $PSScriptRoot -Parent
$release = 'bookstore-{0:yyyyMMdd-HHmmss}' -f [datetime]::UtcNow
$connection = Get-Secret -Name BookstoreDbConnection -AsPlainText

for ($i = 0; $i -lt $ComputerName.Count; $i++) {
    $server = $ComputerName[$i]
    Write-Information "=== $server ($($i + 1)/$($ComputerName.Count))" -InformationAction Continue
    $session = New-PSSession -HostName $server -UserName $UserName
    try {
        # A fresh release folder on the server; the path comes back as a plain string
        $remoteDir = Invoke-Command -Session $session -ScriptBlock {
            (New-Item -Path (Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath $using:release) -ItemType Directory).FullName
        }
        Copy-Item -Path (Join-Path -Path $root -ChildPath 'scripts'), (Join-Path -Path $root -ChildPath 'modules') -Destination $remoteDir -ToSession $session -Recurse
        Copy-Item -Path $PackagePath -Destination "$remoteDir/package" -ToSession $session -Recurse
        $runMigrations = $MigrationBundle -and $i -eq 0
        if ($runMigrations) {
            Copy-Item -Path $MigrationBundle -Destination "$remoteDir/efbundle" -ToSession $session
        }

        $exitCode = Invoke-Command -Session $session -ScriptBlock {
            $deploy = @{
                PackagePath = "$using:remoteDir/package"
                SitePath    = $using:SitePath
                HealthUrl   = $using:HealthUrl
                BackupPath  = Join-Path -Path (Split-Path -Path $using:SitePath -Parent) -ChildPath 'bookstore-backups'
            }
            if ($using:runMigrations) {
                $deploy.MigrationBundle = "$using:remoteDir/efbundle"
                # Copy-Item -ToSession does not keep the executable bit on Linux
                if (-not $IsWindows) { chmod +x $deploy.MigrationBundle }
                $env:BOOKSTORE_DB_CONNECTION = $using:connection
            }
            & "$using:remoteDir/scripts/Deploy-BookstoreApi.ps1" @deploy | ForEach-Object { "    $_" }
            $LASTEXITCODE
        } | ForEach-Object {
            # Strings are progress lines, the int is the exit code of the deploy script
            if ($_ -is [int]) { $_ } else { Write-Information "[$server]$_" -InformationAction Continue }
        }
        if ($exitCode -ne 0) { throw "Deploy failed on $server (exit code $exitCode); the remaining servers were not touched." }
    }
    finally {
        Remove-PSSession -Session $session
    }
}
Write-Output "Rollout OK: $($ComputerName -join ', ')"
