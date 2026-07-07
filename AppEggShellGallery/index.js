import { LogBox } from 'react-native';

// react-native-web app entry (Fable output bootstraps below).
// mount, each with a deep component stack (~15 logcat lines per warning).
const ignoredNativeWarningPatterns = [
  /legacy childContextTypes API/,
  /legacy contextTypes API/,
  /LC\.Icon is being used with legacy styles/,
  /React Router Future Flag Warning/,
  /componentWillUpdate has been renamed/,
  /componentWillReceiveProps has been renamed/,
  /AsyncStorage has been extracted from react-native/,
  /`new NativeEventEmitter\(\)` was called with a non-null argument/,
  /Support for defaultProps will be removed/,
  /TRenderEngineProvider/,
  /MemoizedTNodeRenderer/,
  /TNodeChildrenRenderer/,
  /^\s+in /,
];

const originalConsoleWarn = console.warn;
console.warn = (...args) => {
  const message = typeof args[0] === 'string' ? args[0] : String(args[0]);
  if (ignoredNativeWarningPatterns.some(pattern => pattern.test(message))) {
    return;
  }
  originalConsoleWarn(...args);
};

// React Native's renderer logs legacy contextTypes via console.error in __DEV__.
const originalConsoleError = console.error;
console.error = (...args) => {
  const message = typeof args[0] === 'string' ? args[0] : String(args[0]);
  if (ignoredNativeWarningPatterns.some(pattern => pattern.test(message))) {
    return;
  }
  originalConsoleError(...args);
};

LogBox.ignoreLogs([
  'legacy childContextTypes API',
  'legacy contextTypes API',
  'LC.Icon is being used with legacy styles',
  'React Router Future Flag Warning',
  'componentWillUpdate has been renamed',
  'componentWillReceiveProps has been renamed',
  'AsyncStorage has been extracted from react-native',
  '`new NativeEventEmitter()` was called with a non-null argument',
  'Require cycle: node_modules\\react-native\\Libraries\\Network\\fetch.js',
  'Support for defaultProps will be removed',
  'TRenderEngineProvider',
  'MemoizedTNodeRenderer',
  'TNodeChildrenRenderer',
]);

if (__DEV__) {
  const errorUtils = global.ErrorUtils;
  if (errorUtils && typeof errorUtils.getGlobalHandler === 'function') {
    const defaultGlobalHandler = errorUtils.getGlobalHandler();
    errorUtils.setGlobalHandler((error, isFatal) => {
      if (error && error.stack) {
        originalConsoleError('[EggShell uncaught]', error.message, '\n', error.stack);
      }
      defaultGlobalHandler(error, isFatal);
    });
  }
}

// This file is required by the react native metro bundler as
// starting point of the application.
if (__DEV__) {
  require("./configSourceOverrides.native.js");
} else {
  require("./configSourceOverrides.native.prod.js");
}
require("./.build/native/commonjs/Bootstrap");
