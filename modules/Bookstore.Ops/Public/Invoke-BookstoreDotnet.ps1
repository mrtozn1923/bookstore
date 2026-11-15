function Invoke-BookstoreDotnet {
    <#
    .SYNOPSIS
        Runs one dotnet command, writes its duration and throws when the exit code is not 0.

    .EXAMPLE
        Invoke-BookstoreDotnet -Command test -Arguments ./Bookstore.slnx, '-c', 'Release'
    #>
    [CmdletBinding(PositionalBinding = $false)]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('restore', 'build', 'test', 'publish')]
        [string] $Command,

        [string[]] $Arguments = @()
    )
    # Preferences are per scope: set them here, not only in the calling script
    $ErrorActionPreference = 'Stop'
    $PSNativeCommandUseErrorActionPreference = $true

    Write-Information "==> dotnet $Command" -InformationAction Continue
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet $Command @Arguments --nologo -v quiet
    Write-Information ('    {0} finished in {1:N1} s' -f $Command, $watch.Elapsed.TotalSeconds) -InformationAction Continue
}
