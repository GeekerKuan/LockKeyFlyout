#define MyAppName "LockKeyFlyout"
#define MyAppVersion "1.0.7"
#define MyAppPublisher "LockKeyFlyout Contributors"
#define MyAppExeName "LockKeyFlyout.exe"

[Setup]
AppId={{B75248F4-6CA8-4E08-92C6-2A415E2BBF2F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DisableDirPage=no
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\..\outputs
OutputBaseFilename=LockKeyFlyout-Setup-1.0.7-x64
SetupIconFile=assets\LockKeyFlyout.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=force
CloseApplicationsFilter={#MyAppExeName}
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加快捷方式："; Flags: unchecked

[Files]
Source: "..\..\outputs\LockKeyFlyout-acrylic-portable\LockKeyFlyout.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "default-settings.ini"; DestDir: "{localappdata}\LockKeyFlyout"; DestName: "settings.ini"; Flags: onlyifdoesntexist
Source: "THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\LockKeyFlyout"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 LockKeyFlyout"; Filename: "{uninstallexe}"
Name: "{autodesktop}\LockKeyFlyout"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{sys}\schtasks.exe"; Parameters: "/Create /F /TN ""LockKeyFlyout"" /SC ONLOGON /RL HIGHEST /TR """"{app}\{#MyAppExeName}"""""; Flags: runhidden waituntilterminated
Filename: "{sys}\schtasks.exe"; Parameters: "/Run /TN ""LockKeyFlyout"""; Description: "以管理员权限启动 LockKeyFlyout"; Flags: runhidden postinstall skipifsilent waituntilterminated

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM {#MyAppExeName}"; Flags: runhidden; RunOnceId: "StopLockKeyFlyout"
Filename: "{sys}\schtasks.exe"; Parameters: "/Delete /F /TN ""LockKeyFlyout"""; Flags: runhidden; RunOnceId: "DeleteLockKeyFlyoutTask"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  { Restart Manager does not reliably close an elevated tray process. }
  { Stop the scheduled-task instance first, then terminate older portable }
  { or installed copies by image name before [Files] replaces the EXE. }
  Exec(ExpandConstant('{sys}\schtasks.exe'),
    '/End /TN "LockKeyFlyout"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec(ExpandConstant('{sys}\taskkill.exe'),
    '/F /T /IM "{#MyAppExeName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(500);
  Result := '';
end;
