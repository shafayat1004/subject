/**
 * App health: foreground detection, Metro/RedBox crashes, EggShell top-level errors.
 * Patterns aligned with AppEggShellGallery audit-gallery-app-crash.mjs and android-driver.
 */

import { spawn } from 'child_process';
import { TEST_IDS } from './selectors.mjs';
import { PLATFORM, isNativePlatform } from './platform.mjs';
import { pollUntil } from './timeouts.mjs';
import { TIMEOUTS } from './config.mjs';

/** @typedef {'healthy' | 'loading' | 'background' | 'metro_redbox' | 'native_crash' | 'top_level_error' | 'webpack_overlay' | 'react_runtime_overlay'} AppHealthState */

export const HEALTH = {
  HEALTHY: 'healthy',
  LOADING: 'loading',
  BACKGROUND: 'background',
  METRO_REDBOX: 'metro_redbox',
  NATIVE_CRASH: 'native_crash',
  TOP_LEVEL_ERROR: 'top_level_error',
  WEBPACK_OVERLAY: 'webpack_overlay',
  REACT_RUNTIME_OVERLAY: 'react_runtime_overlay',
  RENDER_ERROR: 'render_error',
};

/** Metro / RN dev error screen text (Android RedBox, iOS LogBox). */
export const METRO_ERROR_PATTERNS = [
  'Unable to load script',
  'Could not connect to development server',
  'Could not connect to the server',
  'Connect to Metro',
  "Make sure you're running Metro",
  'The development server returned response error code',
  'development server returned response error',
  'Unable to resolve module',
  'Module not found',
  '500 Internal Server Error',
  'Invariant Violation',
  'Exception in HostFunction',
  'TransformError',
  'SyntaxError',
];

/** RN LogBox render / runtime error UI (bundle loaded, app crashed on render). */
export const RN_RENDER_ERROR_PATTERNS = [
  'Render Error',
  'Uncaught Error',
  "Property 'crypto' doesn't exist",
  'ReferenceError',
  'TypeError',
];

/** EggShell TopLevelErrorMessage markers (web + native text). */
export const TOP_LEVEL_ERROR_MARKERS = ['Oops!', 'Something went wrong'];

/** RN error screen action buttons — strong signal of RedBox. */
export const RN_ERROR_SCREEN_MARKERS = ['DISMISS (ESC)', 'RELOAD (R, R)', 'Dismiss', 'Reload'];

/**
 * @param {import('playwright').Page | import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 * @param {string[]} patterns
 */
async function anyTextVisible(page, patterns) {
  for (const text of patterns) {
    if (await page.getByText(text, { exact: false }).count()) return text;
  }
  return null;
}

/**
 * @param {import('playwright').Page} page
 */
export async function detectWebCrashOverlay(page) {
  const webpackOverlay = page.locator(
    '#webpack-dev-server-client-overlay, iframe#webpack-dev-server-client-overlay'
  );
  if (await webpackOverlay.count()) {
    const visible = await webpackOverlay.first().isVisible().catch(() => false);
    if (visible) {
      const detail = await page
        .locator('#webpack-dev-server-client-overlay')
        .innerText()
        .catch(() => 'webpack dev-server error overlay visible');
      return { state: HEALTH.WEBPACK_OVERLAY, kind: 'webpack-overlay', detail: String(detail).slice(0, 800) };
    }
  }

  const runtimeBanner = page.getByText(/uncaught runtime errors/i);
  if (await runtimeBanner.count()) {
    const visible = await runtimeBanner.first().isVisible().catch(() => false);
    if (visible) {
      return { state: HEALTH.REACT_RUNTIME_OVERLAY, kind: 'react-runtime-overlay', detail: 'Uncaught runtime errors banner' };
    }
  }

  return null;
}

/**
 * @param {import('playwright').Page | import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 * @param {'web' | 'android' | 'ios'} platform
 */
export async function isTodoUiVisible(page, platform = PLATFORM.WEB) {
  if (await page.locator(`~${TEST_IDS.page}`).count()) return true;
  if (await page.locator(`~${TEST_IDS.newTitle}`).count()) return true;
  if (await page.getByText('Todos', { exact: true }).count()) return true;

  if (!isNativePlatform(platform)) {
    const input = page.locator(`[data-testid="${TEST_IDS.newTitle}"] input`).first();
    if (await input.count()) return true;
  }

  return false;
}

/**
 * Detect Metro RedBox / LogBox render error on native.
 * @param {import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 */
export async function detectMetroRedbox(page) {
  const metro = await anyTextVisible(page, METRO_ERROR_PATTERNS);
  if (metro) {
    return { state: HEALTH.METRO_REDBOX, kind: 'metro-redbox', detail: metro };
  }

  const renderErr = await anyTextVisible(page, RN_RENDER_ERROR_PATTERNS);
  if (renderErr) {
    return { state: HEALTH.RENDER_ERROR, kind: 'logbox-render-error', detail: renderErr };
  }

  // LogBox title is often split; "Render Error" + stack sections is a strong signal.
  const hasRenderTitle = (await page.getByText('Render Error', { exact: true }).count()) > 0;
  const hasCallStack = (await page.getByText('Call Stack', { exact: false }).count()) > 0;
  if (hasRenderTitle && hasCallStack) {
    return { state: HEALTH.RENDER_ERROR, kind: 'logbox-render-error', detail: 'Render Error (LogBox)' };
  }

  let dismissReload = 0;
  for (const marker of RN_ERROR_SCREEN_MARKERS) {
    if (await page.getByText(marker, { exact: false }).count()) dismissReload += 1;
  }
  if (dismissReload >= 2) {
    return { state: HEALTH.METRO_REDBOX, kind: 'rn-error-screen', detail: 'RN error screen (Dismiss + Reload visible)' };
  }

  return null;
}

/**
 * EggShell top-level crash UI (not intentional ErrorBoundary demo).
 * @param {import('playwright').Page | import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 * @param {'web' | 'android' | 'ios'} platform
 */
export async function detectTopLevelError(page, platform = PLATFORM.WEB) {
  const todoVisible = await isTodoUiVisible(page, platform);

  for (const marker of TOP_LEVEL_ERROR_MARKERS) {
    if (isNativePlatform(platform)) {
      if (await page.getByText(marker, { exact: true }).count()) {
        if (!todoVisible) {
          return { state: HEALTH.TOP_LEVEL_ERROR, kind: 'top-level-error', detail: marker };
        }
      }
      continue;
    }

    const pseudo = page.locator(`[data-text-as-pseudo-element="${marker}"]`);
    if (!(await pseudo.count())) continue;
    const visible = await pseudo.first().isVisible().catch(() => false);
    if (!visible) continue;

    if (!todoVisible) {
      return { state: HEALTH.TOP_LEVEL_ERROR, kind: 'top-level-error', detail: marker };
    }

    const reloadBtn = page.locator('[data-text-as-pseudo-element="Reload"]');
    const hasReload = await reloadBtn.count();
    if (hasReload && !todoVisible) {
      return { state: HEALTH.TOP_LEVEL_ERROR, kind: 'top-level-error', detail: `${marker} + Reload` };
    }
  }

  return null;
}

/**
 * Full health probe at a point in time.
 * @param {import('playwright').Page | import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 * @param {'web' | 'android' | 'ios'} platform
 * @param {{ expectedPackage?: string, foregroundPackage?: string | null }} [ctx]
 */
export async function probeAppHealth(page, platform, ctx = {}) {
  const foregroundPackage = ctx.foregroundPackage ?? (await getForegroundPackage(page));
  const expectedPackage = ctx.expectedPackage;

  if (expectedPackage && foregroundPackage && foregroundPackage !== expectedPackage) {
    return {
      state: HEALTH.BACKGROUND,
      healthy: false,
      foreground: false,
      foregroundPackage,
      expectedPackage,
      detail: `Expected ${expectedPackage}, got ${foregroundPackage ?? 'unknown'}`,
    };
  }

  if (platform === PLATFORM.WEB) {
    const webCrash = await detectWebCrashOverlay(page);
    if (webCrash) {
      return { ...webCrash, healthy: false, foreground: true, foregroundPackage: null };
    }
  }

  if (isNativePlatform(platform)) {
    const metro = await detectMetroRedbox(page);
    if (metro) {
      return { ...metro, healthy: false, foreground: true, foregroundPackage };
    }
  }

  const topLevel = await detectTopLevelError(page, platform);
  if (topLevel) {
    return { ...topLevel, healthy: false, foreground: true, foregroundPackage };
  }

  const todoReady = await isTodoUiVisible(page, platform);
  if (todoReady) {
    return {
      state: HEALTH.HEALTHY,
      healthy: true,
      foreground: true,
      foregroundPackage,
      detail: 'Todo UI visible',
    };
  }

  return {
    state: HEALTH.LOADING,
    healthy: false,
    foreground: foregroundPackage ? foregroundPackage === expectedPackage : true,
    foregroundPackage,
    detail: 'App foreground but Todo UI not ready',
  };
}

/**
 * Wait until app is healthy or fail fast on crash UI.
 * @param {import('playwright').Page | import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 * @param {'web' | 'android' | 'ios'} platform
 * @param {{ timeoutMs?: number, pollMs?: number, settleMs?: number, log?: (msg: string) => void, expectedPackage?: string }} [options]
 */
export async function waitForHealthyApp(page, platform, options = {}) {
  const timeoutMs = options.timeoutMs ?? TIMEOUTS.appReadyMs;
  const pollMs = options.pollMs ?? TIMEOUTS.pollMs;
  const settleMs = options.settleMs ?? TIMEOUTS.settleMs;
  const log = options.log ?? (() => {});

  const health = await pollUntil(
    async () => {
      const foregroundPackage = await getForegroundPackage(page);
      const probe = await probeAppHealth(page, platform, {
        expectedPackage: options.expectedPackage,
        foregroundPackage,
      });

      if (
        probe.state === HEALTH.METRO_REDBOX ||
        probe.state === HEALTH.RENDER_ERROR ||
        probe.state === HEALTH.TOP_LEVEL_ERROR ||
        probe.state === HEALTH.WEBPACK_OVERLAY ||
        probe.state === HEALTH.REACT_RUNTIME_OVERLAY ||
        probe.state === HEALTH.NATIVE_CRASH
      ) {
        throw new AppHealthError(probe);
      }

      if (probe.state === HEALTH.BACKGROUND) {
        log(`app in background (${probe.foregroundPackage}) — waiting for foreground`);
        return null;
      }

      if (probe.healthy) return probe;
      return null;
    },
    { timeoutMs, pollMs, log, label: 'healthy Todo app' }
  );

  log(`app healthy (${health.detail})`);
  if (settleMs > 0) await page.waitForTimeout(settleMs);
  return health;
}

export class AppHealthError extends Error {
  /**
   * @param {{ state: string, kind?: string, detail?: string, foregroundPackage?: string | null }} health
   */
  constructor(health) {
    super(formatHealthFailure(health));
    this.name = 'AppHealthError';
    this.health = health;
  }
}

/**
 * @param {{ state?: string, kind?: string, detail?: string }} health
 */
export function formatHealthFailure(health) {
  const kind = health.kind ?? health.state ?? 'unknown';
  const detail = health.detail ?? '';
  return `App not healthy (${kind})${detail ? `: ${detail}` : ''}`;
}

/**
 * @param {import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage | import('playwright').Page} page
 */
export async function getForegroundPackage(page) {
  if (page?.driver?.getCurrentPackage) {
    try {
      return await page.driver.getCurrentPackage();
    } catch {
      return null;
    }
  }
  return null;
}

/**
 * @param {string} packageName
 * @param {string} activity
 */
function adbLaunchApp(packageName, activity) {
  return new Promise((resolve, reject) => {
    const p = spawn('adb', [
      'shell',
      'am',
      'start',
      '-n',
      `${packageName}/${activity}`,
      '-a',
      'android.intent.action.MAIN',
      '-c',
      'android.intent.category.LAUNCHER',
    ]);
    p.on('close', (code) => (code === 0 ? resolve() : reject(new Error(`adb am start exited ${code}`))));
    p.on('error', reject);
  });
}

/**
 * Bring AppTodo to foreground before observe actions.
 * @param {import('./android-driver.mjs').AndroidPage} page
 * @param {{ package: string, activity: string }} app
 * @param {(msg: string) => void} [log]
 */
export async function ensureAppForeground(page, app, log = () => {}) {
  const expected = app.package;
  let current = await getForegroundPackage(page);

  if (current === expected && (await isTodoUiVisible(page, PLATFORM.ANDROID))) {
    log('app already in foreground with UI visible');
    return { didLaunch: false };
  }

  if (current !== expected) {
    log(`foreground is ${current ?? 'unknown'}, activating ${expected}`);
  } else {
    log('app foreground but UI not ready yet');
  }

  try {
    await page.driver.activateApp(expected);
    await page.waitForTimeout(1500);
  } catch (e) {
    log(`activateApp failed (${e.message}), trying adb am start`);
    await adbLaunchApp(expected, app.activity).catch((err) => log(`adb am start failed: ${err.message}`));
    await page.waitForTimeout(2000);
  }

  current = await getForegroundPackage(page);
  if (current !== expected) {
    await adbLaunchApp(expected, app.activity).catch(() => {});
    await page.waitForTimeout(2000);
    current = await getForegroundPackage(page);
  }

  if (current !== expected) {
    throw new Error(`App not in foreground (expected ${expected}, got ${current ?? 'unknown'})`);
  }

  log('app in foreground');
  return { didLaunch: current === expected };
}
