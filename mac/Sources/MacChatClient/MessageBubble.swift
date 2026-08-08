import SwiftUI
import AppKit

/// shared/ChatLogView.cs의 말풍선 한 개와 대응.
struct MessageBubble: View {
    let env: ChatEnvelope
    let isMine: Bool
    let onViewImage: (Data) -> Void

    private static let timeFmt: DateFormatter = {
        let f = DateFormatter()
        f.dateFormat = "MM/dd HH:mm:ss"
        return f
    }()

    private static let bubbleBg = Color(red: 245 / 255, green: 245 / 255, blue: 247 / 255)
    private static let ownColor = Color(red: 0, green: 99 / 255, blue: 177 / 255)
    private static let instructorColor = Color(red: 198 / 255, green: 40 / 255, blue: 40 / 255)
    private static let defaultColor = Color(red: 33 / 255, green: 33 / 255, blue: 33 / 255)

    private var headerColor: Color {
        if env.type == .system { return .gray }
        if env.from == Names.instructor { return Self.instructorColor }
        if isMine { return Self.ownColor }
        return Self.defaultColor
    }

    private var targetLabel: String {
        switch env.target {
        case .instructorOnly: return " (강사에게만)"
        case .specific: return env.to.map { " (개인 메시지 → \($0))" } ?? " (개인 메시지)"
        case .all: return ""
        }
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text("[\(Self.timeFmt.string(from: env.time))] \(env.from)\(targetLabel)")
                .font(.system(size: 12, weight: .bold))
                .italic(env.type == .system)
                .foregroundColor(headerColor)
            content
        }
        .padding(10)
        .background(Self.bubbleBg)
        .cornerRadius(6)
        .frame(maxWidth: 480, alignment: .leading)
        .contextMenu { contextMenuItems }
    }

    @ViewBuilder private var content: some View {
        if let imgData = env.image, let nsImage = NSImage(data: imgData) {
            Image(nsImage: nsImage)
                .resizable()
                .aspectRatio(contentMode: .fit)
                .frame(width: 180, height: 135)
                .background(Color.white)
                .contentShape(Rectangle())
                .onTapGesture { onViewImage(imgData) }
        } else {
            Text(env.text)
                .font(.system(size: 12.5))
                .foregroundColor(Self.defaultColor)
                .textSelection(.enabled)
                .contentShape(Rectangle())
                .onTapGesture(count: 2, perform: copyText)
        }
    }

    @ViewBuilder private var contextMenuItems: some View {
        if let imgData = env.image {
            Button("다운로드") { downloadImage(imgData) }
            Button("이미지 복사") { copyImage(imgData) }
        } else {
            Button("메시지 복사", action: copyText)
        }
    }

    private func copyText() {
        NSPasteboard.general.clearContents()
        NSPasteboard.general.setString(env.text, forType: .string)
    }

    private func copyImage(_ data: Data) {
        guard let img = NSImage(data: data) else { return }
        NSPasteboard.general.clearContents()
        NSPasteboard.general.writeObjects([img])
    }

    private func downloadImage(_ data: Data) {
        let panel = NSSavePanel()
        panel.allowedContentTypes = [.png]
        let df = DateFormatter(); df.dateFormat = "yyyyMMdd_HHmmss"
        panel.nameFieldStringValue = "image_\(df.string(from: Date())).png"
        guard panel.runModal() == .OK, let url = panel.url else { return }
        try? data.write(to: url)
    }
}

/// shared/ChatLogView.cs의 ViewImage(원본 크기 보기)와 대응.
struct ImageViewerSheet: View {
    let data: Data
    let onClose: () -> Void

    var body: some View {
        VStack(spacing: 0) {
            if let img = NSImage(data: data) {
                Image(nsImage: img)
                    .resizable()
                    .aspectRatio(contentMode: .fit)
            }
            HStack {
                Spacer()
                Button("닫기", action: onClose)
                    .keyboardShortcut(.cancelAction)
            }
            .padding(10)
        }
        .frame(minWidth: 320, idealWidth: 700, minHeight: 240, idealHeight: 520)
    }
}
