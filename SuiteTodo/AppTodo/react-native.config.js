const path = require('path');

module.exports = {
  project: {
    android: {
      packageName: 'com.eggshell.apptodo',
    },
  },
  dependencies: {
    '@react-native-community/netinfo': {
      root: path.resolve(__dirname, '../../LibClient/node_modules/@react-native-community/netinfo'),
    },
    'react-native-svg': {
      root: path.resolve(__dirname, '../../LibClient/node_modules/react-native-svg'),
    },
  },
};
