import { chromium } from 'playwright';
import { DEFAULTS } from '../lib/config.mjs';
import { createRunDir } from '../lib/paths.mjs';
import { createWebSession, waitForAppReady } from '../lib/web-session.mjs';
import { waitForTodoReady } from '../lib/selectors.mjs';
import { captureState, writeManifest, appendRunLog } from '../lib/capture.mjs';
import { emitReport, emitStatus } from '../lib/report.mjs';

/**
 * @param {{ baseUrl?: string, headless?: boolean, slowMo?: number, outDir?: string, keepOpen?: boolean, label?: string }} options
 */
export async function runSnapshotWorkflow(options = {}) {
  const baseUrl = options.baseUrl ?? DEFAULTS.baseUrl;
  const headless = options.headless ?? DEFAULTS.headless;
  const slowMo = options.slowMo ?? DEFAULTS.slowMo;
  const outDir = options.outDir ?? createRunDir('snapshot');
  const label = options.label ?? 'current';

  appendRunLog(outDir, `snapshot start baseUrl=${baseUrl} headless=${headless}`);

  const ready = await waitForAppReady(baseUrl, {
    log: (msg) => appendRunLog(outDir, msg),
  });
  if (!ready) {
    emitStatus('fail', `AppTodo not reachable at ${baseUrl}`);
    process.exitCode = 1;
    return null;
  }

  const browser = await chromium.launch({ headless, slowMo });
  const session = await createWebSession(browser, { viewport: DEFAULTS.viewport });

  try {
    const resp = await session.goto(baseUrl);
    await waitForTodoReady(session.page, { bootstrapWaitMs: DEFAULTS.bootstrapWaitMs });

    const capture = await captureState(session.page, outDir, {
      label,
      logs: session.logs,
    });

    const manifest = {
      command: 'snapshot',
      baseUrl,
      headless,
      httpStatus: resp?.status() ?? null,
      artifacts: {
        screenshot: `${label}.png`,
        layoutMetrics: `${label}-layout-metrics.json`,
        domSummary: `${label}-dom-summary.json`,
        uiSnapshot: `${label}-ui-snapshot.json`,
        uiLog: `${label}-ui-log.json`,
        console: `${label}-console.log`,
        pageErrors: `${label}-page-errors.log`,
        networkErrors: `${label}-network-errors.log`,
      },
      cardWidth: capture.layoutMetrics.regions.find((r) => r.testId === 'todo-card')?.width ?? null,
      consoleErrorCount: session.logs.consoleLines.filter((l) => l.startsWith('[error]')).length,
      pageErrorCount: session.logs.pageErrors.length,
    };

    writeManifest(outDir, manifest);
    emitReport(manifest, { outDir: undefined });
    emitStatus('ok', `Snapshot saved to ${outDir}`);

    if (options.keepOpen) {
      appendRunLog(outDir, 'keeping browser open — press Ctrl+C to exit');
      console.log('Browser left open for collaboration. Press Ctrl+C when done.');
      await new Promise(() => {});
    }

    return { outDir, manifest, capture };
  } finally {
    if (!options.keepOpen) {
      await session.close();
      await browser.close();
    }
  }
}
