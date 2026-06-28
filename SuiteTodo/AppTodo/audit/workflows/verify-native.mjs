/**
 * Verify Todo UI on native platforms via observe (health + snapshot + smoke).
 */

import { existsSync } from 'fs';
import { join } from 'path';
import { TIMEOUTS } from '../lib/config.mjs';
import { PLATFORM } from '../lib/platform.mjs';
import { createRunDir } from '../lib/paths.mjs';
import { openObserveSession, prepareTodoUi, AppHealthError } from '../lib/observe-session.mjs';
import { captureObserveState, writeManifest, appendRunLog, writeJson } from '../lib/capture.mjs';
import { fillNewTodoTitle, clickAddTodo } from '../lib/selectors.mjs';
import { cardWidthFromMetrics } from '../lib/dom-analysis.mjs';
import { emitReport, emitStatus } from '../lib/report.mjs';
import { resolveAndroidApp } from '../lib/native-config.mjs';
import { appRoot, run } from '../lib/doctor-toolchain.mjs';
import { HEALTH } from '../lib/app-health.mjs';

/**
 * @param {'android' | 'ios'} platform
 * @param {{ timeoutMs?: number, orientation?: string }} [options]
 */
async function verifyPlatform(platform, options = {}) {
  const timeoutMs = options.timeoutMs ?? TIMEOUTS.appReadyMs;
  const outDir = createRunDir(`verify-${platform}`);
  appendRunLog(outDir, `verify-${platform} start timeoutMs=${timeoutMs}`);

  /** @type {{ platform: string, outDir: string, steps: Array<Record<string, unknown>>, ok: boolean, health?: unknown, error?: string }} */
  const results = {
    platform,
    outDir,
    steps: [],
    ok: true,
  };

  let session;
  try {
    session = await openObserveSession({
      platform,
      outDir,
      timeoutMs,
      orientation: options.orientation,
      log: (m) => appendRunLog(outDir, m),
    });

    await prepareTodoUi(session, { outDir, timeoutMs });

    const empty = await captureObserveState(session, outDir, { label: 'empty' });
    results.health = empty.health;

    if (empty.health && !empty.health.healthy) {
      results.ok = false;
      results.steps.push({
        step: 'health-check',
        ok: false,
        state: empty.health.state,
        detail: empty.health.detail,
      });
      writeManifest(outDir, { command: `verify-${platform}`, ...results });
      return results;
    }

    results.steps.push({
      step: 'health-check',
      ok: true,
      state: empty.health?.state ?? HEALTH.HEALTHY,
      foreground: empty.health?.foreground ?? true,
    });

    results.steps.push({
      step: 'snapshot-empty',
      ok: true,
      cardWidth: cardWidthFromMetrics(empty.layoutMetrics),
      screenshot: 'empty.png',
    });

    const title = `verify-${platform}-${Date.now()}`;
    await fillNewTodoTitle(session.page, title, platform);
    await clickAddTodo(session.page, platform);
    await session.page.getByText(title).waitFor({ timeout: TIMEOUTS.actionMs });
    await session.page.waitForTimeout(600);

    const withTodo = await captureObserveState(session, outDir, { label: 'with-todo' });
    const cardBefore = cardWidthFromMetrics(empty.layoutMetrics);
    const cardAfter = cardWidthFromMetrics(withTodo.layoutMetrics);
    const widthStable = cardBefore === null || cardAfter === null || cardBefore === cardAfter;

    results.steps.push({
      step: 'add-todo',
      ok: true,
      title,
      cardWidthBefore: cardBefore,
      cardWidthAfter: cardAfter,
      widthStable,
      screenshot: 'with-todo.png',
    });

    const searchVisible =
      (await session.page.locator('~todo-search').count()) > 0 ||
      (await session.page.locator('input').count()) >= 2;
    results.steps.push({
      step: 'components-search',
      ok: searchVisible,
      detail: searchVisible ? 'Search input present' : 'Search input not found',
    });

    const todosHeading = (await session.page.getByText('Todos', { exact: true }).count()) > 0;
    results.steps.push({
      step: 'components-heading',
      ok: todosHeading,
      detail: todosHeading ? 'Todos heading visible' : 'Heading missing',
    });

    if (!widthStable || !searchVisible || !todosHeading) results.ok = false;
    if ((withTodo.logSummary?.actionable ?? 0) > 0) results.ok = false;

    writeManifest(outDir, { command: `verify-${platform}`, ...results });
    return results;
  } catch (e) {
    results.ok = false;
    if (e instanceof AppHealthError) {
      results.health = e.health;
      results.error = e.message;
      appendRunLog(outDir, `AppHealthError: ${e.message}`);
      if (session) {
        try {
          await captureObserveState(session, outDir, { label: 'crash' });
        } catch {
          /* best effort */
        }
      }
    } else {
      results.error = e.message ?? String(e);
    }
    writeJson(outDir, 'verify-error.json', { error: results.error, health: results.health ?? null });
    writeManifest(outDir, { command: `verify-${platform}`, ...results });
    return results;
  } finally {
    if (session) await session.close();
  }
}

/**
 * @param {{ platforms?: Array<'android' | 'ios'>, timeoutMs?: number, orientation?: string }} [options]
 */
export async function runVerifyNativeWorkflow(options = {}) {
  const platforms = options.platforms ?? [PLATFORM.ANDROID, PLATFORM.IOS];
  /** @type {Array<Awaited<ReturnType<typeof verifyPlatform>>>} */
  const reports = [];

  for (const p of platforms) {
    emitStatus('ok', `Verifying ${p}...`);
    reports.push(await verifyPlatform(p, { timeoutMs: options.timeoutMs, orientation: options.orientation }));
  }

  const allOk = reports.every((r) => r.ok);
  const summary = {
    command: 'workflow verify-native',
    allOk,
    platforms: reports,
  };

  emitReport(summary);
  if (!allOk) {
    emitStatus('warn', 'Native verification had failures — see artifacts (health.json, crash.png)');
    process.exitCode = 2;
  } else {
    emitStatus('ok', 'Native verification passed on all requested platforms');
  }
  return summary;
}

/**
 * Ensure debug APK installed on connected Android device.
 */
export function ensureAndroidAppInstalled() {
  const app = resolveAndroidApp(appRoot);
  const apk = join(appRoot, 'android/app/build/outputs/apk/debug/app-debug.apk');
  if (!existsSync(apk)) {
    return { ok: false, detail: `APK missing — run: cd android && ./gradlew assembleDebug` };
  }
  const listed = run('adb', ['shell', 'pm', 'list', 'packages', app.package]);
  if (listed.ok && listed.out.includes(app.package)) {
    return { ok: true, detail: `${app.package} already installed` };
  }
  const install = run('adb', ['install', '-r', apk]);
  return {
    ok: install.ok,
    detail: install.ok ? 'Installed app-debug.apk' : `adb install failed: ${install.out.slice(0, 200)}`,
  };
}
