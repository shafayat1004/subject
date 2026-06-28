/**
 * Shared render / LogBox / Metro error signals for UI, page source, and device logs.
 */

/** Metro bundle / dev-server failures. */
export const METRO_ERROR_SIGNALS = [
  'Unable to load script',
  'Could not connect to development server',
  'Could not connect to the server',
  'Connect to Metro',
  "Make sure you're running Metro",
  'The development server returned response error code',
  'development server returned response error',
  'Unable to resolve module',
  'Module not found',
  '500 Internal Server Error',
  'Invariant Violation',
  'Exception in HostFunction',
  'TransformError',
  'SyntaxError',
];

/** RN LogBox render / runtime errors (UI + logcat). */
export const RENDER_ERROR_SIGNALS = [
  'Render Error',
  'Uncaught Error',
  "Property 'crypto' doesn't exist",
  'Native module not found',
  'native module not found',
  'ExpoRandom',
  'getRandomBase64',
  'ReferenceError',
  'TypeError',
];

/** LogBox section headers — paired with an error line in page source. */
export const LOGBOX_SECTION_SIGNALS = ['Call Stack', 'Component Stack', 'Source'];

/** RN error screen chrome. */
export const RN_ERROR_SCREEN_SIGNALS = ['DISMISS (ESC)', 'RELOAD (R, R)', 'Dismiss', 'Reload'];

/** Log line prefixes that indicate a JS render failure in logcat / sim logs. */
export const LOG_RENDER_ERROR_PREFIXES = [
  'error:',
  'error ',
  'render error',
  'uncaught error',
  'native module not found',
  "property 'crypto' doesn't exist",
  'invariant violation',
  'typeerror:',
  'referenceerror:',
];

/**
 * @param {string} text
 */
export function normalizeSignalText(text) {
  return String(text).toLowerCase();
}

/**
 * @param {string} haystack
 * @param {string[]} signals
 */
export function findSignalInText(haystack, signals) {
  const lower = normalizeSignalText(haystack);
  for (const signal of signals) {
    if (lower.includes(normalizeSignalText(signal))) return signal;
  }
  return null;
}

/**
 * @param {string} source
 */
export function scanPageSourceForRenderError(source) {
  if (!source) return null;

  const metro = findSignalInText(source, METRO_ERROR_SIGNALS);
  if (metro) {
    return { state: 'metro_redbox', kind: 'page-source-metro', detail: metro };
  }

  const render = findSignalInText(source, RENDER_ERROR_SIGNALS);
  if (render) {
    return { state: 'render_error', kind: 'page-source-render', detail: render };
  }

  const lower = normalizeSignalText(source);
  const hasRenderTitle = lower.includes('render error');
  const hasStack =
    lower.includes('call stack') || lower.includes('component stack') || lower.includes('source\n');
  if (hasRenderTitle && hasStack) {
    return { state: 'render_error', kind: 'page-source-logbox', detail: 'Render Error (LogBox)' };
  }

  const hasLogPager = /\blog\s+\d+\s+of\s+\d+\b/i.test(source);
  if (hasLogPager && (hasRenderTitle || findSignalInText(source, RENDER_ERROR_SIGNALS))) {
    return { state: 'render_error', kind: 'page-source-logbox', detail: 'LogBox error screen' };
  }

  let dismissReload = 0;
  for (const marker of RN_ERROR_SCREEN_SIGNALS) {
    if (lower.includes(normalizeSignalText(marker))) dismissReload += 1;
  }
  if (dismissReload >= 2) {
    return { state: 'metro_redbox', kind: 'page-source-rn-error', detail: 'RN error screen (Dismiss + Reload)' };
  }

  return null;
}

/**
 * @param {string} text
 */
export function matchLogRenderError(text) {
  const t = normalizeSignalText(text);
  if (!t) return null;

  for (const signal of RENDER_ERROR_SIGNALS) {
    if (t.includes(normalizeSignalText(signal))) return signal;
  }

  for (const signal of METRO_ERROR_SIGNALS) {
    if (t.includes(normalizeSignalText(signal))) return signal;
  }

  for (const prefix of LOG_RENDER_ERROR_PREFIXES) {
    if (t.includes(prefix)) {
      const trimmed = text.trim().slice(0, 240);
      return trimmed || prefix;
    }
  }

  if (t.includes('reactnativejs') && (t.includes('error') || t.includes('exception'))) {
    return text.trim().slice(0, 240);
  }

  return null;
}

/**
 * @param {Array<{ text?: string, tag?: string, type?: string }>} entries
 */
export function findRenderErrorInLogEntries(entries) {
  for (const entry of entries) {
    const text = entry.text ?? '';
    const hit = matchLogRenderError(text);
    if (hit) {
      const tag = entry.tag ? `[${entry.tag}] ` : '';
      return { detail: `${tag}${hit}`, raw: text };
    }
    if (entry.type === 'error' && entry.tag === 'AndroidRuntime') {
      return { detail: `[AndroidRuntime] ${text.slice(0, 240)}`, raw: text };
    }
  }
  return null;
}
