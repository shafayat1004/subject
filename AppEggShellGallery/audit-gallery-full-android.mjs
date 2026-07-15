#!/usr/bin/env node
/**
 * Fast Android gallery audit: navigate every component, capture logcat, classify errors.
 * No interactions beyond navigation + brief settle time.
 *
 * Prerequisites: same as audit-gallery-interactive-android.mjs
 */
import { mkdirSync, writeFileSync } from 'fs';
import { join } from 'path';
import { discoverGalleryComponents } from './audit-gallery-components.mjs';
import { connectAndroidPage, disconnectAndroidPage } from './audit-gallery-android-driver.mjs';
import { navigateToComponent, sidebarLabelFor } from './audit-gallery-nav-native.mjs';
import { clickLabelOrTestId } from './audit-gallery-selectors.mjs';
import { LogcatCapture, filterActionable, assertAdbReady } from './audit-gallery-android-logcat.mjs';
import { classifyForFullAudit } from './audit-gallery-classify.mjs';
import { PLATFORM } from './audit-gallery-platform.mjs';

const args = process.argv.slice(2);
const flags = Object.fromEntries(
  args
    .filter((a) => a.startsWith('--'))
    .map((a) => {
      const [k, v] = a.replace(/^--/, '').split('=');
      return [k, v ?? 'true'];
    })
);

const pauseMs = Number(flags['pause-ms'] ?? 1200);
const appiumHost = flags['appium-host'] ?? '127.0.0.1';
const appiumPort = Number(flags['appium-port'] ?? 4723);
const launchTimeoutMs = Number(flags['launch-timeout-ms'] ?? 120_000);

const outDir = join(process.cwd(), 'audit-android', 'local');
mkdirSync(outDir, { recursive: true });

const components = discoverGalleryComponents().filter((name) => sidebarLabelFor(name));

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {LogcatCapture} logcat
 * @param {string} name
 */
async function auditComponent(page, logcat, name) {
  const sidebarLabel = sidebarLabelFor(name);
  let loadError = null;
  const raw = [];

  await logcat.clearBuffer();

  try {
    await navigateToComponent(page, name, {
      pauseMs,
      log: (msg) => console.log(`  [${name}] ${msg}`),
    });

    if (name === 'QueryGrid') {
      await clickLabelOrTestId(page, { label: 'Submit', platform: PLATFORM.ANDROID, exact: true });
      await page.waitForTimeout(2500);
    }

    if (name === 'Dialogs') {
      await clickLabelOrTestId(page, { label: 'Alert', platform: PLATFORM.ANDROID, exact: true });
      await page.waitForTimeout(800);
      await page.keyboard.press('Escape').catch(() => {});
    }
  } catch (e) {
    loadError = String(e.message ?? e);
  }

  const pageLogPath = join(outDir, 'pages', `${name}.log`);
  mkdirSync(join(outDir, 'pages'), { recursive: true });
  const pageEntries = await logcat.collectForComponent(name, pageLogPath);
  for (const e of pageEntries) {
    raw.push({ type: e.type, text: e.text });
  }

  const classified = raw.map((e) => ({ ...e, ...classifyForFullAudit(e.text, e.type) }));
  const actionable = classified.filter((e) => e.bucket === 'actionable');
  const noise = classified.filter((e) => e.bucket === 'noise');

  return {
    component: name,
    sidebarLabel,
    loadError,
    actionable,
    actionableCount: actionable.length,
    noiseCount: noise.length,
    noiseKinds: [...new Set(noise.map((e) => e.kind))],
  };
}

console.log(`Android full audit — ${components.length} components`);
console.log(`  Appium: ${appiumHost}:${appiumPort}`);
console.log(`  Launch wait: ${launchTimeoutMs}ms`);
console.log(`  Output: ${outDir}\n`);

await assertAdbReady();

const page = await connectAndroidPage({
  appiumHost,
  appiumPort,
  launchTimeoutMs,
  log: (msg) => console.log(`  [connect] ${msg}`),
});
const logcat = new LogcatCapture({ passDir: outDir });
logcat.start();

const results = [];
try {
  for (const name of components) {
    const r = await auditComponent(page, logcat, name);
    results.push(r);
    const flag = r.loadError
      ? 'NAV FAIL'
      : r.actionableCount
        ? `ACTIONABLE:${r.actionableCount}`
        : 'ok';
    console.log(`  ${name.padEnd(28)} ${flag}`);
  }
} finally {
  logcat.stop();
  await disconnectAndroidPage(page);
}

const pagesWithActionable = results.filter((r) => r.actionableCount > 0);
const pagesWithLoadErrors = results.filter((r) => r.loadError);

const report = {
  platform: PLATFORM.ANDROID,
  auditedAt: new Date().toISOString(),
  componentCount: components.length,
  pagesWithActionable: pagesWithActionable.length,
  pagesWithLoadErrors: pagesWithLoadErrors.length,
  results,
};

writeFileSync(join(outDir, 'full-audit.json'), JSON.stringify(report, null, 2));

let md = `# Android full gallery audit\n\n`;
md += `Components: ${components.length}\n\n`;
md += `Actionable pages: ${pagesWithActionable.length}\n\n`;
md += `Navigation failures: ${pagesWithLoadErrors.length}\n\n`;

if (pagesWithLoadErrors.length) {
  md += `## Navigation failures\n\n`;
  for (const p of pagesWithLoadErrors) md += `- **${p.component}**: ${p.loadError}\n`;
  md += `\n`;
}

if (pagesWithActionable.length) {
  md += `## Actionable logcat\n\n`;
  for (const p of pagesWithActionable) {
    md += `### ${p.component}\n\n`;
    for (const a of p.actionable) md += `- \`${a.kind}\`: ${a.summary}\n`;
    md += `\n`;
  }
} else {
  md += `## Actionable logcat\n\nNone.\n\n`;
}

md += `## All pages\n\n| Component | Actionable | Noise kinds |\n`;
md += `|-----------|------------|-------------|\n`;
for (const r of results) {
  md += `| ${r.component} | ${r.actionableCount} | ${r.noiseKinds.join(', ') || '—'} |\n`;
}

writeFileSync(join(outDir, 'full-audit.md'), md);
console.log(`\nDone.\n  ${join(outDir, 'full-audit.json')}\n  ${join(outDir, 'full-audit.md')}\n`);
