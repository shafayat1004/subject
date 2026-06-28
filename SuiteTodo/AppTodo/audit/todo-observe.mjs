#!/usr/bin/env node
/**
 * AppTodo dev observability CLI — screenshots, DOM, layout diff, logs.
 * Headed (non-headless) by default so humans and AI share the same view.
 *
 * Usage:
 *   node audit/todo-observe.mjs snapshot
 *   node audit/todo-observe.mjs state
 *   node audit/todo-observe.mjs add-todo "Buy milk"
 *   node audit/todo-observe.mjs workflow layout-check
 *   node audit/todo-observe.mjs diff audit/out/run-a audit/out/run-b
 *   node audit/todo-observe.mjs open
 *   node audit/todo-observe.mjs --help
 */
import yargs from 'yargs/yargs';
import { hideBin } from 'yargs/helpers';
import { readFileSync, existsSync, readdirSync, statSync } from 'fs';
import { join } from 'path';
import { DEFAULTS, parseBool } from './lib/config.mjs';
import { normalizePlatform, platformNotReadyMessage } from './lib/platform.mjs';
import { OUT_ROOT } from './lib/paths.mjs';
import { diffLayoutMetrics } from './lib/dom-analysis.mjs';
import { emitReport, emitStatus } from './lib/report.mjs';
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

const argv = yargs(hideBin(process.argv))
  .scriptName('todo-observe')
  .usage('$0 <command> [options]')
  .option('base-url', {
    alias: 'u',
    type: 'string',
    default: DEFAULTS.baseUrl,
    describe: 'AppTodo dev-web URL',
  })
  .option('headless', {
    type: 'boolean',
    default: DEFAULTS.headless,
    describe: 'Run browser headless (default: false — visible for collaboration)',
  })
  .option('platform', {
    alias: 'p',
    choices: ['web', 'android', 'ios'],
    default: DEFAULTS.platform,
    describe: 'Target platform (android/ios stubs for now)',
  })
  .command(
    'snapshot',
    'Screenshot + DOM summary + layout metrics + uiSnapshot + logs',
    (y) =>
      y.option('out', { type: 'string', describe: 'Output directory (default: audit/out/<timestamp>-snapshot)' }),
    async (args) => {
      assertPlatform(args);
      await runSnapshotWorkflow({
        baseUrl: args.baseUrl,
        headless: args.headless,
        outDir: args.out,
        label: 'current',
      });
    }
  )
  .command(
    'state',
    'Alias for snapshot — structured JSON bundle for LLM agents',
    (y) => y.option('out', { type: 'string' }),
    async (args) => {
      assertPlatform(args);
      await runSnapshotWorkflow({
        baseUrl: args.baseUrl,
        headless: args.headless,
        outDir: args.out,
        label: 'current',
      });
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
      assertPlatform(args);
      await runAddTodoWorkflow({
        baseUrl: args.baseUrl,
        headless: args.headless,
        title: args.title,
        outDir: args.out,
      });
    }
  )
  .command(
    'workflow <name>',
    'Named workflows (layout-check: before/after add-todo + card width diff)',
    (y) =>
      y
        .positional('name', { choices: ['layout-check'], describe: 'Workflow name' })
        .option('title', { type: 'string', describe: 'Todo title for layout-check' })
        .option('out', { type: 'string' }),
    async (args) => {
      assertPlatform(args);
      if (args.name === 'layout-check') {
        await runLayoutCheckWorkflow({
          baseUrl: args.baseUrl,
          headless: args.headless,
          title: args.title,
          outDir: args.out,
        });
      }
    }
  )
  .command(
    'diff [dirA] [dirB]',
    'Compare layout metrics between two snapshot runs (defaults: two latest in audit/out/)',
    (y) =>
      y
        .positional('dirA', { type: 'string', describe: 'First run directory' })
        .positional('dirB', { type: 'string', describe: 'Second run directory' })
        .option('threshold', { type: 'number', default: 2, describe: 'Px threshold for flagged changes' }),
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

      emitReport({
        command: 'diff',
        dirA,
        dirB,
        layoutDiff: diff,
      });

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
    'Open AppTodo in a visible browser and keep session alive for human + AI pairing',
    () => {},
    async (args) => {
      assertPlatform(args);
      await runSnapshotWorkflow({
        baseUrl: args.baseUrl,
        headless: false,
        keepOpen: true,
        label: 'open',
      });
    }
  )
  .command(
    'logs',
    'Collect console + page errors only (quick smoke)',
    () => {},
    async (args) => {
      assertPlatform(args);
      const result = await runSnapshotWorkflow({
        baseUrl: args.baseUrl,
        headless: args.headless,
        label: 'logs',
      });
      if (result?.manifest) {
        emitStatus(
          result.manifest.pageErrorCount ? 'warn' : 'ok',
          `console errors: ${result.manifest.consoleErrorCount}, page errors: ${result.manifest.pageErrorCount}`
        );
      }
    }
  )
  .demandCommand(1, 'Pick a command: snapshot | state | add-todo | workflow | diff | open | logs')
  .help()
  .parse();

function assertPlatform(args) {
  const platform = normalizePlatform(args.platform);
  const msg = platformNotReadyMessage(platform);
  if (msg) {
    console.error(msg);
    process.exit(2);
  }
}

// yargs parse is sync for diff; async commands handle their own exit
void argv;
