#Requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ReleaseDirectory,
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [Parameter(Mandatory)][datetime]$ReleaseDate,
    [string]$DestinationRoot = '\\nt7g-server\c$\inetpub',
    [switch]$Deploy
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path $PSScriptRoot -Parent
$downloads = (Resolve-Path -LiteralPath $ReleaseDirectory).Path
$names = @("NovaSourceG6Config-$Version-win-x64-Setup.exe", "NovaSourceG6Config-$Version-win-x64.zip",
    "NovaSourceG6Config-$Version-source.zip", 'RELEASE-NOTES.md')
$hashes = @{}
foreach ($line in Get-Content -LiteralPath (Join-Path $downloads 'SHA256SUMS.txt')) {
    if ($line -notmatch '^([a-fA-F0-9]{64})  ([^/\\]+)$') { throw "Malformed checksum entry: $line" }
    $name = $Matches[2]
    if ($name -notin $names -or $hashes.ContainsKey($name)) { throw "Unexpected or duplicate checksum filename: $name" }
    $hashes[$name] = $Matches[1]
}
if ($hashes.Count -ne $names.Count) { throw 'The release checksum list is incomplete.' }
function Confirm-Release([string]$Directory) {
    foreach ($name in $names) {
        if ((Get-FileHash -LiteralPath (Join-Path $Directory $name) -Algorithm SHA256).Hash -ne $hashes[$name]) {
            throw "Checksum mismatch: $name"
        }
    }
    if ((Get-FileHash -LiteralPath (Join-Path $Directory 'SHA256SUMS.txt')).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $downloads 'SHA256SUMS.txt')).Hash) { throw 'Checksum manifest differs.' }
}
Confirm-Release $downloads
$runName = "$Version-" + [guid]::NewGuid().ToString('N')
$prepared = Join-Path $repo "artifacts\web\$runName"
$preparedRelease = Join-Path $prepared "releases\$Version"
New-Item -ItemType Directory -Path $preparedRelease -Force | Out-Null
foreach ($name in $names + @('SHA256SUMS.txt')) {
    Copy-Item -LiteralPath (Join-Path $downloads $name) -Destination $preparedRelease
}
$html = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'web\index.template.html') -Raw
$html = $html.Replace('__VERSION__', $Version).Replace('__DATE__', $ReleaseDate.ToString('MMMM d, yyyy', [cultureinfo]'en-US'))
$html = $html.Replace('__INSTALLER_SIZE__', ('{0:N1} MB' -f ((Get-Item -LiteralPath (Join-Path $downloads $names[0])).Length / 1MB)))
$html = $html.Replace('__ZIP_SIZE__', ('{0:N1} MB' -f ((Get-Item -LiteralPath (Join-Path $downloads $names[1])).Length / 1MB)))
$notes = [System.Net.WebUtility]::HtmlEncode((Get-Content -LiteralPath (Join-Path $downloads 'RELEASE-NOTES.md') -Raw))
$html = $html.Replace('__RELEASE_NOTES__', $notes)
[IO.File]::WriteAllText((Join-Path $prepared 'index.html'), $html, [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'web\web.config') -Destination $prepared
Confirm-Release $preparedRelease
Write-Output "Prepared site: $prepared"
if (-not $Deploy) { Write-Output 'Preview only: no web-server files changed.'; return }
if (-not $DestinationRoot -or -not (Test-Path -LiteralPath $DestinationRoot -PathType Container)) {
    throw 'Supply an existing website parent directory using -DestinationRoot.'
}
$base = (Resolve-Path -LiteralPath $DestinationRoot).ProviderPath.TrimEnd('\')
$site = Join-Path $base 'NovaSourceG6'
$releases = Join-Path $site 'releases'
New-Item -ItemType Directory -Path $releases -Force | Out-Null
$final = Join-Path $releases $Version
if (Test-Path -LiteralPath $final) {
    # Re-deployment is allowed only when the immutable release is identical.
    Confirm-Release $final
} else {
    $stage = Join-Path $releases ('.staging-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage | Out-Null
    foreach ($name in $names + @('SHA256SUMS.txt')) {
        Copy-Item -LiteralPath (Join-Path $preparedRelease $name) -Destination $stage
    }
    Confirm-Release $stage
    # Both resolved paths are constructed beneath this project's release directory.
    $resolvedStage = (Resolve-Path -LiteralPath $stage).ProviderPath
    $resolvedReleases = (Resolve-Path -LiteralPath $releases).ProviderPath.TrimEnd('\') + '\'
    if (-not $resolvedStage.StartsWith($resolvedReleases, [StringComparison]::OrdinalIgnoreCase) -or
        -not $final.StartsWith($resolvedReleases, [StringComparison]::OrdinalIgnoreCase)) { throw 'Staging path escaped the release directory.' }
    [IO.Directory]::Move($resolvedStage, $final)
}
function Publish-PageFile([string]$Name) {
    $target = Join-Path $site $Name
    $temporary = Join-Path $site ('.upload-' + [guid]::NewGuid().ToString('N') + '.tmp')
    Copy-Item -LiteralPath (Join-Path $prepared $Name) -Destination $temporary
    if ((Get-FileHash -LiteralPath $temporary).Hash -ne (Get-FileHash -LiteralPath (Join-Path $prepared $Name)).Hash) {
        throw "Upload mismatch: $Name"
    }
    if (Test-Path -LiteralPath $target) {
        $backupDirectory = Join-Path $site '_page-history'
        New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
        [IO.File]::Replace($temporary, $target, (Join-Path $backupDirectory ($runName + '-' + $Name)))
    } else { [IO.File]::Move($temporary, $target) }
}
Publish-PageFile 'web.config'
# Switch the landing page last, only after the release and server configuration exist.
Publish-PageFile 'index.html'
Write-Output 'Published files for: http://www.ng7m.com:18080/ (IIS binding and router forwarding must be configured separately).'
