# Windows test-release packaging

The primary download is a per-user Inno Setup installer. A second ZIP contains
the same framework-dependent x64 payload for extraction and direct launch.
The application targets .NET 8 and permits major roll-forward; Windows Desktop
Runtimes 8, 9 and 10 have passed the existing automated suite. Windows 11 is the
supported operating system. ARM64-native and x86 packages are not produced.

## One-time preparation

Install **Inno Setup 6.3 or newer** from https://jrsoftware.org/isdl.php.
Use the repository-pinned .NET SDK or install the matching SDK from global.json.
The packaging command fails before publishing if the compiler is missing.

Create the local test signing identity, from PowerShell at the repository root:

```powershell
.\packaging\New-TestSigningCertificate.ps1
```

This reuses an unexpired matching certificate, or creates a one-year RSA/SHA-256
code-signing certificate in `Cert:\CurrentUser\My` with a non-exportable private
key. `.signing/` holds the thumbprint and public certificate only and is ignored
by Git. No root/publisher trust is added. Keep the signing account and machine
protected; losing its key requires a new test certificate. Never put private
keys or certificate-store exports in a source archive or download.

Self-signed signatures are for early testing, not public publisher trust. Windows
may warn about them. Do not instruct users to disable security protections or
install the certificate into Trusted Roots. These offline test signatures are
not timestamped; replace this workflow with trusted, timestamped signing for
public releases. Sign-Artifact verifies that the expected signer is present and
rejects hash mismatches; an untrusted-root result is expected for test signing.

## Create a release (explicit publish/build operation)

Write version-specific release notes first; `RELEASE-NOTES.md` here is a template.
Use a new three-part version for each distributed build. For example:

```powershell
.\packaging\Build-Release.ps1 -Version 0.2.0 -ReleaseNotes .\docs\releases\0.2.0.md
```

Optional parameters: `-IsccPath` for a nonstandard compiler location and
`-CertificateThumbprint` for a different certificate in the current user's
personal store. Version values are applied consistently to the executable,
assembly, About dialog, installer and archive names. The example notes path
must be created before running the example.

The command creates an isolated working-tree source snapshot, publishes from
that snapshot, signs only the application's EXE/DLL plus installer/uninstaller,
and writes these files under `artifacts/releases/<version-and-run>/downloads/`:

- `NovaSourceG6Config-<version>-win-x64-Setup.exe`
- `NovaSourceG6Config-<version>-win-x64.zip`
- `NovaSourceG6Config-<version>-source.zip`
- `RELEASE-NOTES.md`
- `SHA256SUMS.txt`

The source archive includes uncommitted application changes, build/packaging
scripts, project definitions, reference documents and license, but excludes
private keys, SDKs and generated build directories. SOURCE-MANIFEST.json records
the base commit and hashes of the source snapshot. Third-party dependencies are
restored by NuGet; their package metadata and notices accompany the binary
payload. No third-party DLL is re-signed under Max's test identity.

No installer is executed, application launched, tests run or release uploaded by
this command. Normal `dotnet build` remains a developer build, not a packaged
release. It does not change existing release folders. Review all downloads and
the included source before deploying the **complete downloads set** to the
NG7M download site using Deploy-Release.ps1. Publishing is a separate,
explicit action; don't upload only binaries without the matching source archive.

## Installer behavior

- Installs to `%LOCALAPPDATA%\Programs\NovaSourceG6Config`, with a Start menu
  shortcut and optional desktop shortcut; adds the standard uninstall entry.
- Checks registered, stable x64 Desktop and core runtimes of the same major
  version, 8 or newer. Preview-only and x86-only installations do not qualify.
- If no runtime is found, offers Microsoft's .NET 10 download page. The user
  installs Desktop Runtime x64 separately and returns to retry; no downloaded
  program is silently executed. Silent Setup fails clearly when the runtime is
  missing. The prerequisite may require administrator permission.
- Uses a stable AppId so newer installers upgrade the same installation.
- Checks the application's named mutex and asks the user to close it normally.
  Does not terminate sweeps, force-close processes, or automatically restart.
- Retains `%LOCALAPPDATA%\NovaSourceG6Config` preferences on upgrade/uninstall.
  Does not install/remove serial drivers, bridge tools or shared runtimes.

The ZIP uses the same AppData preferences and is not a USB-portable settings
mode. Extract the entire folder, not just the EXE. About's Check for updates
opens the NG7M download page only when clicked; there are no background update checks,
telemetry, automatic downloads or automatic version comparisons.

## Download site deployment

Public page: http://www.ng7m.com:18080/
Project link: https://github.com/ng7m/NovaSourceG6

The page describes the self-signed test certificate. No certificates or security settings are installed by the
page. There are no third-party fonts, scripts, analytics, or automatic downloads.

Prepare a local preview without writing to the web server:

```powershell
.\packaging\Deploy-Release.ps1 -Version 0.2.0 -ReleaseDate 2026-09-05 -ReleaseDirectory <downloads-folder>
```

To publish, add `-Deploy` (defaults to `\\nt7g-server\c$\inetpub`) or
`-Deploy -DestinationRoot <existing-parent-folder>` using the actual
filesystem directory mapped to the public URL. The script creates NovaSourceG6
beneath it, verifies checksums, stages the complete release, verifies it again,
then renames staging to `releases/<version>`. Existing version folders must match
exactly and are never overwritten. `index.html` is replaced last, with its prior
copy retained in `_page-history`. Rollback means republishing the prior version's
landing page using that version's original files. No other web directory is modified.

The project-local web.config enables index.html as the default page, disables
directory listings, and adds plain-text serving of release-note Markdown. Verify
HTTP responses for the page and all five release files after deployment. The web
server must permit these settings in the virtual directory and serve EXE/ZIP files.

Existing 0.2.0 packages and their matching source/checksums remain unchanged.
The update-button and installer URL changes enter the next built release.

### Port 18080 cutover

Activated on September 8, 2026. IIS and the server firewall were configured,
and the router forwards TCP 18080 to 192.168.1.102:18080. The public URL returned
HTTP 200 from the deployment PC and all five downloaded release files matched
the original hashes. The old landing page now redirects to the new URL; its
release files remain available. Release 0.2.0 was not rebuilt or modified.

The site directory is `C:\inetpub\NovaSourceG6` on nt7g-server (192.168.1.102).
Run `packaging/Configure-IisWebsite.ps1` in an elevated Windows PowerShell session
on that server after deployment. Create `C:\ProgramData\NovaSourceG6Deployment`
first for its result log. The script backs up IIS configuration, checks port
conflicts, creates the `NovaSource G6 Downloads` website and isolated app pool,
grants read access, and allows inbound TCP 18080 for local address 192.168.1.102.
Its HTTP binding is `192.168.1.102:18080:www.ng7m.com`.

Verify the page and release downloads over the LAN using hostname www.ng7m.com
resolved to 192.168.1.102 (or an HTTP Host header of www.ng7m.com:18080).
Then forward router **TCP external 18080 to 192.168.1.102 internal 18080**.
Check http://www.ng7m.com:18080/ from outside the LAN before redirecting users.

Until that verification succeeds, keep the original site active at
http://www.ng7m.com/downloads/NG7M/NovaSourceG6/.
Afterward, back up its index.html and replace only that file with
`packaging/web/legacy-redirect.html` in
`\\nt7g-server\c$\WebCluster\downloads_virtual\NG7M\NovaSourceG6`.
Retain its release files so existing direct download links continue working.
The redirect template is prepared, not automatically deployed by Deploy-Release.ps1.

## Release acceptance (run only when requested)

Before distributing, verify installation on a clean Windows 11 x64 machine,
missing-runtime guidance, signature metadata, ZIP launch, shortcuts, uninstall,
upgrade with settings preserved, and refusal to upgrade while the app is open.
Verify behavior for both interactive and silent installation. Check the three
supported runtimes independently. Hardware acceptance remains a separate sprint.
