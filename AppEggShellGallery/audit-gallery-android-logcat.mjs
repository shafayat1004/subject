/**
 * adb logcat capture for Android gallery audits (ReactNativeJS / ReactNative errors).
 */

import { spawn } from 'child_process';
import { appendFileSync, mkdirSync } from 'fs';
import { join } from 'path';
import { classifyTag, textIsDevNoise, isActionable } from './audit-gallery-classify.mjs';

/**
 * @param {string} line
 */
function parseLogcatLine(line) {
  // 06-27 12:00:00.000  1234  5678 E ReactNativeJS: message
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

export class LogcatCapture {
  /**
   * @param {{ passDir: string, packageName?: string }} options
   */
  constructor(options) {
    this.passDir = options.passDir;
    this.packageName = options.packageName ?? 'com.eggshell.appgallery';
    this.entries = [];
    /** @type {import('child_process').ChildProcessWithoutNullStreams | null} */
    this.proc = null;
    this.fullLogPath = join(this.passDir, 'logcat-full.log');
    mkdirSync(this.passDir, { recursive: true });
  }

  start() {
    if (this.proc) return;
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
      appendFileSync(this.fullLogPath, chunk);
    });
  }

  stop() {
    if (!this.proc) return;
    this.proc.kill('SIGTERM');
    this.proc = null;
  }

  /**
   * Clear logcat buffer before a component page.
   */
  async clearBuffer() {
    await new Promise((resolve) => {
      const p = spawn('adb', ['logcat', '-c']);
      p.on('close', () => resolve());
      p.on('error', () => resolve());
    });
  }

  /**
   * Read recent logcat since last clear for one component.
   * @param {string} component
   * @param {string} pageLogPath
   */
  async collectForComponent(component, pageLogPath) {
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
        const pageEntries = [];
        for (const line of out.split('\n')) {
          const parsed = parseLogcatLine(line.trim());
          if (!parsed || !parsed.text) continue;
          const entry = {
            at: new Date().toISOString(),
            component,
            source: parsed.tag === 'AndroidRuntime' ? 'pageerror' : 'console',
            type: parsed.type,
            text: parsed.text,
            tag: parsed.tag,
            classify: classifyTag(parsed.text, parsed.type),
          };
          pageEntries.push(entry);
          appendFileSync(pageLogPath, `[${entry.at}] [${entry.tag}] [${entry.type}] ${entry.text}\n`);
        }
        this.entries.push(...pageEntries);
        resolve(pageEntries);
      });
      p.on('error', () => resolve([]));
    });
  }
}

/**
 * @param {Array<{ classify: string, type: string, text: string }>} entries
 */
export function filterActionable(entries) {
  return entries.filter(
    (e) => isActionable(e.classify, e.type, e.text) && !textIsDevNoise(e.classify, e.text)
  );
}

/**
 * Verify adb and a device/emulator are available.
 */
export async function assertAdbReady() {
  return new Promise((resolve, reject) => {
    const p = spawn('adb', ['devices']);
    let out = '';
    p.stdout.on('data', (d) => {
      out += d.toString();
    });
    p.on('close', (code) => {
      if (code !== 0) {
        reject(new Error('adb devices failed — is Android SDK platform-tools on PATH?'));
        return;
      }
      const lines = out
        .split('\n')
        .slice(1)
        .map((l) => l.trim())
        .filter((l) => l && !l.startsWith('*'));
      const connected = lines.filter((l) => l.endsWith('device'));
      if (!connected.length) {
        reject(new Error('No Android device/emulator connected (adb devices).'));
        return;
      }
      resolve(connected);
    });
    p.on('error', (e) => reject(new Error(`adb not found: ${e.message}`)));
  });
}
