/**
 * Platform routing for AppTodo observability (web first; android/ios stubs).
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

/**
 * @param {'web' | 'android' | 'ios'} platform
 */
export function platformNotReadyMessage(platform) {
  if (platform === PLATFORM.WEB) return null;
  const name = platform === PLATFORM.ANDROID ? 'Android (Appium)' : 'iOS (Appium / XCUITest)';
  return [
    `${name} observability is not wired for AppTodo yet.`,
    'Use --platform web for now (Playwright, headed by default).',
    'Gallery reference: AppEggShellGallery/audit-gallery-android-driver.mjs',
    'When templating, copy that driver and set AppTodo package/activity.',
  ].join('\n');
}
