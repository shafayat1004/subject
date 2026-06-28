/**
 * Appium / WebdriverIO driver for AppTodo Android (Playwright-like facade).
 */

import { remote } from 'webdriverio';
import { spawn } from 'child_process';
import { resolveAndroidApp, APPIUM } from './native-config.mjs';
import { TEST_IDS } from './selectors.mjs';

function escapeUi(text) {
  return String(text).replace(/\\/g, '\\\\').replace(/"/g, '\\"');
}

export function testIdSelector(testId) {
  return `android=new UiSelector().resourceId("${escapeUi(testId)}")`;
}

function uiText(text, exact = false) {
  const e = escapeUi(text);
  return exact
    ? `android=new UiSelector().text("${e}")`
    : `android=new UiSelector().textContains("${e}")`;
}

/**
 * @param {import('webdriverio').Element} el
 */
async function clickNativeElement(el) {
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

class AndroidLocator {
  /** @param {import('webdriverio').Browser} driver @param {string} selector @param {AndroidLocator | null} parent @param {{ index?: number | 'last' }} [meta] */
  constructor(driver, selector, parent = null, meta = {}) {
    this.driver = driver;
    this.selector = selector;
    this.parentLocator = parent;
    this._index = meta.index ?? null;
  }

  _clone(extra = {}) {
    return new AndroidLocator(this.driver, this.selector, this.parentLocator, {
      index: this._index,
      ...extra,
    });
  }

  async _resolveRaw() {
    let els;
    if (this.parentLocator) {
      const parents = await this.parentLocator._resolveRaw();
      els = [];
      for (const p of parents) {
        try {
          els.push(...(await p.$$(this.selector)));
        } catch {
          /* skip */
        }
      }
    } else {
      try {
        els = await this.driver.$$(this.selector);
      } catch {
        els = [];
      }
    }
    if (this._index === 'last') return els.length ? [els[els.length - 1]] : [];
    if (typeof this._index === 'number') return this._index < els.length ? [els[this._index]] : [];
    return els;
  }

  async count() {
    return (await this._resolveRaw()).length;
  }

  first() {
    return this._clone({ index: 0 });
  }

  /** @param {string} sub */
  locator(sub) {
    const translated =
      sub.startsWith('~') ? testIdSelector(sub.slice(1))
      : sub.startsWith('android=') ? sub
      : sub.includes('EditText') ? 'android=new UiSelector().className("android.widget.EditText")'
      : sub;
    return new AndroidLocator(this.driver, translated, this);
  }

  /** @param {{ force?: boolean, timeout?: number }} [opts] */
  async click(opts = {}) {
    const el = (await this._resolveRaw())[0];
    if (!el) throw new Error(`No element to click: ${this.selector}`);
    await el.waitForDisplayed({ timeout: opts.timeout ?? 8000 }).catch(() => {});
    await clickNativeElement(el);
  }

  /** @param {string} value @param {{ timeout?: number }} [opts] */
  async fill(value, opts = {}) {
    const el = (await this._resolveRaw())[0];
    if (!el) throw new Error(`No element to fill: ${this.selector}`);
    await el.waitForDisplayed({ timeout: opts.timeout ?? 8000 }).catch(() => {});
    await el.clearValue().catch(() => {});
    await el.setValue(value);
  }

  async waitFor({ timeout = 30000 } = {}) {
    const deadline = Date.now() + timeout;
    while (Date.now() < deadline) {
      if ((await this.count()) > 0) return;
      await this.driver.pause(200);
    }
    throw new Error(`Timeout waiting for ${this.selector}`);
  }

  async boundingBox() {
    const el = (await this._resolveRaw())[0];
    if (!el) return null;
    const [loc, size] = await Promise.all([el.getLocation(), el.getSize()]);
    return { x: loc.x, y: loc.y, width: size.width, height: size.height };
  }
}

export class AndroidPage {
  /** @param {import('webdriverio').Browser} driver */
  constructor(driver) {
    this.driver = driver;
    this.platform = 'android';
  }

  waitForTimeout(ms) {
    return this.driver.pause(ms);
  }

  /** @param {string} selector */
  locator(selector) {
    if (selector.startsWith('~')) return new AndroidLocator(this.driver, testIdSelector(selector.slice(1)));
    if (selector.startsWith('android=')) return new AndroidLocator(this.driver, selector);
    if (selector === 'input') return new AndroidLocator(this.driver, 'android=new UiSelector().className("android.widget.EditText")');
    return new AndroidLocator(this.driver, selector);
  }

  /** @param {string} text @param {{ exact?: boolean }} [opts] */
  getByText(text, opts = {}) {
    return new AndroidLocator(this.driver, uiText(text, opts.exact ?? false));
  }

  /** @param {'button'} role @param {{ name?: string, exact?: boolean }} [opts] */
  getByRole(role, opts = {}) {
    if (role === 'button' && opts.name) {
      return new AndroidLocator(
        this.driver,
        `android=new UiSelector().clickable(true).textContains("${escapeUi(opts.name)}")`
      );
    }
    return new AndroidLocator(this.driver, 'android=new UiSelector().clickable(true)');
  }

  /** @param {{ path: string }} opts */
  async screenshot(opts) {
    await this.driver.saveScreenshot(opts.path);
  }

  async getPageSource() {
    return this.driver.getPageSource();
  }

  async getWindowSize() {
    const rect = await this.driver.getWindowRect();
    return { width: rect.width, height: rect.height };
  }
}

export function getDefaultAndroidUdid() {
  return new Promise((resolve, reject) => {
    const p = spawn('adb', ['devices']);
    let out = '';
    p.stdout.on('data', (d) => {
      out += d;
    });
    p.on('close', (code) => {
      if (code !== 0) return reject(new Error('adb devices failed'));
      const line = out.split('\n').map((l) => l.trim()).find((l) => l.endsWith('\tdevice'));
      if (!line) return reject(new Error('No adb device connected'));
      resolve(line.split('\t')[0]);
    });
    p.on('error', reject);
  });
}

/**
 * @param {AndroidPage} page
 */
export async function isTodoUiVisible(page) {
  if (await page.locator(`~${TEST_IDS.newTitle}`).count()) return true;
  if (await page.getByText('Todos', { exact: true }).count()) return true;
  return false;
}

/**
 * @param {AndroidPage} page
 */
async function detectMetroLoadError(page) {
  const patterns = [
    'Unable to load script',
    'Could not connect to development server',
    'Could not connect to the server',
    'Connect to Metro',
  ];
  for (const text of patterns) {
    if (await page.getByText(text, { exact: false }).count()) return text;
  }
  return null;
}

/**
 * @param {AndroidPage} page
 * @param {{ timeoutMs?: number, log?: (msg: string) => void }} [options]
 */
export async function waitForTodoAppReady(page, options = {}) {
  const timeoutMs = options.timeoutMs ?? 120_000;
  const log = options.log ?? (() => {});
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    const loadError = await detectMetroLoadError(page);
    if (loadError) {
      throw new Error(`AppTodo failed to load (${loadError}). Run Metro on :8081 and adb reverse tcp:8081 tcp:8081`);
    }
    if (await isTodoUiVisible(page)) {
      log('AppTodo UI ready');
      await page.waitForTimeout(800);
      return;
    }
    await page.waitForTimeout(750);
  }
  throw new Error(`AppTodo UI not ready within ${timeoutMs}ms`);
}

/**
 * @param {{ appiumHost?: string, appiumPort?: number, udid?: string, log?: (msg: string) => void, launchTimeoutMs?: number }} [options]
 */
export async function connectAndroidPage(options = {}) {
  const app = resolveAndroidApp();
  const host = options.appiumHost ?? APPIUM.host;
  const port = Number(options.appiumPort ?? APPIUM.port);
  const log = options.log ?? (() => {});
  const udid = options.udid ?? (await getDefaultAndroidUdid());
  log(`adb device: ${udid}, package: ${app.package}`);

  const driver = await remote({
    hostname: host,
    port,
    path: '/',
    logLevel: 'error',
    capabilities: {
      platformName: 'Android',
      'appium:automationName': 'UiAutomator2',
      'appium:udid': udid,
      'appium:deviceName': udid,
      'appium:appPackage': app.package,
      'appium:appActivity': app.activity,
      'appium:noReset': true,
      'appium:autoGrantPermissions': true,
      'appium:newCommandTimeout': 600,
      'appium:appWaitActivity': app.activity,
      'appium:appWaitPackage': app.package,
      'appium:appWaitDuration': options.launchTimeoutMs ?? 120_000,
      'wdio:enforceWebDriverClassic': true,
    },
  });

  const page = new AndroidPage(driver);
  try {
    await driver.activateApp(app.package);
  } catch {
    /* may already be foreground */
  }
  await page.waitForTimeout(1500);
  await waitForTodoAppReady(page, { log });
  return page;
}

/** @param {AndroidPage} page */
export async function disconnectAndroidPage(page) {
  await page.driver.deleteSession();
}

/**
 * Recent logcat lines for the AppTodo package.
 * @param {string} [packageName]
 */
export function captureLogcat(packageName) {
  const app = resolveAndroidApp();
  const pkg = packageName ?? app.package;
  return new Promise((resolve, reject) => {
    const p = spawn('adb', ['logcat', '-d', '-t', '300']);
    let out = '';
    p.stdout.on('data', (d) => {
      out += d;
    });
    p.on('close', (code) => {
      if (code !== 0) return reject(new Error(`adb logcat exited ${code}`));
      const lines = out.split('\n').filter((l) => l.includes(pkg) || /ReactNative|ReactNativeJS|chromium/i.test(l));
      resolve(lines);
    });
    p.on('error', reject);
  });
}
