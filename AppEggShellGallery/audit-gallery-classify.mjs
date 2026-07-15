/**
 * Shared console/log classification for web and Android gallery audits.
 */

export function normalizeLogText(text) {
  return text
    .replace(/webpack-internal:\/\/\/\S+/g, '<bundle>')
    .replace(/https?:\/\/[^\s)]+/g, '<url>')
    .replace(/\s+/g, ' ')
    .trim();
}

/**
 * @param {string} text
 * @param {string} type  console | warning | error | pageerror | log
 * @returns {string}
 */
export function classifyTag(text, type) {
  const t = text.toLowerCase();
  if (type === 'pageerror') return 'uncaught';
  if (t.includes('possible style leak')) return 'style-leak';
  if (t.includes('objects are not valid as a react child')) return 'react-child';
  if (t.includes('validatedomnesting') || t.includes('validateomnesting')) return 'dom-nesting';
  if (t.includes('invalidvalueerror') || t.includes('typeerror') || t.includes('referenceerror')) {
    return 'runtime';
  }
  if (t.includes('minified react error') || /error #\d+/.test(t)) return 'react-minified';
  if (type === 'error') return 'console-error';
  if (type === 'warning') return 'console-warning';
  if (type === 'info') return 'info';
  return 'log';
}

/**
 * Full audit bucket (actionable / noise / other).
 * @param {string} text
 * @param {string} type
 */
export function classifyForFullAudit(text, type) {
  const t = text.toLowerCase();
  const n = normalizeLogText(text);

  if (type === 'pageerror') {
    return { bucket: 'actionable', kind: 'uncaught-exception', summary: n.slice(0, 200) };
  }
  if (t.includes('objects are not valid as a react child')) {
    return { bucket: 'actionable', kind: 'invalid-react-child', summary: n.slice(0, 200) };
  }
  if (t.includes('validatereactnesting') || t.includes('validateomnesting')) {
    return { bucket: 'actionable', kind: 'invalid-dom-nesting', summary: n.slice(0, 200) };
  }
  if (t.includes('invalidvalueerror') || t.includes('typeerror') || t.includes('referenceerror')) {
    return { bucket: 'actionable', kind: 'runtime-error', summary: n.slice(0, 200) };
  }
  if (t.includes('minified react error') || /error #\d+/.test(t)) {
    return { bucket: 'actionable', kind: 'react-minified-error', summary: n.slice(0, 200) };
  }
  if (t.includes('failed to load') && t.includes('chunk')) {
    return { bucket: 'actionable', kind: 'chunk-load-failure', summary: n.slice(0, 200) };
  }

  if (t.includes('legacy childcontexttypes') || t.includes('legacy contexttypes')) {
    return { bucket: 'noise', kind: 'reactxp-legacy-context', summary: '' };
  }
  if (t.includes('finddomnode is deprecated')) {
    return { bucket: 'noise', kind: 'finddomnode-deprecated', summary: '' };
  }
  if (t.includes('unique "key" prop')) {
    return { bucket: 'noise', kind: 'missing-react-key', summary: n.slice(0, 120) };
  }
  if (t.includes('possible style leak')) {
    return { bucket: 'style-leak', kind: 'style-leak', summary: n.slice(0, 120) };
  }
  if (t.includes('react router future flag')) {
    return { bucket: 'noise', kind: 'react-router-future', summary: '' };
  }
  if (t.includes('[hmr]') || t.includes('webpack-dev-server')) {
    return { bucket: 'noise', kind: 'webpack-hmr', summary: '' };
  }
  if (t.includes('[consolestelemetrysink]') || t.includes('screenview:')) {
    return { bucket: 'noise', kind: 'telemetry', summary: '' };
  }
  if (t.includes('disallowed rule') && t.includes('filtered out')) {
    return { bucket: 'noise', kind: 'filtered-css-rule', summary: n.slice(0, 120) };
  }
  if (t.includes('running "rxapp"')) {
    return { bucket: 'noise', kind: 'rn-startup', summary: '' };
  }

  if (type === 'error' && t.startsWith('warning:')) {
    return { bucket: 'noise', kind: 'react-dev-warning', summary: n.slice(0, 120) };
  }
  if (type === 'warning') {
    return { bucket: 'noise', kind: 'browser-warning', summary: n.slice(0, 120) };
  }

  return { bucket: 'other', kind: 'log', summary: n.slice(0, 120) };
}

/**
 * @param {string} classifyTag
 * @param {string} type
 * @param {string} [text]
 */
export function isActionable(classifyTag, type, text = '') {
  if (type === 'pageerror' || classifyTag === 'uncaught') return true;
  if (['react-child', 'dom-nesting', 'runtime', 'react-minified'].includes(classifyTag)) return true;
  if (classifyTag === 'console-error' && !textIsDevNoise(classifyTag, text)) return true;
  return false;
}

/**
 * @param {string} _
 * @param {string} text
 */
export function textIsDevNoise(_, text) {
  const t = text.toLowerCase();
  return (
    t.includes('legacy childcontexttypes') ||
    t.includes('legacy contexttypes') ||
    t.includes('finddomnode is deprecated') ||
    t.includes('unique "key" prop') ||
    t.includes('possible style leak') ||
    t.includes('react router future flag') ||
    t.includes('[hmr]') ||
    t.includes('webpack-dev-server') ||
    t.includes('[consolestelemetrysink]') ||
    t.includes('screenview:') ||
    t.includes('running "rxapp"')
  );
}
