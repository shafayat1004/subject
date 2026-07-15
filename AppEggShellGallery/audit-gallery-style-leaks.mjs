/**
 * Detect and deduplicate ReactXP "possible style leak" console warnings.
 *
 * Each unique (sourceComponent, styleName) pair is logged once per gallery page;
 * repeat hits are counted without blowing up page logs or the console cap.
 */

import { normalizeLogText } from './audit-gallery-classify.mjs';

/** @typedef {{ styleName: string, sourceComponent: string | null, key: string, sample: string }} StyleLeakIdentity */

/**
 * @param {string} text
 * @returns {boolean}
 */
export function isStyleLeakMessage(text) {
  return text.toLowerCase().includes('possible style leak');
}

/**
 * Parse a ReactXP style-leak warning into a stable dedup key.
 * @param {string} text
 * @returns {StyleLeakIdentity | null}
 */
export function parseStyleLeak(text) {
  if (!isStyleLeakMessage(text)) return null;

  const styleName =
    text.match(/\bStyles[_a-zA-Z0-9]+/)?.[0] ??
    text.match(/\bstyle\s+["']?([A-Za-z0-9_.]+)["']?/i)?.[1] ??
    'unknown-style';

  const sourceComponent =
    text.match(/\b(?:LC|UIAuto|LibAutoUi|LibClient|LibRouter|ThirdParty)\.[A-Za-z0-9_.]+/)?.[0] ?? null;

  const key = sourceComponent ? `${sourceComponent}::${styleName}` : styleName;

  return {
    styleName,
    sourceComponent,
    key,
    sample: normalizeLogText(text).slice(0, 200),
  };
}

/**
 * Per gallery-page tracker: one log line per unique leak key, count repeats.
 */
export function createStyleLeakTracker() {
  /** @type {Map<string, { styleName: string, sourceComponent: string | null, count: number, sample: string, firstAt: string }>} */
  const byKey = new Map();

  return {
    /**
     * @param {StyleLeakIdentity} leak
     * @returns {{ isNew: boolean, count: number }}
     */
    record(leak) {
      const now = new Date().toISOString();
      const existing = byKey.get(leak.key);
      if (existing) {
        existing.count += 1;
        return { isNew: false, count: existing.count };
      }
      byKey.set(leak.key, {
        styleName: leak.styleName,
        sourceComponent: leak.sourceComponent,
        count: 1,
        sample: leak.sample,
        firstAt: now,
      });
      return { isNew: true, count: 1 };
    },

    /** @returns {{ uniqueCount: number, totalCount: number, leaks: Array<{ key: string, styleName: string, sourceComponent: string | null, count: number, sample: string }> }} */
    summary() {
      const leaks = [...byKey.entries()].map(([key, v]) => ({
        key,
        styleName: v.styleName,
        sourceComponent: v.sourceComponent,
        count: v.count,
        sample: v.sample,
      }));
      leaks.sort((a, b) => b.count - a.count || a.key.localeCompare(b.key));
      return {
        uniqueCount: leaks.length,
        totalCount: leaks.reduce((n, l) => n + l.count, 0),
        leaks,
      };
    },
  };
}

/**
 * Merge page-level summaries for pass / run reports.
 * @param {Array<{ component: string, styleLeaks?: ReturnType<ReturnType<typeof createStyleLeakTracker>['summary']> }>} pageResults
 */
export function aggregateStyleLeaks(pageResults) {
  /** @type {Map<string, { styleName: string, sourceComponent: string | null, totalCount: number, galleryPages: Set<string>, sample: string }>} */
  const global = new Map();

  for (const page of pageResults) {
    const summary = page.styleLeaks;
    if (!summary?.uniqueCount) continue;
    for (const leak of summary.leaks) {
      const existing = global.get(leak.key);
      if (existing) {
        existing.totalCount += leak.count;
        existing.galleryPages.add(page.component);
      } else {
        global.set(leak.key, {
          styleName: leak.styleName,
          sourceComponent: leak.sourceComponent,
          totalCount: leak.count,
          galleryPages: new Set([page.component]),
          sample: leak.sample,
        });
      }
    }
  }

  const leaks = [...global.entries()]
    .map(([key, v]) => ({
      key,
      styleName: v.styleName,
      sourceComponent: v.sourceComponent,
      totalCount: v.totalCount,
      galleryPages: [...v.galleryPages].sort(),
      sample: v.sample,
    }))
    .sort((a, b) => b.totalCount - a.totalCount || a.key.localeCompare(b.key));

  return {
    uniqueLeakKeys: leaks.length,
    totalHits: leaks.reduce((n, l) => n + l.totalCount, 0),
    galleryPagesAffected: [...new Set(pageResults.filter((p) => p.styleLeaks?.uniqueCount).map((p) => p.component))].sort(),
    leaks,
  };
}

/**
 * @param {{ uniqueCount: number, totalCount: number, leaks: Array<{ key: string, styleName: string, sourceComponent: string | null, count: number }> }} summary
 * @param {string} [galleryPage]
 */
export function formatStyleLeakSummaryLine(summary, galleryPage = '') {
  if (!summary.uniqueCount) return '';
  const parts = summary.leaks.slice(0, 4).map((l) => {
    const who = l.sourceComponent ?? galleryPage ?? '?';
    const hits = l.count > 1 ? ` ×${l.count}` : '';
    return `${who}/${l.styleName}${hits}`;
  });
  const more = summary.uniqueCount > 4 ? ` (+${summary.uniqueCount - 4} more)` : '';
  return `${summary.uniqueCount} unique, ${summary.totalCount} hits: ${parts.join('; ')}${more}`;
}
