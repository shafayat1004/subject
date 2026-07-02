const path = require('path');
const { getDefaultConfig, mergeConfig } = require('@react-native/metro-config');

const defaultConfig = getDefaultConfig(__dirname);
const libClient = path.resolve(__dirname, '../../LibClient/node_modules');

const externalLibraries = {
  react: path.resolve(libClient, 'react'),
  '@chaldal/reactxp': path.resolve(libClient, '@chaldal/reactxp'),
  'react-is': path.resolve(libClient, 'react-is'),
  'react-native': path.resolve(__dirname, './node_modules/react-native'),
  '@babel/runtime': path.resolve(__dirname, './node_modules/@babel/runtime'),
  simplerestclients: path.resolve(libClient, 'simplerestclients'),
  '@chaldal/reactxp-imagesvg': path.resolve(libClient, '@chaldal/reactxp-imagesvg'),
  '@microsoft/applicationinsights-web': path.resolve(libClient, '@microsoft/applicationinsights-web'),
  '@microsoft/applicationinsights-react-native': path.resolve(
    libClient,
    '@microsoft/applicationinsights-react-native'
  ),
  tslib: path.resolve(libClient, 'tslib'),
  'platform-detect': path.resolve(libClient, 'platform-detect'),
  pako: path.resolve(libClient, 'pako'),
  'base-64': path.resolve(libClient, 'base-64'),
  'abortcontroller-polyfill': path.resolve(libClient, 'abortcontroller-polyfill'),
  '@chaldal/reactxp-netinfo': path.resolve(libClient, '@chaldal/reactxp-netinfo'),
  '@react-native-community/netinfo': path.resolve(libClient, '@react-native-community/netinfo'),
  'react-native-svg': path.resolve(libClient, 'react-native-svg'),
  '@chaldal/reactxp-virtuallistview': path.resolve(libClient, '@chaldal/reactxp-virtuallistview'),
  'react-router': path.resolve(__dirname, '../../LibRouter/node_modules/react-router'),
  'react-router-native': path.resolve(__dirname, '../../LibRouter/node_modules/react-router-native'),
  '@microsoft/signalr': path.resolve(__dirname, '../../LibUiSubject/node_modules/@microsoft/signalr'),
  'fast-memoize': path.resolve(libClient, 'fast-memoize'),
  'react-native-get-random-values': path.resolve(__dirname, './node_modules/react-native-get-random-values'),
  'buffer': path.resolve(__dirname, './node_modules/buffer'),
  '@react-native-picker/picker': path.resolve(__dirname, './node_modules/@react-native-picker/picker'),
};

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
    path.resolve(__dirname, '.build'),
    path.resolve(__dirname, 'public-dev'),
    path.resolve(__dirname, 'images'),
    path.resolve(__dirname, '../../LibClient/src'),
    path.resolve(__dirname, '../../LibClient/node_modules'),
    path.resolve(__dirname, '../../LibRouter/node_modules'),
    path.resolve(__dirname, '../../LibUiSubject/node_modules'),
    path.resolve(__dirname, '../../Meta/LibRtCompilerFileSystemBindings/node_modules'),
    path.resolve(__dirname, '../../LibClient/images'),
  ],
};

module.exports = mergeConfig(defaultConfig, config);
