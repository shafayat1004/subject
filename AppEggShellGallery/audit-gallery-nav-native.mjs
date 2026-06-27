/**
 * Native gallery navigation via the handheld sidebar drawer.
 *
 * Source-of-truth behavior (LibClient + AppEggShellGallery Sidebar.render):
 * - Handheld sidebar is an off-screen Draggable drawer (AppShell.Content + LC.Draggable).
 * - LC.Sidebar.WithClose wraps nav: every item press runs `nav.Go(...); close e` where
 *   close = setSidebarVisibility false — the drawer retracts after each blade tap.
 * - Fixed top (handheld): Docs | Tools | Components | How To | Design — route blades.
 * - ScrollableMiddle: long item list for the active blade (componentsItems when on Components).
 * - Sidebar.Base is 300px wide (VerticallyScrollable: fixed top + ScrollView middle + fixed bottom).
 *
 * Audit nav must: open drawer → enter Components blade → scroll middle list → tap item →
 * wait for drawer close + content load. Never assume the drawer stays open between pages.
 */

import { readFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import {
  ensureGalleryAppForeground,
  waitForGalleryAppReady,
  isGalleryUiVisible,
  clickNativeElement,
} from './audit-gallery-android-driver.mjs';

const ROOT = dirname(fileURLToPath(import.meta.url));
const SIDEBAR_PATH = join(ROOT, 'src/Components/Sidebar/SidebarContent.fs');

/** LC.Sidebar.Base width on native (LibClient Sidebar/Base.fs). */
const DRAWER_WIDTH_PX = 300;
const DRAWER_OPEN_MAX_X = 400;

/** @type {Map<string, string> | null} */
let labelCache = null;
/** @type {string[] | null} */
let orderCache = null;
/** Last componentsItems index reached (for directional scroll). */
let lastDrawerListIndex = 0;

function escapeUi(text) {
  return String(text).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
}

function uiExactText(text) {
  return `android=new UiSelector().text("${escapeUi(text)}")`;
}

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
 * Ordered sidebar labels from `componentsItems` in SidebarContent.fs.
 * @returns {string[]}
 */
export function componentSidebarOrder() {
  if (orderCache) return orderCache;
  if (!existsSync(SIDEBAR_PATH)) return [];
  const text = readFileSync(SIDEBAR_PATH, 'utf8');
  const block = text.match(/let componentsItems[\s\S]*?\|\]/);
  if (!block) return [];
  orderCache = [...block[0].matchAll(/label\s*=\s*"([^"]+)"/g)].map((m) => m[1]);
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
 * Handheld drawer is open when fixed-top blade labels sit on-screen (x in [0, 400)).
 * Off-screen drawer nodes may still exist in the a11y tree with negative x — ignore those.
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 */
export async function isSidebarDrawerOpen(page) {
  for (const anchor of ['Docs', 'Tools', 'How To', 'Design']) {
    const els = await page.driver.$$(uiExactText(anchor));
    for (const el of els) {
      if (await elementInDrawer(el)) return true;
    }
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
  for (const anchor of ['Docs', 'Tools', 'Components', 'How To']) {
    const els = await page.driver.$$(uiExactText(anchor));
    for (const el of els) {
      if (await elementInDrawer(el)) {
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
 * Find a text node inside the open drawer (ignores duplicate labels in main content).
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
 * Open the handheld drawer via the top-nav menu (never edge-swipe — OS back gesture).
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

  throw new Error(
    'Could not open sidebar drawer — tap the top-nav menu (☰). ' +
      'Edge swipe is not used (Android system back).'
  );
}

/** @deprecated use openSidebarDrawer */
export async function openSidebarIfNeeded(page, log = () => {}) {
  return openSidebarDrawer(page, log);
}

/**
 * Tap the fixed-top "Components" blade so componentsItems scroll list is shown.
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {(msg: string) => void} [log]
 */
export async function ensureComponentsBlade(page, log = () => {}) {
  await openSidebarDrawer(page, log);

  if (await findDrawerText(page, 'Components Introduction')) return;
  if (await findDrawerText(page, 'Layout')) return;

  const els = await page.driver.$$(uiExactText('Components'));
  for (const el of els) {
    if (await elementInDrawer(el)) {
      await clickNativeElement(el);
      await page.waitForTimeout(600);
      log('entered Components blade (fixed top)');
      lastDrawerListIndex = 0;
      return;
    }
  }
}

/** @deprecated use ensureComponentsBlade */
export async function ensureComponentsSection(page, log = () => {}) {
  return ensureComponentsBlade(page, log);
}

/**
 * Scroll the Components middle list until `label` is visible in the drawer.
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {string} label
 * @param {(msg: string) => void} [log]
 */
export async function scrollDrawerToLabel(page, label, log = () => {}) {
  if (await findDrawerText(page, label)) return;

  const order = componentSidebarOrder();
  const targetIdx = order.indexOf(label);
  if (targetIdx < 0) {
    throw new Error(`Sidebar label "${label}" not in componentsItems order`);
  }

  const bounds = await getDrawerBounds(page);

  if (targetIdx < lastDrawerListIndex) {
    log(`scroll drawer to top (target ${label} is above last position)`);
    for (let i = 0; i < 30; i++) {
      if (await findDrawerText(page, label)) {
        lastDrawerListIndex = targetIdx;
        return;
      }
      if (await findDrawerText(page, 'Components Introduction')) break;
      await swipeDrawerList(page, bounds, 'up');
    }
    lastDrawerListIndex = 0;
  }

  let steps = Math.max(0, targetIdx - lastDrawerListIndex);
  log(`scroll drawer toward "${label}" (~${steps} steps from index ${lastDrawerListIndex})`);
  for (let i = 0; i < steps + 12; i++) {
    if (await findDrawerText(page, label)) {
      lastDrawerListIndex = targetIdx;
      return;
    }
    await swipeDrawerList(page, bounds, 'down');
  }

  throw new Error(`Could not scroll drawer to "${label}"`);
}

/** @deprecated use scrollDrawerToLabel */
export async function scrollSidebarToLabel(page, label) {
  return scrollDrawerToLabel(page, label);
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {string} label
 */
async function clickDrawerLabel(page, label) {
  const el = await findDrawerText(page, label);
  if (!el) throw new Error(`Drawer item "${label}" not visible to tap`);
  await clickNativeElement(el);
}

/**
 * Wait for drawer retract after item tap (WithClose always runs close e).
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
 * Navigate to a gallery component page via the native sidebar drawer.
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

  await ensureComponentsBlade(page, log);
  await scrollDrawerToLabel(page, label, log);
  await clickDrawerLabel(page, label);
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
