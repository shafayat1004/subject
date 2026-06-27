# Getting started with Native app development

## 1. Setting up the dev environment

Choose your host OS and target platform:

### Windows

* [Android on Windows](./setup-dev-env-windows-android.md)

### macOS

* [Android (React Native docs)](https://reactnative.dev/docs/set-up-your-environment?platform=android)
* [iOS (React Native docs)](https://reactnative.dev/docs/set-up-your-environment?platform=ios)

Install Android Studio and/or Xcode as appropriate. For iOS, also install **CocoaPods** (`gem install cocoapods` or via Homebrew).

### Linux

* [Android (React Native docs)](https://reactnative.dev/docs/set-up-your-environment?platform=android)

## 2. EggShell native prerequisites

After platform SDKs are installed:

1. Repo root: `./initialize`
2. App directory: `./initialize` (creates `configSourceOverrides.native.js` from template)
3. iOS only: `cd ios && pod install`

Then follow [Native Development](../basics/native.md) for the three-terminal workflow.

## 3. Further reading

* [Dev experience notes](./dev-experience.md)
* [Releasing native app](./release-app.md)
