/**
 * Accessibility panel + gallery a11y assertions (A11yPanel section, page heading, new LC a11y primitives).
 */

import { readFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const ROOT = dirname(fileURLToPath(import.meta.url));
const NAV_PATH = join(ROOT, 'src/Navigation.fs');

/** Gallery routes that use XmlDocs/markdown only — no Ui.A11yPanel. */
export const A11Y_PANEL_EXEMPT = new Set([
  'Index',
  'Layout_Row',
  'Layout_Column',
  'Layout_Sized',
  'Layout_Constrained',
  'InProgress',
  'WithExecutor',
]);

/** @type {Map<string, string> | null} */
let cachedPageTitles = null;

/**
 * Parse ComponentItem.pageTitle from Navigation.fs (source of truth).
 * @param {string} componentName
 */
export function galleryPageTitle(componentName) {
  if (!cachedPageTitles) {
    cachedPageTitles = new Map();
    if (existsSync(NAV_PATH)) {
      const text = readFileSync(NAV_PATH, 'utf8');
      for (const m of text.matchAll(/\|\s+(\w+)\s+->\s+"([^"]+)"/g)) {
        cachedPageTitles.set(m[1], m[2]);
      }
    }
  }
  return cachedPageTitles.get(componentName) ?? componentName.replace(/_/g, ' ');
}

/**
 * @param {import('playwright').Page | import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {'web' | 'android'} [platform]
 */
export async function pageHasA11yPanel(page, platform = 'web') {
  const bodyText = await page.locator('body').innerText().catch(() => '');
  // A11yPanel always renders a "Font scaling" fact row.
  if (bodyText.includes('Font scaling')) return true;
  if (bodyText.includes('Accessibility') && bodyText.includes('Role')) return true;

  if (platform === 'android') return false;

  const pseudoFacts = await page
    .locator(
      '[data-text-as-pseudo-element="Role"], [data-text-as-pseudo-element="Component"], [data-text-as-pseudo-element="Font scaling"]'
    )
    .count()
    .catch(() => 0);
  return pseudoFacts >= 2;
}

/**
 * @param {import('playwright').Page} page
 * @param {string} titleFragment
 */
export async function pageHasDisplayHeading(page, titleFragment) {
  if (!titleFragment) return true;
  const escaped = titleFragment.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const re = new RegExp(escaped, 'i');

  const docTitle = await page.title().catch(() => '');
  if (re.test(docTitle)) return true;

  const byHeading = page.getByRole('heading', { name: re });
  if ((await byHeading.count().catch(() => 0)) > 0) return true;

  const byHeader = page.locator('[role="header"]');
  const n = await byHeader.count().catch(() => 0);
  for (let i = 0; i < n; i++) {
    const text = await byHeader.nth(i).innerText().catch(() => '');
    if (re.test(text)) return true;
  }

  const bodyText = await page.locator('body').innerText().catch(() => '');
  return re.test(bodyText);
}

/**
 * Standard checks for gallery pages updated with Ui.A11yPanel.
 * @param {{
 *   page: import('playwright').Page,
 *   check: Function,
 *   componentName: string,
 *   platform?: 'web' | 'android',
 * }} tools
 */
export async function assertGalleryA11yBasics({ page, check, componentName, platform = 'web' }) {
  const titleHint = galleryPageTitle(componentName);

  if (!A11Y_PANEL_EXEMPT.has(componentName)) {
    await check('Accessibility panel present', async () => {
      const passed = await pageHasA11yPanel(page, platform);
      return {
        passed,
        message: passed
          ? 'Page includes Accessibility section with A11yPanel facts (Role, Component, …)'
          : 'Expected Accessibility section with A11yPanel on this gallery page',
      };
    });
  }

  await check('Page display heading visible', async () => {
    const passed = await pageHasDisplayHeading(page, titleHint);
    return {
      passed,
      message: passed
        ? `Page shows heading or title containing "${titleHint}"`
        : `Expected page heading or document title "${titleHint}"`,
    };
  });
}
