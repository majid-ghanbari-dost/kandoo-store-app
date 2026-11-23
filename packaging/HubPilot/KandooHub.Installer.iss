; Kandoo Store — Windows Installer
; ======================================
; Builds a self-contained setup.exe for the Kandoo Store MAUI Windows app.
; No .NET runtime is required on the target machine.
;
; Install layout (installer layout v2, PM-04 in the old Kandoo repo):
;   {app}\app\         KandooStore.App.exe (MAUI) + its own self-contained runtime
;   {app}\hub\         Kandoo.Hub.exe + its own runtime — NOT in this repo yet.
;                      The Hub is a separate component; when it ships, its publish
;                      output goes to publish\hub and the [Files] section below
;                      gains a hub entry. Shortcuts and [Run] point to {app}\app\.
;
; All Source paths are relative to THIS script's folder (packaging\HubPilot).
; CI stages the inputs under packaging\HubPilot\publish:
;   publish\app = Windows app publish output (self-contained, win-x64)
; Compile:
;   iscc packaging\HubPilot\KandooHub.Installer.iss
; Result: packaging\HubPilot\Output\Kandoo-Setup-x64.exe
;
; Hand-written for this repo, following the old KandooHub.Installer.iss
; (main @ 8059f5f4). PARTIAL: not yet tested on a real Windows machine.

#define MyAppName "Kandoo"
#define MyAppPublisher "Kandoo"
#define MyAppURL "https://kandoo.ir"
#define MyAppExeName "KandooStore.App.exe"
#define MyAppVersion "1.2.1"
#define InstallerName "Kandoo-Setup-x64"

[Setup]
; Fixed AppId keeps upgrades replacing the same installation.
AppId={{4F1C2A3E-8B6D-4E1F-9C2D-7A3B5E1D9F40}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
Uninstallable=yes
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
OutputDir=Output
OutputBaseFilename={#InstallerName}

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Windows app publish output, in its own folder, with its own runtime.
Source: "publish\app\*"; DestDir: "{app}\app"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "README.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\app\{#MyAppExeName}"; WorkingDir: "{app}\app"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\app\{#MyAppExeName}"; WorkingDir: "{app}\app"; Tasks: desktopicon

[Run]
Filename: "{app}\app\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent unchecked

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
