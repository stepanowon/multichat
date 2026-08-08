import SwiftUI

private struct ConnectionParams: Identifiable {
    let id = UUID()
    let host: String
    let chatPort: Int
    let filePort: Int
    let name: String
}

@main
struct MacChatClientApp: App {
    @State private var params: ConnectionParams?

    var body: some Scene {
        WindowGroup {
            if let params {
                MainView(host: params.host, chatPort: params.chatPort, filePort: params.filePort, name: params.name)
                    .id(params.id)
            } else {
                ConnectView { host, chatPort, filePort, name in
                    params = ConnectionParams(host: host, chatPort: chatPort, filePort: filePort, name: name)
                }
            }
        }
        .defaultSize(width: 1050, height: 700)
        .windowResizability(.contentSize)
    }
}
