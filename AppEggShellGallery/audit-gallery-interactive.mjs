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
 *   --resume=DIR                     Continue an interrupted run (reads pass-N/checkpoint.json)
 *   --interaction-timeout-ms=N       Override per-page interaction wall clock (default 90s; AutoUi 45s)
 *   --only=NAME[,NAME]|PRESET        Subset of routes (presets: style-leak-fix, style-leak-high-value, style-leak-full, a11y)
 */
import { writeVisualArchiveReadme } from './audit-gallery-visual-archive.mjs';
import {
  createBrowserSession,
  isBrowserBlockedError,
  waitForGalleryReady,
  withBrowserRecovery,
} from './audit-gallery-browser-session.mjs';
import {
  detectAppCrashOverlay,
  escapeFromAppCrash,
  loadBlockedComponents,
  logAppCrash,
  pickSafeFallbackComponent,
  saveBlockedComponents,
  screenshotAppCrash,
} from './audit-gallery-app-crash.mjs';
import {
  InteractionTimeoutError,
  escapeFromStuckInteraction,
  interactionTimeoutMsFor,
  logInteractionTimeout,
  saveTrackedIssue,
  screenshotInteractionTimeout,
  withInteractionTimeout,
} from './audit-gallery-page-issues.mjs';
import { chromium } from 'playwright';
import { mkdirSync, writeFileSync, appendFileSync, readFileSync, existsSync } from 'fs';
import { join } from 'path';
import { interactWithComponent, COMPONENT_HANDLERS } from './audit-gallery-interactions.mjs';
import {
  discoverGalleryComponents,
  buildCoverageReport,
  resolveAuditComponentScope,
} from './audit-gallery-components.mjs';
import { listAssertionComponents } from './audit-gallery-assertions.mjs';
import { classifyTag, isActionable, textIsDevNoise } from './audit-gallery-classify.mjs';
import {
  aggregateStyleLeaks,
  createStyleLeakTracker,
  formatStyleLeakSummaryLine,
  parseStyleLeak,
} from './audit-gallery-style-leaks.mjs';

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
const resumeDir = flags.resume ?? null;
const interactionTimeoutOverride = flags['interaction-timeout-ms']
  ? Number(flags['interaction-timeout-ms'])
  : undefined;
const onlyScope = flags.only ?? null;
if (!['all', 'failures', 'none'].includes(screenshotMode)) {
  console.error(`Invalid --screenshots=${screenshotMode}; use all, failures, or none`);
  process.exit(1);
}

const stamp = new Date().toISOString().replace(/[:.]/g, '-');
const outRoot = resumeDir
  ? (resumeDir.startsWith('/') ? resumeDir : join(process.cwd(), resumeDir))
  : join(process.cwd(), 'audit-browser', 'interactive', stamp);
mkdirSync(outRoot, { recursive: true });

const discoveredComponents = discoverGalleryComponents();
if (!resumeDir) {
  const coverageAtStart = buildCoverageReport(discoveredComponents, discoveredComponents, {
    interactionHandlers: COMPONENT_HANDLERS,
    assertionHandlers: Object.fromEntries(listAssertionComponents().map((k) => [k, true])),
  });
  writeFileSync(join(outRoot, 'coverage-report.json'), JSON.stringify(coverageAtStart, null, 2));
}

const COMPONENTS = onlyScope
  ? resolveAuditComponentScope(onlyScope, discoveredComponents)
  : discoveredComponents;

/** @type {Set<string>} */
let blockedComponents = loadBlockedComponents(outRoot);

function componentPath(name) {
  const desktop = encodeURIComponent(JSON.stringify('Desktop'));
  const comp = encodeURIComponent(JSON.stringify(name));
  return `${baseUrl}/${desktop}/Components/${comp}`;
}

function ts() {
  return new Date().toISOString();
}

function formatLogLine(entry) {
  const tag = entry.classify ? ` [${entry.classify}]` : '';
  return `[${entry.at}] [${entry.source}] [${entry.type}]${tag} ${entry.text}`;
}

function attachConsole(page, component, passDir, entries, maxEntries = 1000) {
  const pageLogPath = join(passDir, 'pages', `${component}.log`);
  mkdirSync(join(passDir, 'pages'), { recursive: true });

  let pageCount = 0;
  let capped = false;
  const styleLeakTracker = createStyleLeakTracker();

  function writeEntry(entry, { countTowardCap = true } = {}) {
    if (capped) return;
    if (countTowardCap) {
      if (pageCount >= maxEntries) {
        capped = true;
        const capEntry = {
          at: ts(),
          component,
          source: 'console',
          type: 'warning',
          text: `[audit] Console log cap (${maxEntries}) reached for ${component}; further messages omitted`,
          classify: 'console-cap',
        };
        entries.push(capEntry);
        appendFileSync(pageLogPath, formatLogLine(capEntry) + '\n');
        appendFileSync(join(passDir, 'console-full.log'), formatLogLine(capEntry) + '\n');
        return;
      }
      pageCount += 1;
    }
    entries.push(entry);
    appendFileSync(pageLogPath, formatLogLine(entry) + '\n');
    appendFileSync(join(passDir, 'console-full.log'), formatLogLine(entry) + '\n');
  }

  const onConsole = (msg) => {
    if (capped) return;
    const text = msg.text();
    const leak = parseStyleLeak(text);
    if (leak) {
      const { isNew, count } = styleLeakTracker.record(leak);
      if (isNew) {
        writeEntry({
          at: ts(),
          component,
          source: 'console',
          type: msg.type(),
          text,
          classify: 'style-leak',
          styleLeakKey: leak.key,
          styleName: leak.styleName,
          sourceComponent: leak.sourceComponent,
        });
      } else if (count === 2 || count === 10 || count === 100 || count % 500 === 0) {
        writeEntry(
          {
            at: ts(),
            component,
            source: 'console',
            type: 'info',
            text: `[audit] style leak repeat ${leak.key} (${count} hits on ${component}; further repeats omitted from log)`,
            classify: 'style-leak-repeat',
            styleLeakKey: leak.key,
          },
          { countTowardCap: false }
        );
      }
      return;
    }

    writeEntry({
      at: ts(),
      component,
      source: 'console',
      type: msg.type(),
      text,
      classify: classifyTag(text, msg.type()),
    });
  };

  const onPageError = (err) => {
    if (capped) return;
    writeEntry({
      at: ts(),
      component,
      source: 'pageerror',
      type: 'pageerror',
      text: err.message,
      classify: classifyTag(err.message, 'pageerror'),
    });
  };

  page.on('console', onConsole);
  page.on('pageerror', onPageError);

  return {
    detach: () => {
      page.off('console', onConsole);
      page.off('pageerror', onPageError);
      const summary = styleLeakTracker.summary();
      if (summary.uniqueCount) {
        const line = formatStyleLeakSummaryLine(summary, component);
        const summaryEntry = {
          at: ts(),
          component,
          source: 'audit',
          type: 'info',
          text: `[audit] style leaks on ${component}: ${line}`,
          classify: 'style-leak-summary',
        };
        writeEntry(summaryEntry, { countTowardCap: false });
      }
    },
    getStats: () => ({
      capped,
      count: pageCount,
      styleLeaks: styleLeakTracker.summary(),
    }),
  };
}

function loadCheckpoint(passDir) {
  const path = join(passDir, 'checkpoint.json');
  if (!existsSync(path)) return null;
  try {
    return JSON.parse(readFileSync(path, 'utf8'));
  } catch {
    return null;
  }
}

function saveCheckpoint(passDir, data) {
  writeFileSync(join(passDir, 'checkpoint.json'), JSON.stringify(data, null, 2));
}

/**
 * Audit one gallery component page.
 * @param {import('playwright').Page} page
 */
async function auditOneComponent(page, name, passDir, passIndex, allEntries, outRoot) {
  const url = componentPath(name);
  const entriesBefore = allEntries.length;
  const interactionsLogPath = join(passDir, 'interactions.log');
  const assertionsLogPath = join(passDir, 'assertions.log');
  const detachConsole = attachConsole(page, name, passDir, allEntries);

  let httpStatus = 0;
  let loadError = null;
  let interactionError = null;
  let interactionTimeout = null;
  let consoleCapHit = false;
  let styleLeaks = { uniqueCount: 0, totalCount: 0, leaks: [] };
  let interactionActions = 0;
  let assertions = [];
  let failedAssertions = [];
  let reviewItems = [];
  let visualArchiveImages = 0;
  let recovered = false;
  let recoveryReason = null;
  /** @type {{ kind: string, detail: string, phase: string, screenshotPath?: string } | null} */
  let appCrash = null;

  try {
    const resp = await page.goto(url, { waitUntil: 'networkidle', timeout: 90000 });
    httpStatus = resp?.status() ?? 0;
    await page.waitForTimeout(pauseMs);

    let crashAfterLoad = await detectAppCrashOverlay(page, { componentName: name });
    if (crashAfterLoad.crashed) {
      appCrash = {
        kind: crashAfterLoad.kind,
        detail: crashAfterLoad.detail ?? '',
        phase: 'after-load',
        screenshotPath: await screenshotAppCrash(page, passDir, name),
      };
    } else {
      const timeoutMs = interactionTimeoutMsFor(name, interactionTimeoutOverride);
      try {
        const outcome = await withInteractionTimeout(
          () =>
            interactWithComponent(
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
            ),
          timeoutMs,
          name
        );
        interactionActions = outcome.actionCount;
        assertions = outcome.assertions ?? [];
        visualArchiveImages = outcome.visualArchive?.images?.length ?? 0;
        failedAssertions = assertions.filter((a) => !a.passed && !a.reviewOnly);
        reviewItems = assertions.filter((a) => a.reviewOnly);
      } catch (e) {
        if (e instanceof InteractionTimeoutError) {
          interactionTimeout = { timeoutMs: e.timeoutMs, phase: e.phase ?? 'interact' };
          interactionError = e.message;
          logInteractionTimeout(passDir, name, interactionTimeout);
          const screenshotPath = await screenshotInteractionTimeout(page, passDir, name);
          saveTrackedIssue(outRoot, {
            component: name,
            kind: 'interaction-timeout',
            detail: e.message,
            at: ts(),
            passIndex,
            screenshotPath,
          });
          appendFileSync(
            interactionsLogPath,
            `[${ts()}] ${name}: INTERACTION TIMEOUT after ${e.timeoutMs}ms — continuing crawl\n`
          );
          await escapeFromStuckInteraction(page, (msg) =>
            appendFileSync(interactionsLogPath, `[${ts()}] ${name}: ${msg}\n`)
          );
        } else {
          interactionError = String(e.message ?? e);
          if (isBrowserBlockedError(e)) throw e;
        }
      }

      const crashAfterInteract = await detectAppCrashOverlay(page, { componentName: name });
      if (crashAfterInteract.crashed) {
        appCrash = {
          kind: crashAfterInteract.kind,
          detail: crashAfterInteract.detail ?? '',
          phase: 'after-interactions',
          screenshotPath: await screenshotAppCrash(page, passDir, name),
        };
      }
    }

    await page.waitForTimeout(400);
  } catch (e) {
    loadError = String(e.message ?? e);
    if (isBrowserBlockedError(e)) throw e;
  } finally {
    const stats = detachConsole.getStats();
    consoleCapHit = stats.capped;
    styleLeaks = stats.styleLeaks;
    if (consoleCapHit) {
      saveTrackedIssue(outRoot, {
        component: name,
        kind: 'console-cap',
        detail: `Console log cap (${stats.count}+ messages) reached on ${name}`,
        at: ts(),
        passIndex,
      });
    }
    detachConsole.detach();
  }

  const pageEntries = allEntries.slice(entriesBefore);
  const actionable = pageEntries.filter(
    (e) => isActionable(e.classify, e.type, e.text) && !textIsDevNoise(e.classify, e.text)
  );
  const uncaught = pageEntries.filter((e) => e.source === 'pageerror');

  return {
    component: name,
    url,
    httpStatus,
    loadError,
    interactionError,
    interactionTimeout,
    consoleCapHit,
    styleLeaks,
    interactionActions,
    assertionCount: assertions.length,
    failedAssertionCount: failedAssertions.length,
    reviewCount: reviewItems.length,
    visualArchiveImages,
    recovered,
    recoveryReason,
    appCrash,
    skippedBlocked: false,
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
}

function formatResultFlag(result) {
  if (result.skippedBlocked) return 'SKIPPED (app crash blocked)';
  if (result.appCrash) return `APP CRASH (${result.appCrash.kind})`;
  if (result.interactionTimeout) return `TIMEOUT (${result.interactionTimeout.timeoutMs}ms)`;
  if (result.consoleCapHit) return `CONSOLE CAP${result.actionableCount ? ` + ACTIONABLE:${result.actionableCount}` : ''}`;
  if (result.styleLeaks?.uniqueCount) {
    return `STYLE-LEAK:${result.styleLeaks.uniqueCount} (${result.styleLeaks.totalCount} hits)`;
  }
  if (result.loadError) return 'LOAD FAIL';
  if (result.failedAssertionCount) return `ASSERT:${result.failedAssertionCount}`;
  if (result.reviewCount) return `REVIEW:${result.reviewCount}`;
  if (result.actionableCount) return `ACTIONABLE:${result.actionableCount}`;
  return `ok (${result.interactionActions} actions, ${result.assertionCount} asserts)`;
}

async function auditPass(browser, passIndex) {
  const passDir = join(outRoot, `pass-${passIndex}`);
  mkdirSync(passDir, { recursive: true });
  mkdirSync(join(passDir, 'screenshots'), { recursive: true });
  if (visualArchiveEnabled) {
    writeVisualArchiveReadme(passDir, passIndex, baseUrl);
  }

  const checkpoint = loadCheckpoint(passDir);
  /** @type {Array<ReturnType<typeof auditOneComponent> extends Promise<infer R> ? R : never>} */
  const results = checkpoint?.results ?? [];
  const completed = new Set(checkpoint?.completedComponents ?? []);
  if (checkpoint?.blockedComponents) {
    for (const c of checkpoint.blockedComponents) blockedComponents.add(c);
  }
  const componentsToRun = COMPONENTS.filter(
    (name) => !completed.has(name) && !blockedComponents.has(name)
  );
  const safeFallback = pickSafeFallbackComponent(COMPONENTS, blockedComponents);
  const safeFallbackUrl = componentPath(safeFallback);
  const allEntries = [];
  const recoveryCount = { n: checkpoint?.recoveryCount ?? 0 };

  const session = await createBrowserSession(browser, {
    passDir,
    log: (msg) => console.log(`  [pass ${passIndex}] ${msg}`),
  });

  writeFileSync(
    join(passDir, 'meta.json'),
    JSON.stringify(
      {
        baseUrl,
        passIndex,
        startedAt: checkpoint?.startedAt ?? ts(),
        resumedAt: checkpoint ? ts() : undefined,
        headless,
        slowMo,
        screenshotMode,
        visualArchiveEnabled,
        resumedFromCheckpoint: !!checkpoint,
        completedBeforeResume: completed.size,
      },
      null,
      2
    )
  );

  console.log(`\n=== Pass ${passIndex}/${passCount} — ${baseUrl} (${headless ? 'headless' : 'headed'}) ===`);
  if (blockedComponents.size) {
    console.log(`  Blocked (app crash): ${[...blockedComponents].join(', ')}`);
  }
  console.log(`  Safe fallback route: ${safeFallback}`);
  if (checkpoint) {
    console.log(`  Resuming: ${completed.size} done, ${componentsToRun.length} remaining\n`);
  } else {
    console.log('');
  }

  // Record skips for components blocked by earlier crashes in this audit run.
  for (const name of COMPONENTS) {
    if (!blockedComponents.has(name) || completed.has(name)) continue;
    const skipResult = {
      component: name,
      url: componentPath(name),
      httpStatus: 0,
      loadError: null,
      interactionError: null,
      interactionTimeout: null,
      consoleCapHit: false,
      styleLeaks: { uniqueCount: 0, totalCount: 0, leaks: [] },
      interactionActions: 0,
      assertionCount: 0,
      failedAssertionCount: 0,
      reviewCount: 0,
      visualArchiveImages: 0,
      recovered: false,
      recoveryReason: null,
      appCrash: null,
      skippedBlocked: true,
      failedAssertions: [],
      consoleCount: 0,
      actionableCount: 0,
      uncaughtCount: 0,
      actionable: [],
    };
    results.push(skipResult);
    completed.add(name);
    console.log(`  [pass ${passIndex}] ${name.padEnd(28)} SKIPPED (app crash blocked)`);
  }

  for (const name of componentsToRun) {
    let result;
    let browserFatal = false;

    const trySessionRecover = async (reason) => {
      try {
        await waitForGalleryReady(baseUrl, {
          log: (msg) => console.log(`  [pass ${passIndex}] [recovery] ${msg}`),
        });
        await session.recover(reason);
        recoveryCount.n += 1;
        return true;
      } catch (e) {
        if (isBrowserBlockedError(e)) {
          console.log(
            `  [pass ${passIndex}] FATAL: browser closed — stopping pass (${e.message ?? e})`
          );
          return false;
        }
        throw e;
      }
    };

    try {
      const { result: auditResult, recovered, recoveryReason } = await withBrowserRecovery(
        session,
        name,
        () => auditOneComponent(session.page, name, passDir, passIndex, allEntries, outRoot),
        {
          baseUrl,
          log: (msg) => console.log(`  [pass ${passIndex}] ${msg}`),
          recoveryCount,
        }
      );
      result = { ...auditResult, recovered, recoveryReason };
    } catch (e) {
      result = {
        component: name,
        url: componentPath(name),
        httpStatus: 0,
        loadError: String(e.message ?? e),
        interactionError: null,
        interactionTimeout: null,
        consoleCapHit: false,
        styleLeaks: { uniqueCount: 0, totalCount: 0, leaks: [] },
        interactionActions: 0,
        assertionCount: 0,
        failedAssertionCount: 0,
        reviewCount: 0,
        visualArchiveImages: 0,
        recovered: false,
        recoveryReason: isBrowserBlockedError(e) ? String(e.message ?? e) : null,
        appCrash: null,
        skippedBlocked: false,
        failedAssertions: [],
        consoleCount: 0,
        actionableCount: 0,
        uncaughtCount: 0,
        actionable: [],
      };
      if (isBrowserBlockedError(e)) {
        browserFatal = true;
      }
    }

    if (result.appCrash) {
      blockedComponents.add(name);
      logAppCrash(passDir, name, result.appCrash);
      saveBlockedComponents(outRoot, blockedComponents, [
        {
          component: name,
          kind: result.appCrash.kind,
          detail: result.appCrash.detail,
          phase: result.appCrash.phase,
          at: ts(),
        },
      ]);
      console.log(`  [pass ${passIndex}] app crash on ${name} — blocking route and escaping`);
      const escaped = await escapeFromAppCrash(session.page, safeFallbackUrl, (msg) =>
        console.log(`  [pass ${passIndex}] ${msg}`)
      );
      if (!escaped) {
        browserFatal = !(await trySessionRecover(`app crash escape failed after ${name}`));
        if (!browserFatal) {
          await escapeFromAppCrash(session.page, safeFallbackUrl, (msg) =>
            console.log(`  [pass ${passIndex}] ${msg}`)
          );
        }
      }
    } else if (!browserFatal && !session.isAlive()) {
      browserFatal = !(await trySessionRecover(`page not alive after ${name}`));
    }

    results.push(result);
    completed.add(name);

    saveCheckpoint(passDir, {
      passIndex,
      startedAt: checkpoint?.startedAt ?? ts(),
      lastUpdated: ts(),
      completedComponents: [...completed],
      blockedComponents: [...blockedComponents],
      safeFallback,
      recoveryCount: recoveryCount.n,
      recoveries: session.recoveries,
      results,
    });

    const flag = result.recovered
      ? `${formatResultFlag(result)} [recovered]`
      : formatResultFlag(result);
    console.log(`  [pass ${passIndex}] ${name.padEnd(28)} ${flag}`);

    if (browserFatal) {
      console.log(
        `  [pass ${passIndex}] pass interrupted after ${name} — resume with --resume=${outRoot}`
      );
      break;
    }
  }

  await session.close();

  const styleLeakRollup = aggregateStyleLeaks(results);
  writeFileSync(join(passDir, 'style-leaks.json'), JSON.stringify(styleLeakRollup, null, 2));

  const summary = {
    baseUrl,
    passIndex,
    finishedAt: ts(),
    pageCount: COMPONENTS.length,
    pagesRun: componentsToRun.length,
    resumedFromCheckpoint: !!checkpoint,
    browserRecoveries: session.recoveries.length,
    blockedComponents: [...blockedComponents],
    pagesWithAppCrashes: results.filter((r) => r.appCrash),
    pagesSkippedBlocked: results.filter((r) => r.skippedBlocked),
    totalConsoleEvents: allEntries.length,
    pagesWithActionable: results.filter((r) => r.actionableCount > 0),
    pagesWithLoadErrors: results.filter((r) => r.loadError),
    pagesWithInteractionTimeouts: results.filter((r) => r.interactionTimeout),
    pagesWithConsoleCap: results.filter((r) => r.consoleCapHit),
    pagesWithStyleLeaks: results.filter((r) => r.styleLeaks?.uniqueCount > 0),
    pagesWithUncaught: results.filter((r) => r.uncaughtCount > 0),
    pagesWithFailedAssertions: results.filter((r) => r.failedAssertionCount > 0),
    styleLeakRollup,
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

  if (summary.pagesWithInteractionTimeouts?.length) {
    md += `## Interaction timeouts\n\n`;
    md += `These pages exceeded the per-component wall-clock budget. The crawl continued; see \`interaction-timeouts.log\`, \`../tracked-issues.json\`, and \`screenshots/timeouts/\`.\n\n`;
    for (const p of summary.pagesWithInteractionTimeouts) {
      md += `- **${p.component}**: ${p.interactionTimeout.timeoutMs}ms`;
      if (p.interactionError) md += ` — ${p.interactionError}`;
      md += `\n`;
    }
    md += `\n`;
  }

  if (summary.pagesWithConsoleCap?.length) {
    md += `## Console log cap hit\n\n`;
    md += `Dev-web console flooded on these pages before deduplication could help. See \`style-leaks.json\` for unique leak keys.\n\n`;
    md += summary.pagesWithConsoleCap.map((p) => p.component).join(', ') + '\n\n';
  }

  if (summary.pagesWithStyleLeaks?.length) {
    md += `## Style leaks (deduplicated)\n\n`;
    md += `Unique ReactXP style-leak keys per page (repeat hits counted, not logged). Full rollup: \`style-leaks.json\`.\n\n`;
    for (const p of summary.pagesWithStyleLeaks) {
      md += `- **${p.component}**: ${formatStyleLeakSummaryLine(p.styleLeaks, p.component)}\n`;
    }
    md += `\n`;
  }

  if (summary.styleLeakRollup?.uniqueLeakKeys) {
    md += `### Cross-page style leak keys\n\n`;
    for (const leak of summary.styleLeakRollup.leaks.slice(0, 20)) {
      const who = leak.sourceComponent ?? 'unknown component';
      md += `- \`${leak.key}\` (${who}): ${leak.totalCount} hit(s) on ${leak.galleryPages.join(', ')}\n`;
    }
    if (summary.styleLeakRollup.leaks.length > 20) {
      md += `\n… and ${summary.styleLeakRollup.leaks.length - 20} more keys in \`style-leaks.json\`.\n`;
    }
    md += `\n`;
  }

  if (summary.pagesWithLoadErrors.length) {
    md += `## Load failures\n\n`;
    for (const p of summary.pagesWithLoadErrors) md += `- **${p.component}**: ${p.loadError}\n`;
    md += `\n`;
  }

  if (summary.browserRecoveries) {
    md += `## Browser recoveries\n\n`;
    md += `${summary.browserRecoveries} context restart(s) during this pass (see \`recoveries.log\`).\n\n`;
  }

  if (summary.pagesWithAppCrashes?.length) {
    md += `## App crashes (full-window error overlay)\n\n`;
    md += `These routes blew up the app and were **blocked** for the rest of the audit. See \`app-crashes.log\` and \`../blocked-components.json\`.\n\n`;
    for (const p of summary.pagesWithAppCrashes) {
      md += `- **${p.component}** (${p.appCrash.kind}, ${p.appCrash.phase}): ${p.appCrash.detail}\n`;
    }
    md += `\n`;
  }

  if (summary.pagesSkippedBlocked?.length) {
    md += `## Skipped (blocked by earlier crash)\n\n`;
    md += summary.pagesSkippedBlocked.map((p) => p.component).join(', ') + '\n\n';
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

  md += `## All pages\n\n| Component | HTTP | Actions | Asserts | Failed | Console | Actionable | Uncaught | Notes |\n`;
  md += `|-----------|------|---------|---------|--------|---------|------------|----------|-------|\n`;
  for (const r of summary.results) {
    const notes = [
      r.interactionTimeout ? 'timeout' : '',
      r.consoleCapHit ? 'console-cap' : '',
      r.styleLeaks?.uniqueCount ? `style-leak:${r.styleLeaks.uniqueCount}` : '',
      r.skippedBlocked ? 'blocked' : '',
      r.appCrash ? 'crash' : '',
    ]
      .filter(Boolean)
      .join(', ');
    md += `| ${r.component} | ${r.httpStatus} | ${r.interactionActions ?? 0} | ${r.assertionCount ?? 0} | ${r.failedAssertionCount ?? 0} | ${r.consoleCount} | ${r.actionableCount} | ${r.uncaughtCount} | ${notes || '—'} |\n`;
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
if (resumeDir) console.log(`  Resume:   ${outRoot}`);
if (onlyScope) {
  console.log(`  Only:     ${onlyScope} (${COMPONENTS.length} route(s))`);
}
if (blockedComponents.size) {
  console.log(`  Blocked components: ${[...blockedComponents].join(', ')}`);
}
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
    interactionTimeouts: s.pagesWithInteractionTimeouts?.length ?? 0,
    consoleCapPages: s.pagesWithConsoleCap?.length ?? 0,
    styleLeakPages: s.pagesWithStyleLeaks?.length ?? 0,
    uniqueStyleLeakKeys: s.styleLeakRollup?.uniqueLeakKeys ?? 0,
  })),
};

if (passSummaries.length >= 2) {
  finalReport.passComparison = comparePasses(passSummaries[0], passSummaries[1]);
}

writeFileSync(join(outRoot, 'final-report.json'), JSON.stringify(finalReport, null, 2));

let finalMd = `# Interactive gallery audit\n\n`;
finalMd += `Output: \`${outRoot}\`\n\n`;
finalMd += `| Pass | Console events | Actionable pages | Failed asserts | Load errors | Timeouts | Style leaks | App crashes |\n`;
finalMd += `|------|----------------|------------------|----------------|-------------|----------|-------------|-------------|\n`;
  for (const s of passSummaries) {
    finalMd += `| ${s.passIndex} | ${s.totalConsoleEvents} | ${s.pagesWithActionable.length} | ${s.pagesWithFailedAssertions?.length ?? 0} | ${s.pagesWithLoadErrors.length} | ${s.pagesWithInteractionTimeouts?.length ?? 0} | ${s.pagesWithStyleLeaks?.length ?? 0} (${s.styleLeakRollup?.uniqueLeakKeys ?? 0} keys) | ${s.pagesWithAppCrashes?.length ?? 0} |\n`;
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
