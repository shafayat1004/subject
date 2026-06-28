import { chromium } from 'playwright';
import { DEFAULTS } from '../lib/config.mjs';
import { createRunDir } from '../lib/paths.mjs';
import { createWebSession, waitForAppReady } from '../lib/web-session.mjs';
import {
  waitForTodoReady,
  fillNewTodoTitle,
  clickAddTodo,
} from '../lib/selectors.mjs';
import { captureState, writeManifest, appendRunLog, writeJson } from '../lib/capture.mjs';
import { diffLayoutMetrics, cardWidthFromMetrics } from '../lib/dom-analysis.mjs';
import { emitReport, emitStatus } from '../lib/report.mjs';

/**
 * Add a todo and capture before/after for layout regression (card width shrink, etc.).
 * @param {{ baseUrl?: string, headless?: boolean, title?: string, outDir?: string }} options
 */
export async function runLayoutCheckWorkflow(options = {}) {
  const baseUrl = options.baseUrl ?? DEFAULTS.baseUrl;
  const headless = options.headless ?? DEFAULTS.headless;
  const title = options.title ?? `observe-${Date.now()}`;
  const outDir = options.outDir ?? createRunDir('layout-check');

  appendRunLog(outDir, `layout-check start title="${title}"`);

  const ready = await waitForAppReady(baseUrl);
  if (!ready) {
    emitStatus('fail', `AppTodo not reachable at ${baseUrl}`);
    process.exitCode = 1;
    return null;
  }

  const browser = await chromium.launch({ headless, slowMo: DEFAULTS.slowMo });
  const session = await createWebSession(browser, { viewport: DEFAULTS.viewport });

  try {
    await session.goto(baseUrl);
    await waitForTodoReady(session.page);

    const before = await captureState(session.page, outDir, { label: 'before', logs: session.logs });

    await fillNewTodoTitle(session.page, title);
    await clickAddTodo(session.page);
    await session.page.getByText(title).waitFor({ timeout: 15000 });
    await session.page.waitForTimeout(400);

    const after = await captureState(session.page, outDir, { label: 'after', logs: session.logs });

    const diff = diffLayoutMetrics(before.layoutMetrics, after.layoutMetrics);
    writeJson(outDir, 'layout-diff.json', diff);

    const report = {
      command: 'workflow layout-check',
      baseUrl,
      headless,
      todoTitle: title,
      outDir,
      cardWidthBefore: cardWidthFromMetrics(before.layoutMetrics),
      cardWidthAfter: cardWidthFromMetrics(after.layoutMetrics),
      layoutDiff: diff,
      regressionLikely: diff.regressionLikely,
      artifacts: {
        beforeScreenshot: 'before.png',
        afterScreenshot: 'after.png',
        layoutDiff: 'layout-diff.json',
      },
    };

    writeManifest(outDir, report);
    emitReport(report);

    if (diff.regressionLikely) {
      emitStatus('warn', diff.summary);
      process.exitCode = 2;
    } else {
      emitStatus('ok', diff.summary);
    }

    return report;
  } finally {
    await session.close();
    await browser.close();
  }
}

/**
 * @param {{ baseUrl?: string, headless?: boolean, title?: string, outDir?: string }} options
 */
export async function runAddTodoWorkflow(options = {}) {
  const baseUrl = options.baseUrl ?? DEFAULTS.baseUrl;
  const headless = options.headless ?? DEFAULTS.headless;
  const title = options.title ?? `todo-${Date.now()}`;
  const outDir = options.outDir ?? createRunDir('add-todo');

  const browser = await chromium.launch({ headless, slowMo: DEFAULTS.slowMo });
  const session = await createWebSession(browser, { viewport: DEFAULTS.viewport });

  try {
    await waitForAppReady(baseUrl);
    await session.goto(baseUrl);
    await waitForTodoReady(session.page);

    await fillNewTodoTitle(session.page, title);
    await clickAddTodo(session.page);
    await session.page.getByText(title).waitFor({ timeout: 15000 });

    const capture = await captureState(session.page, outDir, { label: 'after-add', logs: session.logs });

    const report = {
      command: 'add-todo',
      title,
      outDir,
      cardWidth: cardWidthFromMetrics(capture.layoutMetrics),
    };
    writeManifest(outDir, report);
    emitReport(report);
    emitStatus('ok', `Added "${title}" — artifacts in ${outDir}`);
    return report;
  } finally {
    await session.close();
    await browser.close();
  }
}
