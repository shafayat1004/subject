import { LogBox } from 'react-native';

// ReactXP + React 18 emit legacy contextTypes warnings on every View/Text/Button
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
  /Require cycle: node_modules\\react-native\\Libraries\\Network\\fetch.js/,
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
]);

// This file is required by the react native metro bundler as
// starting point of the application.
require("./configSourceOverrides.native.js");
require("./.build/native/commonjs/Bootstrap");
