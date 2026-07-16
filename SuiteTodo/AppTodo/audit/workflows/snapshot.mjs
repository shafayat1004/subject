import { DEFAULTS, TIMEOUTS } from '../lib/config.mjs';
import { createRunDir } from '../lib/paths.mjs';
import { openObserveSession, prepareTodoUi } from '../lib/observe-session.mjs';
import { captureObserveState, writeManifest, appendRunLog } from '../lib/capture.mjs';
import { cardWidthFromMetrics } from '../lib/dom-analysis.mjs';
import { summarizeConsole } from '../lib/log-classify.mjs';
import { isNativePlatform } from '../lib/platform.mjs';
import { emitReport, emitStatus } from '../lib/report.mjs';

/**
 * @param {{ platform?: string, baseUrl?: string, headless?: boolean, slowMo?: number, outDir?: string, keepOpen?: boolean, label?: string, timeoutMs?: number }} options
 */
export async function runSnapshotWorkflow(options = {}) {
  const platform = options.platform ?? DEFAULTS.platform;
  const baseUrl = options.baseUrl ?? DEFAULTS.baseUrl;
  const headless = options.headless ?? DEFAULTS.headless;
  const timeoutMs = options.timeoutMs ?? TIMEOUTS.appReadyMs;
  const outDir = options.outDir ?? createRunDir('snapshot');
  const label = options.label ?? 'current';

  appendRunLog(outDir, `snapshot start platform=${platform} baseUrl=${baseUrl} headless=${headless} timeoutMs=${timeoutMs}`);

  const session = await openObserveSession({
    platform,
    baseUrl,
    headless,
    slowMo: options.slowMo ?? DEFAULTS.slowMo,
    outDir,
    timeoutMs,
    log: (msg) => appendRunLog(outDir, msg),
  });

  try {
    await prepareTodoUi(session, { outDir, bootstrapWaitMs: DEFAULTS.bootstrapWaitMs, timeoutMs });

    const capture = await captureObserveState(session, outDir, { label });

    const logSummary =
      capture.logSummary ??
      (session.logs
        ? summarizeConsole(
            session.logs.consoleLines.map((line) => {
              const m = line.match(/^\[(\w+)\]\s(.*)$/);
              return { type: m?.[1] ?? 'log', text: m?.[2] ?? line };
            })
          )
        : { actionable: 0, styleLeaks: 0, noise: 0 });

    const health = capture.health ?? { state: 'unknown', healthy: false };
    const unhealthy = health.healthy === false && health.state !== 'loading';

    const manifest = {
      command: 'snapshot',
      platform,
      orientation: session.orientation ?? null,
      baseUrl: isNativePlatform(platform) ? null : baseUrl,
      headless: isNativePlatform(platform) ? null : headless,
      httpStatus: session.httpStatus ?? null,
      health,
      logSummary,
      artifacts: isNativePlatform(platform)
        ? {
            screenshot: `${label}.png`,
            layoutMetrics: `${label}-layout-metrics.json`,
            uiSummary: `${label}-ui-summary.json`,
            uiHierarchy: `${label}-ui-hierarchy.xml`,
            deviceLog: `${label}-device.log`,
            health: `${label}-health.json`,
            console: `${label}-console.log`,
            logSummary: `${label}-log-summary.json`,
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
            health: `${label}-health.json`,
            logSummary: `${label}-log-summary.json`,
          },
      cardWidth: cardWidthFromMetrics(capture.layoutMetrics),
      consoleSummary: logSummary,
      pageErrorCount: session.logs?.pageErrors.length ?? 0,
    };

    writeManifest(outDir, manifest);
    emitReport(manifest);

    if (unhealthy) {
      emitStatus('warn', `Snapshot captured but app unhealthy: ${health.state} — ${health.detail ?? ''}`);
      process.exitCode = 2;
    } else if (logSummary.actionable > 0) {
      emitStatus('warn', `Snapshot saved with ${logSummary.actionable} actionable log(s) — ${outDir}`);
      process.exitCode = 2;
    } else {
      emitStatus('ok', `Snapshot saved to ${outDir}`);
    }

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
