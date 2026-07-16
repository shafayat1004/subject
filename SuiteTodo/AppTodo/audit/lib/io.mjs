import { writeFileSync } from 'fs';
import { join } from 'path';

/**
 * @param {string} dir
 * @param {string} name
 * @param {unknown} data
 */
export function writeJson(dir, name, data) {
  writeFileSync(
    join(dir, name),
    `${JSON.stringify(data, (_key, value) => (typeof value === 'bigint' ? Number(value) : value), 2)}\n`,
    'utf8'
  );
}

/**
 * @param {string} dir
 * @param {string} name
 * @param {string[]} lines
 */
export function writeLines(dir, name, lines) {
  writeFileSync(join(dir, name), lines.length ? `${lines.join('\n')}\n` : '', 'utf8');
}
