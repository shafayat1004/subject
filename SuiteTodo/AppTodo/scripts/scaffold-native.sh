#!/usr/bin/env bash
# Scaffold android/ and ios/ for AppTodo from framework templates.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
REPO="$(cd "$ROOT/../.." && pwd)"
PKG="com.eggshell.apptodo"
APP="AppTodo"

echo "Scaffolding native projects for $APP ($PKG)..."

GITIGNORE_TEMPLATE="$REPO/Meta/LibScaffolding/templates/app/.gitignore.template"
if [[ ! -f "$ROOT/.gitignore" && -f "$GITIGNORE_TEMPLATE" ]]; then
  cp "$GITIGNORE_TEMPLATE" "$ROOT/.gitignore"
  echo "  .gitignore installed from template"
fi

# --- Android (LibScaffolding template, no CodePush) ---
if [[ ! -d "$ROOT/android" ]]; then
  rsync -a \
    --exclude='.gradle' \
    "$REPO/Meta/LibScaffolding/templates/app/android/" \
    "$ROOT/android/"
  find "$ROOT/android" -type f \( -name '*.gradle' -o -name '*.xml' -o -name '*.kt' -o -name '*.properties' -o -name '*.bzl' -o -name 'BUCK' \) \
    -exec sed -i '' "s/com.company.appwhatever/$PKG/g" {} +
  sed -i '' "s/rootProject.name = 'EggShellApp'/rootProject.name = '$APP'/" "$ROOT/android/settings.gradle"

  mkdir -p "$ROOT/android/app/src/main/java/com/eggshell/apptodo"
  mv "$ROOT/android/app/src/main/java/com/appname/"*.kt "$ROOT/android/app/src/main/java/com/eggshell/apptodo/"
  rmdir "$ROOT/android/app/src/main/java/com/appname" 2>/dev/null || true
  sed -i '' "s/com.company.appwhatever/$PKG/g" "$ROOT/android/app/src/main/java/com/eggshell/apptodo/"*.kt

  # AppTodo does not use CodePush yet — drop gradle hook that requires ThirdParty/ReactNativeCodePush.
  sed -i '' '/ReactNativeCodePush/d' "$ROOT/android/app/build.gradle"
  sed -i '' '/codepush.gradle/d' "$ROOT/android/app/build.gradle"
  sed -i '' '/nodeModulesPath/d' "$ROOT/android/app/build.gradle"

  cat > "$ROOT/android/app/src/main/res/values/strings.xml" <<EOF
<resources>
    <string name="app_name">Todo</string>
</resources>
EOF

  # Android 12+ requires android:exported on launcher activity.
  sed -i '' 's/android:windowSoftInputMode="adjustResize">/android:exported="true"\n        android:windowSoftInputMode="adjustResize">/' \
    "$ROOT/android/app/src/main/AndroidManifest.xml" 2>/dev/null || true

  chmod +x "$ROOT/android/gradlew"
  "$ROOT/scripts/install-apptodo-branding.sh"
  echo "  android/ created"
else
  echo "  android/ already exists — skipping"
fi

# --- iOS (from gallery reference, renamed) ---
if [[ ! -d "$ROOT/ios" ]]; then
  rsync -a \
    --exclude='Pods' \
    --exclude='build' \
    --exclude='.xcode.env.local' \
    --exclude='Podfile.lock' \
    "$REPO/AppEggShellGallery/ios/" \
    "$ROOT/ios/"

  mv "$ROOT/ios/AppEggshellGallery" "$ROOT/ios/$APP"
  mv "$ROOT/ios/AppEggshellGallery.xcodeproj" "$ROOT/ios/$APP.xcodeproj"
  mv "$ROOT/ios/AppEggshellGallery.xcworkspace" "$ROOT/ios/$APP.xcworkspace"
  mv "$ROOT/ios/AppEggshellGalleryTests" "$ROOT/ios/${APP}Tests"

  find "$ROOT/ios" -type f \
    -exec sed -i '' \
      -e "s/AppEggshellGalleryTests/${APP}Tests/g" \
      -e "s/AppEggshellGallery/$APP/g" \
      -e "s/com.eggshell.appgallery/$PKG/g" \
      -e "s/Egg Shell Gallery/Todo/g" \
      {} +

  if [[ -f "$ROOT/scripts/ios-branding/LaunchScreen.storyboard" ]]; then
    cp "$ROOT/scripts/ios-branding/LaunchScreen.storyboard" "$ROOT/ios/$APP/LaunchScreen.storyboard"
  fi

  echo "  ios/ created (run: cd ios && pod install)"
else
  echo "  ios/ already exists — skipping"
fi

# --- Shared assets (native Metro bundle requires image-not-found.png) ---
IMG_SRC="$REPO/Meta/LibScaffolding/templates/app/images/image-not-found.png"
if [[ -f "$IMG_SRC" ]]; then
  mkdir -p "$ROOT/images" "$ROOT/public-dev/images"
  cp -f "$IMG_SRC" "$ROOT/images/image-not-found.png"
  cp -f "$IMG_SRC" "$ROOT/public-dev/images/image-not-found.png"
  echo "  images/image-not-found.png synced"
fi

# --- iOS Xcode scheme name (gallery template leaves AppEggshellGallery.xcscheme) ---
SCHEME_DIR="$ROOT/ios/AppTodo.xcodeproj/xcshareddata/xcschemes"
if [[ -f "$SCHEME_DIR/AppEggshellGallery.xcscheme" && ! -f "$SCHEME_DIR/AppTodo.xcscheme" ]]; then
  mv "$SCHEME_DIR/AppEggshellGallery.xcscheme" "$SCHEME_DIR/AppTodo.xcscheme"
  echo "  ios scheme renamed → AppTodo"
fi

echo "Done."
