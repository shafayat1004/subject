import { DEFAULTS } from '../lib/config.mjs';
import { createRunDir } from '../lib/paths.mjs';
import { openObserveSession, prepareTodoUi } from '../lib/observe-session.mjs';
import {
  fillNewTodoTitle,
  clickAddTodo,
} from '../lib/selectors.mjs';
import { captureObserveState, writeManifest, appendRunLog, writeJson } from '../lib/capture.mjs';
import { diffLayoutMetrics, cardWidthFromMetrics } from '../lib/dom-analysis.mjs';
import { isNativePlatform } from '../lib/platform.mjs';
import { emitReport, emitStatus } from '../lib/report.mjs';

/**
 * @param {{ platform?: string, baseUrl?: string, headless?: boolean, title?: string, outDir?: string }} options
 */
export async function runLayoutCheckWorkflow(options = {}) {
  const platform = options.platform ?? DEFAULTS.platform;
  const baseUrl = options.baseUrl ?? DEFAULTS.baseUrl;
  const headless = options.headless ?? DEFAULTS.headless;
  const title = options.title ?? `observe-${Date.now()}`;
  const outDir = options.outDir ?? createRunDir('layout-check');

  appendRunLog(outDir, `layout-check platform=${platform} title="${title}"`);

  const session = await openObserveSession({ platform, baseUrl, headless, outDir });

  try {
    await prepareTodoUi(session, { outDir });

    const before = await captureObserveState(session, outDir, { label: 'before' });

    await fillNewTodoTitle(session.page, title, platform);
    await clickAddTodo(session.page, platform);
    await session.page.getByText(title).waitFor({ timeout: 15000 });
    await session.page.waitForTimeout(400);

    const after = await captureObserveState(session, outDir, { label: 'after' });

    const diff = diffLayoutMetrics(before.layoutMetrics, after.layoutMetrics);
    writeJson(outDir, 'layout-diff.json', diff);

    const report = {
      command: 'workflow layout-check',
      platform,
      baseUrl: isNativePlatform(platform) ? null : baseUrl,
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
  }
}

/**
 * @param {{ platform?: string, baseUrl?: string, headless?: boolean, title?: string, outDir?: string }} options
 */
export async function runAddTodoWorkflow(options = {}) {
  const platform = options.platform ?? DEFAULTS.platform;
  const baseUrl = options.baseUrl ?? DEFAULTS.baseUrl;
  const headless = options.headless ?? DEFAULTS.headless;
  const title = options.title ?? `todo-${Date.now()}`;
  const outDir = options.outDir ?? createRunDir('add-todo');

  const session = await openObserveSession({ platform, baseUrl, headless });

  try {
    await prepareTodoUi(session, { outDir });

    await fillNewTodoTitle(session.page, title, platform);
    await clickAddTodo(session.page, platform);
    await session.page.getByText(title).waitFor({ timeout: 15000 });

    const capture = await captureObserveState(session, outDir, { label: 'after-add' });

    const report = {
      command: 'add-todo',
      platform,
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
  }
}
