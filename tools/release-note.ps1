#Requires -Version 7.3
# Drafts a CHANGELOG entry from a pull request with claude -p and structured output.
#   ./tools/release-note.ps1 -PullRequest 128
param(
    [Parameter(Mandatory = $true)] [int]$PullRequest
)
$ErrorActionPreference = 'Stop'

$schema = @{
    type       = 'object'
    properties = @{
        title    = @{ type = 'string' }
        category = @{ type = 'string'; enum = @('feature', 'fix', 'maintenance') }
        note     = @{ type = 'string' }
    }
    required   = @('title', 'category', 'note')
} | ConvertTo-Json -Depth 5 -Compress

$pr = gh pr view $PullRequest --json title,body | ConvertFrom-Json
$prompt = 'Write a Bookstore release note entry for store staff from the pull request on stdin. ' +
          'Plain language, at most two sentences, no internal class names.'

$json = "$($pr.title)`n`n$($pr.body)" |
    claude -p $prompt --bare --output-format json --json-schema $schema `
        --permission-mode dontAsk --max-turns 2 --max-budget-usd 0.25
if ($LASTEXITCODE -ne 0) { throw "claude -p failed with exit code $LASTEXITCODE" }

$response = $json | ConvertFrom-Json
if (-not $response.structured_output) { throw 'No structured_output in the response.' }

$entry = $response.structured_output
Add-Content -LiteralPath 'CHANGELOG.draft.md' -Value "- [$($entry.category)] $($entry.title): $($entry.note) (#$PullRequest)"
Write-Output ("Added #{0} to CHANGELOG.draft.md, estimated cost {1} USD" -f $PullRequest, $response.total_cost_usd)
