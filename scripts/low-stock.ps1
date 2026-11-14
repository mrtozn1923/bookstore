#Requires -Version 7.6
<#
.SYNOPSIS
    Lists Bookstore books whose stock is below a threshold.
#>
param(
    [string]$BaseUrl = 'http://localhost:5080',
    [int]$Threshold = 5,
    [string]$OutFile
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$books = Invoke-RestMethod "$BaseUrl/api/books"
$low = $books |
    Where-Object stock -lt $Threshold |
    Sort-Object stock |
    Select-Object isbn, title, stock, @{ Name = 'price'; Expression = { [decimal]$_.price } }

if ($OutFile) {
    $low | Export-Csv $OutFile
    Write-Output "$($low.Count) books written to $OutFile"
}
else {
    $low
}
