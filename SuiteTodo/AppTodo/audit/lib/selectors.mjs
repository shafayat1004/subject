/**
 * AppTodo selectors: testId-first with platform fallbacks.
 */

import { PLATFORM, isNativePlatform } from './platform.mjs';

export const TEST_IDS = {
  page: 'todo-page',
  card: 'todo-card',
  newTitle: 'todo-new-title',
  add: 'todo-add',
  search: 'todo-search',
};

export function escapeAttr(text) {
  return String(text).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
}

/**
 * @param {string} testId
 * @param {'android' | 'ios'} platform
 */
export function editableUnderTestIdSelector(testId, platform) {
  const id = escapeAttr(testId);
  if (platform === PLATFORM.ANDROID) {
    return `android=new UiSelector().resourceId("${id}").childSelector(new UiSelector().className("android.widget.EditText"))`;
  }
  if (platform === PLATFORM.IOS) {
    return `-ios class chain:**[\`${id}\`]/XCUIElementTypeTextField`;
  }
  return testIdSelector(testId, platform);
}

/**
 * @param {string} testId
 * @param {'web' | 'android' | 'ios'} [platform]
 */
export function testIdSelector(testId, platform = PLATFORM.WEB) {
  if (isNativePlatform(platform)) {
    return `~${testId}`;
  }
  return `[data-testid="${escapeAttr(testId)}"]`;
}

/**
 * @param {import('playwright').Page | import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 * @param {string} testId
 * @param {'web' | 'android' | 'ios'} [platform]
 */
export function findByTestId(page, testId, platform = PLATFORM.WEB) {
  return page.locator(testIdSelector(testId, platform));
}

/**
 * @param {import('playwright').Page | import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 * @param {string} testId
 * @param {{ timeout?: number, log?: (msg: string) => void, platform?: 'web' | 'android' | 'ios' }} [options]
 */
export async function clickByTestId(page, testId, options = {}) {
  const { timeout = 8000, log = () => {}, platform = PLATFORM.WEB } = options;
  const el = findByTestId(page, testId, platform).first();
  if (!(await el.count())) return false;
  const clickOpts = isNativePlatform(platform) ? { timeout } : { force: true, timeout };
  await el.click(clickOpts);
  log(`click testId "${testId}"`);
  return true;
}

/**
 * @param {import('playwright').Page | import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 * @param {string} value
 * @param {'web' | 'android' | 'ios'} [platform]
 */
export async function fillNewTodoTitle(page, value, platform = PLATFORM.WEB) {
  if (isNativePlatform(platform)) {
    const input = page.locator(editableUnderTestIdSelector(TEST_IDS.newTitle, platform));
    if (await input.count()) {
      await input.first().fill(value);
      return;
    }
    await page.locator('input').first().fill(value);
    return;
  }

  const byTestId = findByTestId(page, TEST_IDS.newTitle, platform).locator('input').first();
  if (await byTestId.count()) {
    await byTestId.fill(value);
    return;
  }
  await page.locator('input').first().fill(value);
}

/**
 * @param {import('playwright').Page | import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 * @param {'web' | 'android' | 'ios'} [platform]
 */
export async function clickAddTodo(page, platform = PLATFORM.WEB) {
  if (await clickByTestId(page, TEST_IDS.add, { platform })) return;

  if (!isNativePlatform(platform)) {
    const pseudo = page.locator('[data-text-as-pseudo-element="Add"]').first();
    if (await pseudo.count()) {
      await pseudo.click({ force: true });
      return;
    }
  }

  await page.getByRole('button', { name: 'Add' }).click();
}

/**
 * @param {import('playwright').Page | import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 * @param {{ bootstrapWaitMs?: number, platform?: 'web' | 'android' | 'ios' }} [options]
 */
export async function waitForTodoReady(page, options = {}) {
  const { bootstrapWaitMs = 8000, platform = PLATFORM.WEB } = options;
  await page.waitForTimeout(bootstrapWaitMs);

  if (isNativePlatform(platform)) {
    const byTestId = findByTestId(page, TEST_IDS.newTitle, platform);
    if (await byTestId.count()) {
      await byTestId.first().waitFor({ timeout: 30000 });
      return;
    }
    const edit = page.locator('input').first();
    if (await edit.count()) {
      await edit.waitFor({ timeout: 30000 });
      return;
    }
    if (await page.getByText('Todos', { exact: true }).count()) return;
    throw new Error(
      'AppTodo native UI not ready. Run: npm run observe -- doctor --platform ' +
        platform +
        ' — ensure Metro + Appium + app installed.'
    );
  }

  const input = findByTestId(page, TEST_IDS.newTitle, platform).locator('input').first();
  if (await input.count()) {
    await input.waitFor({ timeout: 30000 });
    return;
  }
  const fallback = page.locator('input').first();
  if (await fallback.count()) {
    await fallback.waitFor({ timeout: 30000 });
    return;
  }
  const heading = page.getByText('Todos', { exact: true });
  if (await heading.count()) {
    await heading.waitFor({ timeout: 5000 });
    throw new Error(
      'Todo page heading visible but no inputs found — app may have partially crashed. Check dev-web terminal for Fable/webpack errors.'
    );
  }
  throw new Error(
    'Todo UI not ready (no heading or inputs). Start dev-web: ../../eggshell dev-web from AppTodo, then retry.'
  );
}

/**
 * Read eggshell dev hook (DEBUG builds). Tries window.eggshell then window.__eggshell.
 * @param {import('playwright').Page} page
 * @param {string} appName
 */
function readEggshellHook(page, appName, method) {
  if (typeof page.evaluate !== 'function') return null;
  return page.evaluate(
    ({ name, hook }) => {
      const root =
        /** @type {Record<string, Record<string, unknown>> | undefined} */ (window.eggshell) ??
        /** @type {Record<string, Record<string, unknown>> | undefined} */ (window.__eggshell);
      if (!root) return null;
      const app = root[name] ?? Object.values(root)[0];
      const fn = app?.[hook];
      return typeof fn === 'function' ? fn() : null;
    },
    { name: appName, hook: method }
  );
}

/**
 * Dev hook from LibClient UiActionLog (web DEBUG builds only).
 * @param {import('playwright').Page} page
 * @param {string} [appName]
 */
export async function readUiSnapshot(page, appName = 'AppTodo') {
  return readEggshellHook(page, appName, 'uiSnapshot');
}

/**
 * @param {import('playwright').Page} page
 * @param {string} [appName]
 */
export async function readUiLog(page, appName = 'AppTodo') {
  return readEggshellHook(page, appName, 'uiLog');
}
