/**
 * Playwright session lifecycle: detect blocked/crashed browser state and recover.
 */

import { appendFileSync, mkdirSync } from 'fs';
import { join } from 'path';

/** Errors that mean the current page/context is unusable; recreate and retry. */
export const BROWSER_BLOCKED_PATTERNS = [
  /target page, context or browser has been closed/i,
  /target .* has been closed/i,
  /browser has been closed/i,
  /context has been closed/i,
  /protocol error/i,
  /execution context was destroyed/i,
  /page crashed/i,
  /net::err_connection_refused/i,
  /net::err_connection_reset/i,
  /net::err_empty_response/i,
  /net::err_internet_disconnected/i,
  /econnrefused/i,
  /socket hang up/i,
  /navigation failed because page was closed/i,
];

/**
 * @param {unknown} error
 */
export function isBrowserBlockedError(error) {
  const msg = String(error?.message ?? error ?? '');
  return BROWSER_BLOCKED_PATTERNS.some((re) => re.test(msg));
}

/**
 * @param {string} baseUrl
 * @param {{ maxWaitMs?: number, intervalMs?: number, log?: (msg: string) => void }} [options]
 */
export async function waitForGalleryReady(baseUrl, options = {}) {
  const { maxWaitMs = 45000, intervalMs = 2000, log = () => {} } = options;
  const url = baseUrl.replace(/\/$/, '');
  const deadline = Date.now() + maxWaitMs;

  while (Date.now() < deadline) {
    try {
      const res = await fetch(url, { signal: AbortSignal.timeout(8000) });
      if (res.ok || res.status < 500) {
        log(`gallery reachable (HTTP ${res.status})`);
        return true;
      }
      log(`gallery HTTP ${res.status}, retrying...`);
    } catch (e) {
      log(`gallery unreachable (${e.message ?? e}), retrying...`);
    }
    await new Promise((r) => setTimeout(r, intervalMs));
  }
  return false;
}

/**
 * @param {import('playwright').Browser} browser
 * @param {{ viewport?: { width: number, height: number }, passDir?: string, log?: (msg: string) => void }} options
 */
export async function createBrowserSession(browser, options = {}) {
  const {
    viewport = { width: 1400, height: 900 },
    passDir,
    log = () => {},
  } = options;

  /** @type {import('playwright').BrowserContext | null} */
  let context = null;
  /** @type {import('playwright').Page | null} */
  let page = null;
  let alive = true;
  /** @type {Array<{ at: string, reason: string }>} */
  const recoveries = [];

  async function attachFresh() {
    context = await browser.newContext({ viewport });
    page = await context.newPage();
    alive = true;

    context.on('close', () => {
      alive = false;
    });
    page.on('crash', () => {
      alive = false;
      log('browser session: page crash event');
    });

    if (passDir) {
      mkdirSync(passDir, { recursive: true });
      page.on('dialog', async (dialog) => {
        const entry = `[${new Date().toISOString()}] native-${dialog.type()}: ${dialog.message()}`;
        appendFileSync(join(passDir, 'native-dialogs.log'), `${entry}\n`);
        try {
          await dialog.accept();
        } catch {
          await dialog.dismiss().catch(() => {});
        }
      });
    }

    return page;
  }

  await attachFresh();

  return {
    get page() {
      return page;
    },
    get recoveries() {
      return recoveries;
    },

    isAlive() {
      return alive && page !== null && !page.isClosed();
    },

    /**
     * Close the broken context and open a fresh one.
     * @param {string} reason
     */
    async recover(reason) {
      const entry = { at: new Date().toISOString(), reason };
      recoveries.push(entry);
      log(`browser recovery: ${reason}`);
      if (passDir) {
        appendFileSync(join(passDir, 'recoveries.log'), `[${entry.at}] ${reason}\n`);
      }
      try {
        await context?.close();
      } catch {
        /* already closed */
      }
      alive = false;
      return attachFresh();
    },

    async close() {
      try {
        await context?.close();
      } catch {
        /* ignore */
      }
      alive = false;
    },
  };
}

/**
 * Run fn; on browser-blocked failure wait for gallery, recover session, retry once.
 * @template T
 * @param {ReturnType<typeof createBrowserSession> extends Promise<infer S> ? S : never} session
 * @param {string} componentName
 * @param {() => Promise<T>} fn
 * @param {{ baseUrl: string, log?: (msg: string) => void, maxRecoveries?: number, recoveryCount?: { n: number } }} options
 * @returns {Promise<{ result: T, recovered: boolean, recoveryReason: string | null }>}
 */
export async function withBrowserRecovery(session, componentName, fn, options) {
  const { baseUrl, log = () => {}, maxRecoveries = 30, recoveryCount = { n: 0 } } = options;

  const runAttempt = async () => {
    if (!session.isAlive()) {
      throw new Error('Target page, context or browser has been closed');
    }
    return fn();
  };

  try {
    const result = await runAttempt();
    return { result, recovered: false, recoveryReason: null };
  } catch (firstError) {
    if (!isBrowserBlockedError(firstError)) {
      throw firstError;
    }
    if (recoveryCount.n >= maxRecoveries) {
      throw new Error(
        `browser recovery limit (${maxRecoveries}) reached at ${componentName}: ${firstError.message ?? firstError}`
      );
    }
    recoveryCount.n += 1;
    const reason = `${componentName}: ${firstError.message ?? firstError}`;
    await waitForGalleryReady(baseUrl, {
      log: (msg) => log(`  [recovery] ${msg}`),
    });
    await session.recover(reason);
    try {
      const result = await runAttempt();
      return { result, recovered: true, recoveryReason: reason };
    } catch (retryError) {
      if (isBrowserBlockedError(retryError)) {
        throw new Error(
          `browser still blocked after recovery at ${componentName}: ${retryError.message ?? retryError}`
        );
      }
      throw retryError;
    }
  }
}
