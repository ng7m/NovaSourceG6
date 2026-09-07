[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Path,
    [Parameter(Mandatory)][ValidatePattern('^[A-Fa-f0-9]{40}$')][string]$Thumbprint
)
$ErrorActionPreference = 'Stop'
Import-Module Microsoft.PowerShell.Security -ErrorAction Stop
$certificate = Get-Item -LiteralPath "Cert:\CurrentUser\My\$Thumbprint"
if (-not $certificate.HasPrivateKey -or $certificate.NotAfter -le (Get-Date)) {
    throw 'The signing certificate is expired or has no accessible private key.'
}
# Test signing is intentionally offline and does not add certificate trust.
$signature = Set-AuthenticodeSignature -LiteralPath $Path -Certificate $certificate -HashAlgorithm SHA256
$check = Get-AuthenticodeSignature -LiteralPath $Path
if (-not $check.SignerCertificate -or $check.SignerCertificate.Thumbprint -ne $Thumbprint -or
    $check.Status -notin @('Valid', 'NotTrusted', 'UnknownError')) {
    throw "Signing failed for $Path : $($signature.StatusMessage)"
}
# UnknownError can represent CERT_E_UNTRUSTEDROOT for a self-signed certificate.
if ($check.Status -eq 'UnknownError' -and $check.StatusMessage -notmatch '800B0109|not trusted|not a trusted root|not trusted by the trust provider') {
    throw "Unexpected signature verification result: $($check.StatusMessage)"
}
Write-Output "Signed $Path ($($check.Status); test certificate is not publicly trusted)."
