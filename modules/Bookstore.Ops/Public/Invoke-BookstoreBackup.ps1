function Invoke-BookstoreBackup {
    <#
    .SYNOPSIS
        Zips a folder into a time-stamped backup and keeps only the newest -KeepLast backups.

    .PARAMETER Path
        The folder to back up, for example the site folder of Bookstore.Api.

    .PARAMETER Destination
        The folder that holds the backups.

    .PARAMETER KeepLast
        How many backups to keep; older ones are removed.

    .EXAMPLE
        Invoke-BookstoreBackup -Path ./artifacts/api -Destination ./backups -WhatIf
    #>
    [CmdletBinding(SupportsShouldProcess, PositionalBinding = $false)]
    [OutputType([System.IO.FileInfo])]
    param(
        [Parameter(Mandatory)]
        [ValidateScript({ Test-Path -Path $_ -PathType Container }, ErrorMessage = "Folder '{0}' does not exist.")]
        [string] $Path,

        [Parameter(Mandatory)]
        [string] $Destination,

        [ValidateRange(1, 100)]
        [int] $KeepLast = 7
    )

    $zip = Join-Path -Path $Destination -ChildPath ('bookstore-{0:yyyyMMdd-HHmmss}.zip' -f [datetime]::UtcNow)
    if ($PSCmdlet.ShouldProcess($zip, "Back up $Path")) {
        $null = New-Item -Path $Destination -ItemType Directory -Force
        Compress-Archive -Path (Join-Path -Path $Path -ChildPath '*') -DestinationPath $zip
        Write-Verbose "Created $zip"
        Get-Item -Path $zip
    }

    if (-not (Test-Path -Path $Destination)) { return }
    $old = Get-ChildItem -Path $Destination -Filter 'bookstore-*.zip' |
        Sort-Object -Property Name -Descending |
        Select-Object -Skip $KeepLast
    foreach ($file in $old) {
        # Remove-Item sees -WhatIf and -Confirm through the preference variables
        Remove-Item -LiteralPath $file.FullName
    }
}
