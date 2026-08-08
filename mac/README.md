# MacChatClient

Windows `client/`(수강생용 채팅 클라이언트)와 동일한 화면 구조/프로토콜을 쓰는 macOS 네이티브 클라이언트.
Swift + SwiftUI로 작성했고, 외부 의존성은 없다(CryptoKit로 AES-256-GCM, Foundation Stream으로 TCP 소켓).

## 빌드 & 실행

```bash
# 개발 중 바로 실행
swift run

# 배포용 dmg 생성 (저장소 루트 dist/ChatClientMacSetup.dmg)
./build.sh
open ../dist/ChatClientMacSetup.dmg
```

Xcode 없이 Swift Package Manager + macOS 내장 `hdiutil`만으로 빌드/패키징된다. macOS 13(Ventura) 이상 필요.
dmg를 열면 `.app`과 `/Applications` 심볼릭 링크가 보이는 표준 드래그 설치 구성이다.

## 구성

- `Models.swift` — `shared/Protocol.cs`의 `ChatEnvelope`/`MsgType`/`ChatTarget`과 1:1 대응 (PascalCase 키, enum-as-int, byte[]↔base64, DateTime↔ISO8601 그대로 호환)
- `CryptoUtil.swift` — `shared/CryptoUtil.cs`와 동일한 AES-256-GCM 프레이밍(nonce 12B + cipher + tag 16B, base64)
- `TCPStream.swift` — 줄 단위 블로킹 read/write 소켓 래퍼 (Foundation `Stream`)
- `LineProtocol.swift`, `ChatClientCore.swift`, `FileClientCore.swift` — client의 동명 `.cs` 파일과 동일 프로토콜(채팅 TCP 암호화 라인, 파일 포트 LIST/GET/GETKEY)
- `ChatHistoryStore.swift` — `~/Library/Application Support/MultiChat/mac_client_history.jsonl`에 로컬 이력 저장
- `ImageUtil.swift` — 클립보드 이미지 전송용 축소 인코딩(최대 1600px, PNG)
- `ChatViewModel.swift` — 화면 상태/동작(WinForms `MainForm.cs`에 대응)
- `ConnectView.swift`, `MainView.swift`, `MessageBubble.swift` — 접속 다이얼로그, 메인 화면(채팅+공유 파일 목록), 말풍선/이미지 뷰어

## 기능 대응표 (Windows client 기준)

| Windows | Mac |
|---|---|
| Ctrl+Enter 전송 / Enter 줄바꿈 / Ctrl+V 이미지 붙여넣기 | Cmd+Return 전송 / Return 줄바꿈 / Cmd+V 이미지 붙여넣기 (macOS 관례에 맞춤) |
| 전체 수강생 / 강사에게만 라디오 버튼 | 동일 (가로 라디오 그룹) |
| 메시지 더블클릭 → 클립보드 복사 | 동일 |
| 이미지 클릭 → 원본 크기 보기, 우클릭 → 다운로드/복사 | 동일 |
| 공유 파일 목록 다중 선택 + 다운로드 | 동일 (List 다중 선택) |
| 대화 내보내기 (Markdown) | 동일 |

- 강사(서버) 앱은 이 저장소에서 Windows 전용으로 유지된다. 이 클라이언트는 `server/`가 발급하는 세션 키·프로토콜을 그대로 따르는 수강생용 클라이언트만 macOS로 포팅한 것.
- ponytail: 자동 업데이트/서명(코드사이닝)·커스텀 앱 아이콘은 넣지 않음 — 강의실 LAN에서 직접 실행하는 용도로는 불필요. 배포가 필요해지면 `codesign`/`.icns` 추가.
