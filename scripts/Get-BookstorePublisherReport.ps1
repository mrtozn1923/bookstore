#!/usr/bin/env pwsh
#Requires -Version 7.6

<#
.SYNOPSIS
    Summarizes the Bookstore book list per publisher.

.DESCRIPTION
    Reads a book CSV (Isbn, Title, Author, Publisher, Price, Stock, Published),
    converts the columns to real types and returns one object per publisher:
    number of titles, total stock, stock value, average price and newest edition.

.PARAMETER Path
    The book CSV. Defaults to data/books.csv in the repo.

.PARAMETER OutFile
    Optional. Writes the report as a semicolon-separated CSV for Excel.

.EXAMPLE
    ./Get-BookstorePublisherReport.ps1 | Format-Table

.EXAMPLE
    ./Get-BookstorePublisherReport.ps1 -OutFile ./publisher-report.csv
#>
param(
    [string] $Path = (Join-Path -Path $PSScriptRoot -ChildPath '..', 'data', 'books.csv'),
    [string] $OutFile
)

$invariant = [cultureinfo]::InvariantCulture

# CSV columns are strings: convert them once, here, with an explicit culture.
# Price is parsed directly as decimal; a [decimal] cast goes through double on 7.7.
$books = foreach ($row in Import-Csv -Path $Path) {
    [pscustomobject]@{
        Isbn      = $row.Isbn
        Title     = $row.Title
        Publisher = $row.Publisher
        Price     = [decimal]::Parse($row.Price, $invariant)
        Stock     = [int]$row.Stock
        Published = [datetime]::ParseExact($row.Published, 'yyyy-MM-dd', $invariant)
    }
}

$report = foreach ($group in $books | Group-Object Publisher) {
    $stock = 0
    $stockValue = 0d
    $priceSum = 0d
    foreach ($book in $group.Group) {
        $stock += $book.Stock
        $stockValue += $book.Price * $book.Stock
        $priceSum += $book.Price
    }
    $newest = $group.Group | Sort-Object Published -Descending | Select-Object -First 1

    [pscustomobject]@{
        Publisher  = $group.Name
        Titles     = $group.Count
        Stock      = $stock
        StockValue = $stockValue
        AvgPrice   = [math]::Round($priceSum / $group.Count, 2)
        Newest     = $newest.Published.ToString('yyyy-MM-dd', $invariant)
    }
}
$report = $report | Sort-Object StockValue -Descending

if ($OutFile) {
    $csvOptions = @{
        Path      = $OutFile
        Delimiter = ';'
        Encoding  = 'utf8BOM'
        UseQuotes = 'AsNeeded'
    }
    $report | Export-Csv @csvOptions
    Write-Output "Report written to $OutFile ($(@($report).Count) publishers)"
}
else {
    $report
}
