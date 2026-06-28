/**
 * DOM layout metrics and diff for aesthetic / regression analysis.
 */

import { LAYOUT_DIFF_THRESHOLD_PX } from './config.mjs';
import { TEST_IDS } from './selectors.mjs';

/** Region ids used for card width regression (testId or heuristic). */
export const CARD_REGION_IDS = ['todo-card', 'todo-card-panel'];

/**
 * @param {{ regions: Array<{ testId: string, width: number }> }} metrics
 */
export function cardWidthFromMetrics(metrics) {
  for (const id of CARD_REGION_IDS) {
    const region = metrics.regions.find((r) => r.testId === id);
    if (region) return region.width;
  }
  return null;
}

/**
 * Collect bounding boxes for key AppTodo regions.
 * @param {import('playwright').Page} page
 */
export async function collectLayoutMetrics(page) {
  return page.evaluate((testIds) => {
    /** @param {string} testId */
    function rectForTestId(testId) {
      const el = document.querySelector(`[data-testid="${testId}"]`);
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return {
        testId,
        x: Math.round(r.x),
        y: Math.round(r.y),
        width: Math.round(r.width),
        height: Math.round(r.height),
      };
    }

    /** @param {string} selector */
    function rectForSelector(selector, label) {
      const el = document.querySelector(selector);
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return {
        testId: label,
        x: Math.round(r.x),
        y: Math.round(r.y),
        width: Math.round(r.width),
        height: Math.round(r.height),
      };
    }

    /** ReactXP web often omits data-testid; find the white rounded todo card by layout. */
    function rectForTodoCardPanel() {
      /** @type {Element | null} */
      let best = null;
      let bestArea = 0;

      for (const el of document.querySelectorAll('div')) {
        const r = el.getBoundingClientRect();
        if (r.width < 400 || r.width > 620 || r.height < 120) continue;
        const s = getComputedStyle(el);
        if (s.backgroundColor !== 'rgb(255, 255, 255)') continue;
        if (!s.borderRadius.includes('12')) continue;
        if (!el.textContent?.includes('Todos')) continue;
        const area = r.width * r.height;
        if (area > bestArea) {
          bestArea = area;
          best = el;
        }
      }

      if (!best) return null;
      const r = best.getBoundingClientRect();
      return {
        testId: 'todo-card-panel',
        x: Math.round(r.x),
        y: Math.round(r.y),
        width: Math.round(r.width),
        height: Math.round(r.height),
      };
    }

    const regions = [
      rectForTestId(testIds.page),
      rectForTestId(testIds.card),
      rectForTodoCardPanel(),
      rectForTestId(testIds.newTitle),
      rectForTestId(testIds.add),
      rectForSelector('input', 'first-input-fallback'),
      rectForSelector('body', 'viewport-body'),
    ].filter(Boolean);

    const viewport = {
      width: window.innerWidth,
      height: window.innerHeight,
      devicePixelRatio: window.devicePixelRatio,
    };

    return { capturedAt: new Date().toISOString(), viewport, regions };
  }, TEST_IDS);
}

/**
 * Compact DOM tree for LLM consumption (depth-limited).
 * @param {import('playwright').Page} page
 * @param {{ maxDepth?: number, maxNodes?: number }} [options]
 */
export async function collectDomSummary(page, options = {}) {
  const { maxDepth = 6, maxNodes = 400 } = options;
  return page.evaluate(
    ({ maxDepth, maxNodes }) => {
      let count = 0;

      /** @param {Element} el @param {number} depth */
      function walk(el, depth) {
        if (count >= maxNodes || depth > maxDepth) return null;
        count += 1;

        const tag = el.tagName.toLowerCase();
        /** @type {Record<string, string>} */
        const attrs = {};
        for (const name of ['data-testid', 'role', 'aria-label', 'data-text-as-pseudo-element']) {
          const v = el.getAttribute(name);
          if (v) attrs[name] = v;
        }

        const text = (el.childNodes.length === 1 && el.childNodes[0].nodeType === Node.TEXT_NODE
          ? el.textContent?.trim().slice(0, 80)
          : undefined);

        /** @type {Array<ReturnType<typeof walk>>} */
        const children = [];
        if (depth < maxDepth) {
          for (const child of el.children) {
            const node = walk(child, depth + 1);
            if (node) children.push(node);
            if (count >= maxNodes) break;
          }
        }

        return { tag, attrs, text, children };
      }

      const body = document.body;
      return body ? walk(body, 0) : null;
    },
    { maxDepth, maxNodes }
  );
}

/**
 * @param {Awaited<ReturnType<typeof collectLayoutMetrics>>} before
 * @param {Awaited<ReturnType<typeof collectLayoutMetrics>>} after
 * @param {{ thresholdPx?: number }} [options]
 */
export function diffLayoutMetrics(before, after, options = {}) {
  const thresholdPx = options.thresholdPx ?? LAYOUT_DIFF_THRESHOLD_PX;

  /** @type {Map<string, { testId: string, x: number, y: number, width: number, height: number }>} */
  const beforeMap = new Map(before.regions.map((r) => [r.testId, r]));
  /** @type {Map<string, { testId: string, x: number, y: number, width: number, height: number }>} */
  const afterMap = new Map(after.regions.map((r) => [r.testId, r]));

  /** @type {Array<{ testId: string, field: string, before: number, after: number, delta: number, flagged: boolean }>} */
  const changes = [];

  for (const [testId, a] of afterMap) {
    const b = beforeMap.get(testId);
    if (!b) {
      changes.push({ testId, field: 'presence', before: 0, after: 1, delta: 1, flagged: true });
      continue;
    }
    for (const field of ['x', 'y', 'width', 'height']) {
      const delta = a[field] - b[field];
      if (delta !== 0) {
        changes.push({
          testId,
          field,
          before: b[field],
          after: a[field],
          delta,
          flagged: Math.abs(delta) > thresholdPx,
        });
      }
    }
  }

  for (const testId of beforeMap.keys()) {
    if (!afterMap.has(testId)) {
      changes.push({ testId, field: 'presence', before: 1, after: 0, delta: -1, flagged: true });
    }
  }

  const flagged = changes.filter((c) => c.flagged);
  const cardWidthChange = changes.find(
    (c) => CARD_REGION_IDS.includes(c.testId) && c.field === 'width'
  );

  return {
    thresholdPx,
    changeCount: changes.length,
    flaggedCount: flagged.length,
    flagged,
    changes,
    summary: cardWidthChange
      ? `${cardWidthChange.testId} width ${cardWidthChange.before}px → ${cardWidthChange.after}px (Δ${cardWidthChange.delta}px)`
      : flagged.length
        ? `${flagged.length} layout change(s) exceed ${thresholdPx}px`
        : 'No significant layout changes',
    regressionLikely: flagged.some(
      (c) => CARD_REGION_IDS.includes(c.testId) && (c.field === 'width' || c.field === 'height')
    ),
  };
}
