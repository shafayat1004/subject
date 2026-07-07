const path      = require("path")

module.exports = {
  project: {
    ios: {},
    android: {
      packageName: "com.eggshell.appgallery",
    },
  },
  dependencies: {
    '@react-native-community/netinfo': {
      root: path.resolve(__dirname, "../LibClient/node_modules/@react-native-community/netinfo"),
    },
    // react-native-push-notification and react-native-code-push were dropped for the RN 0.86
    // upgrade: both are dead ends (push-notification unmaintained since 2022; code-push's App
    // Center service retired in 2025 and 9.0.1 breaks on RN 0.86). See the RN 0.86 status doc for
    // replacement options (Notifee for notifications; EAS Update / expo-updates for OTA).
    'react-native-fbsdk-next': {
      root: path.resolve(__dirname, "../ThirdParty/FacebookPixel/node_modules/react-native-fbsdk-next"),
    },
    'react-native-svg': {
      root: path.resolve(__dirname, "../LibClient/node_modules/react-native-svg"),
    },
    'react-native-webview': {
      root: path.resolve(__dirname, "../LibClient/node_modules/react-native-webview"),
    },
    'react-native-image-picker': {
      root: path.resolve(__dirname, "../ThirdParty/ImagePicker/node_modules/react-native-image-picker"),
    },
    'react-native-maps': {
      root: path.resolve(__dirname, "../ThirdParty/Map/node_modules/react-native-maps"),
    },
    'react-native-device-info': {
      root: path.resolve(__dirname, "../ThirdParty/ReactNativeDeviceInfo/node_modules/react-native-device-info"),
    },
    '@react-native-firebase/app': {
      root: path.resolve(__dirname, "../ThirdParty/GoogleAnalytics/node_modules/@react-native-firebase/app"),
    },
    '@react-native-firebase/analytics': {
      root: path.resolve(__dirname, "../ThirdParty/GoogleAnalytics/node_modules/@react-native-firebase/analytics"),
    },
  },
  assets: ['./public-dev/fonts']
};