/**
 * Detect EggShell / webpack full-window error overlays and escape to a safe route.
 */

import { appendFileSync, mkdirSync, writeFileSync, readFileSync, existsSync } from 'fs';
import { join } from 'path';

/** Gallery routes unlikely to throw a top-level render error. */
export const SAFE_FALLBACK_COMPONENTS = ['Index', 'Layout_Row', 'Button', 'Heading', 'Pre'];

/** Pages where an in-sample error UI is expected — not an app crash. */
const EXPECTED_ERROR_UI_COMPONENTS = new Set(['ErrorBoundary', 'AsyncData', 'Executor_AlertErrors']);

/**
 * @param {string[]} allComponents
 * @param {Set<string>} blockedComponents
 */
export function pickSafeFallbackComponent(allComponents, blockedComponents) {
  for (const name of SAFE_FALLBACK_COMPONENTS) {
    if (allComponents.includes(name) && !blockedComponents.has(name)) return name;
  }
  for (const name of allComponents) {
    if (!blockedComponents.has(name)) return name;
  }
  return allComponents[0] ?? 'Index';
}

/**
 * @param {string} outRoot
 */
export function loadBlockedComponents(outRoot) {
  const path = join(outRoot, 'blocked-components.json');
  if (!existsSync(path)) return new Set();
  try {
    const data = JSON.parse(readFileSync(path, 'utf8'));
    return new Set(data.components ?? []);
  } catch {
    return new Set();
  }
}

/**
 * @param {string} outRoot
 * @param {Set<string>} blockedComponents
 * @param {{ component: string, kind: string, detail: string, at: string }[]} [entries]
 */
export function saveBlockedComponents(outRoot, blockedComponents, entries = []) {
  const path = join(outRoot, 'blocked-components.json');
  let existing = [];
  if (existsSync(path)) {
    try {
      existing = JSON.parse(readFileSync(path, 'utf8')).entries ?? [];
    } catch {
      existing = [];
    }
  }
  writeFileSync(
    path,
    JSON.stringify(
      {
        components: [...blockedComponents],
        entries: [...existing, ...entries],
        updatedAt: new Date().toISOString(),
      },
      null,
      2
    )
  );
}

/**
 * @param {string} passDir
 * @param {string} component
 * @param {{ kind: string, detail: string, phase: string }} detection
 */
export function logAppCrash(passDir, component, detection) {
  mkdirSync(passDir, { recursive: true });
  const line = `[${new Date().toISOString()}] ${component} (${detection.phase}) ${detection.kind}: ${detection.detail}\n`;
  appendFileSync(join(passDir, 'app-crashes.log'), line);
}

/**
 * Detect webpack overlay or EggShell top-level error screen covering the app.
 * @param {import('playwright').Page} page
 * @param {{ componentName?: string }} [options]
 * @returns {Promise<{ crashed: boolean, kind?: string, detail?: string }>}
 */
export async function detectAppCrashOverlay(page, options = {}) {
  const { componentName = '' } = options;

  if (EXPECTED_ERROR_UI_COMPONENTS.has(componentName)) {
    return { crashed: false };
  }

  // Webpack dev-server error overlay (covers the window in dev-web).
  const webpackOverlay = page.locator(
    '#webpack-dev-server-client-overlay, iframe#webpack-dev-server-client-overlay, webpack-dev-server-client-overlay'
  );
  if (await webpackOverlay.count()) {
    const visible = await webpackOverlay
      .first()
      .isVisible()
      .catch(() => false);
    if (visible) {
      const detail = await page
        .locator('#webpack-dev-server-client-overlay')
        .innerText()
        .catch(() => 'webpack dev-server error overlay visible');
      return { crashed: true, kind: 'webpack-overlay', detail: String(detail).slice(0, 500) };
    }
  }

  const runtimeBanner = page.getByText(/uncaught runtime errors/i);
  if (await runtimeBanner.count()) {
    const visible = await runtimeBanner
      .first()
      .isVisible()
      .catch(() => false);
    if (visible) {
      return { crashed: true, kind: 'react-runtime-overlay', detail: 'Uncaught runtime errors banner' };
    }
  }

  const hasComponentTable = await page
    .locator('.aesg-ContentComponent-table')
    .first()
    .isVisible()
    .catch(() => false);

  // EggShell TopLevelErrorMessage — full-app error when component table is gone.
  const topLevelMarkers = ['Oops!', 'Something went wrong'];
  for (const marker of topLevelMarkers) {
    const pseudo = page.getByText(marker, { exact: true });
    if (!(await pseudo.count())) continue;
    const visible = await pseudo
      .first()
      .isVisible()
      .catch(() => false);
    if (!visible) continue;

    if (!hasComponentTable) {
      return { crashed: true, kind: 'top-level-error', detail: marker };
    }

    // Reload button without gallery chrome usually means the whole shell crashed.
    const reloadBtn = page.getByText('Reload', { exact: true });
    const hasReload = await reloadBtn.count();
    const hasSidebar = await page
      .locator('[data-testid^="sidebar-component-"], .aesg-Sidebar')
      .count()
      .catch(() => 0);
    if (hasReload && !hasSidebar && !hasComponentTable) {
      return { crashed: true, kind: 'top-level-error', detail: `${marker} + Reload (no gallery chrome)` };
    }
  }

  return { crashed: false };
}

/**
 * Navigate away from a crashed page and verify the safe route renders.
 * @param {import('playwright').Page} page
 * @param {string} safeUrl
 * @param {(msg: string) => void} [log]
 */
export async function escapeFromAppCrash(page, safeUrl, log = () => {}) {
  log(`escaping to safe route: ${safeUrl}`);

  await page
    .evaluate(() => {
      document.getElementById('webpack-dev-server-client-overlay')?.remove();
      document.querySelectorAll('iframe#webpack-dev-server-client-overlay').forEach((el) => el.remove());
    })
    .catch(() => {});

  await page.goto(safeUrl, { waitUntil: 'domcontentloaded', timeout: 60000 }).catch(() => {});
  await page.waitForTimeout(900);

  let detection = await detectAppCrashOverlay(page);
  if (detection.crashed) {
    log('safe route still blocked — reloading');
    await page.reload({ waitUntil: 'domcontentloaded', timeout: 60000 }).catch(() => {});
    await page.waitForTimeout(900);
    detection = await detectAppCrashOverlay(page);
  }

  if (detection.crashed) {
    log(`escape failed: ${detection.kind} — ${detection.detail}`);
    return false;
  }

  log('escape ok — gallery responsive again');
  return true;
}

/**
 * @param {import('playwright').Page} page
 * @param {string} passDir
 * @param {string} componentName
 */
export async function screenshotAppCrash(page, passDir, componentName) {
  const dir = join(passDir, 'screenshots', 'crashes');
  mkdirSync(dir, { recursive: true });
  const file = join(dir, `${componentName}.png`);
  await page.screenshot({ path: file, fullPage: false, timeout: 8000 }).catch(() => {});
  return file;
}
