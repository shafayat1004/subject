/**
 * TestId-first selectors for gallery audits (Playwright web + Appium Android).
 *
 * Web: ReactXP `testId` → `[data-testid]`.
 * Android: `~testId` → UiAutomator resource-id (see audit-gallery-android-driver.mjs).
 */

import { PLATFORM } from './audit-gallery-platform.mjs';

export function escapeUi(text) {
  return String(text).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
}

/** UiAutomator resource-id selector (RN testID on Android). */
export function testIdSelector(testId) {
  return `android=new UiSelector().resourceId("${escapeUi(testId)}")`;
}

/** Clickable child inside a testID wrapper (wrapper is often not clickable). */
export function testIdClickSelector(testId) {
  const id = escapeUi(testId);
  return `android=new UiSelector().resourceId("${id}").childSelector(new UiSelector().clickable(true))`;
}

/**
 * @param {string} testId
 * @param {'web' | 'android'} [platform]
 */
export function testIdLocatorSelector(testId, platform = PLATFORM.WEB) {
  if (platform === PLATFORM.ANDROID) {
    return `~${testId}`;
  }
  return `[data-testid="${escapeUi(testId)}"]`;
}

/**
 * @param {import('playwright').Page | import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {string} testId
 * @param {{ platform?: 'web' | 'android', scope?: import('playwright').Locator | null }} [options]
 */
export function findByTestId(page, testId, options = {}) {
  const { platform = PLATFORM.WEB, scope = null } = options;
  const root = scope ?? page;
  return root.locator(testIdLocatorSelector(testId, platform));
}

/**
 * @param {import('playwright').Page | import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {string} testId
 * @param {{ platform?: 'web' | 'android', scope?: import('playwright').Locator | null, log?: (msg: string) => void, timeout?: number }} [options]
 */
export async function clickByTestId(page, testId, options = {}) {
  const { platform = PLATFORM.WEB, scope = null, log = () => {}, timeout = 5000 } = options;
  const el = findByTestId(page, testId, { platform, scope }).first();
  if (!(await el.count())) return false;
  await el.click({ force: true, timeout });
  log(`click testId "${testId}"`);
  return true;
}

/**
 * Try testId first, then pseudo-element (web) or visible text (Android).
 *
 * @param {import('playwright').Page | import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {{ testId?: string, label: string, platform?: 'web' | 'android', scope?: import('playwright').Locator | null, exact?: boolean, log?: (msg: string) => void, timeout?: number }} options
 */
export async function clickLabelOrTestId(page, { testId, label, platform = PLATFORM.WEB, scope = null, exact = true, log = () => {}, timeout = 5000 }) {
  const root = scope ?? page;

  if (testId && (await clickByTestId(page, testId, { platform, scope: root, log, timeout }))) {
    return true;
  }

  if (platform === PLATFORM.ANDROID) {
    const textEl = root.getByText(label, { exact });
    if (await textEl.count()) {
      await textEl.first().click({ force: true, timeout });
      log(`click text "${label}"`);
      return true;
    }
    return false;
  }

  const escaped = label.replace(/"/g, '\\"');
  const pseudo = root.locator(`[data-text-as-pseudo-element="${escaped}"]`).first();
  if (await pseudo.count()) {
    await pseudo.click({ force: true, timeout });
    log(`click pseudo "${label}"`);
    return true;
  }
  if (!exact) {
    const loose = root.locator(`[data-text-as-pseudo-element="${label}"]`).first();
    if (await loose.count()) {
      await loose.click({ force: true, timeout });
      log(`click pseudo "${label}"`);
      return true;
    }
  }

  const textTarget = root.getByText(label, { exact });
  if (await textTarget.count()) {
    const target = textTarget.first();
    const button = target
      .locator('xpath=ancestor::*[.//button[@role="button"]][1]//button[@role="button"]')
      .first();
    if (await button.count()) {
      await button.click({ force: true, timeout });
      log(`click pressable "${label}"`);
      return true;
    }
    await target.click({ force: true, timeout });
    log(`click text "${label}"`);
    return true;
  }

  const roleBtn = root.getByRole('button', { name: label, exact });
  if (await roleBtn.count()) {
    await roleBtn.first().click({ force: true, timeout });
    log(`click button "${label}"`);
    return true;
  }

  return false;
}

/**
 * Dev hook: visible interactives from UiActionLog (DEBUG builds only).
 * @param {import('playwright').Page} page
 */
export async function readUiSnapshot(page) {
  return page.evaluate(() => {
    const eggshell = /** @type {Record<string, { uiSnapshot?: () => unknown }> | undefined} */ (
      window.__eggshell
    );
    if (!eggshell) return null;
    const app = eggshell.AppEggShellGallery ?? Object.values(eggshell)[0];
    return typeof app?.uiSnapshot === 'function' ? app.uiSnapshot() : null;
  });
}
