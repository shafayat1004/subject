#!/usr/bin/env node
// Verify all relative .md links in the docs tree resolve, and every page is in llms.txt.
// usage: check-doc-links.mjs [docsroot]   (exit 1 on broken links)
import { readFileSync, readdirSync, statSync, existsSync } from 'node:fs';
import { join, dirname, resolve, relative } from 'node:path';

const ROOT = process.argv[2] ?? 'AppEggShellGallery/public-dev/docs';
if (!existsSync(ROOT)) { console.error(`no such docs root: ${ROOT}`); process.exit(2); }

function* mdFiles(p) {
  for (const e of readdirSync(p, { withFileTypes: true })) {
    const full = join(p, e.name);
    if (e.isDirectory()) yield* mdFiles(full);
    else if (e.name.endsWith('.md')) yield full;
  }
}

const broken = [];
const pages = [];
for (const file of mdFiles(ROOT)) {
  pages.push(relative(ROOT, file));
  const text = readFileSync(file, 'utf8');
  for (const m of text.matchAll(/\]\(([^)\s]+?\.md)(#[^)]*)?\)/g)) {
    const href = m[1];
    if (/^https?:/.test(href)) continue;
    const targetPath = resolve(dirname(file), href);
    if (!existsSync(targetPath)) broken.push(`${file}: ${href}`);
  }
}
if (broken.length) {
  console.log(`BROKEN LINKS (${broken.length}):`);
  broken.forEach(b => console.log('  ' + b));
}
const llmsPath = join(ROOT, 'llms.txt');
if (existsSync(llmsPath)) {
  const llms = readFileSync(llmsPath, 'utf8');
  const missing = pages.filter(p => !llms.includes(p) && p !== 'llms.txt');
  if (missing.length) {
    console.log(`WARN: ${missing.length} page(s) not listed in llms.txt:`);
    missing.forEach(p => console.log('  ' + p));
  }
}
console.log(broken.length ? 'RESULT: FAIL' : 'RESULT: PASS');
process.exit(broken.length ? 1 : 0);
