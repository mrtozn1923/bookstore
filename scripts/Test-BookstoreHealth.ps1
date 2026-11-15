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

$url = "$($BaseUrl.TrimEnd('/'))/health"

try {
    $response = Invoke-RestMethod -Uri $url -TimeoutSec $TimeoutSec -SkipHttpErrorCheck -StatusCodeVariable statusCode
}
catch {
    Write-Output "FAIL $url ($($_.Exception.Message))"
    exit 1
}

if ($statusCode -eq 200 -and $response.status -eq 'Healthy') {
    Write-Output "OK $url"
    exit 0
}

Write-Output "FAIL $url (HTTP $statusCode, status: $($response.status))"
exit 1
