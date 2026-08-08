// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "MacChatClient",
    platforms: [.macOS(.v13)],
    targets: [
        .executableTarget(name: "MacChatClient", path: "Sources/MacChatClient")
    ]
)
