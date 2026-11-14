# Select-String is the PowerShell counterpart of grep; it is case-insensitive by default
Get-ChildItem sample -Recurse -Filter *.cs | Sort-Object Name |
    Select-String -Pattern 'Log\w+\(\$"' |
    ForEach-Object { '{0}:{1}: {2}' -f $_.Filename, $_.LineNumber, $_.Line.Trim() }
'---'
# -match fills $Matches; -cmatch is the case-sensitive form
'BKS-2026-000123' -match '^bks-(?<year>\d{4})'
$Matches.year
'BKS-2026-000123' -cmatch '^bks-'
'---'
# -replace uses .NET syntax: in double quotes PowerShell expands $2 and $1 as variables first
'Herbert, Frank' -replace '(\w+), (\w+)', '$2 $1'
'Herbert, Frank' -replace '(\w+), (\w+)', "$2 $1"
'Herbert, Frank' -replace '(\w+), (\w+)', "`$2 `$1"
