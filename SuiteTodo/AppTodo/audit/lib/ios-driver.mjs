/**
 * Appium / WebdriverIO driver for AppTodo iOS (XCUITest, Playwright-like facade).
 */

import { remote } from 'webdriverio';
import { spawn, execSync } from 'child_process';
import { resolveIosApp, APPIUM } from './native-config.mjs';
import { TIMEOUTS } from './config.mjs';
import { resolveIosSessionUdid } from './device-targets.mjs';
import {
  resolveDeviceOrientation,
  ensureDeviceOrientation,
  toAppiumOrientation,
} from './device-orientation.mjs';
import { waitForHealthyApp, probeAppHealth, detectMetroRedbox, isTodoUiVisible } from './app-health.mjs';

/** @param {import('webdriverio').Element} el */
async function elementRect(el) {
  const [loc, size] = await Promise.all([el.getLocation(), el.getSize()]);
  return { x: loc.x, y: loc.y, width: size.width, height: size.height };
}

class IosLocator {
  /** @param {import('webdriverio').Browser} driver @param {string} selector @param {{ index?: number }} [meta] */
  constructor(driver, selector, meta = {}) {
    this.driver = driver;
    this.selector = selector;
    this._index = meta.index ?? null;
  }

  _clone(extra = {}) {
    return new IosLocator(this.driver, this.selector, { index: this._index, ...extra });
  }

  async _resolveRaw() {
    let els;
    try {
      els = await this.driver.$$(this.selector);
    } catch {
      els = [];
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
    if (sub.startsWith('~')) return new IosLocator(this.driver, `~${sub.slice(1)}`);
    if (sub === 'input') return new IosLocator(this.driver, '-ios class chain:**/XCUIElementTypeTextField');
    return new IosLocator(this.driver, sub);
  }

  async click(opts = {}) {
    const el = (await this._resolveRaw())[0];
    if (!el) throw new Error(`No element to click: ${this.selector}`);
    await el.waitForDisplayed({ timeout: opts.timeout ?? 8000 }).catch(() => {});
    await el.click();
  }

  async fill(value, opts = {}) {
    const timeout = opts.timeout ?? 15000;
    const deadline = Date.now() + timeout;
    let lastErr;

    while (Date.now() < deadline) {
      const el = (await this._resolveRaw())[0];
      if (!el) {
        await this.driver.pause(250);
        continue;
      }
      try {
        await el.waitForDisplayed({ timeout: Math.min(4000, deadline - Date.now()) });
        await el.click().catch(() => {});
        await this.driver.pause(200);
        await el.clearValue().catch(() => {});
        await el.setValue(value);
        return;
      } catch (e) {
        lastErr = e;
        await this.driver.pause(300);
      }
    }

    throw lastErr ?? new Error(`No element to fill: ${this.selector}`);
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
    try {
      return await elementRect(el);
    } catch {
      return null;
    }
  }
}

export class IosPage {
  /** @param {import('webdriverio').Browser} driver */
  constructor(driver) {
    this.driver = driver;
    this.platform = 'ios';
  }

  waitForTimeout(ms) {
    return this.driver.pause(ms);
  }

  locator(selector) {
    if (selector.startsWith('~')) return new IosLocator(this.driver, selector);
    if (selector === 'input') return new IosLocator(this.driver, '-ios class chain:**/XCUIElementTypeTextField');
    return new IosLocator(this.driver, selector);
  }

  getByText(text, opts = {}) {
    const escaped = String(text).replace(/"/g, '\\"');
    const pred = opts.exact
      ? `name == "${escaped}" OR label == "${escaped}" OR value == "${escaped}"`
      : `name CONTAINS "${escaped}" OR label CONTAINS "${escaped}" OR value CONTAINS "${escaped}"`;
    return new IosLocator(this.driver, `-ios predicate string:${pred}`);
  }

  getByRole(role, opts = {}) {
    if (role === 'button' && opts.name) {
      return new IosLocator(
        this.driver,
        `-ios predicate string:type == 'XCUIElementTypeButton' AND name CONTAINS "${opts.name.replace(/"/g, '\\"')}"`
      );
    }
    return new IosLocator(this.driver, '-ios class chain:**/XCUIElementTypeButton');
  }

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

export function getDefaultIosUdid(options = {}) {
  return resolveIosSessionUdid(options);
}

export { isTodoUiVisible, probeAppHealth, detectMetroRedbox as detectMetroLoadError };

/**
 * @param {IosPage} page
 * @param {{ timeoutMs?: number, log?: (msg: string) => void }} [options]
 */
export async function waitForTodoAppReady(page, options = {}) {
  return waitForHealthyApp(page, 'ios', {
    timeoutMs: options.timeoutMs ?? TIMEOUTS.appReadyMs,
    log: options.log,
    logCollector: options.logCollector ?? null,
  });
}

/**
 * @param {{ appiumHost?: string, appiumPort?: number, udid?: string, log?: (msg: string) => void }} [options]
 */
export async function connectIosPage(options = {}) {
  const app = resolveIosApp();
  const host = options.appiumHost ?? APPIUM.host;
  const port = Number(options.appiumPort ?? APPIUM.port);
  const log = options.log ?? (() => {});
  const udid = options.udid ?? getDefaultIosUdid({ log, bootIfNeeded: options.bootIfNeeded });
  if (!udid) throw new Error('No iOS simulator available. Set defaultIosSimulator: npm run observe -- setup-devices');

  const orientation = resolveDeviceOrientation(options);
  log(`ios simulator: ${udid}, bundle: ${app.bundleId}, orientation: ${orientation}`);

  const driver = await remote({
    hostname: host,
    port,
    path: '/',
    logLevel: 'error',
    capabilities: {
      platformName: 'iOS',
      'appium:automationName': 'XCUITest',
      'appium:udid': udid,
      'appium:bundleId': app.bundleId,
      'appium:noReset': true,
      'appium:newCommandTimeout': 600,
      'appium:orientation': toAppiumOrientation(orientation),
      'wdio:enforceWebDriverClassic': true,
    },
  });

  const page = new IosPage(driver);
  await ensureDeviceOrientation(driver, 'ios', orientation, { udid, log });
  try {
    await driver.activateApp(app.bundleId);
  } catch {
    /* not installed */
  }
  await page.waitForTimeout(1500);
  await waitForTodoAppReady(page, {
    log,
    timeoutMs: TIMEOUTS.sessionConnectMs,
    logCollector: options.logCollector ?? null,
  });
  return page;
}

/** @param {IosPage} page */
export async function disconnectIosPage(page) {
  await page.driver.deleteSession();
}

/**
 * Recent simulator sys log lines mentioning AppTodo / React Native.
 */
export function captureIosLogs() {
  try {
    const out = execSync('xcrun simctl spawn booted log show --last 2m --style compact 2>/dev/null | tail -300', {
      encoding: 'utf8',
      maxBuffer: 2 * 1024 * 1024,
    });
    return out.split('\n').filter((l) => /AppTodo|React|eggshell/i.test(l));
  } catch {
    return [];
  }
}
