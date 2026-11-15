#!/usr/bin/env pwsh
#Requires -Version 7.6

<#
.SYNOPSIS
    Restores, builds, tests and publishes Bookstore.Api; stops at the first failing step.

.DESCRIPTION
    Runs dotnet restore, build, test and publish for Bookstore.slnx. The publish output
    is prepared in a temporary folder and copied to -OutputPath only when every step
    succeeded, so the deploy never picks up the output of a failed build.

.PARAMETER Configuration
    Build configuration: Debug or Release.

.PARAMETER OutputPath
    Folder the deploy picks up. Replaced only after a successful build.

.EXAMPLE
    ./Invoke-BookstoreBuild.ps1

.EXAMPLE
    pwsh -NoProfile -File ./scripts/Invoke-BookstoreBuild.ps1 -Configuration Debug

.OUTPUTS
    Progress lines. Exit codes: 0 success, the failing dotnet exit code, or 1 for other errors.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',
    [string] $OutputPath = (Join-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -ChildPath 'artifacts', 'api')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$root = Split-Path -Path $PSScriptRoot -Parent
$solution = Join-Path -Path $root -ChildPath 'Bookstore.slnx'
$project = Join-Path -Path $root -ChildPath 'src', 'Bookstore.Api'
$staging = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "bookstore-publish-$PID"

function Invoke-Dotnet {
    param(
        [string] $Command,
        [string[]] $Arguments
    )
    Write-Output "==> dotnet $Command"
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet $Command @Arguments --nologo -v quiet
    Write-Output ('    {0} finished in {1:N1} s' -f $Command, $watch.Elapsed.TotalSeconds)
}

try {
    Invoke-Dotnet -Command restore -Arguments $solution
    Invoke-Dotnet -Command build -Arguments $solution, '-c', $Configuration, '--no-restore'
    Invoke-Dotnet -Command test -Arguments $solution, '-c', $Configuration, '--no-build'
    Invoke-Dotnet -Command publish -Arguments $project, '-c', $Configuration, '--no-build', '-o', $staging

    # Replace the drop folder only now: every step above has succeeded
    if (Test-Path -Path $OutputPath) { Remove-Item -Path $OutputPath -Recurse -Force }
    $null = New-Item -Path $OutputPath -ItemType Directory -Force
    Copy-Item -Path (Join-Path -Path $staging -ChildPath '*') -Destination $OutputPath -Recurse
    Write-Output "Build OK, output in $OutputPath"
}
catch [System.Management.Automation.NativeCommandExitException] {
    Write-Output "FAILED: $($_.Exception.Message)"
    exit $_.Exception.ExitCode
}
catch {
    Write-Output "FAILED: $($_.Exception.Message)"
    Write-Output $_.ScriptStackTrace
    exit 1
}
finally {
    if (Test-Path -Path $staging) { Remove-Item -Path $staging -Recurse -Force }
}
