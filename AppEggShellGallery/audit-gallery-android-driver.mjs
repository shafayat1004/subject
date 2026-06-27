/**
 * Playwright-like facade over WebdriverIO/Appium for Android gallery audits.
 * Lets audit-gallery-interactions.mjs and audit-gallery-assertions.mjs share recipes.
 */

import { remote } from 'webdriverio';
import { spawn } from 'child_process';
import { ANDROID_APP } from './audit-gallery-platform.mjs';

function escapeUi(text) {
  return String(text).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
}

/** RN testID → Android resource-id (not accessibility id). WebdriverIO `~` uses the wrong strategy. */
export function testIdSelector(testId) {
  return `android=new UiSelector().resourceId("${escapeUi(testId)}")`;
}

/** Clickable target inside a testID wrapper (wrapper is often not clickable). */
function testIdClickSelector(testId) {
  const id = escapeUi(testId);
  return `android=new UiSelector().resourceId("${id}").childSelector(new UiSelector().clickable(true))`;
}

/**
 * @param {import('webdriverio').Element} el
 */
async function elementRect(el) {
  const [loc, size] = await Promise.all([el.getLocation(), el.getSize()]);
  return { x: loc.x, y: loc.y, width: size.width, height: size.height };
}

/**
 * @param {import('webdriverio').Element} el
 */
async function clickElement(el) {
  try {
    const clickable = await el.$$('android=new UiSelector().clickable(true)');
    if (clickable.length) {
      await clickable[0].click();
      return;
    }
  } catch {
    /* fall through */
  }
  await el.click();
}

function uiText(text, exact = false) {
  const e = escapeUi(text);
  return exact
    ? `android=new UiSelector().text("${e}")`
    : `android=new UiSelector().textContains("${e}")`;
}

function uiClass(name) {
  return `android=new UiSelector().className("${name}")`;
}

/**
 * Map web/CSS selectors used in audit recipes to UiAutomator selectors on Android.
 * @param {string} selector
 */
function translateSelector(selector) {
  if (!selector) return selector;
  if (selector.startsWith('~')) {
    return testIdSelector(selector.slice(1));
  }
  if (selector.startsWith('android=')) return selector;

  const pseudoExact = selector.match(/\[data-text-as-pseudo-element="([^"]+)"\]/);
  if (pseudoExact) return uiText(pseudoExact[1], true);

  if (selector.includes('[data-text-as-pseudo-element]')) {
    return uiClass('android.widget.TextView');
  }

  if (selector.includes('input[type="file"]')) {
    return 'android=new UiSelector().resourceId("aesg-skip-file-input")';
  }
  if (selector.includes('input:not') || selector === 'textarea') {
    return uiClass('android.widget.EditText');
  }
  if (selector.includes('input')) return uiClass('android.widget.EditText');
  if (selector.includes('button:visible') || selector.includes('button:not')) {
    return 'android=new UiSelector().clickable(true)';
  }
  if (selector.includes('img')) return uiClass('android.widget.ImageView');
  if (selector.includes('svg') || selector.includes('recharts')) {
    return uiClass('android.view.View');
  }
  if (selector.includes('.map') || selector.includes('leaflet') || selector.includes('canvas')) {
    return uiClass('android.view.View');
  }
  if (selector.includes('[class*="dot"]') || selector.includes('[class*="Dot"]')) {
    return uiClass('android.view.View');
  }
  if (selector.includes('[class*="ActivityIndicator"]') || selector.includes('[class*="spinner"]')) {
    return uiClass('android.widget.ProgressBar');
  }
  if (selector.includes('[class*="dialog"]') || selector.includes('[class*="Dialog"]')) {
    return uiClass('android.view.View');
  }
  if (selector.includes('[class*="modal"]') || selector.includes('[class*="scrim"]')) {
    return uiClass('android.view.View');
  }
  if (selector === 'body') {
    return `android=new UiSelector().packageName("${ANDROID_APP.package}")`;
  }
  if (selector.startsWith('xpath=')) {
    return selector.replace(/^xpath=/, '');
  }
  return uiClass('android.view.View');
}

class AndroidLocator {
  /**
   * @param {import('webdriverio').Browser} driver
   * @param {string} selector
   * @param {AndroidLocator | null} parentLocator
   * @param {{ index?: number | 'last', filterHas?: AndroidLocator }} [meta]
   */
  constructor(driver, selector, parentLocator = null, meta = {}) {
    this.driver = driver;
    this.selector = selector;
    this.parentLocator = parentLocator;
    this._index = meta.index ?? null;
    this._filterHas = meta.filterHas ?? null;
  }

  _clone(extra = {}) {
    return new AndroidLocator(this.driver, this.selector, this.parentLocator, {
      index: this._index,
      filterHas: this._filterHas,
      ...extra,
    });
  }

  _wdSelector() {
    return translateSelector(this.selector);
  }

  async _resolveRaw() {
    let els;
    const sel = this._wdSelector();
    if (this.parentLocator) {
      const parents = await this.parentLocator._resolveRaw();
      els = [];
      for (const p of parents) {
        try {
          const kids = await p.$$(sel);
          els.push(...kids);
        } catch {
          /* skip */
        }
      }
    } else {
      try {
        els = await this.driver.$$(sel);
      } catch {
        els = [];
      }
    }

    if (this._filterHas) {
      const filtered = [];
      for (const el of els) {
        try {
          const childSel = this._filterHas._wdSelector();
          const kids = await el.$$(childSel);
          if (kids.length) filtered.push(el);
        } catch {
          /* skip */
        }
      }
      els = filtered;
    }

    if (this._index === 'last') {
      return els.length ? [els[els.length - 1]] : [];
    }
    if (typeof this._index === 'number') {
      return this._index < els.length ? [els[this._index]] : [];
    }
    return els;
  }

  async count() {
    const els = await this._resolveRaw();
    return els.length;
  }

  first() {
    return this._clone({ index: 0 });
  }

  /** @param {number} i */
  nth(i) {
    return this._clone({ index: i });
  }

  last() {
    return this._clone({ index: 'last' });
  }

  /** @param {{ has: AndroidLocator }} opts */
  filter(opts) {
    return this._clone({ filterHas: opts.has });
  }

  /** @param {string} sub */
  locator(sub) {
    return new AndroidLocator(this.driver, sub, this);
  }

  /** @param {string} sub */
  getByLabel(sub) {
    return new AndroidLocator(this.driver, uiText(sub, true), this);
  }

  async _firstEl() {
    const els = await this._resolveRaw();
    return els[0] ?? null;
  }

  /** @param {{ force?: boolean, timeout?: number }} [opts] */
  async click(opts = {}) {
    const sel = this._wdSelector();
    const testIdMatch = sel.match(/resourceId\("([^"\\]+(?:\\.[^"\\]*)*)"\)/);
    if (testIdMatch && !this.parentLocator) {
      const rawId = testIdMatch[1].replace(/\\"/g, '"').replace(/\\\\/g, '\\');
      const clickSel = testIdClickSelector(rawId);
      const targets = await this.driver.$$(clickSel).catch(() => []);
      if (targets.length) {
        await targets[0].waitForDisplayed({ timeout: opts.timeout ?? 5000 }).catch(() => {});
        await targets[0].click();
        return;
      }
    }

    const el = await this._firstEl();
    if (!el) throw new Error(`No element to click: ${this.selector}`);
    await el.waitForDisplayed({ timeout: opts.timeout ?? 5000 }).catch(() => {});
    await clickElement(el);
  }

  /** @param {string} value @param {{ timeout?: number }} [opts] */
  async fill(value, opts = {}) {
    const el = await this._firstEl();
    if (!el) throw new Error(`No element to fill: ${this.selector}`);
    await el.waitForDisplayed({ timeout: opts.timeout ?? 5000 }).catch(() => {});
    await el.clearValue().catch(() => {});
    await el.setValue(value);
  }

  async scrollIntoViewIfNeeded() {
    const el = await this._firstEl();
    if (!el) return;
    try {
      await el.scrollIntoView();
    } catch {
      /* best effort */
    }
  }

  /** @param {{ path: string, timeout?: number }} opts */
  async screenshot(opts) {
    const el = await this._firstEl();
    if (el) {
      const png = await el.takeScreenshot();
      const { writeFileSync } = await import('fs');
      writeFileSync(opts.path, Buffer.from(png, 'base64'));
      return;
    }
    await this.driver.saveScreenshot(opts.path);
  }

  async innerText() {
    const el = await this._firstEl();
    if (!el) return '';
    const text = await el.getText().catch(() => '');
    const desc = await el.getAttribute('content-desc').catch(() => '');
    return [text, desc].filter(Boolean).join('\n');
  }

  async inputValue() {
    const el = await this._firstEl();
    if (!el) return '';
    return el.getText().catch(async () => el.getAttribute('text').catch(() => ''));
  }

  /** @param {string} name */
  async getAttribute(name) {
    const el = await this._firstEl();
    if (!el) return null;
    if (name === 'type') return 'text';
    return el.getAttribute(name).catch(() => null);
  }

  async boundingBox() {
    const el = await this._firstEl();
    if (!el) return null;
    try {
      return await elementRect(el);
    } catch {
      return null;
    }
  }

  async hover() {
    /* no-op on Android */
  }

  async setInputFiles() {
    /* native file picker avoided in recipes */
  }

  /** @param {(...args: any[]) => any} _fn @param {any} [_arg] */
  async evaluate(_fn, _arg) {
    return '';
  }

  /** @param {(...args: any[]) => any} _fn @param {any} [_arg] */
  async evaluateAll(_fn, _arg) {
    return [];
  }

  async waitFor({ timeout = 30000 } = {}) {
    const deadline = Date.now() + timeout;
    while (Date.now() < deadline) {
      if ((await this.count()) > 0) return;
      await this.driver.pause(200);
    }
    throw new Error(`Timeout waiting for ${this.selector}`);
  }
}

class AndroidKeyboard {
  /** @param {import('webdriverio').Browser} driver */
  constructor(driver) {
    this.driver = driver;
  }

  /** @param {string} key */
  async press(key) {
    if (key === 'Escape' || key === 'Backspace') {
      await this.driver.back();
      return;
    }
    if (key === 'Enter') {
      await this.driver.pressKeyCode(66);
      return;
    }
    if (key === 'ArrowDown') {
      await this.driver.pressKeyCode(20);
      return;
    }
    if (key === 'ArrowRight') {
      await this.driver.pressKeyCode(22);
      return;
    }
  }
}

class AndroidMouse {
  /** @param {import('webdriverio').Browser} driver */
  constructor(driver) {
    this.driver = driver;
  }

  async move() {}
  async down() {}
  async up() {}
}

export class AndroidPage {
  /** @param {import('webdriverio').Browser} driver */
  constructor(driver) {
    this.driver = driver;
    this.keyboard = new AndroidKeyboard(driver);
    this.mouse = new AndroidMouse(driver);
    this._platform = 'android';
  }

  waitForTimeout(ms) {
    return this.driver.pause(ms);
  }

  /** @param {string} selector */
  locator(selector) {
    return new AndroidLocator(this.driver, selector);
  }

  /** @param {string} text @param {{ exact?: boolean }} [opts] */
  getByText(text, opts = {}) {
    return new AndroidLocator(this.driver, uiText(text, opts.exact ?? false));
  }

  /** @param {'button'} role @param {{ name?: string, exact?: boolean }} [opts] */
  getByRole(role, opts = {}) {
    if (role === 'button' && opts.name) {
      const nameSel = uiText(opts.name, opts.exact ?? false);
      return new AndroidLocator(
        this.driver,
        `android=new UiSelector().clickable(true).${opts.exact ? `text("${escapeUi(opts.name)}")` : `textContains("${escapeUi(opts.name)}")`}`
      );
    }
    return new AndroidLocator(this.driver, 'android=new UiSelector().clickable(true)');
  }

  /** @param {string} label */
  getByLabel(label) {
    return new AndroidLocator(this.driver, uiText(label, true));
  }

  /** @param {{ path: string, fullPage?: boolean, timeout?: number }} opts */
  async screenshot(opts) {
    await this.driver.saveScreenshot(opts.path);
  }

  async evaluate() {
    return undefined;
  }

  async getWindowSize() {
    const rect = await this.driver.getWindowRect();
    return { width: rect.width, height: rect.height };
  }

  async performSwipe(x1, y1, x2, y2) {
    await this.driver.performActions([
      {
        type: 'pointer',
        id: 'finger1',
        parameters: { pointerType: 'touch' },
        actions: [
          { type: 'pointerMove', duration: 0, x: Math.round(x1), y: Math.round(y1) },
          { type: 'pointerDown', button: 0 },
          { type: 'pause', duration: 100 },
          { type: 'pointerMove', duration: 300, x: Math.round(x2), y: Math.round(y2) },
          { type: 'pointerUp', button: 0 },
        ],
      },
    ]);
    await this.driver.releaseActions().catch(() => {});
  }

  async scrollSampleTable() {
    const samples = this.locator('~aesg-sample-visuals');
    const n = await samples.count();
    for (let i = 0; i < n; i++) {
      try {
        await samples.nth(i).scrollIntoViewIfNeeded();
      } catch {
        /* continue */
      }
    }
    // Horizontal swipe on content area for wide sample rows.
    try {
      const { width, height } = await this.getWindowSize();
      const y = height * 0.45;
      for (const [x1, x2] of [
        [width * 0.8, width * 0.2],
        [width * 0.2, width * 0.8],
      ]) {
        await this.performSwipe(x1, y, x2, y);
        await this.waitForTimeout(150);
      }
    } catch {
      /* best effort */
    }
  }

  async back() {
    await this.driver.back();
  }

  /**
   * Tap the handheld top-nav menu button (hamburger). Never uses edge swipe — that
   * triggers Android predictive back instead of opening the in-app sidebar.
   * @param {(msg: string) => void} [log]
   */
  async openHandheldSidebarMenu(log = () => {}) {
    const menu = this.locator('~eggshell-sidebar-menu');
    if (await menu.count()) {
      await menu.first().click({ timeout: 5000 });
      log('tapped sidebar menu (~eggshell-sidebar-menu)');
      await this.waitForTimeout(500);
      return true;
    }

    const { width, height } = await this.getWindowSize();
    const topY = height * 0.22;
    const minX = width * 0.55;
    const els = await this.driver.$$('android=new UiSelector().clickable(true)');
    let best = null;
    let bestX = -1;
    for (const el of els) {
      try {
        const rect = await elementRect(el);
        const cy = rect.y + rect.height / 2;
        if (cy > topY) continue;
        if (rect.x + rect.width / 2 < minX) continue;
        if (rect.x > bestX) {
          bestX = rect.x;
          best = el;
        }
      } catch {
        /* skip */
      }
    }
    if (best) {
      await clickElement(best);
      log('tapped sidebar menu (top-nav rightmost clickable fallback)');
      await this.waitForTimeout(500);
      return true;
    }

    return false;
  }
}

/**
 * @param {AndroidPage} page
 */
export async function isGalleryUiVisible(page) {
  if (await page.locator('~eggshell-sidebar-menu').count()) return true;
  if (await page.locator('~aesg-sample-visuals').count()) return true;
  if (await page.getByText('Components Introduction', { exact: true }).count()) return true;
  for (const hint of ['Docs', 'Tools', 'How To', 'Design', 'Subject', 'Legacy']) {
    if (await page.getByText(hint, { exact: true }).count()) return true;
  }
  return false;
}

/**
 * @param {AndroidPage} page
 */
async function detectGalleryLoadError(page) {
  const patterns = [
    'Unable to load script',
    'Could not connect to development server',
    'Could not connect to the server',
    'Connect to Metro',
    'Make sure you\'re running Metro',
    'Download the React Native CLI',
  ];
  for (const text of patterns) {
    if (await page.getByText(text, { exact: false }).count()) return text;
  }
  return null;
}

/**
 * Wait until the gallery RN shell is interactive (not just the Activity in foreground).
 * @param {AndroidPage} page
 * @param {{ timeoutMs?: number, pollMs?: number, settleMs?: number, log?: (msg: string) => void }} [options]
 */
export async function waitForGalleryAppReady(page, options = {}) {
  const timeoutMs = options.timeoutMs ?? 120_000;
  const pollMs = options.pollMs ?? 750;
  const settleMs = options.settleMs ?? 1_000;
  const log = options.log ?? (() => {});
  const deadline = Date.now() + timeoutMs;
  let lastLogAt = 0;

  while (Date.now() < deadline) {
    const loadError = await detectGalleryLoadError(page);
    if (loadError) {
      throw new Error(
        `Gallery failed to load (${loadError}). Is Metro running on :8081 with adb reverse?`
      );
    }

    if (await isGalleryUiVisible(page)) {
      log(`app UI ready (${Math.round((Date.now() - (deadline - timeoutMs)) / 1000)}s)`);
      if (settleMs > 0) await page.waitForTimeout(settleMs);
      return;
    }

    const now = Date.now();
    if (now - lastLogAt >= 5000) {
      log('waiting for gallery UI (Metro bundle / first render)...');
      lastLogAt = now;
    }
    await page.waitForTimeout(pollMs);
  }

  throw new Error(
    `Gallery app UI not ready within ${timeoutMs}ms. ` +
      'Ensure Metro is up, adb reverse tcp:8081 tcp:8081, and the app finished loading.'
  );
}

/**
 * @param {AndroidPage} page
 */
export async function getForegroundPackage(page) {
  try {
    return await page.driver.getCurrentPackage();
  } catch {
    return null;
  }
}

function adbLaunchGallery() {
  return new Promise((resolve, reject) => {
    const p = spawn('adb', [
      'shell',
      'am',
      'start',
      '-n',
      `${ANDROID_APP.package}/${ANDROID_APP.activity}`,
      '-a',
      'android.intent.action.MAIN',
      '-c',
      'android.intent.category.LAUNCHER',
    ]);
    p.on('close', (code) => {
      if (code === 0) resolve();
      else reject(new Error(`adb am start exited ${code}`));
    });
    p.on('error', reject);
  });
}

/** First connected adb device id (e.g. emulator-5554). */
export function getDefaultAndroidUdid() {
  return new Promise((resolve, reject) => {
    const p = spawn('adb', ['devices']);
    let out = '';
    p.stdout.on('data', (d) => {
      out += d;
    });
    p.on('close', (code) => {
      if (code !== 0) {
        reject(new Error('adb devices failed'));
        return;
      }
      const line = out
        .split('\n')
        .map((l) => l.trim())
        .find((l) => l.endsWith('\tdevice'));
      if (!line) {
        reject(new Error('No adb device connected'));
        return;
      }
      resolve(line.split('\t')[0]);
    });
    p.on('error', reject);
  });
}

/**
 * Bring the EggShell Gallery app to the foreground before navigation/interaction.
 * Uses Appium activateApp, then adb am start if the package is still wrong.
 * @param {AndroidPage} page
 * @param {(msg: string) => void} [log]
 */
export async function ensureGalleryAppForeground(page, log = () => {}) {
  const expected = ANDROID_APP.package;
  let current = await getForegroundPackage(page);
  let didLaunch = false;

  if (current === expected && (await isGalleryUiVisible(page))) {
    log('app already in foreground with UI visible');
    return { didLaunch: false };
  }

  if (current !== expected) {
    log(`foreground is ${current ?? 'unknown'}, activating ${expected}`);
    didLaunch = true;
  } else {
    log('app foreground but UI not ready yet');
  }

  try {
    await page.driver.activateApp(expected);
    await page.waitForTimeout(1500);
  } catch (e) {
    log(`activateApp failed (${e.message}), trying adb am start`);
    await adbLaunchGallery().catch((err) => {
      log(`adb am start failed: ${err.message}`);
    });
    didLaunch = true;
    await page.waitForTimeout(2000);
  }

  current = await getForegroundPackage(page);
  if (current !== expected) {
    await adbLaunchGallery().catch(() => {});
    didLaunch = true;
    await page.waitForTimeout(2000);
    current = await getForegroundPackage(page);
  }

  if (current !== expected) {
    throw new Error(
      `Gallery app not in foreground (expected ${expected}, got ${current ?? 'unknown'})`
    );
  }
  log('gallery app in foreground');
  return { didLaunch };
}

/**
 * Connect to Appium and attach to the gallery app.
 * @param {{ appiumHost?: string, appiumPort?: number, deviceName?: string, noReset?: boolean, launchTimeoutMs?: number, log?: (msg: string) => void }} [options]
 */
export async function connectAndroidPage(options = {}) {
  const host = options.appiumHost ?? '127.0.0.1';
  const port = Number(options.appiumPort ?? 4723);
  const log = options.log ?? (() => {});
  const launchTimeoutMs = options.launchTimeoutMs ?? 120_000;
  const udid = options.udid ?? (await getDefaultAndroidUdid());
  log(`adb device: ${udid}`);
  const driver = await remote({
    hostname: host,
    port,
    path: '/',
    logLevel: 'error',
    capabilities: {
      platformName: 'Android',
      'appium:automationName': 'UiAutomator2',
      'appium:udid': udid,
      'appium:deviceName': options.deviceName ?? udid,
      'appium:appPackage': ANDROID_APP.package,
      'appium:appActivity': ANDROID_APP.activity,
      'appium:noReset': options.noReset !== false,
      'appium:autoGrantPermissions': true,
      'appium:newCommandTimeout': 600,
      'appium:disableWindowAnimation': true,
      'appium:appWaitActivity': ANDROID_APP.activity,
      'appium:appWaitPackage': ANDROID_APP.package,
      'appium:appWaitDuration': launchTimeoutMs,
      'appium:androidInstallTimeout': launchTimeoutMs,
      'wdio:enforceWebDriverClassic': true,
    },
  });
  const page = new AndroidPage(driver);
  await ensureGalleryAppForeground(page, log);
  await waitForGalleryAppReady(page, { timeoutMs: launchTimeoutMs, log });
  return page;
}

/** @param {AndroidPage} page */
export async function disconnectAndroidPage(page) {
  await page.driver.deleteSession();
}
