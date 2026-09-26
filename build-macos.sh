#!/bin/zsh
set -euo pipefail

ROOT_DIR="${0:A:h}"
APP_DIR="$ROOT_DIR/dist/TokenBaby.app"
BIN_DIR="$APP_DIR/Contents/MacOS"
RESOURCE_DIR="$APP_DIR/Contents/Resources"
export CLANG_MODULE_CACHE_PATH="$ROOT_DIR/.build/module-cache"
export SWIFTPM_MODULECACHE_OVERRIDE="$ROOT_DIR/.build/module-cache"
export SWIFTPM_CUSTOM_CACHE_PATH="$ROOT_DIR/.build/swiftpm-cache"

cd "$ROOT_DIR"
mkdir -p "$CLANG_MODULE_CACHE_PATH" "$SWIFTPM_CUSTOM_CACHE_PATH"
SDK_PATH="$(xcrun --sdk macosx --show-sdk-path)"
mkdir -p "$ROOT_DIR/.build/release"
swiftc \
  -O \
  -target "$(uname -m)-apple-macosx13.0" \
  -sdk "$SDK_PATH" \
  -module-cache-path "$CLANG_MODULE_CACHE_PATH" \
  -framework AppKit \
  "$ROOT_DIR/macos/TokenBaby.swift" \
  -o "$ROOT_DIR/.build/release/TokenBaby"
mkdir -p "$BIN_DIR" "$RESOURCE_DIR"
cp .build/release/TokenBaby "$BIN_DIR/TokenBaby"
cp "$ROOT_DIR"/assets/pet-{cry,glance,high,laugh,low,mid}.png "$RESOURCE_DIR/"
cp "$ROOT_DIR/macos/Info.plist" "$APP_DIR/Contents/Info.plist"
codesign --force --sign - "$APP_DIR"
echo "Built $APP_DIR"
