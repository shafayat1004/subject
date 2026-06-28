/**
 * Platform routing for AppTodo observability.
 */

export const PLATFORM = {
  WEB: 'web',
  ANDROID: 'android',
  IOS: 'ios',
};

/**
 * @param {string | undefined} raw
 */
export function normalizePlatform(raw) {
  const p = String(raw ?? PLATFORM.WEB).toLowerCase();
  if (p === PLATFORM.WEB || p === PLATFORM.ANDROID || p === PLATFORM.IOS) return p;
  throw new Error(`Unknown platform "${raw}". Use web, android, or ios.`);
}

export function isNativePlatform(platform) {
  return platform === PLATFORM.ANDROID || platform === PLATFORM.IOS;
}
