/**
 * Defaults for AppTodo dev observability (headed by default for human + AI collaboration).
 */

export const APP_NAME = 'AppTodo';

export const DEFAULTS = {
  baseUrl: 'http://127.0.0.1:9080',
  /** Non-headless so developers and agents see the same browser. */
  headless: false,
  slowMo: 80,
  bootstrapWaitMs: 8000,
  viewport: { width: 1280, height: 900 },
  platform: 'web',
  /** Native phone tests default to portrait; override via --orientation or native.local.json */
  deviceOrientation: 'portrait',
};

/** Timeouts for observe workflows — override via CLI `--timeout-ms`. */
export const TIMEOUTS = {
  /** Wait for Todo UI / healthy app state. */
  appReadyMs: 120_000,
  /** Poll interval while waiting for end states. */
  pollMs: 750,
  /** Settle after app becomes healthy before capture. */
  settleMs: 800,
  /** Single user action (click, fill, waitFor text). */
  actionMs: 30_000,
  /** Appium session connect + launch. */
  sessionConnectMs: 120_000,
};

/** Layout diff threshold in px before flagging a regression. */
export const LAYOUT_DIFF_THRESHOLD_PX = 2;

export function parseBool(value, defaultValue) {
  if (value === undefined || value === null) return defaultValue;
  if (typeof value === 'boolean') return value;
  const s = String(value).toLowerCase();
  if (s === 'true' || s === '1' || s === 'yes') return true;
  if (s === 'false' || s === '0' || s === 'no') return false;
  return defaultValue;
}
