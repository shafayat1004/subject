/**
 * Playwright-like facade over WebdriverIO/Appium for Android gallery audits.
 * Lets audit-gallery-interactions.mjs and audit-gallery-assertions.mjs share recipes.
 */

import { remote } from 'webdriverio';
import { ANDROID_APP } from './audit-gallery-platform.mjs';

function escapeUi(text) {
  return String(text).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
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
  if (!selector || selector.startsWith('~')) return selector;
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
    const el = await this._firstEl();
    if (!el) throw new Error(`No element to click: ${this.selector}`);
    await el.waitForDisplayed({ timeout: opts.timeout ?? 5000 }).catch(() => {});
    await el.click();
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
    const rect = await el.getRect();
    return { x: rect.x, y: rect.y, width: rect.width, height: rect.height };
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
    const samples = this.locator(
      'android=new UiSelector().className("android.widget.HorizontalScrollView")'
    );
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
}

/**
 * Connect to Appium and attach to the gallery app.
 * @param {{ appiumHost?: string, appiumPort?: number, deviceName?: string, noReset?: boolean }} [options]
 */
export async function connectAndroidPage(options = {}) {
  const host = options.appiumHost ?? '127.0.0.1';
  const port = Number(options.appiumPort ?? 4723);
  const driver = await remote({
    hostname: host,
    port,
    path: '/',
    logLevel: 'error',
    capabilities: {
      platformName: 'Android',
      'appium:automationName': 'UiAutomator2',
      'appium:deviceName': options.deviceName ?? 'Android Emulator',
      'appium:appPackage': ANDROID_APP.package,
      'appium:appActivity': ANDROID_APP.activity,
      'appium:noReset': options.noReset !== false,
      'appium:autoGrantPermissions': true,
      'appium:newCommandTimeout': 600,
      'appium:disableWindowAnimation': true,
    },
  });
  return new AndroidPage(driver);
}

/** @param {AndroidPage} page */
export async function disconnectAndroidPage(page) {
  await page.driver.deleteSession();
}
