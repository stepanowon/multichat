import Foundation

/// 강사/수강생 이름 관련 예약어. shared/Protocol.cs의 Names와 대응.
enum Names {
    static let instructor = "강사"
}

enum ChatError: Error, LocalizedError {
    case connectionFailed
    case connectionClosed
    case protocolError(String)

    var errorDescription: String? {
        switch self {
        case .connectionFailed: return "연결에 실패했습니다."
        case .connectionClosed: return "연결이 끊겼습니다."
        case .protocolError(let msg): return msg
        }
    }
}

// C# MsgType/ChatTarget과 동일한 순서로 정의 (System.Text.Json 기본 직렬화는 enum을 int로 씀).
enum MsgType: Int, Codable {
    case join = 0, chat = 1, system = 2, history = 3
}

enum ChatTarget: Int, Codable {
    case all = 0, instructorOnly = 1, specific = 2
}

/// shared/Protocol.cs의 ChatEnvelope와 1:1 대응. PascalCase 키, byte[]<->base64, DateTime<->ISO8601.
struct ChatEnvelope: Codable {
    var type: MsgType
    var from: String = ""
    var target: ChatTarget = .all
    var to: String?
    var text: String = ""
    var time: Date = Date()
    var image: Data?
    var history: [ChatEnvelope]?

    enum CodingKeys: String, CodingKey {
        case type = "Type", from = "From", target = "Target", to = "To"
        case text = "Text", time = "Time", image = "Image", history = "History"
    }

    init(type: MsgType, from: String = "", target: ChatTarget = .all, to: String? = nil,
         text: String = "", time: Date = Date(), image: Data? = nil, history: [ChatEnvelope]? = nil) {
        self.type = type
        self.from = from
        self.target = target
        self.to = to
        self.text = text
        self.time = time
        self.image = image
        self.history = history
    }

    init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        type = try c.decode(MsgType.self, forKey: .type)
        from = try c.decodeIfPresent(String.self, forKey: .from) ?? ""
        target = try c.decodeIfPresent(ChatTarget.self, forKey: .target) ?? .all
        to = try c.decodeIfPresent(String.self, forKey: .to)
        text = try c.decodeIfPresent(String.self, forKey: .text) ?? ""
        let timeStr = try c.decodeIfPresent(String.self, forKey: .time)
        time = timeStr.flatMap(DateCodec.parse) ?? Date()
        image = try c.decodeIfPresent(Data.self, forKey: .image)
        history = try c.decodeIfPresent([ChatEnvelope].self, forKey: .history)
    }

    func encode(to encoder: Encoder) throws {
        var c = encoder.container(keyedBy: CodingKeys.self)
        try c.encode(type, forKey: .type)
        try c.encode(from, forKey: .from)
        try c.encode(target, forKey: .target)
        try c.encodeIfPresent(to, forKey: .to)
        try c.encode(text, forKey: .text)
        try c.encode(DateCodec.format(time), forKey: .time)
        try c.encodeIfPresent(image, forKey: .image)
        try c.encodeIfPresent(history, forKey: .history)
    }
}

/// .NET System.Text.Json 기본 DateTime 포맷(ISO 8601, 초 단위 소수점 유무/오프셋 유무가 다를 수 있음)을
/// 최대한 관용적으로 읽고, 보낼 때는 UTC 'Z' 포맷으로 쓴다.
enum DateCodec {
    private static let withFraction: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        return f
    }()
    private static let noFraction: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime]
        return f
    }()

    static func parse(_ s: String) -> Date? {
        withFraction.date(from: s) ?? noFraction.date(from: s)
    }

    static func format(_ date: Date) -> String {
        withFraction.string(from: date)
    }
}

struct FileEntry {
    let name: String
    let size: Int64
}
