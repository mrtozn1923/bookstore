Set-StrictMode -Version Latest

# Known environments; Get-BookstoreHealth -Environment reads its address from here
$script:BookstoreEnvironments = @{
    Local      = 'http://127.0.0.1:5080'
    Staging    = 'https://staging.bookstore.test'
    Production = 'https://api.bookstore.test'
}

# One function per file: Private first, because Public functions use them
foreach ($folder in 'Private', 'Public') {
    $files = Get-ChildItem -Path (Join-Path -Path $PSScriptRoot -ChildPath $folder) -Filter '*.ps1' -ErrorAction Ignore
    foreach ($file in $files) {
        . $file.FullName
    }
}
