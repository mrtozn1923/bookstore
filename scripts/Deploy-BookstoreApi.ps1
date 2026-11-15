#!/usr/bin/env pwsh
#Requires -Version 7.6

<#
.SYNOPSIS
    Deploys a published Bookstore.Api to an IIS site folder: backup, app_offline, copy,
    migrations, back online, health check; restores the backup when a step fails.

.DESCRIPTION
    The ASP.NET Core Module stops the app while app_offline.htm is in the site folder and
    starts it again with the first request after the file is removed. The database
    connection string for the migration bundle comes from BOOKSTORE_DB_CONNECTION.

.PARAMETER PackagePath
    Output of Invoke-BookstoreBuild.ps1 (dotnet publish).

.PARAMETER SitePath
    Physical path of the IIS site, for example C:\inetpub\bookstore-api.

.PARAMETER MigrationBundle
    Optional. EF Core migration bundle (efbundle.exe) to run before the site goes online.

.PARAMETER BackupPath
    Folder for the site backups taken before the copy.

.PARAMETER HealthUrl
    Base address the health check calls after the deploy.

.PARAMETER HealthTimeoutSec
    How long the health check keeps trying, in seconds.

.EXAMPLE
    ./Deploy-BookstoreApi.ps1 -PackagePath ..\artifacts\api -SitePath C:\inetpub\bookstore-api -WhatIf

.OUTPUTS
    Progress lines. Exit codes: 0 success, 1 failed (the site was restored from the backup when possible).
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -Path $_ -PathType Container }, ErrorMessage = "Package folder '{0}' does not exist.")]
    [string] $PackagePath,

    [Parameter(Mandatory)]
    [string] $SitePath,

    [ValidateScript({ Test-Path -Path $_ -PathType Leaf }, ErrorMessage = "Migration bundle '{0}' does not exist.")]
    [string] $MigrationBundle,

    [string] $BackupPath = (Join-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -ChildPath 'backups'),

    [ValidatePattern('^https?://')]
    [string] $HealthUrl = 'http://127.0.0.1:5080',

    [ValidateRange(5, 600)]
    [int] $HealthTimeoutSec = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Import-Module -Name (Join-Path -Path $PSScriptRoot -ChildPath '..', 'modules', 'Bookstore.Ops') -Force

$offlineFile = Join-Path -Path $SitePath -ChildPath 'app_offline.htm'
$backup = $null
$step = 'backup'

try {
    if (Test-Path -Path (Join-Path -Path $SitePath -ChildPath '*')) {
        # Module functions do not see this script's -WhatIf: pass it on explicitly
        $backup = Invoke-BookstoreBackup -Path $SitePath -Destination $BackupPath -KeepLast 5 -WhatIf:$WhatIfPreference
        if ($backup) { Write-Output "==> backup: $($backup.Name)" }
    }

    $step = 'offline'
    if ($PSCmdlet.ShouldProcess($SitePath, 'Take the site offline (app_offline.htm)')) {
        Set-Content -Path $offlineFile -Value '<!doctype html><title>Bookstore</title><p>Bookstore is being updated. Please try again in a minute.</p>'
        Write-Output '==> app_offline.htm written'
    }

    $step = 'copy'
    if ($PSCmdlet.ShouldProcess($SitePath, "Copy files from $PackagePath")) {
        Copy-Item -Path (Join-Path -Path $PackagePath -ChildPath '*') -Destination $SitePath -Recurse -Force
        Write-Output "==> copied $((Get-ChildItem -Path $PackagePath -Recurse -File).Count) files"
    }

    $step = 'migrate'
    if ($MigrationBundle -and $PSCmdlet.ShouldProcess($MigrationBundle, 'Apply EF Core migrations')) {
        if (-not $env:BOOKSTORE_DB_CONNECTION) { throw 'BOOKSTORE_DB_CONNECTION is not set.' }
        Write-Output '==> migrations'
        & $MigrationBundle --connection $env:BOOKSTORE_DB_CONNECTION
    }

    $step = 'online'
    if ($PSCmdlet.ShouldProcess($SitePath, 'Bring the site online (remove app_offline.htm)')) {
        Remove-Item -Path $offlineFile
        Write-Output '==> app_offline.htm removed'
    }

    $step = 'health'
    if (-not $WhatIfPreference) {
        $health = Wait-BookstoreHealthy -BaseUrl $HealthUrl -TimeoutSec $HealthTimeoutSec -InformationAction Continue
        if (-not $health.Healthy) { throw "Health check failed for $($health.Url)" }
    }
    Write-Output ($WhatIfPreference ? 'WhatIf: nothing was changed' : 'Deploy OK')
}
catch {
    Write-Output "FAILED at step '$step': $($_.Exception.Message)"
    # The files are restored; a migration that already ran stays in the database
    if ($backup -and $step -ne 'offline') {
        Set-Content -Path $offlineFile -Value 'Bookstore is being restored.'
        Expand-Archive -Path $backup.FullName -DestinationPath $SitePath -Force
        Remove-Item -Path $offlineFile -ErrorAction Ignore
        Write-Output "Restored $SitePath from $($backup.Name)"
    }
    exit 1
}
exit 0
