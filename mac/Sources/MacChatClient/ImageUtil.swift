import AppKit

/// shared/ImageUtil.cs와 대응.
enum ImageUtil {
    // ponytail: 전송량/메모리 상한. 강의실 LAN 용도로는 충분, 더 큰 해상도가 필요해지면 값만 올리면 됨.
    static let maxDimension: CGFloat = 1600

    /// 클립보드 등에서 얻은 이미지를 전송 크기 상한에 맞게 축소해 PNG로 인코딩한다.
    static func encodeForTransfer(_ image: NSImage) -> Data? {
        pngData(scaleDown(image, maxWidth: maxDimension, maxHeight: maxDimension))
    }

    static func scaleDown(_ image: NSImage, maxWidth: CGFloat, maxHeight: CGFloat) -> NSImage {
        let size = image.size
        guard size.width > 0, size.height > 0 else { return image }
        let ratio = min(1.0, min(maxWidth / size.width, maxHeight / size.height))
        let newSize = NSSize(width: max(1, size.width * ratio), height: max(1, size.height * ratio))

        let newImage = NSImage(size: newSize)
        newImage.lockFocus()
        NSGraphicsContext.current?.imageInterpolation = .high
        image.draw(in: NSRect(origin: .zero, size: newSize), from: NSRect(origin: .zero, size: size),
                   operation: .copy, fraction: 1.0)
        newImage.unlockFocus()
        return newImage
    }

    static func pngData(_ image: NSImage) -> Data? {
        guard let tiff = image.tiffRepresentation, let rep = NSBitmapImageRep(data: tiff) else { return nil }
        return rep.representation(using: .png, properties: [:])
    }
}
