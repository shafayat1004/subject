/**
 * Device log streaming for native observe sessions (gallery-aligned logcat patterns).
 */

import { spawn } from 'child_process';
import { appendFileSync, mkdirSync } from 'fs';
import { join } from 'path';
import { classifyConsole } from './log-classify.mjs';
import { findRenderErrorInLogEntries } from './render-error-signals.mjs';

/**
 * @param {string} line
 */
function parseLogcatLine(line) {
  const trimmed = line.trim();
  if (!trimmed || trimmed.startsWith('---------')) return null;

  // Filtered compact: 06-28 15:02:12.744 E/ReactNativeJS( 4034): message
  const compact = trimmed.match(
    /^\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{3}\s+([VDIWEF])\/([^:(]+)(?:\(\s*\d+\s*\))?:\s*(.*)$/
  );
  if (compact) {
    const [, level, tag, text] = compact;
    return { tag, type: levelToType(level), text, raw: line };
  }

  // Standard time: 06-28 15:02:12.744 4034 5678 E ReactNativeJS: message
  const standard = trimmed.match(
    /^\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{3}\s+\d+\s+\d+\s+([VDIWEF])\s+([^:]+):\s*(.*)$/
  );
  if (standard) {
    const [, level, tag, text] = standard;
    return { tag: tag.trim(), type: levelToType(level), text, raw: line };
  }

  return null;
}

/** @param {string} level */
function levelToType(level) {
  if (level === 'E') return 'error';
  if (level === 'W') return 'warning';
  if (level === 'I') return 'info';
  return 'log';
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
      'ReactNativeJS:W',
      'ReactNativeJS:E',
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
        'ReactNativeJS:W',
        'ReactNativeJS:E',
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

  /** @returns {{ detail: string, raw: string } | null} */
  findRenderError() {
    return findRenderErrorInLogEntries(this.entries);
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
 * Streaming iOS simulator log collector (React / error lines).
 */
export class IosLogCollector {
  /**
   * @param {{ outDir?: string }} [options]
   */
  constructor(options = {}) {
    this.outDir = options.outDir ?? null;
    /** @type {Array<{ at: string, source: string, type: string, tag: string, text: string }>} */
    this.entries = [];
    /** @type {import('child_process').ChildProcessWithoutNullStreams | null} */
    this.proc = null;
    this.fullLogPath = this.outDir ? join(this.outDir, 'session-ios.log') : null;
  }

  start() {
    if (this.proc) return;
    if (this.outDir) mkdirSync(this.outDir, { recursive: true });

    this.proc = spawn(
      'xcrun',
      ['simctl', 'spawn', 'booted', 'log', 'stream', '--style', 'compact', '--level', 'debug'],
      { stdio: ['ignore', 'pipe', 'pipe'] }
    );
    this.proc.stdout.on('data', (buf) => {
      const chunk = buf.toString();
      if (this.fullLogPath) appendFileSync(this.fullLogPath, chunk);
      for (const line of chunk.split('\n')) {
        const text = line.trim();
        if (!text) continue;
        if (!/react|error|exception|render|native module|crypto|apptodo|eggshell/i.test(text)) continue;
        this.entries.push({
          at: new Date().toISOString(),
          source: 'console',
          type: /error|exception/i.test(text) ? 'error' : 'log',
          tag: 'simlog',
          text,
        });
        if (this.entries.length > 4000) this.entries.splice(0, 500);
      }
    });
  }

  stop() {
    if (!this.proc) return;
    this.proc.kill('SIGTERM');
    this.proc = null;
  }

  /** @returns {{ detail: string, raw: string } | null} */
  findRenderError() {
    return findRenderErrorInLogEntries(this.entries);
  }

  /** @returns {{ consoleLines: string[], pageErrors: string[], networkErrors: string[] }} */
  toSessionLogs() {
    const consoleLines = [];
    const pageErrors = [];
    for (const e of this.entries) {
      const line = `[${e.tag}] ${e.text}`;
      if (e.type === 'error') pageErrors.push(line);
      else consoleLines.push(line);
    }
    return { consoleLines, pageErrors, networkErrors: [] };
  }

  summarize() {
    let actionable = 0;
    for (const e of this.entries) {
      const { bucket } = classifyConsole(e.text, e.type);
      if (bucket === 'actionable') actionable += 1;
    }
    return { actionable, styleLeaks: 0, noise: 0, total: this.entries.length };
  }
}

/**
 * @param {'android' | 'ios'} platform
 * @param {{ outDir?: string, packageName?: string }} [options]
 */
export function createDeviceLogCollector(platform, options = {}) {
  if (platform === 'android') return new AndroidLogCollector(options);
  if (platform === 'ios') return new IosLogCollector(options);
  return null;
}
