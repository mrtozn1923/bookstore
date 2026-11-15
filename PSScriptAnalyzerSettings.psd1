@{
    # All default rules, all severities; CI fails on any finding
    Severity     = @('Error', 'Warning', 'Information')
    IncludeDefaultRules = $true
    ExcludeRules = @()
    Rules        = @{
        PSUseCompatibleSyntax = @{
            Enable         = $true
            TargetVersions = @('7.0')
        }
    }
}
