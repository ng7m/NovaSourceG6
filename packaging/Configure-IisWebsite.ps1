#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [string]$PhysicalPath = 'C:\inetpub\NovaSourceG6',
    [string]$ResultPath = 'C:\ProgramData\NovaSourceG6Deployment\iis-result.json'
)
$ErrorActionPreference = 'Stop'
try {
    Import-Module WebAdministration -ErrorAction Stop
    $siteName = 'NovaSource G6 Downloads'
    $poolName = 'NovaSourceG6Downloads'
    $binding = '192.168.1.102:18080:www.ng7m.com'
    if (-not (Test-Path -LiteralPath (Join-Path $PhysicalPath 'index.html'))) { throw 'Deploy the landing page before configuring IIS.' }
    if (-not (Test-Path -LiteralPath (Join-Path $PhysicalPath 'releases\0.2.0\SHA256SUMS.txt'))) { throw 'Release 0.2.0 is missing.' }
    $existing = Get-Website | Where-Object { $_.Name -eq $siteName }
    $bindingsOnPort = @(Get-WebBinding | Where-Object { $_.bindingInformation -match ':18080:' })
    if ($bindingsOnPort.Count -gt 0 -and (-not $existing -or
        @($existing.bindings.Collection | Where-Object { $_.bindingInformation -eq $binding }).Count -ne $bindingsOnPort.Count)) {
        throw 'Port 18080 is already configured for another IIS site. No changes made.'
    }
    if (-not $existing -and (Get-NetTCPConnection -State Listen -LocalPort 18080 -ErrorAction SilentlyContinue)) {
        throw 'TCP 18080 is already listening. No changes made.'
    }
    if ($existing -and $existing.physicalPath.TrimEnd('\') -ne $PhysicalPath.TrimEnd('\')) {
        throw 'The named IIS site already points to another directory.'
    }
    $backupName = 'Before-NovaSourceG6-18080-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
    Backup-WebConfiguration -Name $backupName
    if (-not (Test-Path "IIS:\AppPools\$poolName")) { New-WebAppPool -Name $poolName | Out-Null }
    Set-ItemProperty "IIS:\AppPools\$poolName" -Name managedRuntimeVersion -Value ''
    Set-ItemProperty "IIS:\AppPools\$poolName" -Name processModel.identityType -Value ApplicationPoolIdentity
    if (-not $existing) {
        New-Website -Name $siteName -IPAddress '192.168.1.102' -Port 18080 -HostHeader 'www.ng7m.com' `
            -PhysicalPath $PhysicalPath -ApplicationPool $poolName | Out-Null
    }
    # Read-only access for the isolated static-content app pool; no write grants.
    $acl = Get-Acl -LiteralPath $PhysicalPath
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "IIS AppPool\$poolName", 'ReadAndExecute', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
    $acl.SetAccessRule($rule)
    Set-Acl -LiteralPath $PhysicalPath -AclObject $acl
    Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Location $siteName `
        -Filter 'system.webServer/security/authentication/anonymousAuthentication' -Name userName -Value ''
    $firewallName = 'NovaSourceG6-HTTP-18080'
    if (Get-NetFirewallRule -Name $firewallName -ErrorAction SilentlyContinue) {
        Set-NetFirewallRule -Name $firewallName -Enabled True -Direction Inbound -Action Allow -Profile Any `
            -Protocol TCP -LocalPort 18080 -LocalAddress '192.168.1.102' -RemoteAddress Any
    } else {
        New-NetFirewallRule -Name $firewallName -DisplayName 'NovaSource G6 Downloads (TCP 18080)' `
            -Enabled True -Direction Inbound -Action Allow -Profile Any -Protocol TCP `
            -LocalPort 18080 -LocalAddress '192.168.1.102' -RemoteAddress Any | Out-Null
    }
    Start-Website -Name $siteName
    [ordered]@{ succeeded = $true; site = $siteName; binding = $binding; physicalPath = $PhysicalPath;
        backup = $backupName; firewallRule = $firewallName } | ConvertTo-Json |
        Set-Content -LiteralPath $ResultPath -Encoding UTF8
} catch {
    [ordered]@{ succeeded = $false; error = $_.Exception.Message } | ConvertTo-Json |
        Set-Content -LiteralPath $ResultPath -Encoding UTF8
    exit 1
}
