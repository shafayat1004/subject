/**
 * Native app identifiers for AppTodo observability (Android / iOS).
 * Override via env or audit/native.local.json (gitignored).
 */

import { existsSync, readFileSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const auditRoot = join(dirname(fileURLToPath(import.meta.url)), '..');

/** @type {Record<string, string> | null} */
let localOverrides = null;

function loadLocalOverrides() {
  if (localOverrides !== null) return localOverrides;
  localOverrides = {};
  const path = join(auditRoot, 'native.local.json');
  if (existsSync(path)) {
    try {
      Object.assign(localOverrides, JSON.parse(readFileSync(path, 'utf8')));
    } catch {
      /* ignore */
    }
  }
  return localOverrides;
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
    local.androidPackage ??
    readGradleApplicationId(appRoot) ??
    'com.eggshell.apptodo';
  const activity =
    process.env.APPTODO_ANDROID_ACTIVITY ??
    local.androidActivity ??
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
    local.iosBundleId ??
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
