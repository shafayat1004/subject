/**
 * Console classification for AppTodo observe (aligned with gallery audit patterns).
 */

/**
 * @param {string} text
 * @param {string} type  console | warning | error | pageerror
 */
export function classifyConsole(text, type) {
  const t = text.toLowerCase();

  if (type === 'pageerror') return { bucket: 'actionable', kind: 'uncaught' };
  if (t.includes('possible style leak')) return { bucket: 'style-leak', kind: 'style-leak' };
  if (t.includes('objects are not valid as a react child')) return { bucket: 'actionable', kind: 'react-child' };
  if (t.includes('typeerror') || t.includes('referenceerror')) return { bucket: 'actionable', kind: 'runtime' };

  if (
    t.includes('legacy childcontexttypes') ||
    t.includes('finddomnode is deprecated') ||
    t.includes('unique "key" prop') ||
    t.includes('react router future flag') ||
    t.includes('[hmr]') ||
    t.includes('webpack-dev-server') ||
    t.includes('[consolestelemetrysink]') ||
    t.includes('download the react devtools') ||
    t.includes('failed to decode downloaded font')
  ) {
    return { bucket: 'noise', kind: 'dev-noise' };
  }

  if (type === 'error' && t.startsWith('warning:')) return { bucket: 'noise', kind: 'react-dev-warning' };
  if (type === 'warning') return { bucket: 'noise', kind: 'browser-warning' };
  if (type === 'error') return { bucket: 'actionable', kind: 'console-error' };

  return { bucket: 'other', kind: 'log' };
}

/**
 * @param {string} text
 * @param {string} type
 */
export function isActionableConsole(text, type) {
  const { bucket } = classifyConsole(text, type);
  return bucket === 'actionable';
}

/**
 * @param {string} text
 */
export function isStyleLeak(text) {
  return text.toLowerCase().includes('possible style leak');
}

/**
 * @param {Array<{ type: string, text: string }>} entries
 */
export function summarizeConsole(entries) {
  let actionable = 0;
  let styleLeaks = 0;
  let noise = 0;
  for (const { type, text } of entries) {
    const { bucket } = classifyConsole(text, type);
    if (bucket === 'actionable') actionable += 1;
    else if (bucket === 'style-leak') styleLeaks += 1;
    else if (bucket === 'noise') noise += 1;
  }
  return { actionable, styleLeaks, noise };
}
