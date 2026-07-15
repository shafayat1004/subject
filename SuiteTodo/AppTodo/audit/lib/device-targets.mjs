/**
 * Android / iOS device inventory, defaults, and session target resolution.
 * Defaults live in audit/native.local.json (gitignored) — see native.local.json.example.
 */

import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import { execSync, spawnSync } from 'child_process';
import { loadNativeLocal, saveNativeLocal } from './native-config.mjs';
import { normalizeOrientation } from './device-orientation.mjs';
import { DEFAULTS } from './config.mjs';

const auditRoot = join(dirname(fileURLToPath(import.meta.url)), '..');

/**
 * @param {(cmd: string, args: string[]) => { ok: boolean, out: string }} run
 */
export function listAndroidAvds(run) {
  const r = run('emulator', ['-list-avds']);
  if (!r.ok) return [];
  return r.out
    .split('\n')
    .map((s) => s.trim())
    .filter(Boolean);
}

/**
 * @param {(cmd: string, args: string[]) => { ok: boolean, out: string }} run
 * @returns {Array<{ udid: string, state: string, avdName: string | null }>}
 */
export function listAdbDevices(run) {
  const r = run('adb', ['devices']);
  if (!r.ok) return [];

  /** @type {Array<{ udid: string, state: string, avdName: string | null }>} */
  const devices = [];
  for (const line of r.out.split('\n')) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('List') || !trimmed.includes('\t')) continue;
    const [udid, state] = trimmed.split('\t');
    if (!udid) continue;
    let avdName = null;
    if (udid.startsWith('emulator-') && state === 'device') {
      const avd = run('adb', ['-s', udid, 'emu', 'avd', 'name']);
      if (avd.ok) avdName = avd.out.trim().split('\n')[0]?.trim() || null;
    }
    devices.push({ udid, state, avdName });
  }
  return devices;
}

/**
 * @param {(cmd: string, args: string[]) => { ok: boolean, out: string }} [run]
 */
export function listIosSimulators(run = defaultRun) {
  const r = run('xcrun', ['simctl', 'list', 'devices', 'available', '-j']);
  if (!r.ok) return parseIosSimulatorsFallback();

  try {
    return parseIosSimulatorsJson(JSON.parse(r.out));
  } catch {
    return parseIosSimulatorsFallback();
  }
}

function parseIosSimulatorsFallback() {
  try {
    const out = execSync('xcrun simctl list devices available -j', { encoding: 'utf8' });
    return parseIosSimulatorsJson(JSON.parse(out));
  } catch {
    return [];
  }
}

/**
 * @param {Record<string, Array<{ name: string, udid: string, state?: string, isAvailable?: boolean }>>} data
 */
function parseIosSimulatorsJson(data) {
  /** @type {Array<{ name: string, udid: string, runtime: string, state: string }>} */
  const devices = [];
  for (const [runtimeKey, devs] of Object.entries(data.devices ?? {})) {
    const runtime = runtimeKey
      .replace('com.apple.CoreSimulator.SimRuntime.', '')
      .replace(/-/g, '.');
    for (const d of devs) {
      if (d.isAvailable === false) continue;
      devices.push({
        name: d.name,
        udid: d.udid,
        runtime,
        state: d.state ?? 'Shutdown',
      });
    }
  }
  return devices;
}

/**
 * @param {(cmd: string, args: string[]) => { ok: boolean, out: string }} run
 */
export function collectDeviceInventory(run = defaultRun) {
  const avds = listAndroidAvds(run);
  const adbDevices = listAdbDevices(run);
  const simulators = listIosSimulators(run);
  const bootedSimulators = simulators.filter((s) => s.state === 'Booted');
  const prefs = loadNativeLocal();

  return {
    avds,
    adbDevices,
    simulators,
    bootedSimulators,
    prefs,
    defaultAndroidAvd: prefs.defaultAndroidAvd ?? null,
    defaultIosSimulator: prefs.defaultIosSimulator ?? null,
    deviceOrientation: normalizeOrientation(
      /** @type {string | undefined} */ (prefs.deviceOrientation),
      DEFAULTS.deviceOrientation
    ),
    multipleIosBooted: bootedSimulators.length > 1,
  };
}

/**
 * Pick adb serial for observe / Appium sessions.
 * @param {{ log?: (msg: string) => void }} [options]
 */
export async function resolveAndroidSessionUdid(options = {}) {
  const log = options.log ?? (() => {});
  const run = defaultRun;
  const prefs = loadNativeLocal();
  const devices = listAdbDevices(run).filter((d) => d.state === 'device');

  if (!devices.length) {
    const avds = listAndroidAvds(run);
    const hint = prefs.defaultAndroidAvd
      ? `Start default AVD: emulator -avd ${prefs.defaultAndroidAvd}`
      : avds[0]
        ? `Start an emulator: emulator -avd ${avds[0]}`
        : 'Create an AVD in Android Studio → Device Manager';
    throw new Error(`No adb device connected. ${hint}`);
  }

  if (prefs.defaultAndroidUdid) {
    const match = devices.find((d) => d.udid === prefs.defaultAndroidUdid);
    if (match) {
      log(`android target: ${match.udid} (configured defaultAndroidUdid)`);
      return match.udid;
    }
    log(`warning: defaultAndroidUdid ${prefs.defaultAndroidUdid} not connected`);
  }

  if (prefs.defaultAndroidAvd) {
    const match = devices.find((d) => d.avdName === prefs.defaultAndroidAvd);
    if (match) {
      log(`android target: ${match.udid} (${prefs.defaultAndroidAvd})`);
      return match.udid;
    }
    if (devices.length === 1) {
      log(
        `warning: connected ${devices[0].udid} is ${devices[0].avdName ?? 'unknown AVD'}, ` +
          `expected default ${prefs.defaultAndroidAvd}`
      );
    }
  }

  if (devices.length > 1) {
    const names = devices.map((d) => `${d.udid}${d.avdName ? ` (${d.avdName})` : ''}`).join(', ');
    throw new Error(
      `Multiple adb devices: ${names}. ` +
        `Set defaultAndroidAvd or defaultAndroidUdid in audit/native.local.json — npm run observe -- setup-devices`
    );
  }

  log(`android target: ${devices[0].udid}${devices[0].avdName ? ` (${devices[0].avdName})` : ''}`);
  return devices[0].udid;
}

/**
 * Pick iOS simulator UDID for observe sessions. Boots default simulator if configured and shut down.
 * @param {{ log?: (msg: string) => void, bootIfNeeded?: boolean }} [options]
 */
export function resolveIosSessionUdid(options = {}) {
  const log = options.log ?? (() => {});
  const bootIfNeeded = options.bootIfNeeded !== false;
  const prefs = loadNativeLocal();
  const simulators = listIosSimulators();
  const booted = simulators.filter((s) => s.state === 'Booted');

  if (prefs.defaultIosSimulator) {
    const matches = simulators.filter((s) => s.name === prefs.defaultIosSimulator);
    if (!matches.length) {
      throw new Error(
        `defaultIosSimulator "${prefs.defaultIosSimulator}" not found. ` +
          `Run: npm run observe -- setup-devices --list`
      );
    }

    const bootedMatch = matches.find((s) => s.state === 'Booted');
    if (bootedMatch) {
      if (booted.length > 1) {
        log(
          `warning: ${booted.length} simulators booted — using configured default "${prefs.defaultIosSimulator}". ` +
            `Quit extras: Simulator → Window → close unused devices`
        );
      }
      log(`ios target: ${bootedMatch.name} (${bootedMatch.udid})`);
      return bootedMatch.udid;
    }

    const target = matches[0];
    if (bootIfNeeded) {
      log(`booting default simulator: ${target.name}`);
      execSync(`xcrun simctl boot ${target.udid}`, { stdio: 'pipe' });
      try {
        execSync('open -a Simulator', { stdio: 'ignore' });
      } catch {
        /* ok */
      }
      log(`ios target: ${target.name} (${target.udid})`);
      return target.udid;
    }

    throw new Error(
      `Default simulator "${prefs.defaultIosSimulator}" is not booted. ` +
        `Run: xcrun simctl boot "${target.udid}" && open -a Simulator`
    );
  }

  if (booted.length === 1) {
    log(`ios target: ${booted[0].name} (${booted[0].udid})`);
    return booted[0].udid;
  }

  if (booted.length > 1) {
    throw new Error(
      `${booted.length} simulators booted (${booted.map((s) => s.name).join(', ')}). ` +
        `Set defaultIosSimulator: npm run observe -- setup-devices --ios "iPhone 17 Pro Max"`
    );
  }

  throw new Error(
    'No booted iOS simulator. Boot one in Xcode, or set defaultIosSimulator: npm run observe -- setup-devices'
  );
}

/**
 * Doctor checks for device inventory + defaults.
 * @param {ReturnType<typeof collectDeviceInventory>} inventory
 */
export function buildDeviceChecks(inventory) {
  /** @type {Array<{ id: string, ok: boolean, detail: string, informational?: boolean }>} */
  const checks = [];

  if (inventory.avds.length) {
    const defaultTag = inventory.defaultAndroidAvd ? ` · default: ${inventory.defaultAndroidAvd}` : '';
    checks.push({
      id: 'android-avds',
      ok: true,
      informational: true,
      detail: `${inventory.avds.length} AVD(s): ${inventory.avds.join(', ')}${defaultTag}`,
    });
  } else {
    checks.push({
      id: 'android-avds',
      ok: false,
      detail: 'No AVDs — Android Studio → Device Manager → Create Device',
    });
  }

  const connected = inventory.adbDevices.filter((d) => d.state === 'device');
  if (connected.length) {
    const detail = connected
      .map((d) => `${d.udid}${d.avdName ? ` (${d.avdName})` : ''}`)
      .join(', ');
    checks.push({
      id: 'android-adb',
      ok: true,
      detail: `Connected: ${detail}`,
    });
  } else {
    const start =
      inventory.defaultAndroidAvd ?? inventory.avds[0]
        ? `emulator -avd ${inventory.defaultAndroidAvd ?? inventory.avds[0]}`
        : 'start an AVD';
    checks.push({
      id: 'android-adb',
      ok: false,
      detail: `No adb device — ${start}`,
    });
  }

  if (inventory.defaultAndroidAvd && !inventory.avds.includes(inventory.defaultAndroidAvd)) {
    checks.push({
      id: 'android-default-avd',
      ok: false,
      detail: `defaultAndroidAvd "${inventory.defaultAndroidAvd}" not installed`,
    });
  } else if (inventory.defaultAndroidAvd) {
    checks.push({
      id: 'android-default-avd',
      ok: true,
      informational: true,
      detail: `defaultAndroidAvd: ${inventory.defaultAndroidAvd}`,
    });
  } else if (inventory.avds.length) {
    checks.push({
      id: 'android-default-avd',
      ok: true,
      informational: true,
      detail: `No default AVD — npm run observe -- setup-devices --android ${inventory.avds[0]}`,
    });
  }

  const simCount = inventory.simulators.length;
  if (simCount) {
    const bootedNames = inventory.bootedSimulators.map((s) => s.name).join(', ') || 'none';
    const defaultTag = inventory.defaultIosSimulator ? ` · default: ${inventory.defaultIosSimulator}` : '';
    checks.push({
      id: 'ios-simulators',
      ok: true,
      informational: true,
      detail: `${simCount} simulator(s) · booted: ${bootedNames}${defaultTag}`,
    });
  } else {
    checks.push({
      id: 'ios-simulators',
      ok: false,
      detail: 'No iOS simulators found — install Xcode runtimes',
    });
  }

  if (inventory.multipleIosBooted) {
    const names = inventory.bootedSimulators.map((s) => s.name).join(', ');
    checks.push({
      id: 'ios-multi-booted',
      ok: !inventory.defaultIosSimulator,
      informational: Boolean(inventory.defaultIosSimulator),
      detail: inventory.defaultIosSimulator
        ? `${inventory.bootedSimulators.length} booted (${names}) — observe uses default "${inventory.defaultIosSimulator}"`
        : `${inventory.bootedSimulators.length} simulators booted (${names}) — quit extras or: npm run observe -- setup-devices --ios "…"`,
    });
  }

  if (inventory.defaultIosSimulator) {
    const exists = inventory.simulators.some((s) => s.name === inventory.defaultIosSimulator);
    checks.push({
      id: 'ios-default-sim',
      ok: exists,
      informational: exists,
      detail: exists
        ? `defaultIosSimulator: ${inventory.defaultIosSimulator}`
        : `defaultIosSimulator "${inventory.defaultIosSimulator}" not found`,
    });
  } else if (inventory.simulators.length) {
    checks.push({
      id: 'ios-default-sim',
      ok: true,
      informational: true,
      detail: 'No default simulator — npm run observe -- setup-devices --ios "iPhone 17 Pro Max"',
    });
  }

  checks.push({
    id: 'device-orientation',
    ok: true,
    informational: true,
    detail: `deviceOrientation: ${inventory.deviceOrientation} (override: --orientation landscape)`,
  });

  return checks;
}

/**
 * @param {{ android?: string, ios?: string, orientation?: string, list?: boolean, json?: boolean }} options
 */
export function runDeviceSetup(options = {}) {
  const inventory = collectDeviceInventory();
  const prefs = loadNativeLocal();

  if (options.list) {
    const payload = {
      command: 'setup-devices',
      avds: inventory.avds,
      adbDevices: inventory.adbDevices,
      simulators: inventory.simulators.map(({ name, udid, runtime, state }) => ({
        name,
        udid,
        runtime,
        state,
      })),
      current: {
        defaultAndroidAvd: prefs.defaultAndroidAvd ?? null,
        defaultIosSimulator: prefs.defaultIosSimulator ?? null,
        defaultAndroidUdid: prefs.defaultAndroidUdid ?? null,
        deviceOrientation: normalizeOrientation(
          /** @type {string | undefined} */ (prefs.deviceOrientation),
          DEFAULTS.deviceOrientation
        ),
      },
    };
    if (options.json) {
      console.log(JSON.stringify(payload, null, 2));
    } else {
      console.log('Android AVDs:', inventory.avds.join(', ') || '(none)');
      console.log('adb devices:', inventory.adbDevices.map((d) => d.udid).join(', ') || '(none)');
      console.log('iOS simulators (available):');
      for (const s of inventory.simulators) {
        console.log(`  ${s.state === 'Booted' ? '●' : '○'} ${s.name} [${s.runtime}] ${s.udid}`);
      }
      console.log('\nCurrent defaults (audit/native.local.json):');
      console.log('  defaultAndroidAvd:', prefs.defaultAndroidAvd ?? '(not set)');
      console.log('  defaultIosSimulator:', prefs.defaultIosSimulator ?? '(not set)');
      console.log(
        '  deviceOrientation:',
        normalizeOrientation(/** @type {string | undefined} */ (prefs.deviceOrientation), DEFAULTS.deviceOrientation)
      );
    }
    return payload;
  }

  /** @type {Record<string, string>} */
  const updates = {};

  if (options.android) {
    if (!inventory.avds.includes(options.android)) {
      throw new Error(`Unknown AVD "${options.android}". Available: ${inventory.avds.join(', ') || '(none)'}`);
    }
    updates.defaultAndroidAvd = options.android;
    const connected = inventory.adbDevices.find((d) => d.avdName === options.android);
    if (connected) updates.defaultAndroidUdid = connected.udid;
  }

  if (options.ios) {
    if (!inventory.simulators.some((s) => s.name === options.ios)) {
      const names = [...new Set(inventory.simulators.map((s) => s.name))].slice(0, 12);
      throw new Error(`Unknown simulator "${options.ios}". Examples: ${names.join(', ')}`);
    }
    updates.defaultIosSimulator = options.ios;
  }

  if (options.orientation) {
    updates.deviceOrientation = normalizeOrientation(options.orientation);
  }

  if (!options.android && !options.ios && !options.orientation) {
    return runDeviceSetup({ ...options, list: true });
  }

  const saved = saveNativeLocal(updates);
  const result = {
    command: 'setup-devices',
    saved: updates,
    path: join(auditRoot, 'native.local.json'),
    config: saved,
  };

  if (options.json) {
    console.log(JSON.stringify(result, null, 2));
  } else {
    console.log(`Saved device defaults → ${result.path}`);
    if (updates.defaultAndroidAvd) console.log(`  defaultAndroidAvd: ${updates.defaultAndroidAvd}`);
    if (updates.defaultIosSimulator) console.log(`  defaultIosSimulator: ${updates.defaultIosSimulator}`);
    if (updates.deviceOrientation) console.log(`  deviceOrientation: ${updates.deviceOrientation}`);
  }

  return result;
}

function defaultRun(cmd, args) {
  const r = spawnSync(cmd, args, { encoding: 'utf8' });
  return { ok: r.status === 0, out: (r.stdout ?? '') + (r.stderr ?? '') };
}

export { loadNativeLocal, saveNativeLocal };
