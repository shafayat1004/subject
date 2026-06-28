/**
 * Capture screenshots, UI hierarchy, and layout metrics on Android / iOS.
 */

import { join } from 'path';
import { writeJson, writeLines } from './io.mjs';
import { TEST_IDS } from './selectors.mjs';
import { CARD_REGION_IDS } from './dom-analysis.mjs';
import { captureLogcat } from './android-driver.mjs';
import { captureIosLogs } from './ios-driver.mjs';
import { PLATFORM } from './platform.mjs';
import { captureHealthState, writeLogArtifacts } from './capture.mjs';

/**
 * Compact summary from Appium page source XML (depth-limited).
 * @param {string} xml
 * @param {{ maxNodes?: number }} [options]
 */
export function summarizeUiHierarchy(xml, options = {}) {
  const maxNodes = options.maxNodes ?? 400;
  /** @type {Array<{ tag: string, attrs: Record<string, string>, depth: number }>} */
  const nodes = [];
  const tagRe = /<(\w+)([^>]*?)(\/?)>/g;
  let depth = 0;
  let m;
  while ((m = tagRe.exec(xml)) !== null && nodes.length < maxNodes) {
    const tag = m[1];
    const selfClosing = m[3] === '/';
    const attrStr = m[2];
    /** @type {Record<string, string>} */
    const attrs = {};
    const attrRe = /([\w:-]+)="([^"]*)"/g;
    let am;
    while ((am = attrRe.exec(attrStr)) !== null) {
      const key = am[1].replace(/^android:/, '').replace(/^XCUIElementType/, '');
      attrs[key] = am[2].slice(0, 120);
    }
    if (tag.endsWith('hierarchy') || tag === '?xml') continue;
    nodes.push({ tag, attrs, depth });
    if (!selfClosing && !tag.startsWith('?')) depth += 1;
  }
  return { nodeCount: nodes.length, nodes: nodes.slice(0, maxNodes) };
}

/**
 * @param {import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage} page
 * @param {'android' | 'ios'} platform
 */
export async function collectNativeLayoutMetrics(page, platform) {
  /** @type {Array<{ testId: string, x: number, y: number, width: number, height: number }>} */
  const regions = [];

  for (const [label, testId] of Object.entries(TEST_IDS)) {
    const loc = page.locator(`~${testId}`);
    if (await loc.count()) {
      const box = await loc.first().boundingBox();
      if (box) {
        regions.push({
          testId,
          x: Math.round(box.x),
          y: Math.round(box.y),
          width: Math.round(box.width),
          height: Math.round(box.height),
        });
      }
    }
  }

  // Card shell via "Todos" heading container bounds (RN testID may be on wrapper).
  const todos = page.getByText('Todos', { exact: true });
  if (await todos.count()) {
    const box = await todos.first().boundingBox();
    if (box) {
      regions.push({
        testId: 'todo-card-panel',
        x: Math.round(box.x - 28),
        y: Math.round(box.y - 28),
        width: Math.round(Math.min(box.width + 56, 560)),
        height: Math.round(box.height + 200),
      });
    }
  }

  const window = await page.getWindowSize();
  regions.push({
    testId: 'viewport-screen',
    x: 0,
    y: 0,
    width: window.width,
    height: window.height,
  });

  return {
    capturedAt: new Date().toISOString(),
    platform,
    viewport: { width: window.width, height: window.height, devicePixelRatio: 1 },
    regions,
  };
}

/**
 * @param {{ page: import('./android-driver.mjs').AndroidPage | import('./ios-driver.mjs').IosPage, platform: 'android' | 'ios', logs?: { consoleLines: string[], pageErrors: string[], networkErrors: string[] }, logCollector?: import('./device-logs.mjs').AndroidLogCollector | null }} session
 * @param {string} outDir
 * @param {{ label?: string }} [options]
 */
export async function captureNativeState(session, outDir, options = {}) {
  const { page, platform, logs, logCollector } = session;
  const label = options.label ?? 'snapshot';

  const screenshotPath = join(outDir, `${label}.png`);
  await page.screenshot({ path: screenshotPath });

  const pageSource = await page.getPageSource();
  const hierarchySummary = summarizeUiHierarchy(pageSource);
  const layoutMetrics = await collectNativeLayoutMetrics(page, platform);
  const health = await captureHealthState(page, platform, outDir, label);

  writeJson(outDir, `${label}-ui-hierarchy.xml.json`, { truncated: pageSource.length > 500_000, length: pageSource.length });
  writeLines(outDir, `${label}-ui-hierarchy.xml`, [pageSource]);
  writeJson(outDir, `${label}-ui-summary.json`, hierarchySummary);
  writeJson(outDir, `${label}-layout-metrics.json`, layoutMetrics);
  writeJson(outDir, `${label}-ui-snapshot.json`, null);
  writeJson(outDir, `${label}-ui-log.json`, null);

  const deviceLogLines =
    platform === PLATFORM.ANDROID
      ? logCollector
        ? await logCollector.snapshot()
        : await captureLogcat().catch(() => [])
      : captureIosLogs();
  writeLines(outDir, `${label}-device.log`, deviceLogLines);

  const mergedLogs = logs ?? { consoleLines: [], pageErrors: [], networkErrors: [] };
  if (logCollector) {
    const streamed = logCollector.toSessionLogs();
    mergedLogs.consoleLines = [...mergedLogs.consoleLines, ...streamed.consoleLines];
    mergedLogs.pageErrors = [...mergedLogs.pageErrors, ...streamed.pageErrors];
  }
  const logSummary = writeLogArtifacts(outDir, label, mergedLogs);

  return {
    screenshotPath,
    layoutMetrics,
    hierarchySummary,
    deviceLogs: deviceLogLines,
    health,
    logSummary,
  };
}

export { CARD_REGION_IDS };
