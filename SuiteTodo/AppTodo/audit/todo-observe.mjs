#!/usr/bin/env node
/**
 * AppTodo dev observability CLI — web (Playwright) + native (Appium Android/iOS).
 * Headed by default on web so humans and AI share the same view.
 *
 * Usage:
 *   npm run observe -- snapshot
 *   npm run observe -- snapshot --platform android
 *   npm run observe -- doctor --platform ios
 *   npm run observe -- workflow layout-check --platform android
 */
import yargs from 'yargs/yargs';
import { hideBin } from 'yargs/helpers';
import { readFileSync, existsSync, readdirSync, statSync } from 'fs';
import { join } from 'path';
import { DEFAULTS } from './lib/config.mjs';
import { normalizePlatform } from './lib/platform.mjs';
import { OUT_ROOT } from './lib/paths.mjs';
import { diffLayoutMetrics } from './lib/dom-analysis.mjs';
import { emitReport, emitStatus } from './lib/report.mjs';
import { runDoctor } from './lib/doctor.mjs';
import { runSnapshotWorkflow } from './workflows/snapshot.mjs';
import { runLayoutCheckWorkflow, runAddTodoWorkflow } from './workflows/layout-check.mjs';

function loadLayoutMetrics(dir, preferLabel) {
  const candidates = preferLabel
    ? [`${preferLabel}-layout-metrics.json`, 'current-layout-metrics.json', 'before-layout-metrics.json', 'after-layout-metrics.json']
    : ['current-layout-metrics.json', 'before-layout-metrics.json', 'after-layout-metrics.json'];

  for (const name of candidates) {
    const path = join(dir, name);
    if (existsSync(path)) {
      return { path, data: JSON.parse(readFileSync(path, 'utf8')) };
    }
  }
  throw new Error(`No layout-metrics JSON found in ${dir}`);
}

function latestRunDirs(limit = 2) {
  if (!existsSync(OUT_ROOT)) return [];
  return readdirSync(OUT_ROOT)
    .map((name) => join(OUT_ROOT, name))
    .filter((p) => statSync(p).isDirectory())
    .sort((a, b) => statSync(b).mtimeMs - statSync(a).mtimeMs)
    .slice(0, limit);
}

function workflowOptions(args) {
  return {
    platform: args.platform,
    baseUrl: args.baseUrl,
    headless: args.headless,
    outDir: args.out,
    title: args.title,
  };
}

const argv = yargs(hideBin(process.argv))
  .scriptName('todo-observe')
  .usage('$0 <command> [options]')
  .option('base-url', {
    alias: 'u',
    type: 'string',
    default: DEFAULTS.baseUrl,
    describe: 'AppTodo dev-web URL (web platform only)',
  })
  .option('headless', {
    type: 'boolean',
    default: DEFAULTS.headless,
    describe: 'Web: run browser headless (default: false — visible for collaboration)',
  })
  .option('platform', {
    alias: 'p',
    choices: ['web', 'android', 'ios'],
    default: DEFAULTS.platform,
    describe: 'Target platform for snapshot/workflows (doctor checks all unless -p set)',
  })
  .command(
    'doctor',
    'Health check for web, Android, and iOS observe (beige UI; use --json for agents)',
    (y) =>
      y.option('json', { type: 'boolean', default: false, describe: 'Machine-readable JSON instead of UI' }),
    async (args) => {
      const singlePlatform = args.platform !== DEFAULTS.platform ? args.platform : undefined;
      const report = await runDoctor({
        platform: singlePlatform,
        baseUrl: args.baseUrl,
        json: args.json,
      });
      if (args.json) {
        emitReport(report);
      }
      if (!report.allReady) {
        if (args.json) {
          emitStatus('warn', `${report.readyCount}/${report.total} platforms ready`);
        }
        process.exitCode = 2;
      } else if (args.json) {
        emitStatus('ok', `All ${report.total} platforms ready`);
      }
    }
  )
  .command(
    'snapshot',
    'Screenshot + layout metrics + DOM (web) or UI hierarchy (native) + logs',
    (y) => y.option('out', { type: 'string' }),
    async (args) => {
      await runSnapshotWorkflow({ ...workflowOptions(args), label: 'current' });
    }
  )
  .command(
    'state',
    'Alias for snapshot — structured JSON bundle for LLM agents',
    (y) => y.option('out', { type: 'string' }),
    async (args) => {
      await runSnapshotWorkflow({ ...workflowOptions(args), label: 'current' });
    }
  )
  .command(
    'add-todo [title]',
    'Add a todo and capture post-action state',
    (y) =>
      y
        .positional('title', { type: 'string', default: `observe-${Date.now()}` })
        .option('out', { type: 'string' }),
    async (args) => {
      await runAddTodoWorkflow(workflowOptions(args));
    }
  )
  .command(
    'workflow <name>',
    'Named workflows (layout-check: before/after add-todo + card width diff)',
    (y) =>
      y
        .positional('name', { choices: ['layout-check'] })
        .option('title', { type: 'string' })
        .option('out', { type: 'string' }),
    async (args) => {
      if (args.name === 'layout-check') {
        await runLayoutCheckWorkflow(workflowOptions(args));
      }
    }
  )
  .command(
    'diff [dirA] [dirB]',
    'Compare layout metrics between two snapshot runs',
    (y) =>
      y
        .positional('dirA', { type: 'string' })
        .positional('dirB', { type: 'string' })
        .option('threshold', { type: 'number', default: 2 }),
    (args) => {
      let dirA = args.dirA;
      let dirB = args.dirB;
      if (!dirA || !dirB) {
        const latest = latestRunDirs(2);
        if (latest.length < 2) {
          emitStatus('fail', 'Need two run directories. Run snapshot twice or pass dirA dirB.');
          process.exitCode = 1;
          return;
        }
        dirB = latest[0];
        dirA = latest[1];
      }

      const before = loadLayoutMetrics(dirA, 'before');
      const after = loadLayoutMetrics(dirB, 'after');
      const diff = diffLayoutMetrics(before.data, after.data, { thresholdPx: args.threshold });

      emitReport({ command: 'diff', dirA, dirB, layoutDiff: diff });

      if (diff.regressionLikely) {
        emitStatus('warn', diff.summary);
        process.exitCode = 2;
      } else {
        emitStatus('ok', diff.summary);
      }
    }
  )
  .command(
    'open',
    'Open AppTodo in a visible browser and keep session alive (web only)',
    () => {},
    async (args) => {
      if (normalizePlatform(args.platform) !== 'web') {
        emitStatus('fail', 'open is web-only. Native: use snapshot (device stays visible).');
        process.exitCode = 2;
        return;
      }
      await runSnapshotWorkflow({
        ...workflowOptions(args),
        headless: false,
        keepOpen: true,
        label: 'open',
      });
    }
  )
  .command(
    'logs',
    'Collect console (web) or device log (native) snapshot',
    () => {},
    async (args) => {
      const result = await runSnapshotWorkflow({ ...workflowOptions(args), label: 'logs' });
      if (result?.manifest) {
        const cs = result.manifest.consoleSummary;
        emitStatus(
          cs?.actionable || result.manifest.pageErrorCount ? 'warn' : 'ok',
          cs
            ? `actionable: ${cs.actionable}, styleLeaks: ${cs.styleLeaks}, noise: ${cs.noise}`
            : `platform ${result.manifest.platform} logs captured`
        );
      }
    }
  )
  .demandCommand(1, 'Pick: doctor | snapshot | state | add-todo | workflow | diff | open | logs')
  .help()
  .parse();

void argv;
