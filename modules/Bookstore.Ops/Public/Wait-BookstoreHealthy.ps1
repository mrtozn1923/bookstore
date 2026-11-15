function Wait-BookstoreHealthy {
    <#
    .SYNOPSIS
        Calls /health until the API is healthy or the timeout expires, and returns the last result.

    .PARAMETER BaseUrl
        Base address of the API, without /health.

    .PARAMETER TimeoutSec
        How long to keep trying, in seconds.

    .PARAMETER IntervalSec
        How long to wait between attempts, in seconds.

    .EXAMPLE
        Wait-BookstoreHealthy -BaseUrl http://127.0.0.1:5080 -TimeoutSec 60 -InformationAction Continue
    #>
    [CmdletBinding(PositionalBinding = $false)]
    [OutputType('Bookstore.Health')]
    param(
        [Parameter(Mandatory)]
        [ValidatePattern('^https?://', ErrorMessage = "'{0}' is not an http or https address.")]
        [string] $BaseUrl,

        [ValidateRange(1, 3600)]
        [int] $TimeoutSec = 60,

        [ValidateRange(1, 60)]
        [int] $IntervalSec = 2
    )

    $deadline = [datetime]::UtcNow.AddSeconds($TimeoutSec)
    $attempt = 0
    do {
        $attempt++
        $result = Get-BookstoreHealth -BaseUrl $BaseUrl -TimeoutSec 5
        $detail = if ($result.Error) { $result.Error } else { "HTTP $($result.StatusCode) $($result.Status)" }
        Write-Information "    health attempt ${attempt}: $detail"
        if ($result.Healthy) { return $result }
        Start-Sleep -Seconds $IntervalSec
    } while ([datetime]::UtcNow -lt $deadline)
    $result
}
