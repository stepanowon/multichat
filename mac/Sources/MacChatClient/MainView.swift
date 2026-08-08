import SwiftUI
import UniformTypeIdentifiers

/// client/MainForm.cs와 대응하는 메인 화면: 왼쪽 채팅, 오른쪽 공유 파일 목록.
struct MainView: View {
    let host: String
    let chatPort: Int
    let name: String

    @StateObject private var vm: ChatViewModel

    init(host: String, chatPort: Int, filePort: Int, name: String) {
        self.host = host
        self.chatPort = chatPort
        self.name = name
        _vm = StateObject(wrappedValue: ChatViewModel(host: host, chatPort: chatPort, filePort: filePort, name: name))
    }

    var body: some View {
        HSplitView {
            chatPane.frame(minWidth: 460)
            filePane.frame(minWidth: 260, idealWidth: 300, maxWidth: 420)
        }
        .frame(minWidth: 940, minHeight: 620)
        .navigationTitle("수강생 채팅 - \(name)")
        .onAppear { vm.start() }
        .alert("알림", isPresented: Binding(
            get: { vm.alertMessage != nil },
            set: { if !$0 { vm.alertMessage = nil } }
        )) {
            Button("확인", role: .cancel) {}
        } message: {
            Text(vm.alertMessage ?? "")
        }
        .sheet(item: $vm.viewingImage) { wrapper in
            ImageViewerSheet(data: wrapper.data) { vm.viewingImage = nil }
        }
    }

    // MARK: - 채팅 영역

    private var chatPane: some View {
        VStack(spacing: 0) {
            header
            Button("대화 내보내기 (Markdown)") { vm.exportMarkdown() }
                .frame(maxWidth: .infinity)
                .padding([.horizontal, .top], 10)
            chatLog
            inputArea
        }
    }

    private var header: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text("수강생 채팅 - \(name)").font(.system(size: 18, weight: .bold))
            Text("서버: \(host):\(chatPort)").font(.system(size: 12)).foregroundColor(.secondary)
            HStack(spacing: 10) {
                Text(vm.encryptionText)
                    .font(.system(size: 12))
                    .foregroundColor(
                        vm.encryptionOK ? Color(red: 0, green: 140 / 255, blue: 60 / 255)
                        : vm.encryptionFailed ? Color(red: 198 / 255, green: 40 / 255, blue: 40 / 255)
                        : .gray
                    )
                if vm.isConnected {
                    Button("연결 종료") { vm.disconnectManually() }
                        .controlSize(.small)
                } else {
                    Button(vm.isConnecting ? "연결 중..." : "재연결") { vm.reconnect() }
                        .controlSize(.small)
                        .disabled(vm.isConnecting)
                }
            }
        }
        .padding(12)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(Color(red: 247 / 255, green: 247 / 255, blue: 249 / 255))
    }

    private var chatLog: some View {
        ScrollViewReader { proxy in
            ScrollView {
                LazyVStack(alignment: .leading, spacing: 6) {
                    ForEach(Array(vm.messages.enumerated()), id: \.offset) { idx, env in
                        MessageBubble(env: env, isMine: env.from == name) { vm.viewingImage = ImageWrapper(data: $0) }
                            .id(idx)
                    }
                }
                .padding(10)
                .frame(maxWidth: .infinity, alignment: .leading)
            }
            .background(Color.white)
            .overlay(RoundedRectangle(cornerRadius: 4).stroke(Color.gray.opacity(0.3)))
            .padding(10)
            .onChange(of: vm.messages.count) { _ in
                if let last = vm.messages.indices.last {
                    withAnimation { proxy.scrollTo(last, anchor: .bottom) }
                }
            }
        }
    }

    private var inputArea: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack {
                Picker("", selection: $vm.target) {
                    Text("전체 수강생").tag(ChatTarget.all)
                    Text("강사에게만").tag(ChatTarget.instructorOnly)
                }
                .pickerStyle(.radioGroup)
                .horizontalRadioGroupLayout()
                .labelsHidden()

                Text("(Cmd+Return로 전송, Return은 줄바꿈, Cmd+V로 이미지 붙여넣기)")
                    .font(.system(size: 11))
                    .foregroundColor(.secondary)
                Spacer()
            }
            HStack(alignment: .bottom, spacing: 8) {
                TextEditor(text: $vm.inputText)
                    .font(.system(size: 13))
                    .frame(height: 70)
                    .overlay(RoundedRectangle(cornerRadius: 4).stroke(Color.gray.opacity(0.3)))
                    .onPasteCommand(of: [.png, .tiff], perform: handlePasteImage)

                Button("전송") { vm.send() }
                    .keyboardShortcut(.return, modifiers: .command)
                    .buttonStyle(.borderedProminent)
                    .frame(width: 90, height: 60)
                    .disabled(!vm.isConnected)
            }
        }
        .padding(10)
    }

    private func handlePasteImage(_ providers: [NSItemProvider]) {
        guard let provider = providers.first else { return }
        let typeId = provider.hasItemConformingToTypeIdentifier(UTType.png.identifier)
            ? UTType.png.identifier : UTType.tiff.identifier
        provider.loadDataRepresentation(forTypeIdentifier: typeId) { data, _ in
            guard let data else { return }
            DispatchQueue.main.async { vm.sendImage(data) }
        }
    }

    // MARK: - 파일 목록 영역

    private var filePane: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("공유 파일 목록 (여러 개 선택 가능)")
                .font(.system(size: 13, weight: .bold))
                .padding(10)
            HStack {
                Spacer()
                Button("다운로드") { vm.downloadSelected() }
                    .buttonStyle(.borderedProminent)
                Button("새로고침") { vm.refreshFiles() }
            }
            .padding(.horizontal, 10)
            .padding(.bottom, 6)

            List(vm.files, id: \.name, selection: $vm.selectedFileNames) { f in
                Text("\(f.name) (\(String(format: "%.1f", Double(f.size) / 1024.0)) KB)")
            }
        }
        .background(Color(red: 247 / 255, green: 247 / 255, blue: 249 / 255))
    }
}
