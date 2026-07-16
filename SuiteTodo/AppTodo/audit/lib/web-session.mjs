/**
 * Playwright session for AppTodo (headed by default).
 */

/**
 * @param {string} baseUrl
 * @param {{ maxWaitMs?: number, intervalMs?: number, log?: (msg: string) => void }} [options]
 */
export async function waitForAppReady(baseUrl, options = {}) {
  const { maxWaitMs = 45000, intervalMs = 2000, log = () => {} } = options;
  const url = baseUrl.replace(/\/$/, '');
  const deadline = Date.now() + maxWaitMs;

  while (Date.now() < deadline) {
    try {
      const res = await fetch(url, { signal: AbortSignal.timeout(8000) });
      if (res.ok || res.status < 500) {
        log(`AppTodo reachable (HTTP ${res.status})`);
        return true;
      }
      log(`AppTodo HTTP ${res.status}, retrying...`);
    } catch (e) {
      log(`AppTodo unreachable (${e.message ?? e}), retrying...`);
    }
    await new Promise((r) => setTimeout(r, intervalMs));
  }
  return false;
}

/**
 * @param {import('playwright').Browser} browser
 * @param {{ viewport?: { width: number, height: number }, passDir?: string, log?: (msg: string) => void }} options
 */
export async function createWebSession(browser, options = {}) {
  const {
    viewport = { width: 1280, height: 900 },
    passDir,
    log = () => {},
  } = options;

  const context = await browser.newContext({ viewport });
  const page = await context.newPage();

  /** @type {string[]} */
  const consoleLines = [];
  /** @type {string[]} */
  const pageErrors = [];
  /** @type {string[]} */
  const networkErrors = [];

  page.on('console', (msg) => {
    const line = `[${msg.type()}] ${msg.text()}`;
    consoleLines.push(line);
    if (passDir && msg.type() === 'error') {
      log(`console error: ${msg.text()}`);
    }
  });
  page.on('pageerror', (err) => {
    const line = err.message ?? String(err);
    pageErrors.push(line);
    log(`pageerror: ${line}`);
  });
  page.on('requestfailed', (req) => {
    const line = `${req.method()} ${req.url()} — ${req.failure()?.errorText ?? 'failed'}`;
    networkErrors.push(line);
  });

  return {
    context,
    page,
    get logs() {
      return { consoleLines, pageErrors, networkErrors };
    },
    async goto(baseUrl) {
      const url = baseUrl.replace(/\/$/, '');
      const resp = await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60000 });
      return resp;
    },
    async close() {
      try {
        await context.close();
      } catch {
        /* ignore */
      }
    },
  };
}
