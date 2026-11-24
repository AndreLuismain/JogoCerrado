; ============================================================================
; Script Inno Setup — Instalador Oficial EICerrado (Versão Beta Demonstrativa)
; Projeto Educativo 3D sobre o Bioma Cerrado — Universidade de São Paulo (USP)
; ============================================================================

#define MyAppName "EICerrado - Exploracao e Fotografia no Cerrado (Beta)"
#define MyAppShortName "EICerrado"
#define MyAppVersion "1.0.0-beta"
#define MyAppPublisher "Universidade de Sao Paulo (USP) - Equipe EICerrado"
#define MyAppURL "https://www5.usp.br/"
#define MyAppExeName "EICerrado.exe"
#define BuildSourceDir "..\Builds\Beta_EICerrado"

[Setup]
AppId={{8E4C7A92-5D3B-4E1F-9B6A-2C8F0D1E7A55}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppShortName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Permite instalar sem exigir privilegios de administrador caso o usuario prefira
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\Builds\Installer
OutputBaseFilename=EICerrado_Beta_Setup
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#BuildSourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppShortName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppShortName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppShortName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
