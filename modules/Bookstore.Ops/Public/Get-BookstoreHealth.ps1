function Get-BookstoreHealth {
    <#
    .SYNOPSIS
        Calls /health on one or more Bookstore.Api instances and returns one result per instance.

    .PARAMETER Environment
        A known environment: Local, Staging or Production.

    .PARAMETER BaseUrl
        Base address of an API instance, without /health. Accepts pipeline input,
        also from objects with a BaseUrl or Url property.

    .PARAMETER TimeoutSec
        How long to wait for each instance, in seconds.

    .EXAMPLE
        Get-BookstoreHealth -Environment Staging

    .EXAMPLE
        Import-Csv ./servers.csv | Get-BookstoreHealth
    #>
    [CmdletBinding(DefaultParameterSetName = 'Environment', PositionalBinding = $false)]
    [OutputType('Bookstore.Health')]
    param(
        [Parameter(ParameterSetName = 'Environment')]
        [ValidateSet('Local', 'Staging', 'Production')]
        [string] $Environment = 'Local',

        [Parameter(Mandatory, ParameterSetName = 'Url', ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('Url')]
        [ValidatePattern('^https?://', ErrorMessage = "'{0}' is not an http or https address.")]
        [string] $BaseUrl,

        [ValidateRange(1, 60)]
        [int] $TimeoutSec = 5
    )

    process {
        $url = '{0}/health' -f (Resolve-BookstoreBaseUrl -Environment $Environment -BaseUrl $BaseUrl)
        Write-Verbose "GET $url (timeout $TimeoutSec s)"
        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        $statusCode = 0
        $status = $null
        $errorMessage = $null
        try {
            # Invoke-WebRequest returns the status code on the object; -StatusCodeVariable cannot be mocked
            $response = Invoke-WebRequest -Uri $url -TimeoutSec $TimeoutSec -SkipHttpErrorCheck
            $statusCode = [int]$response.StatusCode
            $status = ($response.Content | ConvertFrom-Json).status
        }
        catch {
            # Unreachable is a result, not an exception: the caller decides what to do
            $errorMessage = $_.Exception.Message
        }
        [pscustomobject]@{
            PSTypeName = 'Bookstore.Health'
            Url        = $url
            StatusCode = $statusCode
            Status     = $status
            Healthy    = $statusCode -eq 200 -and $status -eq 'Healthy'
            Ms         = $watch.ElapsedMilliseconds
            Error      = $errorMessage
        }
    }
}
