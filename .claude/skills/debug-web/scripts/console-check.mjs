#!/usr/bin/env node
// console-check.mjs — capture browser console errors/warnings from a running dev-web server.
//
// Usage (run from the app dir so `playwright` resolves, e.g. AppEggShellGallery/):
//   node ../../.claude/skills/debug-web/scripts/console-check.mjs [url1 url2 ...]
//   node ../../.claude/skills/debug-web/scripts/console-check.mjs            # defaults below
//   node ../../.claude/skills/debug-web/scripts/console-check.mjs --port 9080
//
// Loads each URL, waits for networkidle + a settle delay, captures every console
// error and warning (deduplicated), prints them, and exits 1 if any ERRORS were
// found (warnings alone exit 0). Use to verify a fix actually cleared a console
// error, or to triage which errors remain after an edit.
//
// Filter to specific patterns:
//   node ... --filter "key prop|BackHandler|boxShadow"
//
// To also dump the React component stack for key warnings (helps locate the F#
// source), pass --full (slower; serializes all console args).
import { chromium } from 'playwright';

const args = process.argv.slice(2);
const portArg = args.findIndex(a => a === '--port');
const port = portArg >= 0 ? args[portArg + 1] : '8082';
const filterIdx = args.findIndex(a => a === '--filter');
const filterRe = filterIdx >= 0 ? new RegExp(args[filterIdx + 1], 'i') : null;
const full = args.includes('--full');
const urls = args.filter(a => !a.startsWith('--') && a !== port && (portArg < 0 || a !== args[portArg + 1]) && (filterIdx < 0 || a !== args[filterIdx + 1]));

const defaultUrls = [
  `http://localhost:${port}/`,
  `http://localhost:${port}/modernization/fsql-server-to-postgres.md`,
];
const targetUrls = urls.length > 0 ? urls : defaultUrls;

const browser = await chromium.launch();
const all = [];

for (const url of targetUrls) {
  const page = await browser.newPage();
  const local = [];
  page.on('console', (m) => {
    const t = m.type();
    if (t !== 'error' && t !== 'warning') return;
    let text = (m.text() || '').replace(/\n/g, ' ');
    if (filterRe && !filterRe.test(text)) return;
    if (full) {
      // serialize args to capture the React component stack / full warning text
      m.args().forEach(async (a, i) => {
        try { text += `\n  [arg${i}] ${JSON.stringify(await a.jsonValue()).slice(0, 300)}`; } catch {}
      });
    }
    local.push(`[${t}] ${text.slice(0, 300)}`);
  });
  page.on('pageerror', (e) => local.push(`[pageerror] ${String(e).slice(0, 300)}`));

  try {
    await page.goto(url, { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(2500);
  } catch (e) {
    local.push(`[load-error] ${String(e).slice(0, 200)}`);
  }
  all.push({ url, lines: local });
  await page.close();
}

await browser.close();

// dedupe globally
const seen = new Set();
let errorCount = 0;
console.log(`=== console-check: ${targetUrls.length} URL(s) ===`);
for (const { url, lines } of all) {
  const uniq = lines.filter(x => { if (seen.has(x)) return false; seen.add(x); return true; });
  if (uniq.length === 0) continue;
  console.log(`\n--- ${url} (${uniq.length} unique) ---`);
  for (const x of uniq) {
    console.log(x);
    if (x.startsWith('[error]') || x.startsWith('[pageerror]')) errorCount++;
  }
}
if (errorCount > 0) {
  console.log(`\nFAIL: ${errorCount} error(s) found.`);
  process.exit(1);
} else {
  console.log('\nPASS: no console errors (warnings may exist above).');
  process.exit(0);
}
