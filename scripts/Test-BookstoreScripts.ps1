#!/usr/bin/env pwsh
#Requires -Version 7.6

<#
.SYNOPSIS
    Runs PSScriptAnalyzer and the Pester tests for the Bookstore scripts and the Bookstore.Ops module.

.DESCRIPTION
    The same script runs on a developer machine and on the Windows and Ubuntu CI runners.
    Writes the test results as JUnit XML to artifacts/test-results.

.OUTPUTS
    Analyzer findings and test output. Exit codes: 0 clean, 1 analyzer findings or failed tests.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# Import-Module, not #Requires: with #Requires -Modules Pester, pwsh -File lost the exit code in the lab
Import-Module -Name Pester -MinimumVersion 6.0.0
Import-Module -Name PSScriptAnalyzer -MinimumVersion 1.25.0

$root = Split-Path -Path $PSScriptRoot -Parent
$settings = Join-Path -Path $root -ChildPath 'PSScriptAnalyzerSettings.psd1'

$findings = foreach ($folder in 'scripts', 'modules', 'tests') {
    Invoke-ScriptAnalyzer -Path (Join-Path -Path $root -ChildPath $folder) -Recurse -Settings $settings
}
if ($findings) {
    $findings | Format-Table -Property RuleName, Severity, ScriptName, Line, Message -AutoSize -Wrap | Out-String -Width 160
    Write-Output "PSScriptAnalyzer: $(@($findings).Count) finding(s)"
    exit 1
}
Write-Output 'PSScriptAnalyzer: no findings'

$config = New-PesterConfiguration
$config.Run.Path = Join-Path -Path $root -ChildPath 'tests'
$config.Run.PassThru = $true
$config.Output.Verbosity = 'Detailed'
$config.Output.RenderMode = 'Plaintext'
$config.TestResult.Enabled = $true
$config.TestResult.OutputFormat = 'JUnitXml'
$config.TestResult.OutputPath = Join-Path -Path $root -ChildPath 'artifacts', 'test-results', 'pester.xml'

$result = Invoke-Pester -Configuration $config
exit ($result.FailedCount -gt 0 ? 1 : 0)
