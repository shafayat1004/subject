#!/usr/bin/env node
/**
 * Interactive full-gallery audit for Android (Appium + shared interaction recipes).
 *
 * Prerequisites:
 *   - Metro on :8081, app on emulator (`npx react-native run-android`)
 *   - Appium 2 on :4723 (`appium`)
 *   - adb device connected, `adb reverse tcp:8081 tcp:8081`
 *   - Rebuild native after ComponentSample testID change (`../eggshell dev-native`)
 *
 * Shared recipes use testId-first selectors (audit-gallery-selectors.mjs) via audit-gallery-interactions.mjs.
 * Navigation: audit-gallery-nav-native.mjs (~eggshell-sidebar-menu, ~sidebar-component-*).
 *
 * Usage:
 *   node audit-gallery-interactive-android.mjs
 *   node audit-gallery-interactive-android.mjs --passes=1 --slow-mo=200
 *
 * Options:
 *   --passes=N           Full crawl repetitions (default: 2)
 *   --pause-ms=N         Wait after each navigation (default: 800)
 *   --slow-mo=N          Delay between actions (default: 120)
 *   --screenshots=all|failures|none  (default: all)
 *   --visual-archive=on|off          (default: on)
 *   --appium-host=HOST   (default: 127.0.0.1)
 *   --appium-port=N      (default: 4723)
 *   --launch-timeout-ms=N  Wait for Metro + first RN render (default: 120000)
 */
import { writeVisualArchiveReadme } from './audit-gallery-visual-archive.mjs';
import { mkdirSync, writeFileSync, appendFileSync } from 'fs';
import { join } from 'path';
import { interactWithComponent, COMPONENT_HANDLERS } from './audit-gallery-interactions.mjs';
import {
  discoverGalleryComponents,
  buildCoverageReport,
} from './audit-gallery-components.mjs';
import { listAssertionComponents } from './audit-gallery-assertions.mjs';
import { connectAndroidPage, disconnectAndroidPage } from './audit-gallery-android-driver.mjs';
import { navigateToComponent, sidebarLabelFor } from './audit-gallery-nav-native.mjs';
import {
  LogcatCapture,
  assertAdbReady,
} from './audit-gallery-android-logcat.mjs';
import { isActionable, textIsDevNoise } from './audit-gallery-classify.mjs';
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

const passCount = Number(flags.passes ?? 2);
const pauseMs = Number(flags['pause-ms'] ?? 800);
const slowMo = Number(flags['slow-mo'] ?? 120);
const screenshotMode = flags.screenshots ?? 'all';
const visualArchiveEnabled = flags['visual-archive'] !== 'off';
const appiumHost = flags['appium-host'] ?? '127.0.0.1';
const appiumPort = Number(flags['appium-port'] ?? 4723);
const launchTimeoutMs = Number(flags['launch-timeout-ms'] ?? 120_000);

if (!['all', 'failures', 'none'].includes(screenshotMode)) {
  console.error(`Invalid --screenshots=${screenshotMode}; use all, failures, or none`);
  process.exit(1);
}

const stamp = new Date().toISOString().replace(/[:.]/g, '-');
const outRoot = join(process.cwd(), 'audit-android', 'interactive', stamp);
mkdirSync(outRoot, { recursive: true });

const discoveredComponents = discoverGalleryComponents();
const coverageAtStart = buildCoverageReport(discoveredComponents, discoveredComponents, {
  interactionHandlers: COMPONENT_HANDLERS,
  assertionHandlers: Object.fromEntries(listAssertionComponents().map((k) => [k, true])),
});
writeFileSync(join(outRoot, 'coverage-report.json'), JSON.stringify(coverageAtStart, null, 2));

const COMPONENTS = discoveredComponents.filter((name) => sidebarLabelFor(name));

function ts() {
  return new Date().toISOString();
}

function renderPassMarkdown(summary) {
  let md = `# Android interactive audit pass ${summary.passIndex}\n\n`;
  md += `Platform: Android (Appium)\n\n`;
  md += `Finished: ${summary.finishedAt}\n\n`;
  md += `Logcat events: ${summary.totalConsoleEvents}\n\n`;

  if (summary.pagesWithLoadErrors.length) {
    md += `## Navigation failures\n\n`;
    for (const p of summary.pagesWithLoadErrors) {
      md += `- **${p.component}**: ${p.loadError}\n`;
    }
    md += `\n`;
  }

  if (summary.pagesWithFailedAssertions?.length) {
    md += `## Failed UI assertions\n\n`;
    for (const p of summary.pagesWithFailedAssertions) {
      md += `### ${p.component}\n\n`;
      for (const a of p.failedAssertions) {
        md += `- **${a.name}**: ${a.message}`;
        if (a.screenshotPath) md += ` — \`${a.screenshotPath}\``;
        md += `\n`;
      }
      md += `\n`;
    }
  }

  if (summary.pagesWithActionable.length) {
    md += `## Actionable logcat issues\n\n`;
    for (const p of summary.pagesWithActionable) {
      md += `### ${p.component}\n\n`;
      for (const a of p.actionable) md += `- \`${a.classify}\`: ${a.text}\n`;
      md += `\n`;
    }
  } else {
    md += `## Actionable logcat issues\n\nNone.\n\n`;
  }

  md += `## All pages\n\n| Component | Label | Actions | Asserts | Failed | Logcat | Actionable | Uncaught |\n`;
  md += `|-----------|-------|---------|---------|--------|--------|------------|----------|\n`;
  for (const r of summary.results) {
    md += `| ${r.component} | ${r.sidebarLabel ?? ''} | ${r.interactionActions ?? 0} | ${r.assertionCount ?? 0} | ${r.failedAssertionCount ?? 0} | ${r.consoleCount} | ${r.actionableCount} | ${r.uncaughtCount} |\n`;
  }
  return md;
}

/**
 * @param {import('./audit-gallery-android-driver.mjs').AndroidPage} page
 * @param {LogcatCapture} logcat
 * @param {number} passIndex
 */
async function auditPass(page, logcat, passIndex) {
  const passDir = join(outRoot, `pass-${passIndex}`);
  mkdirSync(passDir, { recursive: true });
  const interactionsLogPath = join(passDir, 'interactions.log');
  const assertionsLogPath = join(passDir, 'assertions.log');
  mkdirSync(join(passDir, 'screenshots'), { recursive: true });
  mkdirSync(join(passDir, 'pages'), { recursive: true });
  if (visualArchiveEnabled) {
    writeVisualArchiveReadme(passDir, passIndex, 'android://com.eggshell.appgallery');
  }

  writeFileSync(
    join(passDir, 'meta.json'),
    JSON.stringify(
      {
        platform: PLATFORM.ANDROID,
        passIndex,
        startedAt: ts(),
        slowMo,
        pauseMs,
        screenshotMode,
        visualArchiveEnabled,
        appiumHost,
        appiumPort,
      },
      null,
      2
    )
  );

  console.log(`\n=== Android pass ${passIndex}/${passCount} ===\n`);

  const results = [];
  const allEntries = [];

  for (const name of COMPONENTS) {
    const sidebarLabel = sidebarLabelFor(name);
    const pageLogPath = join(passDir, 'pages', `${name}.log`);
    let loadError = null;
    let interactionError = null;
    let interactionActions = 0;
    let assertions = [];
    let failedAssertions = [];
    let visualArchiveImages = 0;

    await logcat.clearBuffer();

    try {
      await navigateToComponent(page, name, {
        pauseMs,
        log: (msg) => appendFileSync(interactionsLogPath, `[${ts()}] ${name}: ${msg}\n`),
      });
      if (slowMo > 0) await page.waitForTimeout(slowMo);

      try {
        const outcome = await interactWithComponent(
          page,
          name,
          (msg) => {
            appendFileSync(interactionsLogPath, `[${ts()}] ${name}: ${msg}\n`);
            if (msg.startsWith('ASSERT ') || msg.startsWith('visual archive:')) {
              appendFileSync(assertionsLogPath, `[${ts()}] ${name}: ${msg}\n`);
            }
          },
          {
            passDir,
            screenshotMode,
            visualArchive: visualArchiveEnabled,
            passIndex,
            baseUrl: 'android://com.eggshell.appgallery',
            url: `sidebar:${sidebarLabel}`,
            platform: PLATFORM.ANDROID,
          }
        );
        interactionActions = outcome.actionCount;
        assertions = outcome.assertions ?? [];
        visualArchiveImages = outcome.visualArchive?.images?.length ?? 0;
        failedAssertions = assertions.filter((a) => !a.passed && !a.reviewOnly);
      } catch (e) {
        interactionError = String(e.message ?? e);
      }

      await page.waitForTimeout(400);
    } catch (e) {
      loadError = String(e.message ?? e);
    }

    const pageEntries = await logcat.collectForComponent(name, pageLogPath);
    allEntries.push(...pageEntries);

    const actionable = pageEntries.filter(
      (e) => isActionable(e.classify, e.type, e.text) && !textIsDevNoise(e.classify, e.text)
    );

    const uncaught = pageEntries.filter((e) => e.source === 'pageerror');

    const result = {
      component: name,
      sidebarLabel,
      loadError,
      interactionError,
      interactionActions,
      assertionCount: assertions.length,
      failedAssertionCount: failedAssertions.length,
      visualArchiveImages,
      failedAssertions: failedAssertions.map((a) => ({
        name: a.name,
        message: a.message,
        screenshotPath: a.screenshotPath,
      })),
      consoleCount: pageEntries.length,
      actionableCount: actionable.length,
      uncaughtCount: uncaught.length,
      actionable: actionable.map((e) => ({ classify: e.classify, text: e.text.slice(0, 300) })),
    };
    results.push(result);

    const flag = loadError
      ? 'NAV FAIL'
      : failedAssertions.length
        ? `ASSERT:${failedAssertions.length}`
        : actionable.length
          ? `ACTIONABLE:${actionable.length}`
          : `ok (${interactionActions} actions, ${assertions.length} asserts)`;
    console.log(`  [pass ${passIndex}] ${name.padEnd(28)} ${flag}`);
  }

  const summary = {
    platform: PLATFORM.ANDROID,
    passIndex,
    finishedAt: ts(),
    pageCount: COMPONENTS.length,
    totalConsoleEvents: allEntries.length,
    pagesWithActionable: results.filter((r) => r.actionableCount > 0),
    pagesWithLoadErrors: results.filter((r) => r.loadError),
    pagesWithUncaught: results.filter((r) => r.uncaughtCount > 0),
    pagesWithFailedAssertions: results.filter((r) => r.failedAssertionCount > 0),
    results,
  };

  writeFileSync(
    join(passDir, 'assertions-summary.json'),
    JSON.stringify(
      results.map((r) => ({
        component: r.component,
        assertionCount: r.assertionCount,
        failedAssertionCount: r.failedAssertionCount,
        failedAssertions: r.failedAssertions,
      })),
      null,
      2
    )
  );

  writeFileSync(join(passDir, 'summary.json'), JSON.stringify(summary, null, 2));
  writeFileSync(join(passDir, 'summary.md'), renderPassMarkdown(summary));

  return summary;
}

function comparePasses(s1, s2) {
  const cmp = {
    pass1Actionable: s1.pagesWithActionable.length,
    pass2Actionable: s2.pagesWithActionable.length,
    onlyInPass1: [],
    onlyInPass2: [],
    inBoth: [],
  };
  const m1 = new Map(s1.pagesWithActionable.map((p) => [p.component, p]));
  const m2 = new Map(s2.pagesWithActionable.map((p) => [p.component, p]));
  for (const [comp] of m1) {
    if (m2.has(comp)) cmp.inBoth.push(comp);
    else cmp.onlyInPass1.push(comp);
  }
  for (const comp of m2.keys()) {
    if (!m1.has(comp)) cmp.onlyInPass2.push(comp);
  }
  return cmp;
}

console.log('Interactive Android gallery audit');
console.log(`  Appium:   ${appiumHost}:${appiumPort}`);
console.log(`  Passes:   ${passCount}`);
console.log(`  SlowMo:   ${slowMo}ms`);
console.log(`  Screenshots: ${screenshotMode}`);
console.log(`  Visual archive: ${visualArchiveEnabled ? 'on' : 'off'}`);
console.log(`  Launch wait: ${launchTimeoutMs}ms`);
console.log(`  Output:   ${outRoot}`);

await assertAdbReady();

const page = await connectAndroidPage({
  appiumHost,
  appiumPort,
  launchTimeoutMs,
  log: (msg) => console.log(`  [connect] ${msg}`),
});
const logcat = new LogcatCapture({ passDir: join(outRoot, 'pass-0') });
logcat.start();

const passSummaries = [];
try {
  for (let i = 1; i <= passCount; i++) {
    const passDir = join(outRoot, `pass-${i}`);
    logcat.passDir = passDir;
    mkdirSync(passDir, { recursive: true });
    passSummaries.push(await auditPass(page, logcat, i));
  }
} finally {
  logcat.stop();
  await disconnectAndroidPage(page);
}

const finalReport = {
  platform: PLATFORM.ANDROID,
  auditedAt: ts(),
  passCount,
  slowMo,
  outputDir: outRoot,
  appium: { host: appiumHost, port: appiumPort },
  passes: passSummaries.map((s) => ({
    passIndex: s.passIndex,
    totalConsoleEvents: s.totalConsoleEvents,
    actionablePages: s.pagesWithActionable.length,
    failedAssertionPages: s.pagesWithFailedAssertions?.length ?? 0,
    loadErrors: s.pagesWithLoadErrors.length,
  })),
};

if (passSummaries.length >= 2) {
  finalReport.passComparison = comparePasses(passSummaries[0], passSummaries[1]);
}

writeFileSync(join(outRoot, 'final-report.json'), JSON.stringify(finalReport, null, 2));

let finalMd = `# Android interactive gallery audit\n\n`;
finalMd += `Output: \`${outRoot}\`\n\n`;
finalMd += `| Pass | Logcat events | Actionable pages | Failed asserts | Nav errors |\n`;
finalMd += `|------|---------------|------------------|----------------|------------|\n`;
for (const s of passSummaries) {
  finalMd += `| ${s.passIndex} | ${s.totalConsoleEvents} | ${s.pagesWithActionable.length} | ${s.pagesWithFailedAssertions?.length ?? 0} | ${s.pagesWithLoadErrors.length} |\n`;
}
if (finalReport.passComparison) {
  const c = finalReport.passComparison;
  finalMd += `\n## Pass 1 vs pass 2\n\n`;
  finalMd += `- Actionable pass 1: ${c.pass1Actionable}\n`;
  finalMd += `- Actionable pass 2: ${c.pass2Actionable}\n`;
  if (c.onlyInPass1.length) finalMd += `- Only in pass 1: ${c.onlyInPass1.join(', ')}\n`;
  if (c.onlyInPass2.length) finalMd += `- Only in pass 2: ${c.onlyInPass2.join(', ')}\n`;
}
writeFileSync(join(outRoot, 'final-report.md'), finalMd);

console.log(`\nDone. Reports in:\n  ${outRoot}\n`);
