#Requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [Parameter(Mandatory)][string]$ReleaseNotes,
    [string]$IsccPath,
    [string]$CertificateThumbprint
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path $PSScriptRoot -Parent
$notes = (Resolve-Path -LiteralPath $ReleaseNotes).Path
$parsedVersion = [version]$Version
if ($parsedVersion.Major -gt 65534 -or $parsedVersion.Minor -gt 65534 -or $parsedVersion.Build -gt 65534) {
    throw 'Each version component must be at most 65534.'
}
if (-not $IsccPath) {
    $compiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($compiler) { $IsccPath = $compiler.Source }
    else {
        foreach ($candidate in @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe")) {
            if (Test-Path -LiteralPath $candidate) { $IsccPath = $candidate; break }
        }
    }
}
if (-not $IsccPath -or -not (Test-Path -LiteralPath $IsccPath)) {
    throw 'Install Inno Setup 6.3 or newer from https://jrsoftware.org/isdl.php, or supply -IsccPath.'
}
$IsccPath = (Resolve-Path -LiteralPath $IsccPath).Path
if (-not $CertificateThumbprint) {
    $thumbprintPath = Join-Path $repo '.signing\thumbprint.txt'
    if (-not (Test-Path -LiteralPath $thumbprintPath)) {
        throw 'Run packaging\New-TestSigningCertificate.ps1 once before creating a test release.'
    }
    $CertificateThumbprint = (Get-Content -LiteralPath $thumbprintPath -Raw).Trim()
}
if ($CertificateThumbprint -notmatch '^[A-Fa-f0-9]{40}$') { throw 'Invalid signing certificate thumbprint.' }
$certificate = Get-Item -LiteralPath "Cert:\CurrentUser\My\$CertificateThumbprint"
if (-not $certificate.HasPrivateKey -or $certificate.NotAfter -le (Get-Date)) { throw 'Signing certificate is unavailable or expired.' }
$dotnet = Join-Path $repo '.dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }

# Each invocation writes to a new folder, never deleting or replacing an earlier release.
$run = Join-Path $repo ("artifacts\releases\$Version-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
$downloads = Join-Path $run 'downloads'
$source = Join-Path $run 'source'
$payload = Join-Path $run 'payload'
New-Item -ItemType Directory -Path $downloads, $source, $payload | Out-Null

# Snapshot the actual working source, including uncommitted implementation files.
# Builds use this snapshot so the source ZIP matches the published binaries.
$files = @(& git -C $repo -c core.quotepath=false ls-files --cached --others --exclude-standard)
if ($LASTEXITCODE -ne 0) { throw 'Could not enumerate the release source.' }
foreach ($relative in ($files | Sort-Object -Unique)) {
    if ($relative -notmatch '^(application/|tests/|packaging/|docs/|README\.md$|LICENSE$|global\.json$|NuGet\.Config$|Directory\.[^/]+$|[^/]+\.slnx?$|\.gitignore$)') { continue }
    if ($relative -match '(^|/)(bin|obj|\.signing|\.dotnet)/|\.(pfx|p12|key)$') { continue }
    $from = Join-Path $repo $relative
    if (-not (Test-Path -LiteralPath $from -PathType Leaf)) { continue }
    $to = Join-Path $source $relative
    New-Item -ItemType Directory -Path (Split-Path $to -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $from -Destination $to
}
Copy-Item -LiteralPath $notes -Destination (Join-Path $source 'RELEASE-NOTES.md')
Copy-Item -LiteralPath $notes -Destination (Join-Path $downloads 'RELEASE-NOTES.md')
$commit = (& git -C $repo rev-parse HEAD).Trim()
$sourceHashes = @(Get-ChildItem -LiteralPath $source -File -Recurse | ForEach-Object {
    [ordered]@{ path = $_.FullName.Substring($source.Length + 1).Replace('\', '/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
[ordered]@{ version = $Version; baseCommit = $commit; source = 'working-tree snapshot'; files = $sourceHashes } |
    ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $source 'SOURCE-MANIFEST.json') -Encoding UTF8

Push-Location $source
try {
    & $dotnet publish 'application\NovaSourceG6Config\NovaSourceG6Config.csproj' -c Release -r win-x64 `
        --self-contained false -p:PublishSingleFile=false -p:PublishTrimmed=false `
        "-p:Version=$Version" "-p:AssemblyVersion=$Version.0" "-p:FileVersion=$Version.0" `
        -p:IncludeSourceRevisionInInformationalVersion=false -o $payload
    if ($LASTEXITCODE -ne 0) { throw 'Release publishing failed.' }
} finally { Pop-Location }

# Ship the notices supplied by resolved runtime packages without signing third-party DLLs.
$assets = Get-Content -LiteralPath (Join-Path $source 'application\NovaSourceG6Config\obj\project.assets.json') -Raw | ConvertFrom-Json
$noticeDirectory = Join-Path $payload 'third-party-notices'
New-Item -ItemType Directory -Path $noticeDirectory | Out-Null
foreach ($library in $assets.libraries.PSObject.Properties) {
    if ($library.Value.type -ne 'package') { continue }
    foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
        $package = Join-Path $folder $library.Value.path
        if (-not (Test-Path -LiteralPath $package)) { continue }
        $target = Join-Path $noticeDirectory ($library.Name.Replace('/', '-'))
        New-Item -ItemType Directory -Path $target -Force | Out-Null
        Get-ChildItem -LiteralPath $package -File | Where-Object { $_.Name -match 'license|notice|\.nuspec$' } |
            Copy-Item -Destination $target
        break
    }
}
Copy-Item -LiteralPath (Join-Path $source 'packaging\USER-README.txt') -Destination (Join-Path $payload 'README.txt')
Copy-Item -LiteralPath (Join-Path $source 'packaging\THIRD-PARTY-NOTICES.txt') -Destination $payload
Copy-Item -LiteralPath $notes -Destination (Join-Path $payload 'RELEASE-NOTES.md')
Export-Certificate -Cert $certificate -FilePath (Join-Path $payload 'NovaSourceG6Config-test.cer') | Out-Null
$signer = Join-Path $source 'packaging\Sign-Artifact.ps1'
& $signer -Path (Join-Path $payload 'NovaSourceG6Config.exe') -Thumbprint $CertificateThumbprint
& $signer -Path (Join-Path $payload 'NovaSourceG6Config.dll') -Thumbprint $CertificateThumbprint

$powershell = Join-Path $PSHOME 'pwsh.exe'
if (-not (Test-Path -LiteralPath $powershell)) { $powershell = Join-Path $PSHOME 'powershell.exe' }
$signCommand = '$q' + $powershell + '$q -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $q' +
    $signer.Replace('$', '$$') + '$q -Thumbprint ' + $CertificateThumbprint + ' -Path $f'
& $IsccPath "/DAppVersion=$Version" "/DPublishDir=$payload" "/DReleaseDir=$downloads" `
    "/Sg6test=$signCommand" (Join-Path $source 'packaging\NovaSourceG6Config.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation/signing failed.' }

Compress-Archive -Path "$payload\*" -DestinationPath (Join-Path $downloads "NovaSourceG6Config-$Version-win-x64.zip")
# Only snapshot files enter the source archive, not publish-generated bin/obj directories.
$sourceArchive = Join-Path $downloads "NovaSourceG6Config-$Version-source.zip"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($sourceArchive, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($entry in @($sourceHashes.path) + @('SOURCE-MANIFEST.json')) {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $source $entry), $entry) | Out-Null
    }
} finally { $archive.Dispose() }
Get-ChildItem -LiteralPath $downloads -File | Sort-Object Name | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
} | Set-Content -LiteralPath (Join-Path $downloads 'SHA256SUMS.txt') -Encoding ASCII
Write-Output "Release downloads: $downloads"
Write-Output 'No release was uploaded, no installer was executed, and no real device was accessed.'
