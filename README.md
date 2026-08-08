# multichat

> 🤖 이 프로젝트는 [Claude Code](https://claude.com/claude-code)로 작성되었습니다.

오프라인 강의 진행시에 강의장 안에서 강사 PC(server)와 수강생 PC(client)가 LAN으로 채팅 + 파일 공유하는 앱. 강사용 서버와 Windows 수강생 클라이언트, macOS 수강생 클라이언트를 제공한다.

## 다운로드
- [강사용 서버 설치 파일 (ChatServerSetup.exe)](dist/ChatServerSetup.exe)
- [수강생용 클라이언트 설치 파일 - Windows (ChatClientSetup.exe)](dist/ChatClientSetup.exe)
- [수강생용 클라이언트 설치 파일 - macOS (ChatClientMacSetup.dmg)](dist/ChatClientMacSetup.dmg)

> 저장소에 원격 호스팅이 없어 저장소 내 상대 경로 링크다. GitHub 등에 올리면 그대로 다운로드 링크로 동작하고, 로컬에서는 `dist/` 폴더의 파일을 직접 받으면 된다.

## 구성
- `shared/` — 채팅 프로토콜, 채팅 로그 UI, 이미지 처리, 로컬 이력 저장 공용 라이브러리 (C#, Windows)
- `server/` — 강사용 앱 (`ChatServer.exe`, Windows)
- `client/` — 수강생용 앱 (`ChatClient.exe`, Windows)
- `mac/` — 수강생용 macOS 네이티브 클라이언트 (Swift/SwiftUI, `client/`와 동일 프로토콜). 빌드 방법은 [mac/README.md](mac/README.md) 참고
- `installer/` — Inno Setup 스크립트, `dist/` — 완성된 설치 파일(`ChatServerSetup.exe`, `ChatClientSetup.exe`)

---

## 설치
`dist\ChatServerSetup.exe`는 강사 PC에, `dist\ChatClientSetup.exe`는 수강생 PC에 각각 설치한다.
.NET 런타임을 따로 설치할 필요 없음(자체 포함 빌드).

---

## 강사용 서버 사용법

1. **채팅 서버**를 실행한다.
2. 상단에서 **채팅 포트**(기본 9000), **파일 포트**(기본 9001), **공유 폴더**(수강생이 다운로드할 파일을 미리 넣어둘 폴더)를 지정하고 **서버 시작**을 누른다.
3. 시작하면 화면 상단에 **서버 주소(로컬 IPv4 전체) + 포트**가 표시된다. 수강생에게 이 주소를 알려주면 된다.
4. 하단 입력창에 메시지를 입력하고 **Ctrl+Enter**로 전송한다(**Enter**는 줄바꿈 — 여러 줄 메시지 가능).
5. **받는 사람** 콤보박스에서 **(전체 수강생)** 또는 접속 중인 특정 수강생 이름을 골라 전송 대상을 지정할 수 있다.
   - 전체 수강생에게 보낸 메시지는 늦게 접속하는 수강생에게도 자동으로 전달된다(접속 시점까지의 전체-공지 이력을 받음).
   - 특정 수강생 지정 메시지는 그 사람에게만 가고 이력에는 남지 않는다.
6. 접속한 수강생이 보낸 메시지도 이 창에 표시된다. "강사에게만" 보낸 귓속말은 서버 화면에만 보이고 다른 수강생에게는 전달되지 않는다.
7. **이미지 전송**: 화면 캡처 후 입력창에서 **Ctrl+V** 하면 클립보드 이미지가 바로 전송된다(별도 첨부 절차 없음).
8. 오른쪽 목록에 현재 접속한 수강생 이름이 표시된다.

### 공유 폴더 파일
서버 시작 시 지정한 폴더에 파일을 넣어두면 수강생이 다운로드할 수 있다. 서버가 실행 중일 때 폴더에 파일을 추가/삭제하면 수강생 쪽에서 새로고침만 하면 바로 반영된다(별도 등록 절차 없음).

---

## 수강생용 클라이언트 사용법

1. **채팅 클라이언트**를 실행하면 접속 다이얼로그가 뜬다.
2. **서버 주소**(강사가 알려준 IP), **채팅 포트**, **파일 포트**, 본인 **이름**을 입력하고 **접속**을 누른다.
   - "강사"라는 이름은 예약어라 사용할 수 없다.
3. 메인 화면 왼쪽이 채팅, 오른쪽이 공유 파일 목록이다.
4. 메시지 입력 후 **Ctrl+Enter**로 전송(**Enter**는 줄바꿈). 받는 대상은 **전체 수강생** / **강사에게만** 라디오 버튼으로 선택한다.
5. **이미지 전송**: 스크린샷 등을 복사한 뒤 입력창에서 **Ctrl+V** 하면 바로 이미지 메시지로 전송된다.
6. 접속하는 순간, 접속 전에 강사가 전체 공지로 보낸 메시지 이력을 자동으로 받아 채팅창에 채워진다.

### 메시지 다루기
- 텍스트 메시지: **더블클릭**하면 그 메시지 텍스트만 클립보드로 복사된다.
- 이미지 메시지: 채팅창에는 **작은 고정 크기 썸네일**로 보인다. **클릭**하면 원본 크기로 보여주는 창이 뜨고, **우클릭 메뉴**에서 **다운로드**(파일로 저장) 또는 **이미지 복사**(클립보드)를 할 수 있다.
- **대화 내보내기(Markdown)** 버튼으로 지금까지의 전체 대화를 `.md` 파일로 저장할 수 있다(이미지는 텍스트로 대체 표시되고 파일 자체는 포함되지 않음).

### 파일 다운로드
오른쪽 파일 목록에서 파일을 하나 또는 여러 개(Ctrl/Shift 클릭) 선택 후 **다운로드**를 누른다.
- 1개 선택: 저장 위치/파일명을 직접 지정.
- 여러 개 선택: 저장할 폴더만 지정하면 원래 파일명 그대로 한 번에 저장.
- **새로고침** 버튼으로 서버 공유 폴더의 최신 파일 목록을 다시 받아온다.

---

## 채팅 이력 로컬 저장 (자동)

서버와 클라이언트 각각 자신이 화면에 표시한 모든 메시지(텍스트+이미지)를 실행 PC의
`%AppData%\MultiChat\` 폴더에 저장한다(`server_history.jsonl` / `client_history.jsonl`).
**다음 날 다시 실행해도 이전 대화 내역이 채팅창에 그대로 복원된다.** 서버 재시작 여부와
무관하게 각 앱이 로컬에서 독립적으로 기억하는 방식이다.

> ponytail: 이력 파일은 계속 누적되며 자동 로테이션/삭제가 없다. 강의실 단기 사용 기준으로는
> 문제없으나, 장기간 대량 이미지 전송 시 파일이 커질 수 있음 — 필요해지면 날짜별 분리/정리 기능 추가.

---

## 메시지 라우팅 규칙

- 수강생 → 전체: 서버가 다른 모든 수강생에게 브로드캐스트 + 강사 화면에도 표시.
- 수강생 → 강사에게만: 강사 화면에만 표시, 다른 수강생에게는 전달 안 됨.
- 강사 → 전체: 모든 접속 클라이언트에 브로드캐스트되고, 서버 메모리의 공지 이력에 저장되어 늦게 접속하는 클라이언트도 받는다.
- 강사 → 특정 수강생: 그 사람에게만 전달, 공지 이력에는 남지 않음(다른 사람 늦은 접속 시 재전달 안 됨).

---

## 통신 방식 (기술 세부사항)

순수 TCP 소켓 두 개만 사용(HTTP/외부 패키지 없음 — 강의실 PC에서 관리자 권한이나
방화벽 예외 설정 없이 바로 뜨게 하려고 `HttpListener` 대신 이 방식을 선택).

- **채팅 포트**(기본 9000): 접속 유지, 줄바꿈 구분 JSON(`ChatEnvelope`) 송수신. 이미지는 PNG 바이트를 JSON 내 base64로 실어 보낸다(전송 전 최대 1600px로 축소).
- **파일 포트**(기본 9001): 요청마다 새 연결. `LIST`로 목록, `GET|파일명`으로 다운로드, `GETKEY`로 채팅 암호화 키를 받는다.

### 채팅 메시지 암호화
"서버 시작"을 누를 때마다 서버가 무작위 AES-256 키를 새로 생성한다. 클라이언트는 접속 과정에서
파일 포트의 `GETKEY`로 이 키를 먼저 받아온 뒤, 그 키로 채팅 TCP 연결의 모든 줄(JSON)을
AES-256-GCM으로 암호화/복호화한다. 그 결과 LAN에서 패킷을 스니핑해도 채팅 내용이 평문으로
보이지 않는다.

> ponytail: 키 교환 자체는 평문 TCP라 TLS/인증서 기반 상호 인증까지는 아니다(능동적 중간자
> 공격까지 막지는 못함). "네트워크 스니핑으로 메시지가 그대로 보이는 것"을 막는 게 목표이며,
> 더 강한 보장이 필요해지면 `SslStream` + 인증서로 교체.

---

## 빌드 / 실행 (개발자용)
```
dotnet build server\Server.csproj
dotnet build client\Client.csproj
dotnet run --project server\Server.csproj
dotnet run --project client\Client.csproj
```

## 패키징 (설치 파일 생성)
```
dotnet publish server\Server.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\server
dotnet publish client\Client.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\client
& "$env:LocalAppData\Programs\Inno Setup 6\ISCC.exe" installer\server.iss
& "$env:LocalAppData\Programs\Inno Setup 6\ISCC.exe" installer\client.iss
```
결과물: `dist\ChatServerSetup.exe`, `dist\ChatClientSetup.exe`
