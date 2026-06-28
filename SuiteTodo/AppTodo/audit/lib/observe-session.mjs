/**
 * Unified observe session: web (Playwright), Android (Appium), iOS (XCUITest).
 */

import { chromium } from 'playwright';
import { DEFAULTS } from './config.mjs';
import { PLATFORM, normalizePlatform } from './platform.mjs';
import { createWebSession, waitForAppReady } from './web-session.mjs';
import { waitForTodoReady } from './selectors.mjs';
import { appendRunLog } from './capture.mjs';

/**
 * @typedef {Object} ObserveSession
 * @property {'web' | 'android' | 'ios'} platform
 * @property {import('playwright').Page | import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 * @property {{ consoleLines: string[], pageErrors: string[], networkErrors: string[] }} [logs]
 * @property {() => Promise<void>} close
 */

/**
 * @param {{ platform?: string, baseUrl?: string, headless?: boolean, slowMo?: number, outDir?: string, log?: (msg: string) => void, skipNavigation?: boolean }} options
 * @returns {Promise<ObserveSession>}
 */
export async function openObserveSession(options = {}) {
  const platform = normalizePlatform(options.platform ?? DEFAULTS.platform);
  const log = options.log ?? (() => {});
  const headless = options.headless ?? DEFAULTS.headless;
  const slowMo = options.slowMo ?? DEFAULTS.slowMo;
  const baseUrl = options.baseUrl ?? DEFAULTS.baseUrl;

  if (platform === PLATFORM.WEB) {
    const ready = await waitForAppReady(baseUrl, { log });
    if (!ready) throw new Error(`AppTodo not reachable at ${baseUrl}. Start: ../../eggshell dev-web`);

    const browser = await chromium.launch({ headless, slowMo });
    const session = await createWebSession(browser, { viewport: DEFAULTS.viewport });
    const resp = await session.goto(baseUrl);

    return {
      platform,
      page: session.page,
      logs: session.logs,
      httpStatus: resp?.status() ?? null,
      async close() {
        await session.close();
        await browser.close();
      },
    };
  }

  if (platform === PLATFORM.ANDROID) {
    const { connectAndroidPage, disconnectAndroidPage } = await import('./android-driver.mjs');
    const page = await connectAndroidPage({ log });
    return {
      platform,
      page,
      logs: { consoleLines: [], pageErrors: [], networkErrors: [] },
      async close() {
        await disconnectAndroidPage(page);
      },
    };
  }

  if (platform === PLATFORM.IOS) {
    const { connectIosPage, disconnectIosPage } = await import('./ios-driver.mjs');
    const page = await connectIosPage({ log });
    return {
      platform,
      page,
      logs: { consoleLines: [], pageErrors: [], networkErrors: [] },
      async close() {
        await disconnectIosPage(page);
      },
    };
  }

  throw new Error(`Unsupported platform: ${platform}`);
}

/**
 * @param {ObserveSession} session
 * @param {{ bootstrapWaitMs?: number, outDir?: string }} [options]
 */
export async function prepareTodoUi(session, options = {}) {
  const { bootstrapWaitMs = DEFAULTS.bootstrapWaitMs, outDir } = options;
  if (outDir) appendRunLog(outDir, `prepareTodoUi platform=${session.platform}`);
  await waitForTodoReady(session.page, {
    bootstrapWaitMs,
    platform: session.platform,
  });
}
