/**
 * Device orientation for native observe sessions (default portrait for phone tests).
 */

import { spawnSync } from 'child_process';
import { loadNativeLocal } from './native-config.mjs';
import { DEFAULTS } from './config.mjs';

export const ORIENTATIONS = ['portrait', 'landscape'];

/**
 * @param {string | undefined | null} value
 * @param {string} [fallback]
 */
export function normalizeOrientation(value, fallback = DEFAULTS.deviceOrientation) {
  const v = String(value ?? fallback).toLowerCase();
  if (v === 'landscape' || v === 'l') return 'landscape';
  return 'portrait';
}

/** Appium / WebDriver orientation token. */
export function toAppiumOrientation(orientation) {
  return normalizeOrientation(orientation) === 'landscape' ? 'LANDSCAPE' : 'PORTRAIT';
}

/**
 * Resolve orientation: CLI > env > native.local.json > default.
 * @param {{ orientation?: string }} [options]
 */
export function resolveDeviceOrientation(options = {}) {
  const prefs = loadNativeLocal();
  const fromEnv = process.env.APPTODO_DEVICE_ORIENTATION;
  const raw =
    options.orientation ??
    fromEnv ??
    /** @type {string | undefined} */ (prefs.deviceOrientation) ??
    DEFAULTS.deviceOrientation;
  return normalizeOrientation(raw);
}

/**
 * @param {string} [udid]
 * @returns {{ width: number, height: number, naturalLandscape: boolean } | null}
 */
export function readAndroidDisplaySize(udid) {
  const adbArgs = (args) => (udid ? ['-s', udid, ...args] : args);
  const r = spawnSync('adb', adbArgs(['shell', 'wm', 'size']), { encoding: 'utf8' });
  const m = String(r.stdout).match(/Physical size:\s*(\d+)x(\d+)/);
  if (!m) return null;
  const width = Number(m[1]);
  const height = Number(m[2]);
  return { width, height, naturalLandscape: width > height };
}

/**
 * Map desired orientation to Android user_rotation for this device.
 * @param {'portrait' | 'landscape'} orientation
 * @param {boolean} naturalLandscape
 */
export function androidUserRotationFor(orientation, naturalLandscape) {
  const wantPortrait = normalizeOrientation(orientation) === 'portrait';
  if (naturalLandscape) {
    return wantPortrait ? 1 : 0;
  }
  return wantPortrait ? 0 : 1;
}

/**
 * Lock Android emulator rotation via adb (before or without Appium).
 * @param {'portrait' | 'landscape'} orientation
 * @param {string} [udid]
 */
export function setAndroidOrientationViaAdb(orientation, udid) {
  const adbArgs = (args) => (udid ? ['-s', udid, ...args] : args);
  const display = readAndroidDisplaySize(udid);
  const naturalLandscape = display?.naturalLandscape ?? true;
  const rotation = androidUserRotationFor(orientation, naturalLandscape);

  spawnSync('adb', adbArgs(['shell', 'settings', 'put', 'system', 'accelerometer_rotation', '0']), {
    encoding: 'utf8',
  });
  spawnSync('adb', adbArgs(['shell', 'settings', 'put', 'system', 'user_rotation', String(rotation)]), {
    encoding: 'utf8',
  });
  // API 29+ — lock rotation explicitly
  spawnSync('adb', adbArgs(['shell', 'cmd', 'window', 'set-user-rotation', 'lock', String(rotation)]), {
    encoding: 'utf8',
  });
  return rotation;
}

/**
 * Apply orientation on an active Appium session (Android + iOS).
 * @param {import('webdriverio').Browser} driver
 * @param {'android' | 'ios'} platform
 * @param {'portrait' | 'landscape'} orientation
 * @param {{ udid?: string, log?: (msg: string) => void }} [options]
 */
export async function ensureDeviceOrientation(driver, platform, orientation, options = {}) {
  const log = options.log ?? (() => {});
  const normalized = normalizeOrientation(orientation);
  const appiumOri = toAppiumOrientation(normalized);

  if (platform === 'android') {
    setAndroidOrientationViaAdb(normalized, options.udid);
  }

  if (typeof driver.setOrientation === 'function') {
    try {
      await driver.setOrientation(appiumOri);
      log(`device orientation: ${normalized}`);
      return normalized;
    } catch (e) {
      log(`setOrientation failed (${e.message}), trying mobile command`);
    }
  }

  try {
    await driver.execute('mobile: orientation', { orientation: appiumOri });
    log(`device orientation: ${normalized} (mobile: orientation)`);
    return normalized;
  } catch {
    if (platform === 'android') {
      setAndroidOrientationViaAdb(normalized, options.udid);
      log(`device orientation: ${normalized} (adb)`);
      return normalized;
    }
    log(`warning: could not set iOS orientation to ${normalized}`);
    return normalized;
  }
}

/**
 * @param {import('webdriverio').Browser} driver
 */
export async function readDeviceOrientation(driver) {
  if (typeof driver.getOrientation === 'function') {
    try {
      const o = await driver.getOrientation();
      return String(o).toLowerCase().includes('land') ? 'landscape' : 'portrait';
    } catch {
      /* fall through */
    }
  }
  return null;
}
