const path = require('path');

const patchPath = path.resolve(__dirname, '../vendor/reactxp-native-common/TextInput.tsx');

function createReactXpTextInputResolveRequest(defaultResolveRequest) {
  return (context, moduleName, platform) => {
    if (
      moduleName === '../native-common/TextInput' &&
      context.originModulePath.includes(`${path.sep}@chaldal${path.sep}reactxp${path.sep}`)
    ) {
      return { filePath: patchPath, type: 'sourceFile' };
    }

    if (defaultResolveRequest) {
      return defaultResolveRequest(context, moduleName, platform);
    }

    return context.resolveRequest(context, moduleName, platform);
  };
}

module.exports = { createReactXpTextInputResolveRequest, patchPath };
