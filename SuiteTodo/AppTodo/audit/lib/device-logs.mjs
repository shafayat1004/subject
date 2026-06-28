/**
 * Device log streaming for native observe sessions (gallery-aligned logcat patterns).
 */

import { spawn } from 'child_process';
import { appendFileSync, mkdirSync } from 'fs';
import { join } from 'path';
import { classifyConsole } from './log-classify.mjs';

/**
 * @param {string} line
 */
function parseLogcatLine(line) {
  const m = line.match(
    /^\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{3}\s+\d+\s+\d+\s+([VDIWEF])\s+(\S+):\s*(.*)$/
  );
  if (!m) return null;
  const [, level, tag, text] = m;
  let type = 'log';
  if (level === 'E') type = 'error';
  else if (level === 'W') type = 'warning';
  else if (level === 'I') type = 'info';
  return { tag, type, text, raw: line };
}

/**
 * Streaming logcat collector — start at session open, flush on capture.
 */
export class AndroidLogCollector {
  /**
   * @param {{ outDir?: string, packageName?: string }} [options]
   */
  constructor(options = {}) {
    this.outDir = options.outDir ?? null;
    this.packageName = options.packageName ?? 'com.eggshell.apptodo';
    /** @type {Array<{ at: string, source: string, type: string, tag: string, text: string, classify: ReturnType<typeof classifyConsole> }>} */
    this.entries = [];
    /** @type {import('child_process').ChildProcessWithoutNullStreams | null} */
    this.proc = null;
    this.fullLogPath = this.outDir ? join(this.outDir, 'session-logcat.log') : null;
  }

  start() {
    if (this.proc) return;
    if (this.outDir) mkdirSync(this.outDir, { recursive: true });

    const args = [
      'logcat',
      '-v',
      'time',
      'ReactNativeJS:I',
      'ReactNative:W',
      'ReactNative:E',
      'AndroidRuntime:E',
      '*:S',
    ];
    this.proc = spawn('adb', args, { stdio: ['ignore', 'pipe', 'pipe'] });
    this.proc.stdout.on('data', (buf) => {
      const chunk = buf.toString();
      if (this.fullLogPath) appendFileSync(this.fullLogPath, chunk);
      for (const line of chunk.split('\n')) {
        const parsed = parseLogcatLine(line.trim());
        if (!parsed?.text) continue;
        const source = parsed.tag === 'AndroidRuntime' ? 'pageerror' : 'console';
        this.entries.push({
          at: new Date().toISOString(),
          source,
          type: parsed.type,
          tag: parsed.tag,
          text: parsed.text,
          classify: classifyConsole(parsed.text, source === 'pageerror' ? 'pageerror' : parsed.type),
        });
      }
    });
  }

  stop() {
    if (!this.proc) return;
    this.proc.kill('SIGTERM');
    this.proc = null;
  }

  async clearBuffer() {
    await new Promise((resolve) => {
      const p = spawn('adb', ['logcat', '-c']);
      p.on('close', () => resolve());
      p.on('error', () => resolve());
    });
  }

  /**
   * Snapshot recent logcat (includes buffer since session start + drain).
   */
  async snapshot() {
    return new Promise((resolve) => {
      const args = [
        'logcat',
        '-d',
        '-v',
        'time',
        'ReactNativeJS:I',
        'ReactNative:W',
        'ReactNative:E',
        'AndroidRuntime:E',
        '*:S',
      ];
      const p = spawn('adb', args);
      let out = '';
      p.stdout.on('data', (d) => {
        out += d.toString();
      });
      p.on('close', () => {
        const lines = out.split('\n').filter(Boolean);
        resolve(lines);
      });
      p.on('error', () => resolve([]));
    });
  }

  /** @returns {{ consoleLines: string[], pageErrors: string[], networkErrors: string[] }} */
  toSessionLogs() {
    const consoleLines = [];
    const pageErrors = [];
    for (const e of this.entries) {
      const line = `[${e.tag}] ${e.text}`;
      if (e.source === 'pageerror') pageErrors.push(line);
      else consoleLines.push(`[${e.type}] ${e.text}`);
    }
    return { consoleLines, pageErrors, networkErrors: [] };
  }

  summarize() {
    let actionable = 0;
    let styleLeaks = 0;
    let noise = 0;
    for (const e of this.entries) {
      const bucket = e.classify.bucket;
      if (bucket === 'actionable') actionable += 1;
      else if (bucket === 'style-leak') styleLeaks += 1;
      else if (bucket === 'noise') noise += 1;
    }
    return { actionable, styleLeaks, noise, total: this.entries.length };
  }
}

/**
 * @param {'android' | 'ios'} platform
 * @param {{ outDir?: string, packageName?: string }} [options]
 */
export function createDeviceLogCollector(platform, options = {}) {
  if (platform === 'android') return new AndroidLogCollector(options);
  return null;
}
