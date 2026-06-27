#!/usr/bin/env node
/**
 * Full gallery audit: visit every component page, classify console output,
 * report actionable issues only (+ summary of dev noise).
 */
import { chromium } from 'playwright';
import { mkdirSync, writeFileSync } from 'fs';
import { join } from 'path';

const baseUrl = (process.argv[2] ?? 'http://127.0.0.1:8082').replace(/\/$/, '');
const isProd = baseUrl.includes('eggshell.dev');
const outDir = join(process.cwd(), 'audit-browser', isProd ? 'prod' : 'local');
mkdirSync(outDir, { recursive: true });

const components = [
  'Index', 'Layout_Row', 'Layout_Column', 'Layout_Sized', 'Layout_Constrained',
  'Buttons', 'Button', 'IconButton', 'FloatingActionButton', 'TextButton', 'ToggleButtons',
  'Forms', 'Input_Checkbox', 'Input_ChoiceList', 'Input_Date', 'Input_DayOfTheWeek',
  'Input_Decimal', 'Input_Duration', 'Input_EmailAddress', 'Input_LocalTime',
  'Input_File', 'Input_Image', 'Input_Picker', 'Input_PhoneNumber', 'Input_PositiveInteger',
  'Input_PositiveDecimal', 'Input_Quantity', 'Input_Text', 'Input_UnsignedInteger', 'Input_UnsignedDecimal',
  'Card', 'Carousel', 'Dialogs', 'Draggable', 'ImageCard', 'InfoMessage', 'ItemList', 'Section_Padded', 'Tabs',
  'AnimatableImage', 'AnimatableText', 'AnimatableTextInput', 'AnimatableView',
  'Grid', 'QueryGrid',
  'Heading', 'Pre', 'Tag', 'TimeSpan', 'Timestamp',
  'Avatar', 'Icon', 'IconWithBadge', 'Thumb', 'Thumbs', 'Scrim', 'Stars',
  'ContextMenu', 'Sidebar', 'Nav_Top', 'Nav_Bottom',
  'ErrorBoundary', 'Executor_AlertErrors', 'AsyncData', 'WithContext', 'TriStateful', 'QuadStateful',
  'Responsive', 'InProgress', 'WithExecutor', 'WithDataFlowControl',
  'ThirdParty_Map', 'ThirdParty_Recharts',
  'DateSelector', 'TouchableOpacity',
];

function componentPath(name) {
  const desktop = encodeURIComponent(JSON.stringify('Desktop'));
  const comp = encodeURIComponent(JSON.stringify(name));
  return `${baseUrl}/${desktop}/Components/${comp}`;
}

function normalize(text) {
  return text
    .replace(/webpack-internal:\/\/\/\S+/g, '<bundle>')
    .replace(/https?:\/\/[^\s)]+/g, '<url>')
    .replace(/\s+/g, ' ')
    .trim();
}

function classify(text, type) {
  const t = text.toLowerCase();
  const n = normalize(text);

  // Uncaught / hard failures
  if (type === 'pageerror') return { bucket: 'actionable', kind: 'uncaught-exception', summary: n.slice(0, 200) };
  if (t.includes('objects are not valid as a react child')) return { bucket: 'actionable', kind: 'invalid-react-child', summary: n.slice(0, 200) };
  if (t.includes('validatereactnesting') || t.includes('validateomnesting')) return { bucket: 'actionable', kind: 'invalid-dom-nesting', summary: n.slice(0, 200) };
  if (t.includes('invalidvalueerror') || t.includes('typeerror') || t.includes('referenceerror')) return { bucket: 'actionable', kind: 'runtime-error', summary: n.slice(0, 200) };
  if (t.includes('minified react error') || /error #\d+/.test(t)) return { bucket: 'actionable', kind: 'react-minified-error', summary: n.slice(0, 200) };
  if (t.includes('failed to load') && t.includes('chunk')) return { bucket: 'actionable', kind: 'chunk-load-failure', summary: n.slice(0, 200) };

  // Dev noise (not actionable for gallery upgrade work)
  if (t.includes('legacy childcontexttypes') || t.includes('legacy contexttypes')) return { bucket: 'noise', kind: 'reactxp-legacy-context', summary: '' };
  if (t.includes('finddomnode is deprecated')) return { bucket: 'noise', kind: 'finddomnode-deprecated', summary: '' };
  if (t.includes('unique "key" prop')) return { bucket: 'noise', kind: 'missing-react-key', summary: n.slice(0, 120) };
  if (t.includes('possible style leak')) return { bucket: 'noise', kind: 'style-leak', summary: n.slice(0, 120) };
  if (t.includes('react router future flag')) return { bucket: 'noise', kind: 'react-router-future', summary: '' };
  if (t.includes('[hmr]') || t.includes('webpack-dev-server')) return { bucket: 'noise', kind: 'webpack-hmr', summary: '' };
  if (t.includes('[consolestelemetrysink]') || t.includes('screenview:')) return { bucket: 'noise', kind: 'telemetry', summary: '' };
  if (t.includes('disallowed rule') && t.includes('filtered out')) return { bucket: 'noise', kind: 'filtered-css-rule', summary: n.slice(0, 120) };

  if (type === 'error' && t.startsWith('warning:')) return { bucket: 'noise', kind: 'react-dev-warning', summary: n.slice(0, 120) };
  if (type === 'warning') return { bucket: 'noise', kind: 'browser-warning', summary: n.slice(0, 120) };

  return { bucket: 'other', kind: 'log', summary: n.slice(0, 120) };
}

async function auditComponent(browser, name) {
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await context.newPage();
  const url = componentPath(name);

  const raw = [];
  page.on('console', (msg) => {
    if (msg.type() === 'debug' || msg.type() === 'log') return;
    raw.push({ type: msg.type(), text: msg.text() });
  });
  page.on('pageerror', (err) => raw.push({ type: 'pageerror', text: err.message }));

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

  const classified = raw.map((e) => ({ ...e, ...classify(e.text, e.type) }));
  const actionable = classified.filter((e) => e.bucket === 'actionable');
  const noise = classified.filter((e) => e.bucket === 'noise');

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
  const flag = r.loadError ? 'LOAD FAIL' : r.actionableCount ? `ACTIONABLE: ${r.actionableCount}` : 'ok';
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

const report = {
  baseUrl,
  auditedAt: new Date().toISOString(),
  pageCount: components.length,
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
    actionable: r.actionable.map((a) => ({ kind: a.kind, summary: a.summary })),
  })),
};

const outFile = join(outDir, 'full-audit.json');
const mdFile = join(outDir, 'full-audit.md');
writeFileSync(outFile, JSON.stringify(report, null, 2));

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

md += `## Per-page summary\n\n`;
md += `| Component | HTTP | Actionable | Noise kinds |\n`;
md += `|-----------|------|------------|-------------|\n`;
for (const r of report.fullResults) {
  md += `| ${r.component} | ${r.httpStatus} | ${r.actionableCount} | ${r.noiseKinds.join(', ') || '—'} |\n`;
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
console.log(`Wrote ${mdFile}`);
