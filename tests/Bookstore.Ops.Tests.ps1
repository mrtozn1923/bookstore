#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.0.0' }
# Pester shares variables between BeforeEach and It; PSScriptAnalyzer cannot see that
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', '', Justification = 'Pester scoping')]
param()

BeforeAll {
    Import-Module -Name (Join-Path -Path $PSScriptRoot -ChildPath '..', 'modules', 'Bookstore.Ops') -Force
}

Describe 'Get-BookstoreHealth' {
    It 'reports a healthy API' {
        Mock -ModuleName Bookstore.Ops Invoke-WebRequest {
            [pscustomobject]@{ StatusCode = 200; Content = '{"status":"Healthy"}' }
        }

        $result = Get-BookstoreHealth -BaseUrl 'http://api.bookstore.test'

        $result.Url | Should-Be 'http://api.bookstore.test/health'
        $result.Healthy | Should-BeTrue
        Should-Invoke -ModuleName Bookstore.Ops Invoke-WebRequest -Times 1 -Exactly
    }

    It 'reports 503 as unhealthy' {
        Mock -ModuleName Bookstore.Ops Invoke-WebRequest {
            [pscustomobject]@{ StatusCode = 503; Content = '{"status":"Unhealthy"}' }
        }

        $result = Get-BookstoreHealth -BaseUrl 'http://api.bookstore.test'

        $result.Healthy | Should-BeFalse
        $result.StatusCode | Should-Be 503
    }

    It 'turns a connection error into a result instead of throwing' {
        Mock -ModuleName Bookstore.Ops Invoke-WebRequest {
            throw [System.Net.Http.HttpRequestException]::new('Connection refused')
        }

        $result = Get-BookstoreHealth -BaseUrl 'http://api.bookstore.test'

        $result.Healthy | Should-BeFalse
        $result.Error | Should-Be 'Connection refused'
    }

    It 'rejects an address without http or https' {
        { Get-BookstoreHealth -BaseUrl 'api.bookstore.test' } |
            Should-Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
    }
}

Describe 'Wait-BookstoreHealthy' {
    It 'tries again until the API is healthy' {
        $script:calls = 0
        Mock -ModuleName Bookstore.Ops Start-Sleep { }
        Mock -ModuleName Bookstore.Ops Get-BookstoreHealth {
            $script:calls++
            [pscustomobject]@{ Url = 'u'; StatusCode = 503; Status = 'Unhealthy'; Healthy = $script:calls -ge 3; Error = $null }
        }

        $result = Wait-BookstoreHealthy -BaseUrl 'http://api.bookstore.test' -TimeoutSec 60

        $result.Healthy | Should-BeTrue
        Should-Invoke -ModuleName Bookstore.Ops Get-BookstoreHealth -Times 3 -Exactly
    }
}

Describe 'Invoke-BookstoreBackup' {
    BeforeEach {
        $site = New-Item -Path (Join-Path -Path $TestDrive -ChildPath 'site') -ItemType Directory -Force
        Set-Content -Path (Join-Path -Path $site -ChildPath 'Bookstore.Api.dll') -Value 'fake dll'
        $backups = Join-Path -Path $TestDrive -ChildPath 'backups'
    }

    It 'creates nothing with -WhatIf' {
        Invoke-BookstoreBackup -Path $site -Destination $backups -WhatIf

        Test-Path -Path $backups | Should-BeFalse
    }

    It 'keeps only the newest backups' {
        1..3 | ForEach-Object { $null = Invoke-BookstoreBackup -Path $site -Destination $backups -KeepLast 2 }

        Get-ChildItem -Path $backups -Filter '*.zip' | Should-BeCollection -Count 2
    }
}

Describe 'Restart-BookstorePool' {
    It 'does not touch IIS with -WhatIf' {
        # The classic Should syntax still works in Pester 6
        { Restart-BookstorePool -Name BookstoreApi -WhatIf } | Should -Not -Throw
    }

    It 'refuses to run without IIS' -Skip:$IsWindows {
        { Restart-BookstorePool -Confirm:$false } | Should-Throw -ExceptionType ([System.PlatformNotSupportedException])
    }
}
