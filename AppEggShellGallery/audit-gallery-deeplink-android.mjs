#!/usr/bin/env node
/**
 * Deep-link-based gallery audit: navigate every component via adb am start
 * (bypasses the GestureView-swallowed synthetic touches under RN Fabric),
 * capture logcat, and classify errors. Uses Appium only for element checks
 * (aesg-sample-visuals presence) and logcat capture.
 */
import { execSync, spawn } from 'child_process';
import { mkdirSync, writeFileSync } from 'fs';
import { join } from 'path';
import { discoverGalleryComponents } from './audit-gallery-components.mjs';
import { connectAndroidPage, disconnectAndroidPage } from './audit-gallery-android-driver.mjs';
import { LogcatCapture, assertAdbReady } from './audit-gallery-android-logcat.mjs';
import { classifyForFullAudit } from './audit-gallery-classify.mjs';
import { PLATFORM } from './audit-gallery-platform.mjs';

const DEVICE = 'bf08f3ed';
const PACKAGE = 'com.eggshell.appgallery';
const PAUSE_MS = 1500;
const outDir = join(process.cwd(), 'audit-android', 'deeplink');
mkdirSync(outDir, { recursive: true });
mkdirSync(join(outDir, 'pages'), { recursive: true });

const components = discoverGalleryComponents();

function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

function deepLinkTo(path) {
  execSync(
    `adb -s ${DEVICE} shell am start -a android.intent.action.VIEW -d "http://example.app/${path}" ${PACKAGE}`,
    { stdio: 'pipe', timeout: 10000 }
  );
}

async function auditComponent(page, logcat, name) {
  let loadError = null;
  const raw = [];

  await logcat.clearBuffer();

  try {
    deepLinkTo(`components/${name}`);
    // Wait for aesg-sample-visuals to appear (component page loaded)
    const deadline = Date.now() + 30_000;
    let found = false;
    while (Date.now() < deadline) {
      const els = await page.driver.$$('android=new UiSelector().resourceId("aesg-sample-visuals")');
      if (els.length > 0) { found = true; break; }
      await sleep(500);
    }
    if (!found && name !== 'Index') {
      loadError = 'aesg-sample-visuals not found within 30s';
    }
    await sleep(PAUSE_MS);
  } catch (e) {
    loadError = String(e.message ?? e);
  }

  const pageLogPath = join(outDir, 'pages', `${name}.log`);
  const pageEntries = await logcat.collectForComponent(name, pageLogPath);
  for (const e of pageEntries) raw.push({ type: e.type, text: e.text });

  const classified = raw.map((e) => ({ ...e, ...classifyForFullAudit(e.text, e.type) }));
  const actionable = classified.filter((e) => e.bucket === 'actionable');
  const noise = classified.filter((e) => e.bucket === 'noise');

  return {
    component: name,
    loadError,
    actionable,
    actionableCount: actionable.length,
    noiseCount: noise.length,
    noiseKinds: [...new Set(noise.map((e) => e.kind))],
  };
}

console.log(`Deep-link gallery audit — ${components.length} components`);
console.log(`  Device: ${DEVICE}`);
console.log(`  Output: ${outDir}\n`);

await assertAdbReady();

// Ensure adb reverse is set
execSync(`adb -s ${DEVICE} reverse tcp:8081 tcp:8081`, { stdio: 'pipe' });

console.log('Connecting to Appium...');
const page = await connectAndroidPage({
  launchTimeoutMs: 90_000,
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
      ? 'LOAD FAIL'
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
  method: 'deep-link',
  auditedAt: new Date().toISOString(),
  componentCount: components.length,
  pagesWithActionable: pagesWithActionable.length,
  pagesWithLoadErrors: pagesWithLoadErrors.length,
  results,
};

writeFileSync(join(outDir, 'full-audit.json'), JSON.stringify(report, null, 2));

let md = `# Deep-link gallery audit\n\n`;
md += `Components: ${components.length}\n\n`;
md += `Load failures: ${pagesWithLoadErrors.length}\n\n`;
md += `Actionable pages: ${pagesWithActionable.length}\n\n`;

if (pagesWithLoadErrors.length) {
  md += `## Load failures\n\n`;
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
