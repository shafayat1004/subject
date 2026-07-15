#!/bin/zsh
# iOS dev-loop preflight: booted simulator + Metro. See runbooks/ios.md.
set -u
SIM="${1:-iPhone 17 Pro Max}"
if xcrun simctl list devices | grep -q "Booted"; then
  echo "simulator booted:"; xcrun simctl list devices | grep Booted
else
  echo "booting '$SIM'..."
  xcrun simctl boot "$SIM" || { echo "FAIL: cannot boot '$SIM'; available:"; xcrun simctl list devices available | tail -20; exit 0; }
fi
open -a Simulator
if curl -sf --max-time 2 http://localhost:8081/status | grep -q running; then
  echo "Metro OK on :8081"
else
  echo "WARN: Metro not running. From the app dir: npx react-native start --port 8081"
fi
