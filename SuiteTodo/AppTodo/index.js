// Must be the very first import: sets up the gesture-handler native module before
// anything renders (required on Android, no-op on web).
import 'react-native-gesture-handler';
import 'react-native-get-random-values';
import { LogBox } from 'react-native';

const ignoredNativeWarningPatterns = [
  /legacy childContextTypes API/,
  /legacy contextTypes API/,
  /React Router Future Flag Warning/,
];

const originalConsoleWarn = console.warn;
console.warn = (...args) => {
  const message = typeof args[0] === 'string' ? args[0] : String(args[0]);
  if (ignoredNativeWarningPatterns.some(pattern => pattern.test(message))) {
    return;
  }
  originalConsoleWarn(...args);
};

LogBox.ignoreLogs([
  'legacy childContextTypes API',
  'legacy contextTypes API',
  'React Router Future Flag Warning',
]);

require("./configSourceOverrides.native.js");
require("./.build/native/commonjs/Bootstrap");
