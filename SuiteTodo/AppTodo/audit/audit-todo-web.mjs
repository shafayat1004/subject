#!/usr/bin/env node
/**
 * Smoke audit for AppTodo (Phase 5D stub).
 * Requires dev-web on baseUrl and Todo DevelopmentHost on localhost:5001.
 *
 * Usage: node audit/audit-todo-web.mjs http://127.0.0.1:9081
 */
import { chromium } from 'playwright';

const baseUrl = (process.argv[2] ?? 'http://127.0.0.1:9080').replace(/\/$/, '');

const errors = [];

async function main () {
  const browser = await chromium.launch();
  const page = await browser.newPage();

  page.on('pageerror', (err) => errors.push(`pageerror: ${err.message}`));
  page.on('console', (msg) => {
    if (msg.type() === 'error') errors.push(`console: ${msg.text()}`);
  });

  const resp = await page.goto(baseUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
  if (!resp || resp.status() >= 400) {
    errors.push(`HTTP ${resp?.status() ?? 'no response'} for ${baseUrl}`);
  }

  await page.waitForTimeout(8000);
  await page.locator('input').first().waitFor({ timeout: 30000 });

  const title = `audit-${Date.now()}`;
  await page.locator('input').first().fill(title);
  await page.getByRole('button', { name: 'Add' }).click();
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
