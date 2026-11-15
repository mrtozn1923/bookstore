function Restart-BookstorePool {
    <#
    .SYNOPSIS
        Recycles the IIS application pool of Bookstore.Api or Bookstore.Web (Windows, run as administrator).

    .PARAMETER Name
        Application pool name. Accepts pipeline input.

    .EXAMPLE
        Restart-BookstorePool -Name BookstoreApi -WhatIf

    .EXAMPLE
        'BookstoreApi', 'BookstoreWeb' | Restart-BookstorePool -Confirm:$false
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High', PositionalBinding = $false)]
    param(
        [Parameter(ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateSet('BookstoreApi', 'BookstoreWeb')]
        [string[]] $Name = 'BookstoreApi'
    )

    process {
        foreach ($pool in $Name) {
            # The work stays inside the if: if ShouldProcess fails, nothing runs
            if ($PSCmdlet.ShouldProcess("application pool $pool", 'Recycle')) {
                if (-not $IsWindows) {
                    throw [System.PlatformNotSupportedException]::new('Restart-BookstorePool needs IIS on Windows.')
                }
                # Microsoft.Web.Administration works in PowerShell 7 without the compatibility layer
                Add-Type -Path (Join-Path -Path $env:SystemRoot -ChildPath 'System32\inetsrv\Microsoft.Web.Administration.dll')
                $manager = [Microsoft.Web.Administration.ServerManager]::new()
                try {
                    $appPool = $manager.ApplicationPools[$pool]
                    if (-not $appPool) { throw "Application pool '$pool' was not found." }
                    $state = $appPool.Recycle()
                    Write-Verbose "Recycled $pool, state: $state"
                }
                finally {
                    $manager.Dispose()
                }
            }
        }
    }
}
