function Test-BookstoreCover {
    <#
    .SYNOPSIS
        Sends a HEAD request for each book cover in parallel and returns one result per ISBN.

    .PARAMETER Isbn
        ISBN-13 without dashes. Accepts pipeline input.

    .PARAMETER BaseUrl
        Base address of the server that serves the covers.

    .PARAMETER ThrottleLimit
        How many requests run at the same time.

    .EXAMPLE
        Get-Content ./data/cover-isbns.txt | Test-BookstoreCover | Where-Object StatusCode -ne 200
    #>
    [CmdletBinding(PositionalBinding = $false)]
    [OutputType('Bookstore.CoverCheck')]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [ValidatePattern('^\d{13}$')]
        [string] $Isbn,

        [string] $BaseUrl = $script:BookstoreEnvironments['Local'],

        [ValidateRange(1, 64)]
        [int] $ThrottleLimit = 10,

        [ValidateRange(1, 60)]
        [int] $TimeoutSec = 5
    )

    begin {
        $isbns = [System.Collections.Generic.List[string]]::new()
    }

    process {
        $isbns.Add($Isbn)
    }

    end {
        $isbns | ForEach-Object -ThrottleLimit $ThrottleLimit -Parallel {
            $ProgressPreference = 'SilentlyContinue'
            $url = "$($using:BaseUrl)/covers/$_.jpg"
            $watch = [System.Diagnostics.Stopwatch]::StartNew()
            $statusCode = 0
            $errorMessage = $null
            try {
                $response = Invoke-WebRequest -Uri $url -Method Head -TimeoutSec $using:TimeoutSec -SkipHttpErrorCheck
                $statusCode = [int]$response.StatusCode
            }
            catch {
                $errorMessage = $_.Exception.Message
            }
            [pscustomobject]@{
                PSTypeName = 'Bookstore.CoverCheck'
                Isbn       = $_
                StatusCode = $statusCode
                Ms         = $watch.ElapsedMilliseconds
                Url        = $url
                Error      = $errorMessage
            }
        }
    }
}
