import Foundation

/// shared/ChatHistoryStore.cs와 대응. ~/Library/Application Support/MultiChat/에 한 줄당 JSON 하나씩 저장.
final class ChatHistoryStore {
    private let url: URL

    init(fileName: String) {
        let dir = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("MultiChat")
        try? FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        url = dir.appendingPathComponent(fileName)
    }

    func load() -> [ChatEnvelope] {
        guard let text = try? String(contentsOf: url, encoding: .utf8) else { return [] }
        var result: [ChatEnvelope] = []
        for line in text.split(separator: "\n") {
            if line.trimmingCharacters(in: .whitespaces).isEmpty { continue }
            if let data = line.data(using: .utf8), let env = try? JSONDecoder().decode(ChatEnvelope.self, from: data) {
                result.append(env)
            }
        }
        return result
    }

    // ponytail: 파일이 무한정 커질 수 있음 - 강의실 단기 사용 기준으로는 문제 없음. 필요해지면 날짜별 로테이션 추가.
    func append(_ env: ChatEnvelope) {
        guard let data = try? JSONEncoder().encode(env), let json = String(data: data, encoding: .utf8) else { return }
        let line = Data((json + "\n").utf8)
        if let handle = try? FileHandle(forWritingTo: url) {
            defer { try? handle.close() }
            handle.seekToEndOfFile()
            handle.write(line)
        } else {
            try? line.write(to: url)
        }
    }
}
