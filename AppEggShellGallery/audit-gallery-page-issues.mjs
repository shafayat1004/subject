/**
 * Track non-fatal audit issues (timeouts, console floods) without blocking the crawl.
 */

import { appendFileSync, mkdirSync, writeFileSync, readFileSync, existsSync } from 'fs';
import { join } from 'path';

/** Default wall-clock budget for interactWithComponent (handler + assertions + generic sweep). */
export const DEFAULT_INTERACTION_TIMEOUT_MS = 90_000;

/** Shorter budgets for pages known to be heavy or flaky in dev-web. */
export const COMPONENT_INTERACTION_TIMEOUT_MS = {
  AutoUi_InputForm: 45_000,
  DateSelector: 30_000,
  Input_Date: 30_000,
  ThirdParty_Map: 60_000,
  Input_File: 60_000,
  ThirdParty_Recharts: 45_000,
};

export class InteractionTimeoutError extends Error {
  /**
   * @param {string} component
   * @param {number} timeoutMs
   * @param {string} [phase]
   */
  constructor(component, timeoutMs, phase = 'interact') {
    super(`Interaction timed out after ${timeoutMs}ms (${component}, ${phase})`);
    this.name = 'InteractionTimeoutError';
    this.component = component;
    this.timeoutMs = timeoutMs;
    this.phase = phase;
  }
}

/**
 * @param {string} componentName
 * @param {number} [overrideMs]
 */
export function interactionTimeoutMsFor(componentName, overrideMs = undefined) {
  if (overrideMs != null && Number.isFinite(overrideMs) && overrideMs > 0) return overrideMs;
  return COMPONENT_INTERACTION_TIMEOUT_MS[componentName] ?? DEFAULT_INTERACTION_TIMEOUT_MS;
}

/**
 * @param {() => Promise<T>} fn
 * @param {number} timeoutMs
 * @param {string} label
 * @returns {Promise<T>}
 */
export async function withInteractionTimeout(fn, timeoutMs, label) {
  /** @type {ReturnType<typeof setTimeout> | undefined} */
  let timer;
  const timeoutPromise = new Promise((_, reject) => {
    timer = setTimeout(() => reject(new InteractionTimeoutError(label, timeoutMs)), timeoutMs);
  });
  try {
    return await Promise.race([fn(), timeoutPromise]);
  } finally {
    if (timer) clearTimeout(timer);
  }
}

/**
 * @param {string} outRoot
 */
export function loadTrackedIssues(outRoot) {
  const path = join(outRoot, 'tracked-issues.json');
  if (!existsSync(path)) return { entries: [] };
  try {
    return JSON.parse(readFileSync(path, 'utf8'));
  } catch {
    return { entries: [] };
  }
}

/**
 * @param {string} outRoot
 * @param {{ component: string, kind: string, detail: string, at: string, passIndex?: number, screenshotPath?: string }} entry
 */
export function saveTrackedIssue(outRoot, entry) {
  const path = join(outRoot, 'tracked-issues.json');
  const data = loadTrackedIssues(outRoot);
  data.entries = [...(data.entries ?? []), entry];
  data.updatedAt = new Date().toISOString();
  writeFileSync(path, JSON.stringify(data, null, 2));
}

/**
 * @param {string} passDir
 * @param {string} component
 * @param {{ timeoutMs: number, phase?: string }} info
 */
export function logInteractionTimeout(passDir, component, info) {
  mkdirSync(passDir, { recursive: true });
  const line = `[${new Date().toISOString()}] ${component}: interaction timeout after ${info.timeoutMs}ms (${info.phase ?? 'interact'})\n`;
  appendFileSync(join(passDir, 'interaction-timeouts.log'), line);
}

/**
 * Best-effort cleanup so the next route can load after a wedged page.
 * @param {import('playwright').Page} page
 * @param {(msg: string) => void} [log]
 */
export async function escapeFromStuckInteraction(page, log = () => {}) {
  log('interaction timeout — dismissing overlays');
  for (let i = 0; i < 3; i++) {
    await page.keyboard.press('Escape').catch(() => {});
  }
  await page.waitForTimeout(250);
  await page
    .evaluate(() => {
      document.getElementById('webpack-dev-server-client-overlay')?.remove();
      document.querySelectorAll('iframe#webpack-dev-server-client-overlay').forEach((el) => el.remove());
    })
    .catch(() => {});
}

/**
 * @param {import('playwright').Page} page
 * @param {string} passDir
 * @param {string} componentName
 */
export async function screenshotInteractionTimeout(page, passDir, componentName) {
  const dir = join(passDir, 'screenshots', 'timeouts');
  mkdirSync(dir, { recursive: true });
  const file = join(dir, `${componentName}.png`);
  await page.screenshot({ path: file, fullPage: false, timeout: 8000 }).catch(() => {});
  return file;
}
