#!/bin/zsh
# Android dev-loop preflight: device, adb reverse, Metro. See runbooks/android.md.
set -u
DEV=$(adb devices | tail -n +2 | awk '$2=="device"{print $1}' | head -1)
if [[ -z "$DEV" ]]; then
  echo "FAIL: no device/emulator attached."
  echo "  emulator -avd Medium_Phone_API_35 -no-snapshot-load &  && adb wait-for-device"
  exit 0
fi
echo "device: $DEV"
adb -s "$DEV" reverse tcp:8081 tcp:8081 && echo "adb reverse tcp:8081 OK"
if curl -sf --max-time 2 http://localhost:8081/status | grep -q running; then
  echo "Metro OK on :8081"
else
  echo "WARN: Metro not running. From the app dir: npx react-native start --port 8081"
fi
