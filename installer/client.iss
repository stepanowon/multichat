#define AppName "ChatClient"
#define AppDisplayName "채팅 클라이언트 (수강생용)"
#define AppVersion "1.0.0"
#define PublishDir "..\publish\client"

[Setup]
AppId={{9A3F1D7B-2E4C-4A8F-8D5B-3C2E7F9A1002}
AppName={#AppDisplayName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppDisplayName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=ChatClientSetup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#AppDisplayName}"; Filename: "{app}\ChatClient.exe"
Name: "{autodesktop}\{#AppDisplayName}"; Filename: "{app}\ChatClient.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "바탕 화면에 바로 가기 만들기"; GroupDescription: "추가 아이콘:"

[Run]
Filename: "{app}\ChatClient.exe"; Description: "설치 후 바로 실행"; Flags: nowait postinstall skipifsilent
