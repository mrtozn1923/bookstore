#!/usr/bin/env pwsh
#Requires -Version 7.6

<#
.SYNOPSIS
    Checks the health endpoint of the Bookstore API.

.DESCRIPTION
    Calls /health on Bookstore.Api and prints one status line.
    The exit code is the contract for CI pipelines, deploy scripts and monitoring tools.

.PARAMETER BaseUrl
    Base address of the API, without /health.

.PARAMETER TimeoutSec
    How long to wait for the API, in seconds.

.EXAMPLE
    ./Test-BookstoreHealth.ps1

    Checks the local API at http://127.0.0.1:5080.

.EXAMPLE
    ./Test-BookstoreHealth.ps1 -BaseUrl https://staging.bookstore.test -TimeoutSec 10

    Checks the staging API and waits up to 10 seconds.

.OUTPUTS
    None. The result is the exit code: 0 healthy, 1 unhealthy or unreachable.
#>
param(
    [string] $BaseUrl = 'http://127.0.0.1:5080',
    [int] $TimeoutSec = 5
)

Import-Module -Name (Join-Path -Path $PSScriptRoot -ChildPath '..', 'modules', 'Bookstore.Ops') -Force

# The module returns an object; this script turns it into a status line and an exit code
$result = Get-BookstoreHealth -BaseUrl $BaseUrl -TimeoutSec $TimeoutSec
if ($result.Healthy) {
    Write-Output "OK $($result.Url)"
    exit 0
}

$reason = if ($result.Error) { $result.Error } else { "HTTP $($result.StatusCode), status: $($result.Status)" }
Write-Output "FAIL $($result.Url) ($reason)"
exit 1
