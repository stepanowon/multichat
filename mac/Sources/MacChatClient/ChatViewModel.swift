import AppKit
import UniformTypeIdentifiers

struct ImageWrapper: Identifiable {
    let id = UUID()
    let data: Data
}

/// client/MainForm.cs의 상태/동작을 SwiftUI용 ObservableObject로 옮긴 것.
final class ChatViewModel: ObservableObject {
    @Published var messages: [ChatEnvelope] = []
    @Published var encryptionText = "🔒 암호화 연결 확인 중..."
    @Published var encryptionOK = false
    @Published var encryptionFailed = false
    @Published var files: [FileEntry] = []
    @Published var selectedFileNames: Set<String> = []
    @Published var inputText = ""
    @Published var target: ChatTarget = .all
    @Published var alertMessage: String?
    @Published var viewingImage: ImageWrapper?
    @Published var isConnected = false
    @Published var isConnecting = false

    let host: String
    let chatPort: Int
    let filePort: Int
    let name: String

    private let chat = ChatClientCore()
    private let fileClient: FileClientCore
    private let history: ChatHistoryStore
    private var started = false
    private var userDisconnected = false

    init(host: String, chatPort: Int, filePort: Int, name: String) {
        self.host = host
        self.chatPort = chatPort
        self.filePort = filePort
        self.name = name
        fileClient = FileClientCore(host: host, port: filePort)
        history = ChatHistoryStore(fileName: "mac_client_history.jsonl")
    }

    func start() {
        guard !started else { return }
        started = true

        for env in history.load() { messages.append(env) }

        chat.onMessage = { [weak self] env in
            // 서버가 다른 수강생의 입장/퇴장을 System 메시지로 브로드캐스트하는데,
            // 본인 채팅창에는 표시하지 않는다(자기 자신의 접속/종료 메시지는 로컬에서 별도로 appendSystem됨).
            guard env.type != .system else { return }
            self?.append(env)
        }
        chat.onHistory = { [weak self] list in list.forEach { self?.append($0) } }
        chat.onDisconnected = { [weak self] in
            guard let self else { return }
            self.isConnected = false
            self.encryptionText = "⚠ 연결이 종료되었습니다"
            self.encryptionOK = false
            self.encryptionFailed = true
            if self.userDisconnected {
                self.userDisconnected = false
            } else {
                self.appendSystem("서버와 연결이 끊어졌습니다.")
            }
        }

        connectFlow()
    }

    /// 실패 시 재연결, 수동 종료 후 재접속에도 재사용된다.
    private func connectFlow() {
        guard !isConnecting, !isConnected else { return }
        isConnecting = true
        encryptionText = "🔒 암호화 연결 확인 중..."
        encryptionOK = false
        encryptionFailed = false

        Thread.detachNewThread { [weak self] in
            guard let self else { return }
            do {
                let key = try self.fileClient.getKey()
                try self.chat.connect(host: self.host, chatPort: self.chatPort, name: self.name, key: key)
                DispatchQueue.main.async {
                    self.isConnecting = false
                    self.isConnected = true
                    self.encryptionText = "🔒 서버-클라이언트 종단 암호화 연결됨 (AES-256-GCM)"
                    self.encryptionOK = true
                    self.appendSystem("서버에 접속했습니다.")
                    self.refreshFiles()
                }
            } catch {
                DispatchQueue.main.async {
                    self.isConnecting = false
                    self.encryptionText = "⚠ 암호화 연결 실패"
                    self.encryptionFailed = true
                    self.alertMessage = "서버 접속 실패: \(error.localizedDescription)"
                }
            }
        }
    }

    func reconnect() {
        connectFlow()
    }

    func disconnectManually() {
        guard isConnected else { return }
        userDisconnected = true
        chat.disconnect()
        appendSystem("연결을 종료했습니다.")
    }

    private func append(_ env: ChatEnvelope, persist: Bool = true) {
        messages.append(env)
        if persist { history.append(env) }
    }

    private func appendSystem(_ text: String) {
        append(ChatEnvelope(type: .system, from: "시스템", text: text))
    }

    func send() {
        let text = inputText.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !text.isEmpty else { return }
        inputText = ""
        append(ChatEnvelope(type: .chat, from: name, target: target, text: text))
        chat.send(text: text, target: target)
    }

    func sendImage(_ rawData: Data) {
        guard let nsImage = NSImage(data: rawData), let png = ImageUtil.encodeForTransfer(nsImage) else { return }
        append(ChatEnvelope(type: .chat, from: name, target: target, image: png))
        chat.sendImage(png: png, target: target)
    }

    func refreshFiles() {
        Thread.detachNewThread { [weak self] in
            guard let self else { return }
            do {
                let list = try self.fileClient.list()
                DispatchQueue.main.async { self.files = list }
            } catch {
                DispatchQueue.main.async { self.alertMessage = "파일 목록을 가져오지 못했습니다: \(error.localizedDescription)" }
            }
        }
    }

    func downloadSelected() {
        let items = files.filter { selectedFileNames.contains($0.name) }
        guard !items.isEmpty else { return }

        if items.count == 1 {
            let panel = NSSavePanel()
            panel.nameFieldStringValue = items[0].name
            guard panel.runModal() == .OK, let url = panel.url else { return }
            Thread.detachNewThread { [weak self] in
                guard let self else { return }
                do {
                    try self.fileClient.download(fileName: items[0].name, to: url)
                    DispatchQueue.main.async { self.alertMessage = "다운로드가 완료되었습니다." }
                } catch {
                    DispatchQueue.main.async { self.alertMessage = "다운로드 실패: \(error.localizedDescription)" }
                }
            }
            return
        }

        let panel = NSOpenPanel()
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        panel.canCreateDirectories = true
        panel.prompt = "다운로드할 폴더를 선택하세요."
        guard panel.runModal() == .OK, let dir = panel.url else { return }

        Thread.detachNewThread { [weak self] in
            guard let self else { return }
            var ok = 0, fail = 0
            for item in items {
                do {
                    try self.fileClient.download(fileName: item.name, to: dir.appendingPathComponent(item.name))
                    ok += 1
                } catch { fail += 1 }
            }
            DispatchQueue.main.async { self.alertMessage = "다운로드 완료: \(ok)개 성공, \(fail)개 실패" }
        }
    }

    func exportMarkdown() {
        let panel = NSSavePanel()
        panel.allowedContentTypes = [UTType(filenameExtension: "md") ?? .plainText]
        let nameFmt = DateFormatter()
        nameFmt.dateFormat = "yyyyMMdd_HHmmss"
        panel.nameFieldStringValue = "chat_\(nameFmt.string(from: Date())).md"
        guard panel.runModal() == .OK, let url = panel.url else { return }

        let imageDir = url.deletingLastPathComponent().appendingPathComponent("export_images")
        let headerFmt = DateFormatter(); headerFmt.dateFormat = "yyyy-MM-dd HH:mm"
        let itemFmt = DateFormatter(); itemFmt.dateFormat = "MM/dd HH:mm:ss"
        let imgFmt = DateFormatter(); imgFmt.dateFormat = "yyyyMMdd_HHmmss_SSS"

        var text = "# 채팅 기록 (\(headerFmt.string(from: Date())))\n\n"
        for m in messages {
            let targetLabel: String
            switch m.target {
            case .instructorOnly: targetLabel = " (강사에게만)"
            case .specific: targetLabel = m.to.map { " (개인 메시지 → \($0))" } ?? " (개인 메시지)"
            case .all: targetLabel = ""
            }
            text += "- **[\(itemFmt.string(from: m.time))] \(m.from)\(targetLabel)**:\n\n"
            if let img = m.image {
                try? FileManager.default.createDirectory(at: imageDir, withIntermediateDirectories: true)
                let fname = "img_\(imgFmt.string(from: m.time)).png"
                try? img.write(to: imageDir.appendingPathComponent(fname))
                text += "  ![이미지](export_images/\(fname))\n"
            } else {
                for line in m.text.replacingOccurrences(of: "\r\n", with: "\n").split(separator: "\n", omittingEmptySubsequences: false) {
                    text += "  \(line)\n"
                }
            }
            text += "\n"
        }
        do {
            try text.write(to: url, atomically: true, encoding: .utf8)
            alertMessage = "내보내기가 완료되었습니다."
        } catch {
            alertMessage = "내보내기 실패: \(error.localizedDescription)"
        }
    }
}
