#!/usr/bin/env pwsh
#Requires -Version 7.6

<#
.SYNOPSIS
    Checks Bookstore book cover URLs in parallel and reports the broken ones.

.DESCRIPTION
    Takes ISBNs from the pipeline or from -Path, sends a HEAD request to
    <BaseUrl>/covers/<isbn>.jpg for each one and returns one result per ISBN.
    With -OutFile, every cover that did not return HTTP 200 is written to a CSV file.

.PARAMETER Isbn
    An ISBN-13. Accepts pipeline input.

.PARAMETER Path
    A text file with one ISBN per line.

.PARAMETER BaseUrl
    Base address of the server that serves the covers.

.PARAMETER ThrottleLimit
    How many requests run at the same time.

.PARAMETER TimeoutSec
    How long to wait for each request, in seconds.

.PARAMETER OutFile
    Optional. CSV file for the broken covers.

.EXAMPLE
    ./Test-BookstoreCovers.ps1 -Path ../data/cover-isbns.txt -OutFile ./broken-covers.csv

.EXAMPLE
    Get-Content ../data/cover-isbns.txt | ./Test-BookstoreCovers.ps1 -ThrottleLimit 20
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromPipeline)]
    [string] $Isbn,
    [string] $Path,
    [string] $BaseUrl = 'http://127.0.0.1:5080',
    [ValidateRange(1, 64)]
    [int] $ThrottleLimit = 10,
    [int] $TimeoutSec = 5,
    [string] $OutFile
)

begin {
    $isbns = [System.Collections.Generic.List[string]]::new()
    if ($Path) {
        foreach ($line in Get-Content -Path $Path) {
            if ($line.Trim()) { $isbns.Add($line.Trim()) }
        }
    }
}

process {
    if ($Isbn) { $isbns.Add($Isbn.Trim()) }
}

end {
    # Each block runs in its own runspace: outer variables come in only through $using:
    $results = $isbns | ForEach-Object -ThrottleLimit $ThrottleLimit -Parallel {
        # No progress bar for every single request
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
            # Connection refused, timeout, DNS: keep the reason, do not hide it
            $errorMessage = $_.Exception.Message
        }
        [pscustomobject]@{
            Isbn       = $_
            StatusCode = $statusCode
            Ms         = $watch.ElapsedMilliseconds
            Url        = $url
            Error      = $errorMessage
        }
    }

    $broken = @($results | Where-Object StatusCode -ne 200)
    Write-Verbose "Checked $(@($results).Count) covers, $($broken.Count) broken"
    if ($OutFile) {
        $broken | Sort-Object Isbn | Export-Csv -Path $OutFile -UseQuotes AsNeeded
    }
    $results
}
