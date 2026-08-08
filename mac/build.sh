#!/bin/bash
# MacChatClient.app을 빌드해 dmg로 패키징하고 저장소 루트 dist/에 저장한다.
# Xcode 프로젝트도, create-dmg 같은 외부 도구도 없이 SwiftPM + hdiutil(macOS 내장)만 사용.
set -euo pipefail
cd "$(dirname "$0")"

CONFIG="release"
APP_NAME="MacChatClient"
VOLUME_NAME="수강생 채팅 (Mac)"
STAGE=".build/dmg-stage"
ROOT_DIST="../dist"
DMG_PATH="$ROOT_DIST/ChatClientMacSetup.dmg"

swift build -c "$CONFIG"

rm -rf "$STAGE"
APP="$STAGE/$APP_NAME.app"
mkdir -p "$APP/Contents/MacOS"
cp ".build/$CONFIG/$APP_NAME" "$APP/Contents/MacOS/$APP_NAME"

cat > "$APP/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>수강생 채팅</string>
    <key>CFBundleDisplayName</key><string>수강생 채팅</string>
    <key>CFBundleIdentifier</key><string>com.multichat.macclient</string>
    <key>CFBundleVersion</key><string>1.0</string>
    <key>CFBundleShortVersionString</key><string>1.0</string>
    <key>CFBundleExecutable</key><string>MacChatClient</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>LSMinimumSystemVersion</key><string>13.0</string>
    <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
PLIST

# SwiftPM이 바이너리에 붙이는 ad-hoc 링커 서명은 번들로 감싸면 깨져 Gatekeeper가
# "손상되었습니다"로 막는다 - 번들 전체를 다시 서명해야 함.
codesign --force --deep --sign - "$APP"

# Finder에서 dmg를 열었을 때 앱을 /Applications로 드래그해 설치하는 관례적 UX
ln -s /Applications "$STAGE/Applications"

mkdir -p "$ROOT_DIST"
rm -f "$DMG_PATH"
hdiutil create -volname "$VOLUME_NAME" -srcfolder "$STAGE" -ov -format UDZO "$DMG_PATH"

echo "완료: $DMG_PATH"
