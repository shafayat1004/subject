#!/usr/bin/env node
/**
 * Smoke audit for AppTodo.
 * Requires dev-web on baseUrl (fake backend works without Todo DevelopmentHost).
 *
 * Usage: npm run audit:web
 *        node audit/audit-todo-web.mjs http://127.0.0.1:9080 --headless
 */
import { chromium } from 'playwright';
import { DEFAULTS, parseBool } from './lib/config.mjs';
import { waitForAppReady } from './lib/web-session.mjs';
import { waitForTodoReady, fillNewTodoTitle, clickAddTodo } from './lib/selectors.mjs';
import { isActionableConsole } from './lib/log-classify.mjs';

const args = process.argv.slice(2);
const positional = args.filter((a) => !a.startsWith('--'));
const headlessFlag = args.find((a) => a.startsWith('--headless'));
const headless = headlessFlag
  ? parseBool(headlessFlag.replace(/^--headless=*/, ''), true)
  : true;

const baseUrl = (positional[0] ?? DEFAULTS.baseUrl).replace(/\/$/, '');

/** @type {string[]} */
const errors = [];

async function main () {
  const ready = await waitForAppReady(baseUrl);
  if (!ready) {
    console.error(`AppTodo not reachable at ${baseUrl}`);
    process.exit(1);
  }

  const browser = await chromium.launch({ headless });
  const page = await browser.newPage();

  page.on('pageerror', (err) => errors.push(`pageerror: ${err.message}`));
  page.on('console', (msg) => {
    if (msg.type() === 'error' && isActionableConsole(msg.text(), 'error')) {
      errors.push(`console: ${msg.text()}`);
    }
  });

  const resp = await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
  if (!resp || resp.status() >= 400) {
    errors.push(`HTTP ${resp?.status() ?? 'no response'} for ${baseUrl}`);
  }

  await waitForTodoReady(page, { bootstrapWaitMs: DEFAULTS.bootstrapWaitMs });

  const title = `audit-${Date.now()}`;
  await fillNewTodoTitle(page, title);
  await clickAddTodo(page);
  await page.getByText(title).waitFor({ timeout: 15000 });

  await browser.close();

  if (errors.length) {
    console.error('AppTodo audit failed:');
    for (const e of errors) console.error(' ', e);
    process.exit(1);
  }

  console.log('AppTodo audit passed.');
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
