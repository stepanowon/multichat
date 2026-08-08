import SwiftUI

/// client/ConnectForm.cs와 대응하는 접속 다이얼로그.
struct ConnectView: View {
    @State private var host = "127.0.0.1"
    @State private var chatPort = "9000"
    @State private var filePort = "9001"
    @State private var name = ""
    @State private var errorMessage: String?

    let onConnect: (String, Int, Int, String) -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            Text("서버 접속").font(.title2).bold()

            Form {
                TextField("서버 주소:", text: $host)
                TextField("채팅 포트:", text: $chatPort)
                TextField("파일 포트:", text: $filePort)
                TextField("이름:", text: $name)
                    .onSubmit(attemptConnect)
            }

            if let errorMessage {
                Text(errorMessage).foregroundColor(.red).font(.caption)
            }

            HStack {
                Spacer()
                Button("접속", action: attemptConnect)
                    .keyboardShortcut(.defaultAction)
            }
        }
        .padding(20)
        .frame(width: 360)
        .navigationTitle("서버 접속")
    }

    private func attemptConnect() {
        let trimmedHost = host.trimmingCharacters(in: .whitespaces)
        let trimmedName = name.trimmingCharacters(in: .whitespaces)
        guard !trimmedHost.isEmpty, !trimmedName.isEmpty,
              let cp = Int(chatPort.trimmingCharacters(in: .whitespaces)),
              let fp = Int(filePort.trimmingCharacters(in: .whitespaces)) else {
            errorMessage = "모든 항목을 올바르게 입력하세요."
            return
        }
        if trimmedName == Names.instructor {
            errorMessage = "사용할 수 없는 이름입니다."
            return
        }
        errorMessage = nil
        onConnect(trimmedHost, cp, fp, trimmedName)
    }
}
