/**
 * Unified observe session: web (Playwright), Android (Appium), iOS (XCUITest).
 * Logs are collected by default for every session.
 */

import { chromium } from 'playwright';
import { DEFAULTS, TIMEOUTS } from './config.mjs';
import { PLATFORM, normalizePlatform, isNativePlatform } from './platform.mjs';
import { createWebSession, waitForAppReady } from './web-session.mjs';
import { waitForTodoReady } from './selectors.mjs';
import { appendRunLog } from './capture.mjs';
import { waitForHealthyApp, probeAppHealth, AppHealthError } from './app-health.mjs';
import { createDeviceLogCollector } from './device-logs.mjs';
import { resolveDeviceOrientation } from './device-orientation.mjs';
import { resolveAndroidApp } from './native-config.mjs';

/**
 * @typedef {Object} ObserveSession
 * @property {'web' | 'android' | 'ios'} platform
 * @property {import('playwright').Page | import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 * @property {{ consoleLines: string[], pageErrors: string[], networkErrors: string[] }} logs
 * @property {import('./device-logs.mjs').AndroidLogCollector | null} [logCollector]
 * @property {() => Promise<void>} close
 */

/**
 * @param {{ platform?: string, baseUrl?: string, headless?: boolean, slowMo?: number, outDir?: string, log?: (msg: string) => void, skipNavigation?: boolean, timeoutMs?: number, orientation?: string }} options
 * @returns {Promise<ObserveSession>}
 */
export async function openObserveSession(options = {}) {
  const platform = normalizePlatform(options.platform ?? DEFAULTS.platform);
  const log = options.log ?? (() => {});
  const headless = options.headless ?? DEFAULTS.headless;
  const slowMo = options.slowMo ?? DEFAULTS.slowMo;
  const baseUrl = options.baseUrl ?? DEFAULTS.baseUrl;
  const timeoutMs = options.timeoutMs ?? TIMEOUTS.appReadyMs;
  const orientation = resolveDeviceOrientation(options);

  if (platform === PLATFORM.WEB) {
    const ready = await waitForAppReady(baseUrl, { log, maxWaitMs: timeoutMs });
    if (!ready) throw new Error(`AppTodo not reachable at ${baseUrl}. Start: ../../eggshell dev-web`);

    const browser = await chromium.launch({ headless, slowMo });
    const session = await createWebSession(browser, { viewport: DEFAULTS.viewport });
    const resp = await session.goto(baseUrl);

    return {
      platform,
      page: session.page,
      logs: session.logs,
      logCollector: null,
      httpStatus: resp?.status() ?? null,
      async close() {
        await session.close();
        await browser.close();
      },
    };
  }

  if (platform === PLATFORM.ANDROID) {
    const { connectAndroidPage, disconnectAndroidPage } = await import('./android-driver.mjs');
    const app = resolveAndroidApp();
    const logCollector = createDeviceLogCollector('android', {
      outDir: options.outDir,
      packageName: app.package,
    });
    logCollector?.start();

    const page = await connectAndroidPage({
      log,
      orientation,
      logCollector,
      launchTimeoutMs: options.timeoutMs ?? TIMEOUTS.sessionConnectMs,
    });

    return {
      platform,
      page,
      orientation,
      logs: logCollector?.toSessionLogs() ?? { consoleLines: [], pageErrors: [], networkErrors: [] },
      logCollector,
      async close() {
        logCollector?.stop();
        await disconnectAndroidPage(page);
      },
    };
  }

  if (platform === PLATFORM.IOS) {
    const { connectIosPage, disconnectIosPage } = await import('./ios-driver.mjs');
    const logCollector = createDeviceLogCollector('ios', { outDir: options.outDir });
    logCollector?.start();

    const page = await connectIosPage({ log, orientation, logCollector });

    return {
      platform,
      page,
      orientation,
      logs: logCollector?.toSessionLogs() ?? { consoleLines: [], pageErrors: [], networkErrors: [] },
      logCollector,
      async close() {
        logCollector?.stop();
        await disconnectIosPage(page);
      },
    };
  }

  throw new Error(`Unsupported platform: ${platform}`);
}

/**
 * Wait for healthy Todo UI — crash-aware with configurable timeout.
 * @param {ObserveSession} session
 * @param {{ bootstrapWaitMs?: number, outDir?: string, timeoutMs?: number }} [options]
 */
export async function prepareTodoUi(session, options = {}) {
  const { bootstrapWaitMs = DEFAULTS.bootstrapWaitMs, outDir, timeoutMs = TIMEOUTS.appReadyMs } = options;
  const log = outDir ? (msg) => appendRunLog(outDir, msg) : () => {};

  if (outDir) appendRunLog(outDir, `prepareTodoUi platform=${session.platform} timeoutMs=${timeoutMs}`);

  if (session.platform === PLATFORM.WEB) {
    await session.page.waitForTimeout(bootstrapWaitMs);
    try {
      await waitForHealthyApp(session.page, 'web', { timeoutMs, log });
    } catch (e) {
      if (e instanceof AppHealthError && outDir) {
        appendRunLog(outDir, `health failure: ${e.message}`);
      }
      throw e;
    }
    await waitForTodoReady(session.page, { bootstrapWaitMs: 0, platform: session.platform });
    return;
  }

  if (isNativePlatform(session.platform)) {
    const app = session.platform === PLATFORM.ANDROID ? resolveAndroidApp() : null;
    try {
      await waitForHealthyApp(session.page, session.platform, {
        timeoutMs,
        log,
        expectedPackage: app?.package,
        logCollector: session.logCollector ?? null,
      });
    } catch (e) {
      if (e instanceof AppHealthError && outDir) {
        appendRunLog(outDir, `health failure: ${e.message}`);
      }
      throw e;
    }
  }
}

/**
 * Probe current app health (foreground, crash UI, todo ready).
 * @param {ObserveSession} session
 */
export async function probeSessionHealth(session) {
  const app = session.platform === PLATFORM.ANDROID ? resolveAndroidApp() : null;
  return probeAppHealth(session.page, session.platform, {
    expectedPackage: app?.package,
    logCollector: session.logCollector ?? null,
  });
}

export { AppHealthError, probeAppHealth, waitForHealthyApp };
