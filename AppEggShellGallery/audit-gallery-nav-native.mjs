/**
 * Native gallery navigation: component route id → sidebar label, Appium nav helpers.
 */

import { readFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import { ensureGalleryAppForeground, waitForGalleryAppReady, isGalleryUiVisible } from './audit-gallery-android-driver.mjs';

const ROOT = dirname(fileURLToPath(import.meta.url));
const SIDEBAR_PATH = join(ROOT, 'src/Components/Sidebar/SidebarContent.fs');

/** Sidebar drawer is open when one of these is visible. */
const SIDEBAR_OPEN_HINTS = [
  'Components Introduction',
  'Docs',
  'Tools',
  'Components',
  'How To',
  'Design',
  'Subject',
  'Legacy',
];

/** @type {Map<string, string> | null} */
let labelCache = null;

/**
 * @returns {Map<string, string>}
 */
export function componentSidebarLabels() {
  if (labelCache) return labelCache;
  if (!existsSync(SIDEBAR_PATH)) {
    throw new Error(`SidebarContent.fs not found: ${SIDEBAR_PATH}`);
  }
  const text = readFileSync(SIDEBAR_PATH, 'utf8');
  const map = new Map();
  for (const m of text.matchAll(
    /LC\.Sidebar\.Item\s*\(\s*label\s*=\s*"([^"]+)"[\s\S]*?state\s*=\s*itemState\s+(?:ComponentItem\.)?(\w+)/g
  )) {
    map.set(m[2], m[1]);
  }
  map.set('Index', 'Components Introduction');
  labelCache = map;
  return map;
}

/**
 * @param {string} componentName
 * @returns {string | undefined}
 */
export function sidebarLabelFor(componentName) {
  return componentSidebarLabels().get(componentName);
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 */
export async function isSidebarOpen(page) {
  for (const hint of SIDEBAR_OPEN_HINTS) {
    if (await page.getByText(hint, { exact: true }).count()) return true;
  }
  return false;
}

/**
 * Open the handheld sidebar drawer via the top-nav menu button.
 * Does not use left-edge swipe (conflicts with Android system back gesture).
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {(msg: string) => void} [log]
 */
export async function openSidebarIfNeeded(page, log = () => {}) {
  if (await isSidebarOpen(page)) return;

  const menuSelectors = [
    '~eggshell-sidebar-menu',
    'android=new UiSelector().descriptionContains("menu")',
    'android=new UiSelector().descriptionContains("Menu")',
  ];
  for (const sel of menuSelectors) {
    const el = page.locator(sel).first();
    if (await el.count()) {
      await el.click({ timeout: 5000 }).catch(() => {});
      await page.waitForTimeout(500);
      if (await isSidebarOpen(page)) {
        log(`opened sidebar via ${sel}`);
        return;
      }
    }
  }

  if (await page.openHandheldSidebarMenu(log)) {
    if (await isSidebarOpen(page)) return;
  }

  throw new Error(
    'Could not open sidebar — tap the top-nav menu (☰) manually. ' +
      'Edge swipe is not used because it triggers Android back navigation.'
  );
}

/**
 * Enter Components section so the component list is visible in the sidebar.
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {(msg: string) => void} [log]
 */
export async function ensureComponentsSection(page, log = () => {}) {
  await openSidebarIfNeeded(page, log);

  const onList =
    (await page.getByText('Components Introduction', { exact: true }).count()) > 0 ||
    (await page.getByText('Layout', { exact: true }).count()) > 0;
  if (onList) return;

  const componentsNav = page.getByText('Components', { exact: true });
  if (await componentsNav.count()) {
    await componentsNav.first().click({ timeout: 5000 });
    await page.waitForTimeout(600);
  }
}

/**
 * Scroll sidebar until label is visible, then tap it.
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {string} label
 */
export async function scrollSidebarToLabel(page, label) {
  const escaped = label.replace(/"/g, '\\"');
  const scrollSel = `android=new UiScrollable(new UiSelector().scrollable(true)).scrollIntoView(new UiSelector().text("${escaped}"))`;
  try {
    await page.locator(scrollSel).first().click({ timeout: 8000 }).catch(() => {});
  } catch {
    /* fall through to direct tap */
  }
}

/**
 * Navigate to a gallery component page via the native sidebar.
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {string} componentName
 * @param {{ pauseMs?: number, readyTimeoutMs?: number, log?: (msg: string) => void }} [options]
 */
export async function navigateToComponent(page, componentName, options = {}) {
  const pauseMs = options.pauseMs ?? 800;
  const log = options.log ?? (() => {});
  const label = sidebarLabelFor(componentName);
  if (!label) {
    throw new Error(`No sidebar label mapped for component "${componentName}"`);
  }

  await ensureGalleryAppForeground(page, log);
  if (!(await isGalleryUiVisible(page))) {
    await waitForGalleryAppReady(page, { timeoutMs: options.readyTimeoutMs ?? 30_000, log });
  }
  await ensureComponentsSection(page, log);
  await scrollSidebarToLabel(page, label);

  const item = page.getByText(label, { exact: true });
  if (!(await item.count())) {
    throw new Error(`Sidebar item "${label}" not found for ${componentName}`);
  }
  await item.first().click({ timeout: 10000 });
  await page.waitForTimeout(pauseMs);

  // Wait for sample visuals (Index may have none).
  if (componentName !== 'Index') {
    const samples = page.locator('~aesg-sample-visuals');
    const deadline = Date.now() + 30000;
    while (Date.now() < deadline) {
      if ((await samples.count()) > 0) return;
      await page.waitForTimeout(300);
    }
  }
}
