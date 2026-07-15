#!/usr/bin/env bash
# Install AppTodo native launch screen + launcher icons.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BRAND="$ROOT/scripts/android-branding"
RES="$ROOT/android/app/src/main/res"
ICON="$ROOT/images/apptodo-icon.png"

if [[ ! -d "$RES" ]]; then
  echo "install-apptodo-branding: android res missing — run scripts/scaffold-native.sh first" >&2
  exit 1
fi

if [[ ! -f "$ICON" ]]; then
  echo "install-apptodo-branding: missing $ICON" >&2
  exit 1
fi

mkdir -p "$RES/values" "$RES/drawable" "$RES/layout"

cp "$BRAND/colors.xml" "$RES/values/colors.xml"
cp "$BRAND/apptodo_launch_background.xml" "$RES/drawable/apptodo_launch_background.xml"
cp "$BRAND/apptodo_launch_logo.xml" "$RES/drawable/apptodo_launch_logo.xml"
cp "$BRAND/launch_screen.xml" "$RES/layout/launch_screen.xml"

rm -f "$RES/drawable-xhdpi/app_logo.png" "$RES/drawable-xhdpi/splash_screen_background.png"
rmdir "$RES/drawable-xhdpi" 2>/dev/null || true

for spec in "mipmap-mdpi:48" "mipmap-hdpi:72" "mipmap-xhdpi:96" "mipmap-xxhdpi:144" "mipmap-xxxhdpi:192"; do
  dir="${spec%%:*}"
  size="${spec##*:}"
  mkdir -p "$RES/$dir"
  sips -z "$size" "$size" "$ICON" --out "$RES/$dir/ic_launcher.png" >/dev/null
  cp "$RES/$dir/ic_launcher.png" "$RES/$dir/ic_launcher_round.png"
done

echo "AppTodo native branding installed (Android launcher + launch screen)."
