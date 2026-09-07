[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$subject = 'CN=Max NG7M - NovaSource G6 Config Test Signing'
$certificate = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Subject -eq $subject -and $_.HasPrivateKey -and $_.NotAfter -gt (Get-Date).AddDays(30) } |
    Sort-Object NotAfter -Descending | Select-Object -First 1
if (-not $certificate) {
    $certificate = New-SelfSignedCertificate -Type CodeSigningCert -Subject $subject `
        -FriendlyName 'NovaSource G6 Config test signing (not publicly trusted)' `
        -CertStoreLocation Cert:\CurrentUser\My -KeyAlgorithm RSA -KeyLength 3072 `
        -HashAlgorithm SHA256 -KeyExportPolicy NonExportable -NotAfter (Get-Date).AddYears(1)
}
$directory = Join-Path (Split-Path $PSScriptRoot -Parent) '.signing'
New-Item -ItemType Directory -Path $directory -Force | Out-Null
$certificate.Thumbprint | Set-Content -LiteralPath (Join-Path $directory 'thumbprint.txt')
Export-Certificate -Cert $certificate -FilePath (Join-Path $directory 'NovaSourceG6Config-test.cer') -Force | Out-Null
Write-Output "Test signing certificate ready: $($certificate.Thumbprint)"
Write-Output 'Private key stays in the current user certificate store. No trusted-root or trusted-publisher entry was added.'
