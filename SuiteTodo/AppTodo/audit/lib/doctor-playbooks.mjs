/**
 * Multi-terminal setup playbooks shown by doctor (web + native dev + observe).
 */

import { existsSync } from 'fs';
import { join } from 'path';
import { METRO } from './native-config.mjs';
import { PLATFORM } from './platform.mjs';

const APP_CWD = 'SuiteTodo/AppTodo';

/**
 * @param {(cmd: string, args: string[]) => { ok: boolean, out: string }} run
 */
export function listEmulatorAvds(run) {
  const r = run('emulator', ['-list-avds']);
  if (!r.ok) return [];
  return r.out
    .split('\n')
    .map((s) => s.trim())
    .filter(Boolean);
}

/**
 * Build contextual terminal playbooks from doctor report state.
 * @param {{ platforms: Array<{ platform: string, ready: boolean, checks: Array<{ id: string, ok: boolean }> }> }} report
 * @param {{ avds?: string[], appRoot?: string }} ctx
 */
export function buildDevPlaybooks(report, ctx = {}) {
  const avds = ctx.avds ?? [];
  const firstAvd = avds[0];
  const hasAndroidDir = ctx.appRoot ? existsSync(join(ctx.appRoot, 'android')) : false;

  const webSection = report.platforms.find((p) => p.platform === PLATFORM.WEB);
  const androidSection = report.platforms.find((p) => p.platform === PLATFORM.ANDROID);
  const iosSection = report.platforms.find((p) => p.platform === PLATFORM.IOS);

  /** @type {Array<{ id: string, title: string, subtitle?: string, highlight?: boolean, terminals: Array<{ n: number, label: string, cwd?: string, lines: string[] }> }>} */
  const playbooks = [];

  playbooks.push({
    id: 'web',
    title: 'Web dev · 1 terminal',
    subtitle: 'Fake backend by default (no Todo DevelopmentHost needed)',
    highlight: webSection && !webSection.ready,
    terminals: [
      {
        n: 1,
        label: 'Webpack dev server',
        cwd: APP_CWD,
        lines: ['../../eggshell dev-web', '# → http://127.0.0.1:9080'],
      },
    ],
  });

  if (!hasAndroidDir) {
    playbooks.push({
      id: 'android-first',
      title: 'Android · first-time (1 terminal, then daily below)',
      subtitle: 'Scaffolds android/, builds APK, starts Metro, installs on emulator',
      highlight: true,
      terminals: [
        {
          n: 1,
          label: 'Scaffold + build + install',
          cwd: APP_CWD,
          lines: [
            '# Start an emulator first (see next playbook) OR plug in a device',
            '../../eggshell dev-android',
          ],
        },
      ],
    });
  }

  const emulatorLines = [
    '# Option A — Android Studio → Device Manager → ▶ on an AVD',
    'emulator -list-avds',
  ];
  if (firstAvd) {
    emulatorLines.push(`emulator -avd ${firstAvd}`);
  } else {
    emulatorLines.push('emulator -avd <AVD_NAME>   # from list above');
  }
  emulatorLines.push('# Leave this terminal running — closing it stops the emulator');

  playbooks.push({
    id: 'android-daily',
    title: 'Android dev · 4–5 terminals (keep each open)',
    subtitle: 'Day-to-day after android/ exists — Fable watch + Metro + emulator + observe',
    highlight: androidSection && !androidSection.ready,
    terminals: [
      { n: 1, label: 'Android emulator', lines: emulatorLines },
      {
        n: 2,
        label: 'Fable → JS watch',
        cwd: APP_CWD,
        lines: ['../../eggshell dev-native'],
      },
      {
        n: 3,
        label: 'Metro bundler',
        cwd: APP_CWD,
        lines: [
          'npx react-native start',
          '# or: ../../eggshell dev-native-server',
          `# → http://127.0.0.1:${METRO.port}`,
        ],
      },
      {
        n: 4,
        label: 'adb reverse (once per emulator boot)',
        lines: [`adb reverse tcp:${METRO.port} tcp:${METRO.port}`],
      },
      {
        n: 5,
        label: 'Appium (for observe / Appium tests)',
        cwd: APP_CWD,
        lines: ['npm run appium', '# → http://127.0.0.1:4723'],
      },
    ],
  });

  playbooks.push({
    id: 'android-observe',
    title: 'Android observe · after stack is up',
    subtitle: 'Run in any terminal once T1–T5 are healthy',
    highlight: false,
    terminals: [
      {
        n: 1,
        label: 'Doctor + snapshot',
        cwd: APP_CWD,
        lines: [
          'npm run observe:doctor',
          'npm run observe -- snapshot --platform android',
          'npm run observe -- workflow layout-check --platform android',
        ],
      },
    ],
  });

  playbooks.push({
    id: 'ios-daily',
    title: 'iOS dev · 4 terminals (macOS + Xcode)',
    subtitle: 'First time: ../../eggshell dev-ios from AppTodo',
    highlight: iosSection && !iosSection.ready,
    terminals: [
      {
        n: 1,
        label: 'Simulator',
        lines: ['open -a Simulator', '# or Xcode → Window → Devices and Simulators'],
      },
      {
        n: 2,
        label: 'Fable watch',
        cwd: APP_CWD,
        lines: ['../../eggshell dev-native'],
      },
      {
        n: 3,
        label: 'Metro',
        cwd: APP_CWD,
        lines: ['npx react-native start'],
      },
      {
        n: 4,
        label: 'Appium',
        cwd: APP_CWD,
        lines: ['npm run appium'],
      },
    ],
  });

  return playbooks;
}
