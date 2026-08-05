#define AppName "ChatServer"
#define AppDisplayName "채팅 서버 (강사용)"
#define AppVersion "1.0.0"
#define PublishDir "..\publish\server"

[Setup]
AppId={{6C2B9E5A-1F3D-4C7E-9B4A-2F1A6D8C9001}
AppName={#AppDisplayName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppDisplayName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=ChatServerSetup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#AppDisplayName}"; Filename: "{app}\ChatServer.exe"
Name: "{autodesktop}\{#AppDisplayName}"; Filename: "{app}\ChatServer.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "바탕 화면에 바로 가기 만들기"; GroupDescription: "추가 아이콘:"

[Run]
Filename: "{app}\ChatServer.exe"; Description: "설치 후 바로 실행"; Flags: nowait postinstall skipifsilent
