/**
 * Prerequisite checks for AppTodo observe — web, Android, and iOS.
 */

import { existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import { spawnSync } from 'child_process';
import { DEFAULTS } from './config.mjs';
import { PLATFORM } from './platform.mjs';
import { resolveAndroidApp, resolveIosApp, APPIUM, METRO } from './native-config.mjs';
import { waitForAppReady } from './web-session.mjs';
import { renderDoctorUi } from './doctor-ui.mjs';
import { buildDevPlaybooks, listEmulatorAvds } from './doctor-playbooks.mjs';

const appRoot = join(dirname(fileURLToPath(import.meta.url)), '../..');

const PLATFORM_LABELS = {
  [PLATFORM.WEB]: 'Web · Playwright',
  [PLATFORM.ANDROID]: 'Android · Appium',
  [PLATFORM.IOS]: 'iOS · XCUITest',
};

function run(cmd, args) {
  const r = spawnSync(cmd, args, { encoding: 'utf8' });
  return { ok: r.status === 0, out: (r.stdout ?? '') + (r.stderr ?? ''), code: r.status };
}

async function checkMetro() {
  const url = `http://127.0.0.1:${METRO.port}`;
  try {
    const res = await fetch(`${url}/status`, { signal: AbortSignal.timeout(2500) });
    if (res.ok) {
      return { id: 'metro', ok: true, detail: `Metro bundler on :${METRO.port}` };
    }
  } catch {
    /* try root */
  }
  try {
    const res = await fetch(url, { signal: AbortSignal.timeout(2500) });
    return {
      id: 'metro',
      ok: res.ok,
      detail: res.ok
        ? `Metro bundler on :${METRO.port}`
        : `Port :${METRO.port} open but not Metro — run: npx react-native start`,
    };
  } catch {
    return {
      id: 'metro',
      ok: false,
      detail: `Not running — in AppTodo: npx react-native start (or ../../eggshell dev-native-server)`,
    };
  }
}

/**
 * @param {string[]} avds
 */
function adbDeviceDetail(avds) {
  if (avds.length) {
    return `No device — start emulator: emulator -avd ${avds[0]} (or Android Studio → Device Manager)`;
  }
  return 'No device — create/start an AVD in Android Studio → Device Manager, then: emulator -list-avds';
}

async function checkAppium() {
  try {
    const res = await fetch(`http://${APPIUM.host}:${APPIUM.port}/status`, {
      signal: AbortSignal.timeout(3000),
    });
    return {
      id: 'appium',
      ok: res.ok,
      detail: res.ok ? `Appium listening on :${APPIUM.port}` : `HTTP ${res.status} from Appium`,
    };
  } catch (e) {
    return {
      id: 'appium',
      ok: false,
      detail: `Not reachable — run: npm run appium (${e.message ?? e})`,
    };
  }
}

/**
 * @param {'web' | 'android' | 'ios'} platform
 * @param {{ baseUrl?: string, appium?: { id: string, ok: boolean, detail: string }, metro?: { id: string, ok: boolean, detail: string }, avds?: string[] }} ctx
 */
async function collectPlatformChecks(platform, ctx = {}) {
  const baseUrl = ctx.baseUrl ?? DEFAULTS.baseUrl;
  /** @type {Array<{ id: string, ok: boolean, detail: string, informational?: boolean }>} */
  const checks = [];

  if (platform === PLATFORM.WEB) {
    const ready = await waitForAppReady(baseUrl);
    checks.push({
      id: 'dev-web',
      ok: ready,
      detail: ready ? `${baseUrl} reachable` : `Start ../../eggshell dev-web (${baseUrl})`,
    });
  }

  if (platform === PLATFORM.ANDROID) {
    const androidDir = join(appRoot, 'android');
    checks.push({
      id: 'android-project',
      ok: existsSync(androidDir),
      detail: existsSync(androidDir)
        ? 'android/ scaffold present'
        : 'Missing android/ — run ../../eggshell dev-android',
    });

    const adb = run('adb', ['devices']);
    const deviceLine = adb.out.split('\n').find((l) => l.trim().endsWith('device') && !l.includes('List'));
    const avds = ctx.avds ?? [];
    checks.push({
      id: 'adb-device',
      ok: Boolean(deviceLine),
      detail: deviceLine ? `Connected · ${deviceLine.split('\t')[0]}` : adbDeviceDetail(avds),
    });

    if (ctx.metro) {
      checks.push({ ...ctx.metro, id: 'metro-bundler' });
    }

    const app = resolveAndroidApp(appRoot);
    checks.push({
      id: 'android-package',
      ok: true,
      informational: true,
      detail: `${app.package} · ${app.activity}`,
    });

    const reverse = run('adb', ['reverse', '--list']);
    checks.push({
      id: 'metro-reverse',
      ok: reverse.out.includes(`tcp:${METRO.port}`),
      detail: reverse.out.includes(`tcp:${METRO.port}`)
        ? `adb reverse tcp:${METRO.port} → host`
        : `Run: adb reverse tcp:${METRO.port} tcp:${METRO.port}`,
    });

    if (ctx.appium) {
      checks.push({ ...ctx.appium, id: 'appium-android' });
    }
  }

  if (platform === PLATFORM.IOS) {
    const iosDir = join(appRoot, 'ios');
    checks.push({
      id: 'ios-project',
      ok: existsSync(iosDir),
      detail: existsSync(iosDir)
        ? 'ios/ scaffold present'
        : 'Missing ios/ — run ../../eggshell dev-ios (macOS + Xcode)',
    });

    const sim = run('xcrun', ['simctl', 'list', 'devices', 'booted']);
    const booted = /Booted/.test(sim.out);
    checks.push({
      id: 'ios-simulator',
      ok: booted,
      detail: booted ? 'Simulator booted' : 'open -a Simulator  (or Xcode → Device Manager)',
    });

    if (ctx.metro) {
      checks.push({ ...ctx.metro, id: 'metro-bundler' });
    }

    const app = resolveIosApp(appRoot);
    checks.push({
      id: 'ios-bundle',
      ok: true,
      informational: true,
      detail: app.bundleId,
    });

    if (ctx.appium) {
      checks.push({ ...ctx.appium, id: 'appium-ios' });
    }
  }

  const ready = checks.every((c) => c.ok || c.informational);
  return {
    platform,
    label: PLATFORM_LABELS[platform] ?? platform,
    ready,
    checks,
  };
}

/**
 * Run doctor for web + Android + iOS (default).
 * @param {{ baseUrl?: string, platforms?: Array<'web' | 'android' | 'ios'>, json?: boolean }} [options]
 */
export async function runDoctorAll(options = {}) {
  const baseUrl = options.baseUrl ?? DEFAULTS.baseUrl;
  const platforms = options.platforms ?? [PLATFORM.WEB, PLATFORM.ANDROID, PLATFORM.IOS];

  const needsAppium = platforms.includes(PLATFORM.ANDROID) || platforms.includes(PLATFORM.IOS);
  const needsMetro = platforms.includes(PLATFORM.ANDROID) || platforms.includes(PLATFORM.IOS);
  const appium = needsAppium ? await checkAppium() : null;
  const metro = needsMetro ? await checkMetro() : null;
  const avds = listEmulatorAvds(run);

  /** @type {Array<Awaited<ReturnType<typeof collectPlatformChecks>>>} */
  const sections = [];
  for (const p of platforms) {
    sections.push(
      await collectPlatformChecks(p, {
        baseUrl,
        appium: appium ?? undefined,
        metro: metro ?? undefined,
        avds,
      })
    );
  }

  const report = {
    command: 'doctor',
    baseUrl,
    shared: [
      ...(appium ? [appium] : []),
      ...(avds.length
        ? [{ id: 'emulator-avds', ok: true, informational: true, detail: `AVDs installed: ${avds.join(', ')}` }]
        : [
            {
              id: 'emulator-avds',
              ok: false,
              detail: 'No AVDs found — Android Studio → Device Manager → Create Device',
            },
          ]),
    ],
    platforms: sections,
    readyCount: sections.filter((s) => s.ready).length,
    total: sections.length,
    allReady: sections.every((s) => s.ready),
    avds,
    playbooks: buildDevPlaybooks(
      { platforms: sections },
      { avds, appRoot }
    ),
  };

  if (!options.json) {
    console.log(renderDoctorUi(report));
  }

  return report;
}

/**
 * @param {{ platform?: string, baseUrl?: string, json?: boolean }} [options]
 */
export async function runDoctor(options = {}) {
  if (options.platform && options.platform !== 'all') {
    const section = await collectPlatformChecks(
      options.platform === 'web' ? PLATFORM.WEB : options.platform === 'android' ? PLATFORM.ANDROID : PLATFORM.IOS,
      {
        baseUrl: options.baseUrl,
        appium:
          options.platform === 'android' || options.platform === 'ios'
            ? await checkAppium()
            : undefined,
      }
    );
    const report = {
      command: 'doctor',
      baseUrl: options.baseUrl ?? DEFAULTS.baseUrl,
      shared: [],
      platforms: [section],
      readyCount: section.ready ? 1 : 0,
      total: 1,
      allReady: section.ready,
      avds: listEmulatorAvds(run),
      playbooks: buildDevPlaybooks({ platforms: [section] }, { avds: listEmulatorAvds(run), appRoot }),
    };
    if (!options.json) {
      console.log(renderDoctorUi(report));
    }
    return report;
  }

  return runDoctorAll(options);
}

export { renderDoctorUi };
