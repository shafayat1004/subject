/**
 * Native app identifiers for AppTodo observability (Android / iOS).
 * Override via env or audit/native.local.json (gitignored).
 */

import { existsSync, readFileSync, writeFileSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import { spawnSync } from 'child_process';

const auditRoot = join(dirname(fileURLToPath(import.meta.url)), '..');

/** @type {Record<string, unknown> | null} */
let localOverrides = null;

/**
 * @returns {Record<string, unknown>}
 */
export function loadNativeLocal() {
  if (localOverrides !== null) return { ...localOverrides };
  localOverrides = {};
  const path = join(auditRoot, 'native.local.json');
  if (existsSync(path)) {
    try {
      Object.assign(localOverrides, JSON.parse(readFileSync(path, 'utf8')));
    } catch {
      /* ignore */
    }
  }
  return { ...localOverrides };
}

/**
 * Merge updates into audit/native.local.json and reload cache.
 * @param {Record<string, unknown>} updates
 */
export function saveNativeLocal(updates) {
  const path = join(auditRoot, 'native.local.json');
  const current = loadNativeLocal();
  localOverrides = null;
  const merged = { ...current, ...updates };
  writeFileSync(path, `${JSON.stringify(merged, null, 2)}\n`, 'utf8');
  localOverrides = merged;
  return { ...merged };
}

function loadLocalOverrides() {
  return loadNativeLocal();
}

/**
 * @param {string} appRoot
 */
function readGradleApplicationId(appRoot) {
  const gradle = join(appRoot, 'android/app/build.gradle');
  if (!existsSync(gradle)) return null;
  const text = readFileSync(gradle, 'utf8');
  const m = text.match(/applicationId\s+"([^"]+)"/);
  return m?.[1] ?? null;
}

/**
 * @param {string} appRoot
 */
function readIosBundleId(appRoot) {
  const plist = join(appRoot, 'ios/AppTodo/Info.plist');
  if (existsSync(plist)) {
    const text = readFileSync(plist, 'utf8');
    const m = text.match(/<key>CFBundleIdentifier<\/key>\s*<string>([^<]+)<\/string>/);
    if (m?.[1] && !m[1].includes('$(')) return m[1];
  }
  const pbx = join(appRoot, 'ios/AppTodo.xcodeproj/project.pbxproj');
  if (existsSync(pbx)) {
    const text = readFileSync(pbx, 'utf8');
    const m = text.match(/PRODUCT_BUNDLE_IDENTIFIER = ([^;]+);/);
    if (m?.[1]) return m[1].trim().replace(/"/g, '');
  }
  return null;
}

/**
 * @param {string} [appRoot]
 */
export function resolveAndroidApp(appRoot = join(auditRoot, '..')) {
  const local = loadLocalOverrides();
  const pkg =
    process.env.APPTODO_ANDROID_PACKAGE ??
    /** @type {string | undefined} */ (local.androidPackage) ??
    readGradleApplicationId(appRoot) ??
    'com.eggshell.apptodo';
  const activity =
    process.env.APPTODO_ANDROID_ACTIVITY ??
    /** @type {string | undefined} */ (local.androidActivity) ??
    `${pkg}.MainActivity`;
  return { package: pkg, activity };
}

/**
 * @param {string} [appRoot]
 */
export function resolveIosApp(appRoot = join(auditRoot, '..')) {
  const local = loadLocalOverrides();
  const bundleId =
    process.env.APPTODO_IOS_BUNDLE_ID ??
    /** @type {string | undefined} */ (local.iosBundleId) ??
    readIosBundleId(appRoot) ??
    'com.eggshell.apptodo';
  return { bundleId };
}

export const APPIUM = {
  host: process.env.APPIUM_HOST ?? '127.0.0.1',
  port: Number(process.env.APPIUM_PORT ?? 4723),
};

export const METRO = {
  port: Number(process.env.METRO_PORT ?? 8081),
};
