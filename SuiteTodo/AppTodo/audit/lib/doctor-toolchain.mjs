/**
 * Toolchain PATH / install checks for observe doctor.
 */

import { existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import { spawnSync } from 'child_process';

const appRoot = join(dirname(fileURLToPath(import.meta.url)), '../..');
const repoRoot = join(appRoot, '../..');

function run(cmd, args, opts = {}) {
  const r = spawnSync(cmd, args, { encoding: 'utf8', ...opts });
  return {
    ok: r.status === 0,
    out: ((r.stdout ?? '') + (r.stderr ?? '')).trim(),
    code: r.status,
  };
}

/** Login-shell `which` catches nvm / Android SDK paths npm scripts may miss. */
function which(name) {
  const r = run('sh', ['-lc', `command -v ${name} 2>/dev/null || which ${name} 2>/dev/null`]);
  if (r.ok && r.out) return r.out.split('\n')[0].trim();
  return null;
}

/**
 * @param {string} name
 * @param {string[]} fallbacks Absolute paths to try if not on PATH.
 */
function checkBinary(name, fallbacks = []) {
  const path = which(name) ?? fallbacks.find((p) => existsSync(p)) ?? null;
  return {
    id: `path-${name}`,
    ok: Boolean(path),
    informational: Boolean(path),
    detail: path ?? `Not on PATH — install ${name} or add to shell profile`,
  };
}

function checkEggshell() {
  const candidates = [
    which('eggshell'),
    join(repoRoot, 'eggshell'),
    join(appRoot, '../../eggshell'),
  ].filter(Boolean);
  const path = candidates.find((p) => p && (p.includes('/') ? existsSync(p) : true));
  const resolved = path && path.includes('/') ? path : which('eggshell');
  return {
    id: 'path-eggshell',
    ok: Boolean(resolved && (resolved.includes('/') ? existsSync(resolved) : true)),
    informational: true,
    detail: resolved
      ? resolved.includes('/') ? resolved : `eggshell → ${resolved}`
      : `Not found — use ${join(repoRoot, 'eggshell')} from AppTodo`,
  };
}

function checkAndroidSdk() {
  const home =
    process.env.ANDROID_HOME ??
    process.env.ANDROID_SDK_ROOT ??
    (process.env.HOME ? join(process.env.HOME, 'Library/Android/sdk') : null);
  const adbPath = home ? join(home, 'platform-tools/adb') : null;
  const emuPath = home ? join(home, 'emulator/emulator') : null;
  const adb = which('adb') ?? (adbPath && existsSync(adbPath) ? adbPath : null);
  const emulator = which('emulator') ?? (emuPath && existsSync(emuPath) ? emuPath : null);
  return [
    {
      id: 'android-sdk',
      ok: Boolean(home && existsSync(home)),
      informational: true,
      detail: home && existsSync(home) ? `ANDROID_SDK → ${home}` : 'Set ANDROID_HOME or install Android Studio SDK',
    },
    checkBinary('adb', adbPath ? [adbPath] : []),
    {
      id: 'path-emulator',
      ok: Boolean(emulator),
      informational: Boolean(emulator),
      detail: emulator ?? (emuPath ? `Not on PATH (try ${emuPath})` : 'Install Android emulator via SDK Manager'),
    },
  ];
}

function checkReactNativeCli() {
  const localCli = join(appRoot, 'node_modules/.bin/react-native');
  const path = which('react-native') ?? (existsSync(localCli) ? localCli : null);
  return {
    id: 'path-react-native',
    ok: Boolean(path),
    informational: true,
    detail: path ?? 'Run via npx react-native from AppTodo after npm install',
  };
}

function checkXcode() {
  const xcrun = which('xcrun');
  const xcodebuild = which('xcodebuild');
  const pod = which('pod');
  return [
    checkBinary('xcrun'),
    {
      id: 'path-xcodebuild',
      ok: Boolean(xcodebuild),
      informational: Boolean(xcodebuild),
      detail: xcodebuild ?? 'Install Xcode command-line tools: xcode-select --install',
    },
    checkBinary('pod'),
  ];
}

function checkNodeToolchain() {
  const node = which('node');
  const npx = which('npx');
  return [
    {
      id: 'path-node',
      ok: Boolean(node),
      informational: true,
      detail: node ? `${node}${process.version ? '' : ''}` : 'Node.js not on PATH',
    },
    {
      id: 'path-npx',
      ok: Boolean(npx),
      informational: true,
      detail: npx ?? 'npx not on PATH',
    },
    checkReactNativeCli(),
  ];
}

/** @param {{ includeIos?: boolean }} [options] */
export function collectToolchainChecks(options = {}) {
  const checks = [
    ...checkNodeToolchain(),
    checkEggshell(),
    ...checkAndroidSdk(),
    checkBinary('java'),
  ];
  if (options.includeIos !== false) {
    checks.push(...checkXcode());
  }
  return checks;
}

export { appRoot, repoRoot, which, run };
