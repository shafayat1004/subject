/**
 * Native gallery navigation via the handheld sidebar drawer (testId-first).
 *
 * Stable testIds (LibClient + AppEggShellGallery):
 * - eggshell-sidebar-menu — open drawer
 * - sidebar-blade-{docs|tools|components|how-to|design} — fixed-top blades
 * - sidebar-component-{ComponentItemCase} — components scroll list
 * - sidebar-scroll-middle — middle ScrollView
 *
 * Drawer retracts after every item tap (LC.Sidebar.WithClose).
 */

import { readFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import {
  ensureGalleryAppForeground,
  waitForGalleryAppReady,
  isGalleryUiVisible,
  clickNativeElement,
  testIdSelector,
} from './audit-gallery-android-driver.mjs';

const ROOT = dirname(fileURLToPath(import.meta.url));
const SIDEBAR_PATH = join(ROOT, 'src/Components/Sidebar/SidebarContent.fs');

const DRAWER_WIDTH_PX = 300;
const DRAWER_OPEN_MAX_X = 400;

/** @type {Map<string, string> | null} */
let labelCache = null;
/** @type {string[] | null} */
let orderCache = null;
let lastDrawerListIndex = 0;

export function componentTestId(componentName) {
  return `sidebar-component-${componentName}`;
}

function escapeUi(text) {
  return String(text).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
}

function uiExactText(text) {
  return `android=new UiSelector().text("${escapeUi(text)}")`;
}

/**
 * @returns {Map<string, string>} ComponentItem case name → sidebar label
 */
export function componentSidebarLabels() {
  if (labelCache) return labelCache;
  if (!existsSync(SIDEBAR_PATH)) {
    throw new Error(`SidebarContent.fs not found: ${SIDEBAR_PATH}`);
  }
  const text = readFileSync(SIDEBAR_PATH, 'utf8');
  const map = new Map();
  for (const m of text.matchAll(/compItem(?:Icon)?\s+"([^"]+)"\s+(?:ComponentItem\.)?(\w+)/g)) {
    map.set(m[2], m[1]);
  }
  map.set('Index', 'Components Introduction');
  labelCache = map;
  return map;
}

/**
 * Ordered ComponentItem case names from componentsItems.
 * @returns {string[]}
 */
export function componentSidebarOrder() {
  if (orderCache) return orderCache;
  if (!existsSync(SIDEBAR_PATH)) return [];
  const text = readFileSync(SIDEBAR_PATH, 'utf8');
  const block = text.match(/let componentsItems[\s\S]*?\|\]/);
  if (!block) return [];
  orderCache = [...block[0].matchAll(/compItem(?:Icon)?\s+"[^"]+"\s+(?:ComponentItem\.)?(\w+)/g)].map(
    (m) => m[1]
  );
  return orderCache;
}

/**
 * @param {string} componentName
 * @returns {string | undefined}
 */
export function sidebarLabelFor(componentName) {
  return componentSidebarLabels().get(componentName);
}

/**
 * @param {import('webdriverio').Element} el
 */
async function elementInDrawer(el) {
  const loc = await el.getLocation();
  return loc.x >= -5 && loc.x < DRAWER_OPEN_MAX_X;
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {string} testId
 */
async function findDrawerTestId(page, testId) {
  const els = await page.driver.$$(testIdSelector(testId));
  for (const el of els) {
    if (await elementInDrawer(el)) return el;
  }
  return null;
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 */
export async function isSidebarDrawerOpen(page) {
  for (const id of ['sidebar-blade-docs', 'sidebar-blade-components', 'sidebar-blade-tools']) {
    if (await findDrawerTestId(page, id)) return true;
  }
  return false;
}

/** @deprecated use isSidebarDrawerOpen */
export async function isSidebarOpen(page) {
  return isSidebarDrawerOpen(page);
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 */
async function getDrawerBounds(page) {
  const { width, height } = await page.getWindowSize();
  for (const id of ['sidebar-blade-components', 'sidebar-blade-docs', 'sidebar-blade-tools']) {
    const el = await findDrawerTestId(page, id);
    if (el) {
      const loc = await el.getLocation();
      return {
        x: loc.x,
        y: 0,
        width: DRAWER_WIDTH_PX,
        height,
        swipeTop: Math.round(height * 0.28),
        swipeBottom: Math.round(height * 0.82),
      };
    }
  }
  return {
    x: 0,
    y: 0,
    width: Math.min(DRAWER_WIDTH_PX, Math.round(width * 0.88)),
    height,
    swipeTop: Math.round(height * 0.28),
    swipeBottom: Math.round(height * 0.82),
  };
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {string} label
 */
async function findDrawerText(page, label) {
  const els = await page.driver.$$(uiExactText(label));
  for (const el of els) {
    if (await elementInDrawer(el)) return el;
  }
  return null;
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {{ x: number, width: number, swipeTop: number, swipeBottom: number }} bounds
 * @param {'up' | 'down'} direction
 */
async function swipeDrawerList(page, bounds, direction) {
  const cx = Math.round(bounds.x + bounds.width * 0.45);
  const yHigh = bounds.swipeTop;
  const yLow = bounds.swipeBottom;
  if (direction === 'down') {
    await page.performSwipe(cx, yLow, cx, yHigh);
  } else {
    await page.performSwipe(cx, yHigh, cx, yLow);
  }
  await page.waitForTimeout(180);
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {(msg: string) => void} [log]
 */
export async function openSidebarDrawer(page, log = () => {}) {
  if (await isSidebarDrawerOpen(page)) {
    log('drawer already open');
    return;
  }

  const menu = page.locator('~eggshell-sidebar-menu');
  if (await menu.count()) {
    await menu.first().click({ timeout: 5000 });
    await page.waitForTimeout(700);
    if (await isSidebarDrawerOpen(page)) {
      log('opened drawer via ~eggshell-sidebar-menu');
      return;
    }
  }

  if (await page.openHandheldSidebarMenu(log)) {
    await page.waitForTimeout(700);
    if (await isSidebarDrawerOpen(page)) return;
  }

  throw new Error('Could not open sidebar drawer via ~eggshell-sidebar-menu');
}

/** @deprecated use openSidebarDrawer */
export async function openSidebarIfNeeded(page, log = () => {}) {
  return openSidebarDrawer(page, log);
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {(msg: string) => void} [log]
 */
export async function ensureComponentsBlade(page, log = () => {}) {
  await openSidebarDrawer(page, log);

  if (await findDrawerTestId(page, componentTestId('Index'))) return;
  if (await findDrawerTestId(page, componentTestId('Layout_Row'))) return;

  const blade = page.locator('~sidebar-blade-components');
  if (await blade.count()) {
    await blade.first().click({ timeout: 5000 });
    await page.waitForTimeout(600);
    log('entered Components blade (~sidebar-blade-components)');
    lastDrawerListIndex = 0;
    return;
  }

  const el = await findDrawerText(page, 'Components');
  if (el) {
    await clickNativeElement(el);
    await page.waitForTimeout(600);
    log('entered Components blade (text fallback)');
    lastDrawerListIndex = 0;
  }
}

/** @deprecated use ensureComponentsBlade */
export async function ensureComponentsSection(page, log = () => {}) {
  return ensureComponentsBlade(page, log);
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {string} componentName
 * @param {(msg: string) => void} [log]
 */
export async function scrollDrawerToComponent(page, componentName, log = () => {}) {
  const testId = componentTestId(componentName);
  if (await findDrawerTestId(page, testId)) return;

  const order = componentSidebarOrder();
  const targetIdx = order.indexOf(componentName);
  if (targetIdx < 0) {
    throw new Error(`Component "${componentName}" not in componentsItems order`);
  }

  const bounds = await getDrawerBounds(page);

  if (targetIdx < lastDrawerListIndex) {
    log(`scroll drawer to top (target ${componentName} is above last position)`);
    for (let i = 0; i < 30; i++) {
      if (await findDrawerTestId(page, testId)) {
        lastDrawerListIndex = targetIdx;
        return;
      }
      if (await findDrawerTestId(page, componentTestId('Index'))) break;
      await swipeDrawerList(page, bounds, 'up');
    }
    lastDrawerListIndex = 0;
  }

  let steps = Math.max(0, targetIdx - lastDrawerListIndex);
  log(`scroll drawer toward ~${testId} (~${steps} steps from index ${lastDrawerListIndex})`);
  for (let i = 0; i < steps + 12; i++) {
    if (await findDrawerTestId(page, testId)) {
      lastDrawerListIndex = targetIdx;
      return;
    }
    await swipeDrawerList(page, bounds, 'down');
  }

  throw new Error(`Could not scroll drawer to ~${testId}`);
}

/** @deprecated use scrollDrawerToComponent */
export async function scrollDrawerToLabel(page, label, log = () => {}) {
  const order = componentSidebarOrder();
  const labels = componentSidebarLabels();
  const componentName = order.find((name) => labels.get(name) === label);
  if (!componentName) throw new Error(`No component for label "${label}"`);
  return scrollDrawerToComponent(page, componentName, log);
}

/** @deprecated */
export async function scrollSidebarToLabel(page, label) {
  return scrollDrawerToLabel(page, label);
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {string} testId
 */
async function clickDrawerTestId(page, testId) {
  const el = await findDrawerTestId(page, testId);
  if (!el) throw new Error(`Drawer item ~${testId} not visible to tap`);
  await clickNativeElement(el);
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {number} [timeoutMs]
 */
async function waitForDrawerClosed(page, timeoutMs = 6000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    if (!(await isSidebarDrawerOpen(page))) return;
    await page.waitForTimeout(100);
  }
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {string} componentName
 * @param {{ pauseMs?: number, readyTimeoutMs?: number, log?: (msg: string) => void }} [options]
 */
export async function navigateToComponent(page, componentName, options = {}) {
  const pauseMs = options.pauseMs ?? 800;
  const log = options.log ?? (() => {});

  await ensureGalleryAppForeground(page, log);
  if (!(await isGalleryUiVisible(page))) {
    await waitForGalleryAppReady(page, { timeoutMs: options.readyTimeoutMs ?? 30_000, log });
  }

  await ensureComponentsBlade(page, log);
  await scrollDrawerToComponent(page, componentName, log);
  await clickDrawerTestId(page, componentTestId(componentName));
  await waitForDrawerClosed(page);
  await page.waitForTimeout(pauseMs);

  if (componentName !== 'Index') {
    const samples = page.locator('~aesg-sample-visuals');
    const deadline = Date.now() + 30_000;
    while (Date.now() < deadline) {
      if ((await samples.count()) > 0) return;
      await page.waitForTimeout(300);
    }
    throw new Error(`Page "${componentName}" loaded without sample visuals (~aesg-sample-visuals)`);
  }
}
