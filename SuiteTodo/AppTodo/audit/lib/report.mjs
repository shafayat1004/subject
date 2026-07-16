import { writeFileSync } from 'fs';
import { join } from 'path';

/**
 * Print JSON summary to stdout (primary LLM entry) and optionally save report.
 * @param {unknown} report
 * @param {{ outDir?: string, fileName?: string, pretty?: boolean }} [options]
 */
export function emitReport(report, options = {}) {
  const { outDir, fileName = 'report.json', pretty = true } = options;
  const text = pretty ? JSON.stringify(report, null, 2) : JSON.stringify(report);
  console.log(text);
  if (outDir) {
    writeFileSync(join(outDir, fileName), `${text}\n`, 'utf8');
  }
}

/**
 * One-line status for agents scanning terminal output.
 * @param {'ok' | 'warn' | 'fail'} status
 * @param {string} message
 */
export function emitStatus(status, message) {
  console.log(`OBSERVE_${status.toUpperCase()}: ${message}`);
}
