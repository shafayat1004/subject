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
