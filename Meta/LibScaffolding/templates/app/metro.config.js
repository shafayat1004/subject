const path = require("path")
const {getDefaultConfig, mergeConfig} = require('@react-native/metro-config');

/**
 * Metro configuration for React Native
 * https://github.com/facebook/react-native
 */

let externalLibraries = {
  "react-native":                                path.resolve(__dirname, "./node_modules/react-native"),
  "react":                                       path.resolve(__dirname, "../../LibClient/node_modules/react"),
  "reactxp":                                     path.resolve(__dirname, "../../LibClient/node_modules/reactxp"),
  "react-is":                                    path.resolve(__dirname, "../../LibClient/node_modules/react-is"),
  "@babel/runtime":                              path.resolve(__dirname, "../../LibClient/node_modules/@babel/runtime"),
  "simplerestclients":                           path.resolve(__dirname, "../../LibClient/node_modules/simplerestclients"),
  "reactxp-imagesvg":                            path.resolve(__dirname, "../../LibClient/node_modules/reactxp-imagesvg"),
  "@microsoft/applicationinsights-web":          path.resolve(__dirname, "../../LibClient/node_modules/@microsoft/applicationinsights-web"),
  "@microsoft/applicationinsights-react-native": path.resolve(__dirname, "../../LibClient/node_modules/@microsoft/applicationinsights-react-native"),
  "platform-detect":                             path.resolve(__dirname, "../../LibClient/node_modules/platform-detect"),
  "base-64":                                     path.resolve(__dirname, "../../LibClient/node_modules/base-64"),
  "abortcontroller-polyfill":                    path.resolve(__dirname, "../../LibClient/node_modules/abortcontroller-polyfill"),
  "@chaldal/reactxp-netinfo":                      path.resolve(__dirname, "../../LibClient/node_modules/@chaldal/reactxp-netinfo"),
  "@react-native-community/netinfo":               path.resolve(__dirname, "../../LibClient/node_modules/@react-native-community/netinfo"),
  "react-native-svg":                              path.resolve(__dirname, "../../LibClient/node_modules/react-native-svg"),
  "reactxp-virtuallistview":                     path.resolve(__dirname, "../../LibClient/node_modules/reactxp-virtuallistview"),
  "react-router":                                path.resolve(__dirname, "../../LibRouter/node_modules/react-router"),
  "react-router-native":                         path.resolve(__dirname, "../../LibRouter/node_modules/react-router-native"),
  "react-native-device-info":                    path.resolve(__dirname, "../../ThirdParty/ReactNativeDeviceInfo/node_modules/react-native-device-info"),
  "@microsoft/signalr":                          path.resolve(__dirname, "../../LibUiSubject/node_modules/@microsoft/signalr"),
  "react-native-code-push":                      path.resolve(__dirname, "../../ThirdParty/ReactNativeCodePush/node_modules/react-native-code-push"),
}

const config = {
  transformer: {
    getTransformOptions: async () => ({
      transform: {
        experimentalImportSupport: true,
        inlineRequires: true,
      },
    }),
  },
  resolver: {
    extraNodeModules: externalLibraries,
  },
  watchFolders: [
    path.resolve(__dirname, "../../LibClient/node_modules"),
    path.resolve(__dirname, "../../LibRouter/node_modules"),
    path.resolve(__dirname, "../../LibUiSubject/node_modules"),
    path.resolve(__dirname, "../../Meta/LibRtCompilerFileSystemBindings/node_modules"),
    path.resolve(__dirname, "../../ThirdParty/ReactNativeDeviceInfo/node_modules"),
    path.resolve(__dirname, "../../ThirdParty/ReactNativeCodePush/node_modules")
  ]
}

module.exports = mergeConfig(getDefaultConfig(__dirname), config);
