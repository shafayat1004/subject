#!/usr/bin/env node
/**
 * Full gallery audit: visit every component page, classify console output,
 * report actionable issues + deduplicated style-leak tracking.
 */
import { chromium } from 'playwright';
import { mkdirSync, writeFileSync } from 'fs';
import { join } from 'path';
import { discoverGalleryComponents } from './audit-gallery-components.mjs';
import { classifyForFullAudit } from './audit-gallery-classify.mjs';
import {
  aggregateStyleLeaks,
  createStyleLeakTracker,
  formatStyleLeakSummaryLine,
  parseStyleLeak,
} from './audit-gallery-style-leaks.mjs';

const baseUrl = (process.argv[2] ?? 'http://127.0.0.1:8082').replace(/\/$/, '');
const isProd = baseUrl.includes('eggshell.dev');
const outDir = join(process.cwd(), 'audit-browser', isProd ? 'prod' : 'local');
mkdirSync(outDir, { recursive: true });

const components = discoverGalleryComponents();

function componentPath(name) {
  const desktop = encodeURIComponent(JSON.stringify('Desktop'));
  const comp = encodeURIComponent(JSON.stringify(name));
  return `${baseUrl}/${desktop}/Components/${comp}`;
}

async function auditComponent(browser, name) {
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await context.newPage();
  const url = componentPath(name);

  const raw = [];
  const styleLeakTracker = createStyleLeakTracker();

  page.on('console', (msg) => {
    if (msg.type() === 'debug' || msg.type() === 'log') return;
    const text = msg.text();
    const leak = parseStyleLeak(text);
    if (leak) {
      const { isNew } = styleLeakTracker.record(leak);
      if (isNew) {
        raw.push({ type: msg.type(), text, bucket: 'style-leak', kind: 'style-leak', summary: leak.sample });
      }
      return;
    }
    raw.push({ type: msg.type(), text, ...classifyForFullAudit(text, msg.type()) });
  });
  page.on('pageerror', (err) => {
    raw.push({ type: 'pageerror', text: err.message, ...classifyForFullAudit(err.message, 'pageerror') });
  });

  let httpStatus = 0;
  let loadError = null;
  try {
    const resp = await page.goto(url, { waitUntil: 'networkidle', timeout: 60000 });
    httpStatus = resp?.status() ?? 0;
    await page.waitForTimeout(1200);

    if (name === 'QueryGrid') {
      const submit = page.getByRole('button', { name: 'Submit' });
      if (await submit.count()) {
        await submit.click();
        await page.waitForTimeout(2500);
      }
    }

    // Dialogs page: open first dialog trigger if present
    if (name === 'Dialogs') {
      const btn = page.getByRole('button').first();
      if (await btn.count()) {
        await btn.click().catch(() => {});
        await page.waitForTimeout(800);
      }
    }
  } catch (e) {
    loadError = String(e.message ?? e);
  }

  const classified = raw;
  const actionable = classified.filter((e) => e.bucket === 'actionable');
  const noise = classified.filter((e) => e.bucket === 'noise');
  const styleLeaks = styleLeakTracker.summary();

  await context.close();

  return {
    component: name,
    url,
    httpStatus,
    loadError,
    actionable,
    actionableCount: actionable.length,
    noiseCount: noise.length,
    noiseKinds: [...new Set(noise.map((e) => e.kind))],
    styleLeaks,
  };
}

function dedupeActionable(items) {
  const map = new Map();
  for (const item of items) {
    const key = `${item.kind}::${item.summary}`;
    if (!map.has(key)) map.set(key, { ...item, components: new Set(), count: 0 });
    const entry = map.get(key);
    entry.count++;
  }
  return map;
}

const browser = await chromium.launch({ headless: true });
const results = [];

console.log(`Auditing ${components.length} pages on ${baseUrl} ...\n`);

for (const name of components) {
  const r = await auditComponent(browser, name);
  results.push(r);
  let flag = r.loadError ? 'LOAD FAIL' : r.actionableCount ? `ACTIONABLE: ${r.actionableCount}` : 'ok';
  if (r.styleLeaks.uniqueCount) {
    flag = r.actionableCount ? `${flag} + STYLE-LEAK:${r.styleLeaks.uniqueCount}` : `STYLE-LEAK:${r.styleLeaks.uniqueCount} (${r.styleLeaks.totalCount} hits)`;
  }
  console.log(`${name.padEnd(28)} ${flag}`);
}

await browser.close();

// Aggregate actionable across pages
const allActionable = [];
for (const r of results) {
  for (const a of r.actionable) {
    allActionable.push({ component: r.component, ...a });
  }
}

const byKind = new Map();
for (const a of allActionable) {
  const key = a.kind;
  if (!byKind.has(key)) byKind.set(key, []);
  byKind.get(key).push(a);
}

const styleLeakRollup = aggregateStyleLeaks(results);

const report = {
  baseUrl,
  auditedAt: new Date().toISOString(),
  pageCount: components.length,
  pagesWithStyleLeaks: results.filter((r) => r.styleLeaks.uniqueCount > 0).map((r) => ({
    component: r.component,
    summary: formatStyleLeakSummaryLine(r.styleLeaks, r.component),
    styleLeaks: r.styleLeaks,
  })),
  styleLeakRollup,
  pagesWithActionable: results.filter((r) => r.actionableCount > 0).map((r) => ({
    component: r.component,
    count: r.actionableCount,
    issues: r.actionable.map((a) => ({ kind: a.kind, summary: a.summary })),
  })),
  pagesWithLoadErrors: results.filter((r) => r.loadError).map((r) => ({ component: r.component, error: r.loadError })),
  actionableByKind: Object.fromEntries(
    [...byKind.entries()].map(([kind, items]) => [
      kind,
      {
        occurrenceCount: items.length,
        components: [...new Set(items.map((i) => i.component))],
        samples: [...new Map(items.map((i) => [i.summary, i.component])).entries()].slice(0, 5).map(([summary, component]) => ({ component, summary })),
      },
    ])
  ),
  fullResults: results.map((r) => ({
    component: r.component,
    httpStatus: r.httpStatus,
    loadError: r.loadError,
    actionableCount: r.actionableCount,
    noiseCount: r.noiseCount,
    noiseKinds: r.noiseKinds,
    styleLeaks: r.styleLeaks,
    actionable: r.actionable.map((a) => ({ kind: a.kind, summary: a.summary })),
  })),
};

const outFile = join(outDir, 'full-audit.json');
const mdFile = join(outDir, 'full-audit.md');
const styleLeaksFile = join(outDir, 'style-leaks.json');
writeFileSync(outFile, JSON.stringify(report, null, 2));
writeFileSync(styleLeaksFile, JSON.stringify(styleLeakRollup, null, 2));

let md = `# Gallery audit — ${baseUrl}\n\n`;
md += `Date: ${report.auditedAt}\n\n`;
md += `Pages: ${report.pageCount}\n\n`;

if (report.pagesWithLoadErrors.length) {
  md += `## Load failures\n\n`;
  for (const p of report.pagesWithLoadErrors) md += `- **${p.component}**: ${p.error}\n`;
  md += `\n`;
}

const kinds = Object.keys(report.actionableByKind);
if (!kinds.length) {
  md += `## Actionable issues\n\nNone.\n`;
} else {
  md += `## Actionable issues (by kind)\n\n`;
  for (const kind of kinds) {
    const k = report.actionableByKind[kind];
    md += `### ${kind} (${k.occurrenceCount} hits, ${k.components.length} pages)\n\n`;
    md += `Pages: ${k.components.join(', ')}\n\n`;
    for (const s of k.samples) md += `- **${s.component}**: ${s.summary}\n`;
    md += `\n`;
  }
}

if (report.pagesWithStyleLeaks.length) {
  md += `## Style leaks (deduplicated)\n\n`;
  for (const p of report.pagesWithStyleLeaks) {
    md += `- **${p.component}**: ${p.summary}\n`;
  }
  md += `\nSee \`style-leaks.json\` for cross-page keys.\n\n`;
}

md += `## Per-page summary\n\n`;
md += `| Component | HTTP | Actionable | Style leaks | Noise kinds |\n`;
md += `|-----------|------|------------|-------------|-------------|\n`;
for (const r of report.fullResults) {
  const leakNote = r.styleLeaks?.uniqueCount ? `${r.styleLeaks.uniqueCount} (${r.styleLeaks.totalCount} hits)` : '—';
  md += `| ${r.component} | ${r.httpStatus} | ${r.actionableCount} | ${leakNote} | ${r.noiseKinds.join(', ') || '—'} |\n`;
}

writeFileSync(mdFile, md);

console.log(`\n--- Actionable summary ---\n`);
if (!kinds.length) {
  console.log('No actionable console errors found.');
} else {
  for (const kind of kinds) {
    const k = report.actionableByKind[kind];
    console.log(`${kind}: ${k.occurrenceCount} hit(s) on ${k.components.join(', ')}`);
    for (const s of k.samples.slice(0, 2)) console.log(`  e.g. ${s.component}: ${s.summary.slice(0, 100)}`);
  }
}

console.log(`\nWrote ${outFile}`);
console.log(`Wrote ${styleLeaksFile}`);
console.log(`Wrote ${mdFile}`);
