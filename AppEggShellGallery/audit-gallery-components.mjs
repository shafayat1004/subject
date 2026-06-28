/**
 * Discover gallery component routes from the generated router (source of truth).
 * Compare audit script coverage vs route table.
 */

import { readFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const ROOT = dirname(fileURLToPath(import.meta.url));

const ROUTER_PATH = join(
  ROOT,
  'src/Components/Route/Components/Components.fs'
);

/** Gallery routes that exercise recent LibClient style-leak memo fixes. */
export const STYLE_LEAK_FIX_SCOPE = [
  'Button',
  'LabelledFormField',
  'Input_Text',
  'Input_Picker',
];

/** Fix scope plus pages that embed pickers/grids/forms (high leak counts). */
export const STYLE_LEAK_HIGH_VALUE_SCOPE = [
  ...STYLE_LEAK_FIX_SCOPE,
  'Forms',
  'Grid',
  'QueryGrid',
  'Input_LocalTime',
];

/** @typedef {'style-leak-fix' | 'style-leak-high-value'} AuditScopePreset */

/** @type {Record<AuditScopePreset, string[]>} */
export const AUDIT_SCOPE_PRESETS = {
  'style-leak-fix': STYLE_LEAK_FIX_SCOPE,
  'style-leak-high-value': STYLE_LEAK_HIGH_VALUE_SCOPE,
};

/**
 * @param {string | undefined} onlyFlag --only= preset name or comma-separated route names
 * @param {string[]} discovered
 * @returns {string[]}
 */
export function resolveAuditComponentScope(onlyFlag, discovered) {
  if (!onlyFlag) return discovered;
  const preset = AUDIT_SCOPE_PRESETS[/** @type {AuditScopePreset} */ (onlyFlag)];
  const names = preset ?? onlyFlag.split(',').map((s) => s.trim()).filter(Boolean);
  const discoveredSet = new Set(discovered);
  const unknown = names.filter((n) => !discoveredSet.has(n));
  if (unknown.length) {
    throw new Error(
      `Unknown --only component(s): ${unknown.join(', ')}. ` +
        `Presets: ${Object.keys(AUDIT_SCOPE_PRESETS).join(', ')}`
    );
  }
  return names;
}

/** Labels we must never click (OS file picker, etc.). */
export const SKIP_CLICK_LABELS = new Set([
  'Select File',
  'Remove Selected',
  'Upload',
  'Choose File',
  'Browse',
]);

/**
 * @returns {string[]}
 */
export function discoverGalleryComponents() {
  if (!existsSync(ROUTER_PATH)) {
    throw new Error(`Gallery router not found: ${ROUTER_PATH}. Run eggshell build first.`);
  }
  const text = readFileSync(ROUTER_PATH, 'utf8');
  const names = new Set();
  for (const m of text.matchAll(/\|\s+(?:ComponentItem\.)?(\w+)\s+->/g)) {
    const n = m[1];
    if (n !== '_') names.add(n);
  }
  return [...names].sort();
}

/**
 * @param {string[]} discovered
 * @param {string[]} audited
 * @param {{ interactionHandlers?: Record<string, unknown>, assertionHandlers?: Record<string, unknown> }} recipeKeys
 */
export function buildCoverageReport(discovered, audited, recipeKeys = {}) {
  const discoveredSet = new Set(discovered);
  const auditedSet = new Set(audited);
  const interactionKeys = new Set(Object.keys(recipeKeys.interactionHandlers ?? {}));
  const assertionKeys = new Set(Object.keys(recipeKeys.assertionHandlers ?? {}));
  interactionKeys.delete('_default');
  assertionKeys.delete('_default');

  const missingFromAudit = discovered.filter((n) => !auditedSet.has(n));
  const staleInAudit = audited.filter((n) => !discoveredSet.has(n));
  const genericInteractionOnly = audited.filter(
    (n) => discoveredSet.has(n) && !interactionKeys.has(n)
  );
  const genericAssertionOnly = audited.filter(
    (n) => discoveredSet.has(n) && !assertionKeys.has(n)
  );

  return {
    discoveredCount: discovered.length,
    auditedCount: audited.length,
    missingFromAudit,
    staleInAudit,
    genericInteractionOnly,
    genericAssertionOnly,
    discovered,
    audited,
  };
}
