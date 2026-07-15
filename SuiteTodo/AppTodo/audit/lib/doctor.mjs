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
import { buildDevPlaybooks } from './doctor-playbooks.mjs';
import { collectToolchainChecks } from './doctor-toolchain.mjs';
import { collectDeviceInventory, buildDeviceChecks } from './device-targets.mjs';

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
 * @param {{ baseUrl?: string, appium?: { id: string, ok: boolean, detail: string }, metro?: { id: string, ok: boolean, detail: string }, inventory?: ReturnType<typeof collectDeviceInventory> }} ctx
 */
async function collectPlatformChecks(platform, ctx = {}) {
  const baseUrl = ctx.baseUrl ?? DEFAULTS.baseUrl;
  const inventory = ctx.inventory;
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

    const connected = inventory?.adbDevices.filter((d) => d.state === 'device') ?? [];
    const deviceLine = connected[0];
    checks.push({
      id: 'adb-device',
      ok: Boolean(deviceLine),
      detail: deviceLine
        ? `Connected · ${deviceLine.udid}${deviceLine.avdName ? ` (${deviceLine.avdName})` : ''}`
        : inventory?.defaultAndroidAvd
          ? `No device — start: emulator -avd ${inventory.defaultAndroidAvd}`
          : inventory?.avds[0]
            ? `No device — start: emulator -avd ${inventory.avds[0]}`
            : 'No device — create/start an AVD in Android Studio',
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

    const booted = inventory?.bootedSimulators ?? [];
    const bootedOk = booted.length >= 1;
    let simDetail;
    if (booted.length === 0) {
      simDetail = inventory?.defaultIosSimulator
        ? `None booted — observe will boot: ${inventory.defaultIosSimulator}`
        : 'open -a Simulator  (or set default: npm run observe -- setup-devices)';
    } else if (booted.length === 1) {
      simDetail = `Booted · ${booted[0].name}`;
    } else {
      simDetail = inventory?.defaultIosSimulator
        ? `${booted.length} booted — observe uses default "${inventory.defaultIosSimulator}"`
        : `${booted.length} booted (${booted.map((s) => s.name).join(', ')}) — quit extras or set defaultIosSimulator`;
    }
    checks.push({
      id: 'ios-simulator',
      ok: bootedOk || Boolean(inventory?.defaultIosSimulator),
      informational: booted.length > 1 && Boolean(inventory?.defaultIosSimulator),
      detail: simDetail,
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
 * @param {{ baseUrl?: string, platforms?: Array<'web' | 'android' | 'ios'>, json?: boolean }} [options]
 */
export async function runDoctorAll(options = {}) {
  const baseUrl = options.baseUrl ?? DEFAULTS.baseUrl;
  const platforms = options.platforms ?? [PLATFORM.WEB, PLATFORM.ANDROID, PLATFORM.IOS];

  const needsAppium = platforms.includes(PLATFORM.ANDROID) || platforms.includes(PLATFORM.IOS);
  const needsMetro = platforms.includes(PLATFORM.ANDROID) || platforms.includes(PLATFORM.IOS);
  const appium = needsAppium ? await checkAppium() : null;
  const metro = needsMetro ? await checkMetro() : null;
  const inventory = collectDeviceInventory(run);
  const deviceChecks = buildDeviceChecks(inventory);

  /** @type {Array<Awaited<ReturnType<typeof collectPlatformChecks>>>} */
  const sections = [];
  for (const p of platforms) {
    sections.push(
      await collectPlatformChecks(p, {
        baseUrl,
        appium: appium ?? undefined,
        metro: metro ?? undefined,
        inventory,
      })
    );
  }

  const toolchain = collectToolchainChecks({ includeIos: platforms.includes(PLATFORM.IOS) });

  const report = {
    command: 'doctor',
    baseUrl,
    toolchain,
    devices: {
      inventory: {
        avds: inventory.avds,
        adbDevices: inventory.adbDevices,
        bootedSimulators: inventory.bootedSimulators.map((s) => s.name),
        simulatorCount: inventory.simulators.length,
        multipleIosBooted: inventory.multipleIosBooted,
        defaults: {
          defaultAndroidAvd: inventory.defaultAndroidAvd,
          defaultIosSimulator: inventory.defaultIosSimulator,
        },
      },
      checks: deviceChecks,
    },
    shared: [...(appium ? [appium] : [])],
    platforms: sections,
    readyCount: sections.filter((s) => s.ready).length,
    total: sections.length,
    allReady: sections.every((s) => s.ready),
    playbooks: buildDevPlaybooks({ platforms: sections }, { inventory, appRoot }),
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
  const inventory = collectDeviceInventory(run);

  if (options.platform && options.platform !== 'all') {
    const section = await collectPlatformChecks(
      options.platform === 'web' ? PLATFORM.WEB : options.platform === 'android' ? PLATFORM.ANDROID : PLATFORM.IOS,
      {
        baseUrl: options.baseUrl,
        appium:
          options.platform === 'android' || options.platform === 'ios'
            ? await checkAppium()
            : undefined,
        inventory,
      }
    );
    const report = {
      command: 'doctor',
      baseUrl: options.baseUrl ?? DEFAULTS.baseUrl,
      toolchain: collectToolchainChecks({
        includeIos: options.platform === 'ios',
      }),
      devices: {
        inventory: {
          avds: inventory.avds,
          adbDevices: inventory.adbDevices,
          bootedSimulators: inventory.bootedSimulators.map((s) => s.name),
          defaults: {
            defaultAndroidAvd: inventory.defaultAndroidAvd,
            defaultIosSimulator: inventory.defaultIosSimulator,
          },
        },
        checks: buildDeviceChecks(inventory),
      },
      shared: [],
      platforms: [section],
      readyCount: section.ready ? 1 : 0,
      total: 1,
      allReady: section.ready,
      playbooks: buildDevPlaybooks({ platforms: [section] }, { inventory, appRoot }),
    };
    if (!options.json) {
      console.log(renderDoctorUi(report));
    }
    return report;
  }

  return runDoctorAll(options);
}

export { renderDoctorUi };
