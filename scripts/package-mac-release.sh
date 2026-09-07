#!/usr/bin/env bash
# Mac版リリース資産のパッケージングスクリプト。
# ビルド済み .app バンドルを受け取り、.DS_Store 等を含めずにzip化する。
# ditto はリソースフォーク・拡張属性を保ったまま安全にzip化できる macOS 標準コマンド。
#
# Usage:
#   scripts/package-mac-release.sh <path/to/YourApp.app> <output.zip>

set -euo pipefail

if [ "$#" -ne 2 ]; then
  echo "Usage: $0 <path/to/YourApp.app> <output.zip>" >&2
  exit 1
fi

APP_PATH="$1"
OUTPUT_ZIP="$2"

if [ ! -d "$APP_PATH" ]; then
  echo "App bundle not found: $APP_PATH" >&2
  exit 1
fi

rm -f "$OUTPUT_ZIP"
ditto -c -k --sequesterRsrc --keepParent "$APP_PATH" "$OUTPUT_ZIP"
echo "Created $OUTPUT_ZIP"
