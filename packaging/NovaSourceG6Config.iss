; Compile through Build-Release.ps1 so version, payload and signing are explicit.
#ifndef AppVersion
  #error AppVersion must be supplied by Build-Release.ps1
#endif
#ifndef PublishDir
  #error PublishDir must be supplied by Build-Release.ps1
#endif
#ifndef ReleaseDir
  #error ReleaseDir must be supplied by Build-Release.ps1
#endif

[Setup]
AppId={{859A3975-7AD1-43C8-B31F-F365D72193B1}
AppName=NovaSource G6 Config
AppVersion={#AppVersion}
AppPublisher=Max NG7M
AppPublisherURL=https://github.com/ng7m/NovaSourceG6
AppUpdatesURL=http://www.ng7m.com/downloads/NG7M/NovaSourceG6/
DefaultDirName={localappdata}\Programs\NovaSourceG6Config
DefaultGroupName=NovaSource G6 Config
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir={#ReleaseDir}
OutputBaseFilename=NovaSourceG6Config-{#AppVersion}-win-x64-Setup
SetupIconFile=..\application\NovaSourceG6Config\Assets\NovaSourceG6Config.ico
UninstallDisplayIcon={app}\NovaSourceG6Config.exe
LicenseFile=..\LICENSE
InfoBeforeFile=USER-README.txt
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
AppMutex=Local\NovaSourceG6Config.Running
SetupMutex=Local\NovaSourceG6Config.Setup
CloseApplications=no
RestartApplications=no
SignTool=g6test
SignedUninstaller=yes
Uninstallable=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\NovaSource G6 Config"; Filename: "{app}\NovaSourceG6Config.exe"
Name: "{autodesktop}\NovaSource G6 Config"; Filename: "{app}\NovaSourceG6Config.exe"; Tasks: desktopicon

; No Run or UninstallDelete section: do not launch/control hardware, remove
; preferences, or remove the shared .NET runtime on installation/uninstallation.
[Code]
const
  RuntimeDownload = 'https://dotnet.microsoft.com/en-us/download/dotnet/10.0';

function HasCompatibleDesktopRuntime: Boolean;
var
  DesktopVersions, CoreVersions: TArrayOfString;
  I, J, DotPosition, MajorVersion: Integer;
  MajorPrefix: String;
begin
  Result := False;
  if not RegGetValueNames(HKLM64,
    'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', DesktopVersions) then Exit;
  if not RegGetValueNames(HKLM64,
    'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App', CoreVersions) then Exit;
  for I := 0 to GetArrayLength(DesktopVersions) - 1 do
  begin
    DotPosition := Pos('.', DesktopVersions[I]);
    if (DotPosition > 1) and (Pos('-', DesktopVersions[I]) = 0) then
    begin
      MajorPrefix := Copy(DesktopVersions[I], 1, DotPosition);
      MajorVersion := StrToIntDef(Copy(MajorPrefix, 1, Length(MajorPrefix) - 1), 0);
      if MajorVersion >= 8 then
        for J := 0 to GetArrayLength(CoreVersions) - 1 do
          if (Pos(MajorPrefix, CoreVersions[J]) = 1) and (Pos('-', CoreVersions[J]) = 0) then
          begin
            Result := True;
            Exit;
          end;
    end;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ErrorCode: Integer;
begin
  Result := '';
  if CheckForMutexes('Local\NovaSourceG6Config.Running') then
  begin
    Result := 'Close NovaSource G6 Config normally before continuing. Setup will not stop an active sweep or serial command.';
    Exit;
  end;
  if not HasCompatibleDesktopRuntime then
  begin
    Result := 'Install the Microsoft .NET 10 Desktop Runtime (x64), then return here and click Install to retry. A registered .NET 8 or newer Desktop Runtime is required.';
    if not WizardSilent then
      if MsgBox('A compatible .NET Desktop Runtime (x64) was not found.' + #13#10 + #13#10 +
        'Open Microsoft''s download page? Choose Desktop Runtime for Windows x64, run its installer, and then return to Setup.' + #13#10 +
        'The runtime installer may request administrator permission.', mbConfirmation, MB_YESNO) = IDYES then
        ShellExec('open', RuntimeDownload, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;
