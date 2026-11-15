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
    Import-Module -Name (Join-Path -Path $PSScriptRoot -ChildPath '..', 'modules', 'Bookstore.Ops') -Force
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
    # The parallel check lives in Bookstore.Ops (Test-BookstoreCover) since post 05
    $results = $isbns | Test-BookstoreCover -BaseUrl $BaseUrl -ThrottleLimit $ThrottleLimit -TimeoutSec $TimeoutSec
    $broken = @($results | Where-Object StatusCode -ne 200)
    Write-Verbose "Checked $(@($results).Count) covers, $($broken.Count) broken"
    if ($OutFile) {
        $broken | Sort-Object Isbn | Export-Csv -Path $OutFile -UseQuotes AsNeeded
    }
    $results
}
