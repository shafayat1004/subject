import { writeFileSync, appendFileSync } from 'fs';
import { join } from 'path';
import { collectDomSummary, collectLayoutMetrics } from './dom-analysis.mjs';
import { readUiLog, readUiSnapshot } from './selectors.mjs';
import { APP_NAME } from './config.mjs';

/**
 * @param {string} dir
 * @param {string} name
 * @param {unknown} data
 */
export function writeJson(dir, name, data) {
  writeFileSync(join(dir, name), `${JSON.stringify(data, null, 2)}\n`, 'utf8');
}

/**
 * @param {string} dir
 * @param {string} name
 * @param {string[]} lines
 */
export function writeLines(dir, name, lines) {
  writeFileSync(join(dir, name), lines.length ? `${lines.join('\n')}\n` : '', 'utf8');
}

/**
 * Full capture bundle for one point in time.
 * @param {import('playwright').Page} page
 * @param {string} outDir
 * @param {{ label?: string, logs?: { consoleLines: string[], pageErrors: string[], networkErrors: string[] } }} [options]
 */
export async function captureState(page, outDir, options = {}) {
  const { label = 'snapshot', logs } = options;

  const screenshotPath = join(outDir, `${label}.png`);
  await page.screenshot({ path: screenshotPath, fullPage: true });

  const layoutMetrics = await collectLayoutMetrics(page);
  const domSummary = await collectDomSummary(page);
  const uiSnapshot = await readUiSnapshot(page, APP_NAME);
  const uiLog = await readUiLog(page, APP_NAME);

  writeJson(outDir, `${label}-layout-metrics.json`, layoutMetrics);
  writeJson(outDir, `${label}-dom-summary.json`, domSummary);
  writeJson(outDir, `${label}-ui-snapshot.json`, uiSnapshot);
  writeJson(outDir, `${label}-ui-log.json`, uiLog);

  if (logs) {
    writeLines(outDir, `${label}-console.log`, logs.consoleLines);
    writeLines(outDir, `${label}-page-errors.log`, logs.pageErrors);
    writeLines(outDir, `${label}-network-errors.log`, logs.networkErrors);
  }

  return {
    screenshotPath,
    layoutMetrics,
    domSummary,
    uiSnapshot,
    uiLog,
  };
}

/**
 * LLM-oriented manifest tying artifacts together.
 * @param {string} outDir
 * @param {Record<string, unknown>} meta
 */
export function writeManifest(outDir, meta) {
  writeJson(outDir, 'manifest.json', {
    generatedAt: new Date().toISOString(),
    ...meta,
  });
}

/**
 * Append a human-readable line to run log.
 * @param {string} outDir
 * @param {string} line
 */
export function appendRunLog(outDir, line) {
  appendFileSync(join(outDir, 'run.log'), `[${new Date().toISOString()}] ${line}\n`, 'utf8');
}
