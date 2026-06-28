import { DEFAULTS } from '../lib/config.mjs';
import { createRunDir } from '../lib/paths.mjs';
import { openObserveSession, prepareTodoUi } from '../lib/observe-session.mjs';
import { captureObserveState, writeManifest, appendRunLog } from '../lib/capture.mjs';
import { cardWidthFromMetrics } from '../lib/dom-analysis.mjs';
import { summarizeConsole } from '../lib/log-classify.mjs';
import { isNativePlatform } from '../lib/platform.mjs';
import { emitReport, emitStatus } from '../lib/report.mjs';

/**
 * @param {{ platform?: string, baseUrl?: string, headless?: boolean, slowMo?: number, outDir?: string, keepOpen?: boolean, label?: string }} options
 */
export async function runSnapshotWorkflow(options = {}) {
  const platform = options.platform ?? DEFAULTS.platform;
  const baseUrl = options.baseUrl ?? DEFAULTS.baseUrl;
  const headless = options.headless ?? DEFAULTS.headless;
  const outDir = options.outDir ?? createRunDir('snapshot');
  const label = options.label ?? 'current';

  appendRunLog(outDir, `snapshot start platform=${platform} baseUrl=${baseUrl} headless=${headless}`);

  const session = await openObserveSession({
    platform,
    baseUrl,
    headless,
    slowMo: options.slowMo ?? DEFAULTS.slowMo,
    outDir,
    log: (msg) => appendRunLog(outDir, msg),
  });

  try {
    await prepareTodoUi(session, { outDir, bootstrapWaitMs: DEFAULTS.bootstrapWaitMs });

    const capture = await captureObserveState(session, outDir, { label });

    const consoleSummary = session.logs
      ? summarizeConsole(
          session.logs.consoleLines.map((line) => {
            const m = line.match(/^\[(\w+)\]\s(.*)$/);
            return { type: m?.[1] ?? 'log', text: m?.[2] ?? line };
          })
        )
      : { actionable: 0, styleLeaks: 0, noise: 0 };

    const manifest = {
      command: 'snapshot',
      platform,
      baseUrl: isNativePlatform(platform) ? null : baseUrl,
      headless: isNativePlatform(platform) ? null : headless,
      httpStatus: session.httpStatus ?? null,
      artifacts: isNativePlatform(platform)
        ? {
            screenshot: `${label}.png`,
            layoutMetrics: `${label}-layout-metrics.json`,
            uiSummary: `${label}-ui-summary.json`,
            uiHierarchy: `${label}-ui-hierarchy.xml`,
            deviceLog: `${label}-device.log`,
          }
        : {
            screenshot: `${label}.png`,
            layoutMetrics: `${label}-layout-metrics.json`,
            domSummary: `${label}-dom-summary.json`,
            uiSnapshot: `${label}-ui-snapshot.json`,
            uiLog: `${label}-ui-log.json`,
            console: `${label}-console.log`,
            pageErrors: `${label}-page-errors.log`,
            networkErrors: `${label}-network-errors.log`,
          },
      cardWidth: cardWidthFromMetrics(capture.layoutMetrics),
      consoleSummary,
      pageErrorCount: session.logs?.pageErrors.length ?? 0,
    };

    writeManifest(outDir, manifest);
    emitReport(manifest);
    emitStatus('ok', `Snapshot saved to ${outDir}`);

    if (options.keepOpen && !isNativePlatform(platform)) {
      appendRunLog(outDir, 'keeping browser open — press Ctrl+C to exit');
      console.log('Browser left open for collaboration. Press Ctrl+C when done.');
      await new Promise(() => {});
    }

    return { outDir, manifest, capture };
  } finally {
    if (!options.keepOpen) {
      await session.close();
    }
  }
}
