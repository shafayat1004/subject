import { mkdirSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
export const AUDIT_ROOT = join(__dirname, '..');
export const OUT_ROOT = join(AUDIT_ROOT, 'out');

/**
 * @param {string} [label]
 */
export function createRunDir(label = 'run') {
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  const dir = join(OUT_ROOT, `${stamp}-${label}`);
  mkdirSync(dir, { recursive: true });
  return dir;
}

/**
 * @param {string} dir
 */
export function ensureDir(dir) {
  if (!existsSync(dir)) mkdirSync(dir, { recursive: true });
  return dir;
}
