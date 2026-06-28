/**
 * AppTodo selectors: testId-first with ReactXP web fallbacks.
 */

import { PLATFORM } from './platform.mjs';

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
 * @param {'web' | 'android' | 'ios'} [platform]
 */
export function testIdSelector(testId, platform = PLATFORM.WEB) {
  if (platform === PLATFORM.ANDROID || platform === PLATFORM.IOS) {
    return `~${testId}`;
  }
  return `[data-testid="${escapeAttr(testId)}"]`;
}

/**
 * @param {import('playwright').Page} page
 * @param {string} testId
 */
export function findByTestId(page, testId) {
  return page.locator(testIdSelector(testId));
}

/**
 * @param {import('playwright').Page} page
 * @param {string} testId
 * @param {{ timeout?: number, log?: (msg: string) => void }} [options]
 */
export async function clickByTestId(page, testId, options = {}) {
  const { timeout = 8000, log = () => {} } = options;
  const el = findByTestId(page, testId).first();
  if (!(await el.count())) return false;
  await el.click({ force: true, timeout });
  log(`click testId "${testId}"`);
  return true;
}

/**
 * Fill new-todo input (testId, then first text input fallback).
 * @param {import('playwright').Page} page
 * @param {string} value
 */
export async function fillNewTodoTitle(page, value) {
  const byTestId = findByTestId(page, TEST_IDS.newTitle).locator('input').first();
  if (await byTestId.count()) {
    await byTestId.fill(value);
    return;
  }
  await page.locator('input').first().fill(value);
}

/**
 * Click Add (testId, pseudo-element, role button).
 * @param {import('playwright').Page} page
 */
export async function clickAddTodo(page) {
  if (await clickByTestId(page, TEST_IDS.add)) return;

  const pseudo = page.locator('[data-text-as-pseudo-element="Add"]').first();
  if (await pseudo.count()) {
    await pseudo.click({ force: true });
    return;
  }

  await page.getByRole('button', { name: 'Add' }).click();
}

/**
 * Wait until todo UI is interactive.
 * @param {import('playwright').Page} page
 * @param {{ bootstrapWaitMs?: number }} [options]
 */
export async function waitForTodoReady(page, options = {}) {
  const { bootstrapWaitMs = 8000 } = options;
  await page.waitForTimeout(bootstrapWaitMs);
  const input = findByTestId(page, TEST_IDS.newTitle).locator('input').first();
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
 * Dev hook from LibClient UiActionLog (DEBUG builds).
 * @param {import('playwright').Page} page
 * @param {string} [appName]
 */
export async function readUiSnapshot(page, appName = 'AppTodo') {
  return page.evaluate((name) => {
    const eggshell = /** @type {Record<string, { uiSnapshot?: () => unknown }> | undefined} */ (
      window.__eggshell
    );
    if (!eggshell) return null;
    const app = eggshell[name] ?? Object.values(eggshell)[0];
    return typeof app?.uiSnapshot === 'function' ? app.uiSnapshot() : null;
  }, appName);
}

/**
 * @param {import('playwright').Page} page
 * @param {string} [appName]
 */
export async function readUiLog(page, appName = 'AppTodo') {
  return page.evaluate((name) => {
    const eggshell = /** @type {Record<string, { uiLog?: () => unknown }> | undefined} */ (
      window.__eggshell
    );
    if (!eggshell) return null;
    const app = eggshell[name] ?? Object.values(eggshell)[0];
    return typeof app?.uiLog === 'function' ? app.uiLog() : null;
  }, appName);
}
