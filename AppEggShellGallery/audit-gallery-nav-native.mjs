/**
 * Native gallery navigation: component route id → sidebar label, Appium nav helpers.
 */

import { readFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import { ensureGalleryAppForeground } from './audit-gallery-android-driver.mjs';

const ROOT = dirname(fileURLToPath(import.meta.url));
const SIDEBAR_PATH = join(ROOT, 'src/Components/Sidebar/SidebarContent.fs');

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
export async function openSidebarIfNeeded(page) {
  const sidebarHints = ['Components Introduction', 'Docs', 'Tools', 'Components', 'How To'];
  for (const hint of sidebarHints) {
    if (await page.getByText(hint, { exact: true }).count()) return;
  }

  const menuSelectors = [
    'android=new UiSelector().descriptionContains("menu")',
    'android=new UiSelector().descriptionContains("Menu")',
    'android=new UiSelector().descriptionContains("navigation")',
    'android=new UiSelector().descriptionContains("drawer")',
  ];
  for (const sel of menuSelectors) {
    const el = page.locator(sel).first();
    if (await el.count()) {
      await el.click({ timeout: 3000 }).catch(() => {});
      await page.waitForTimeout(400);
      return;
    }
  }

  // Swipe from left edge to open drawer (handheld sidebar).
  try {
    const { width, height } = await page.getWindowSize();
    await page.performSwipe(width * 0.02, height * 0.5, width * 0.6, height * 0.5);
    await page.waitForTimeout(400);
  } catch {
    /* best effort */
  }
}

/**
 * Enter Components section so the component list is visible in the sidebar.
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 */
export async function ensureComponentsSection(page) {
  await openSidebarIfNeeded(page);

  const onList =
    (await page.getByText('Components Introduction', { exact: true }).count()) > 0 ||
    (await page.getByText('Layout', { exact: true }).count()) > 0 ||
    (await page.locator('~aesg-sample-visuals').count()) > 0;
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
 * @param {{ pauseMs?: number, log?: (msg: string) => void }} [options]
 */
export async function navigateToComponent(page, componentName, options = {}) {
  const pauseMs = options.pauseMs ?? 800;
  const log = options.log ?? (() => {});
  const label = sidebarLabelFor(componentName);
  if (!label) {
    throw new Error(`No sidebar label mapped for component "${componentName}"`);
  }

  await ensureGalleryAppForeground(page, log);
  await ensureComponentsSection(page);
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
