/**
 * Playwright gate for AppTodo ui2 fixes: phone centering, compact rows, category scroll.
 * Usage: node audit/workflows/verify-ui2-web.mjs [--headless]
 */
import { chromium } from 'playwright';
import { DEFAULTS, parseBool } from '../lib/config.mjs';
import { waitForAppReady } from '../lib/web-session.mjs';
import { waitForTodoReady } from '../lib/selectors.mjs';
import { probeAppHealth } from '../lib/app-health.mjs';

const args = process.argv.slice(2);
const headlessFlag = args.find((a) => a.startsWith('--headless'));
const headless = headlessFlag
  ? parseBool(headlessFlag.replace(/^--headless=*/, ''), true)
  : true;

const baseUrl = DEFAULTS.baseUrl.replace(/\/$/, '');

/** @type {string[]} */
const failures = [];

function isIgnorableConsole(text, type) {
  const t = text.toLowerCase();
  if (t.includes('use color.grey') && t.includes('instead of color.hex')) return true;
  if (t.includes('legacy childcontexttypes')) return true;
  if (t.includes('failed to decode downloaded font')) return true;
  if (t.startsWith('warning:') && type === 'error') return true;
  return false;
}

async function main() {
  const ready = await waitForAppReady(baseUrl);
  if (!ready) {
    console.error(`FAIL: dev-web not reachable at ${baseUrl}`);
    process.exit(1);
  }

  const browser = await chromium.launch({ headless });
  const page = await browser.newPage({ viewport: DEFAULTS.viewport });

  /** @type {string[]} */
  const consoleErrors = [];
  page.on('pageerror', (err) => consoleErrors.push(`pageerror: ${err.message}`));
  page.on('console', (msg) => {
    if (msg.type() === 'error' && !isIgnorableConsole(msg.text(), 'error')) {
      consoleErrors.push(`console: ${msg.text().slice(0, 200)}`);
    }
  });

  const resp = await page.goto(baseUrl, { waitUntil: 'networkidle', timeout: 90000 });
  if (!resp || resp.status() >= 400) {
    failures.push(`HTTP ${resp?.status() ?? 'no response'} for ${baseUrl}`);
  }

  await waitForTodoReady(page, { bootstrapWaitMs: DEFAULTS.bootstrapWaitMs });
  await page.getByText('Welcome to AppTodo', { exact: true }).waitFor({ timeout: 30000 });

  const health = await probeAppHealth(page, 'web');
  if (!health.healthy) {
    failures.push(`App unhealthy: ${health.state} — ${health.detail ?? ''}`);
  }

  const checks = await page.evaluate(() => {
    /** @type {HTMLElement | null} */
    let card = null;
    let bestArea = 0;

    for (const el of document.querySelectorAll('div')) {
      if (!(el instanceof HTMLElement)) continue;
      const r = el.getBoundingClientRect();
      if (r.width < 380 || r.width > 460 || r.height < 400) continue;
      if (!el.textContent?.includes('Todos')) continue;
      if (!el.textContent?.includes('Welcome to AppTodo')) continue;
      const area = r.width * r.height;
      if (area > bestArea) {
        bestArea = area;
        card = el;
      }
    }

    const cardRect = card?.getBoundingClientRect();
    const cardMetrics = cardRect
      ? {
          x: Math.round(cardRect.x),
          width: Math.round(cardRect.width),
          marginLeft: Math.round(cardRect.x),
          marginRight: Math.round(window.innerWidth - (cardRect.x + cardRect.width)),
        }
      : null;

    const rowCandidates = [...document.querySelectorAll('div')].filter((d) => {
      const t = d.textContent || '';
      return t.includes('Welcome to AppTodo') && t.includes('High') && !t.includes('Try dark mode');
    });
    rowCandidates.sort((a, b) => (a.textContent?.length ?? 0) - (b.textContent?.length ?? 0));
    const rowText = rowCandidates[0]?.textContent?.replace(/\s+/g, ' ').trim() ?? '';

    const scroller = [...document.querySelectorAll('div')].find(
      (d) =>
        d instanceof HTMLElement &&
        d.textContent?.includes('No category') &&
        d.textContent?.includes('Other') &&
        d.scrollWidth > d.clientWidth + 5
    );

    let categoryScroll = { canScroll: false, scrollMoved: false, scrollWidth: 0, clientWidth: 0 };
    if (scroller instanceof HTMLElement) {
      const before = scroller.scrollLeft;
      scroller.scrollLeft = scroller.scrollWidth;
      const after = scroller.scrollLeft;
      scroller.scrollLeft = before;
      categoryScroll = {
        canScroll: true,
        scrollMoved: after > before,
        scrollWidth: scroller.scrollWidth,
        clientWidth: scroller.clientWidth,
      };
    }

    return {
      card: cardMetrics,
      rowText,
      categoryScroll,
    };
  });

  if (!checks.card) {
    failures.push('Todo phone card not found by layout heuristic');
  } else {
    const centerSkew = Math.abs(checks.card.marginLeft - checks.card.marginRight);
    console.log(
      `card: x=${checks.card.x} width=${checks.card.width} skew=${centerSkew}px`
    );
    if (checks.card.marginLeft < 200) {
      failures.push(`Phone card stuck left: x=${checks.card.x}px`);
    }
    if (centerSkew > 100) {
      failures.push(
        `Phone card not centered: left ${checks.card.marginLeft}px, right ${checks.card.marginRight}px`
      );
    }
  }

  console.log(`row: ${checks.rowText}`);
  if (!checks.rowText.includes('Welcome to AppTodo')) {
    failures.push('First todo row not found');
  }
  if (/Edit .+ Delete/.test(checks.rowText)) {
    failures.push(`Garbled compact row: "${checks.rowText}"`);
  }
  if (/Edit Welcome/.test(checks.rowText) || /Delete Welcome/.test(checks.rowText)) {
    failures.push(`Desktop action labels merged into row: "${checks.rowText}"`);
  }

  console.log('category scroll:', checks.categoryScroll);
  if (!checks.categoryScroll.canScroll) {
    failures.push('Category pill row does not overflow — horizontal scroll unavailable');
  } else if (!checks.categoryScroll.scrollMoved) {
    failures.push('Category scroll container found but scrollLeft did not change');
  }

  await browser.close();

  if (consoleErrors.length) {
    failures.push(...consoleErrors);
  }

  if (failures.length) {
    console.error('\nverify-ui2-web FAILED:');
    for (const f of failures) console.error('  -', f);
    process.exit(1);
  }

  console.log('\nverify-ui2-web PASSED (centering, compact rows, category scroll).');
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
