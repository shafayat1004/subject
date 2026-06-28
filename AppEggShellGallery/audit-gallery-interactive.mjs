#!/usr/bin/env node
/**
 * Interactive full-gallery audit (headed by default).
 *
 * Visits every component page, runs page-specific interactions, captures all
 * console output + page errors, saves logs to disk, then repeats (2 passes).
 *
 * Usage:
 *   node audit-gallery-interactive.mjs [baseUrl]
 *   node audit-gallery-interactive.mjs http://127.0.0.1:8082
 *   node audit-gallery-interactive.mjs https://eggshell.dev --headless
 *
 * Options (env or argv):
 *   --headless       Run without visible browser (default: headed)
 *   --slow-mo=N      Ms delay between actions (default: 120)
 *   --passes=N       Number of full crawl passes (default: 2)
 *   --pause-ms=N     Wait after each page load (default: 800)
 *   --screenshots=all|failures|none  Screenshot per assertion (default: all)
 *   --visual-archive=on|off          Archival PNGs for human/AI review (default: on)
 */
import { writeVisualArchiveReadme } from './audit-gallery-visual-archive.mjs';
import { chromium } from 'playwright';
import { mkdirSync, writeFileSync, appendFileSync } from 'fs';
import { join } from 'path';
import { interactWithComponent, COMPONENT_HANDLERS } from './audit-gallery-interactions.mjs';
import {
  discoverGalleryComponents,
  buildCoverageReport,
} from './audit-gallery-components.mjs';
import { listAssertionComponents } from './audit-gallery-assertions.mjs';

const args = process.argv.slice(2);
const positional = args.filter((a) => !a.startsWith('--'));
const flags = Object.fromEntries(
  args
    .filter((a) => a.startsWith('--'))
    .map((a) => {
      const [k, v] = a.replace(/^--/, '').split('=');
      return [k, v ?? 'true'];
    })
);

const baseUrl = (positional[0] ?? 'http://127.0.0.1:8082').replace(/\/$/, '');
const headless = flags.headless === 'true';
const slowMo = Number(flags['slow-mo'] ?? 120);
const passCount = Number(flags.passes ?? 2);
const pauseMs = Number(flags['pause-ms'] ?? 800);
const screenshotMode = flags.screenshots ?? 'all';
const visualArchiveEnabled = flags['visual-archive'] !== 'off';
if (!['all', 'failures', 'none'].includes(screenshotMode)) {
  console.error(`Invalid --screenshots=${screenshotMode}; use all, failures, or none`);
  process.exit(1);
}

const stamp = new Date().toISOString().replace(/[:.]/g, '-');
const outRoot = join(process.cwd(), 'audit-browser', 'interactive', stamp);
mkdirSync(outRoot, { recursive: true });

const discoveredComponents = discoverGalleryComponents();
const coverageAtStart = buildCoverageReport(discoveredComponents, discoveredComponents, {
  interactionHandlers: COMPONENT_HANDLERS,
  assertionHandlers: Object.fromEntries(listAssertionComponents().map((k) => [k, true])),
});
writeFileSync(join(outRoot, 'coverage-report.json'), JSON.stringify(coverageAtStart, null, 2));

const COMPONENTS = discoveredComponents;

function componentPath(name) {
  const desktop = encodeURIComponent(JSON.stringify('Desktop'));
  const comp = encodeURIComponent(JSON.stringify(name));
  return `${baseUrl}/${desktop}/Components/${comp}`;
}

function ts() {
  return new Date().toISOString();
}

function formatLogLine(entry) {
  return `[${entry.at}] [${entry.source}] [${entry.type}] ${entry.text}`;
}

function classify(text, type) {
  const t = text.toLowerCase();
  if (type === 'pageerror') return 'uncaught';
  if (t.includes('objects are not valid as a react child')) return 'react-child';
  if (t.includes('validatedomnesting')) return 'dom-nesting';
  if (t.includes('invalidvalueerror') || t.includes('typeerror') || t.includes('referenceerror')) return 'runtime';
  if (t.includes('minified react error') || /error #\d+/.test(t)) return 'react-minified';
  if (type === 'error') return 'console-error';
  if (type === 'warning') return 'console-warning';
  if (type === 'info') return 'info';
  return 'log';
}

function isActionable(classifyTag, type, text = '') {
  if (type === 'pageerror' || classifyTag === 'uncaught') return true;
  if (['react-child', 'dom-nesting', 'runtime', 'react-minified'].includes(classifyTag)) return true;
  if (classifyTag === 'console-error' && !textIsDevNoise(classifyTag, text)) return true;
  return false;
}

function textIsDevNoise(_, text) {
  const t = text.toLowerCase();
  return (
    t.includes('legacy childcontexttypes') ||
    t.includes('finddomnode is deprecated') ||
    t.includes('unique "key" prop') ||
    t.includes('possible style leak') ||
    t.includes('react router future flag') ||
    t.includes('[hmr]') ||
    t.includes('webpack-dev-server') ||
    t.includes('[consolestelemetrysink]') ||
    t.includes('screenview:')
  );
}

function attachConsole(page, component, passDir, entries, maxEntries = 1000) {
  const pageLogPath = join(passDir, 'pages', `${component}.log`);
  mkdirSync(join(passDir, 'pages'), { recursive: true });

  let pageCount = 0;
  let capped = false;

  const onConsole = (msg) => {
    if (capped) return;
    if (pageCount >= maxEntries) {
      capped = true;
      const entry = {
        at: ts(),
        component,
        source: 'console',
        type: 'warning',
        text: `[audit] Console log cap (${maxEntries}) reached for ${component}; further messages omitted`,
        classify: 'console-cap',
      };
      entries.push(entry);
      appendFileSync(pageLogPath, formatLogLine(entry) + '\n');
      appendFileSync(join(passDir, 'console-full.log'), formatLogLine(entry) + '\n');
      return;
    }
    pageCount += 1;
    const entry = {
      at: ts(),
      component,
      source: 'console',
      type: msg.type(),
      text: msg.text(),
      classify: classify(msg.text(), msg.type()),
    };
    entries.push(entry);
    appendFileSync(pageLogPath, formatLogLine(entry) + '\n');
    appendFileSync(join(passDir, 'console-full.log'), formatLogLine(entry) + '\n');
  };

  const onPageError = (err) => {
    if (capped) return;
    if (pageCount >= maxEntries) {
      capped = true;
      return;
    }
    pageCount += 1;
    const entry = {
      at: ts(),
      component,
      source: 'pageerror',
      type: 'pageerror',
      text: err.message,
      classify: classify(err.message, 'pageerror'),
    };
    entries.push(entry);
    appendFileSync(pageLogPath, formatLogLine(entry) + '\n');
    appendFileSync(join(passDir, 'console-full.log'), formatLogLine(entry) + '\n');
  };

  page.on('console', onConsole);
  page.on('pageerror', onPageError);

  return () => {
    page.off('console', onConsole);
    page.off('pageerror', onPageError);
  };
}

async function auditPass(browser, passIndex) {
  const passDir = join(outRoot, `pass-${passIndex}`);
  mkdirSync(passDir, { recursive: true });
  const interactionsLogPath = join(passDir, 'interactions.log');
  const assertionsLogPath = join(passDir, 'assertions.log');
  mkdirSync(join(passDir, 'screenshots'), { recursive: true });
  if (visualArchiveEnabled) {
    writeVisualArchiveReadme(passDir, passIndex, baseUrl);
  }

  const context = await browser.newContext({
    viewport: { width: 1400, height: 900 },
  });
  const page = await context.newPage();
  const results = [];
  const allEntries = [];
  const nativeDialogs = [];

  page.on('dialog', async (dialog) => {
    const entry = `[${ts()}] native-${dialog.type()}: ${dialog.message()}`;
    nativeDialogs.push(entry);
    appendFileSync(join(passDir, 'native-dialogs.log'), entry + '\n');
    try {
      await dialog.accept();
    } catch {
      await dialog.dismiss().catch(() => {});
    }
  });

  writeFileSync(
    join(passDir, 'meta.json'),
    JSON.stringify({ baseUrl, passIndex, startedAt: ts(), headless, slowMo, screenshotMode, visualArchiveEnabled }, null, 2)
  );

  console.log(`\n=== Pass ${passIndex}/${passCount} — ${baseUrl} (${headless ? 'headless' : 'headed'}) ===\n`);

  for (const name of COMPONENTS) {
    const url = componentPath(name);
    const entriesBefore = allEntries.length;
    const detach = attachConsole(page, name, passDir, allEntries);

    let httpStatus = 0;
    let loadError = null;
    let interactionError = null;
    let interactionActions = 0;
    let assertions = [];
    let failedAssertions = [];
    let reviewItems = [];
    let visualArchiveImages = 0;

    try {
      const resp = await page.goto(url, { waitUntil: 'networkidle', timeout: 90000 });
      httpStatus = resp?.status() ?? 0;
      await page.waitForTimeout(pauseMs);

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
            baseUrl,
            url,
          }
        );
        interactionActions = outcome.actionCount;
        assertions = outcome.assertions ?? [];
        visualArchiveImages = outcome.visualArchive?.images?.length ?? 0;
        failedAssertions = assertions.filter((a) => !a.passed && !a.reviewOnly);
        reviewItems = assertions.filter((a) => a.reviewOnly);
      } catch (e) {
        interactionError = String(e.message ?? e);
      }

      await page.waitForTimeout(400);
    } catch (e) {
      loadError = String(e.message ?? e);
    } finally {
      detach();
    }

    const pageEntries = allEntries.slice(entriesBefore);
    const actionable = pageEntries.filter((e) => isActionable(e.classify, e.type, e.text) && !textIsDevNoise(e.classify, e.text));
    const uncaught = pageEntries.filter((e) => e.source === 'pageerror');

    const result = {
      component: name,
      url,
      httpStatus,
      loadError,
      interactionError,
      interactionActions,
      assertionCount: assertions.length,
      failedAssertionCount: failedAssertions.length,
      reviewCount: reviewItems.length,
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
      ? 'LOAD FAIL'
      : failedAssertions.length
        ? `ASSERT:${failedAssertions.length}`
        : (reviewItems?.length ?? 0)
          ? `REVIEW:${reviewItems.length}`
          : actionable.length
            ? `ACTIONABLE:${actionable.length}`
            : `ok (${interactionActions} actions, ${assertions.length} asserts)`;
    console.log(`  [pass ${passIndex}] ${name.padEnd(28)} ${flag}`);
  }

  await context.close();

  const summary = {
    baseUrl,
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

  writeFileSync(join(passDir, 'assertions-summary.json'), JSON.stringify(
    results.map((r) => ({
      component: r.component,
      assertionCount: r.assertionCount,
      failedAssertionCount: r.failedAssertionCount,
      failedAssertions: r.failedAssertions,
    })),
    null,
    2
  ));

  writeFileSync(join(passDir, 'summary.json'), JSON.stringify(summary, null, 2));
  writeFileSync(join(passDir, 'summary.md'), renderPassMarkdown(summary));

  return summary;
}

function renderPassMarkdown(summary) {
  let md = `# Interactive audit pass ${summary.passIndex}\n\n`;
  md += `Base URL: ${summary.baseUrl}\n\n`;
  md += `Finished: ${summary.finishedAt}\n\n`;
  md += `Console events: ${summary.totalConsoleEvents}\n\n`;

  if (summary.pagesWithLoadErrors.length) {
    md += `## Load failures\n\n`;
    for (const p of summary.pagesWithLoadErrors) md += `- **${p.component}**: ${p.loadError}\n`;
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
    md += `## Actionable issues\n\n`;
    for (const p of summary.pagesWithActionable) {
      md += `### ${p.component}\n\n`;
      for (const a of p.actionable) md += `- \`${a.classify}\`: ${a.text}\n`;
      md += `\n`;
    }
  } else {
    md += `## Actionable issues\n\nNone.\n\n`;
  }

  md += `## All pages\n\n| Component | HTTP | Actions | Asserts | Failed | Console | Actionable | Uncaught |\n`;
  md += `|-----------|------|---------|---------|--------|---------|------------|----------|\n`;
  for (const r of summary.results) {
    md += `| ${r.component} | ${r.httpStatus} | ${r.interactionActions ?? 0} | ${r.assertionCount ?? 0} | ${r.failedAssertionCount ?? 0} | ${r.consoleCount} | ${r.actionableCount} | ${r.uncaughtCount} |\n`;
  }
  return md;
}

function comparePasses(s1, s2) {
  const cmp = {
    pass1Actionable: s1.pagesWithActionable.length,
    pass2Actionable: s2.pagesWithActionable.length,
    onlyInPass1: [],
    onlyInPass2: [],
    inBoth: [],
  };

  const key = (p) => `${p.component}::${p.actionable.map((a) => a.classify + ':' + a.text).join('|')}`;
  const m1 = new Map(s1.pagesWithActionable.map((p) => [p.component, p]));
  const m2 = new Map(s2.pagesWithActionable.map((p) => [p.component, p]));

  for (const [comp, p] of m1) {
    if (m2.has(comp)) cmp.inBoth.push(comp);
    else cmp.onlyInPass1.push(comp);
  }
  for (const comp of m2.keys()) {
    if (!m1.has(comp)) cmp.onlyInPass2.push(comp);
  }

  return cmp;
}

console.log(`Interactive gallery audit`);
console.log(`  URL:      ${baseUrl}`);
console.log(`  Headed:   ${!headless}`);
console.log(`  SlowMo:   ${slowMo}ms`);
console.log(`  Passes:   ${passCount}`);
console.log(`  Screenshots: ${screenshotMode}`);
console.log(`  Visual archive: ${visualArchiveEnabled ? 'on' : 'off'}`);
console.log(`  Output:   ${outRoot}`);

const browser = await chromium.launch({
  headless,
  slowMo,
  devtools: false,
});

const passSummaries = [];
for (let i = 1; i <= passCount; i++) {
  passSummaries.push(await auditPass(browser, i));
}

await browser.close();

const finalReport = {
  baseUrl,
  auditedAt: ts(),
  passCount,
  headless,
  slowMo,
  outputDir: outRoot,
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

let finalMd = `# Interactive gallery audit\n\n`;
finalMd += `Output: \`${outRoot}\`\n\n`;
finalMd += `| Pass | Console events | Actionable pages | Failed asserts | Load errors |\n`;
finalMd += `|------|----------------|------------------|----------------|-------------|\n`;
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
  if (c.inBoth.length) finalMd += `- In both passes: ${c.inBoth.join(', ')}\n`;
  if (!c.onlyInPass1.length && !c.onlyInPass2.length && !c.pass1Actionable && !c.pass2Actionable) {
    finalMd += `\nBoth passes clean.\n`;
  }
}
writeFileSync(join(outRoot, 'final-report.md'), finalMd);

console.log(`\nDone. Logs and reports in:\n  ${outRoot}\n`);
console.log(`  pass-1/visual-archive/`);
console.log(`  pass-2/visual-archive/`);
console.log(`  final-report.md`);
