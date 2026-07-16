const path = require("path")

module.exports = {
  project: {
    android: {
      packageName: "com.company.appwhatever",
    },
  },
  dependencies: {
    '@react-native-community/netinfo': {
      root: path.resolve(__dirname, "../../LibClient/node_modules/@react-native-community/netinfo"),
    },
    'react-native-svg': {
      root: path.resolve(__dirname, "../../LibClient/node_modules/react-native-svg"),
    },
    'react-native-device-info': {
      root: path.resolve(__dirname, "../../ThirdParty/ReactNativeDeviceInfo/node_modules/react-native-device-info"),
    },
    'react-native-code-push': {
      root: path.resolve(__dirname, "../../ThirdParty/ReactNativeCodePush/node_modules/react-native-code-push"),
      platforms: {
        android: {
          sourceDir: path.resolve(__dirname, "../../ThirdParty/ReactNativeCodePush/node_modules/react-native-code-push/android/app"),
        },
      },
    },
  },
};
