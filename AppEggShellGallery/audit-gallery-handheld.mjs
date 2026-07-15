#!/usr/bin/env node
/**
 * Handheld usability audit for the EggShell Gallery.
 *
 * What this checks (390x844 viewport = iPhone 14 Pro):
 *   - Horizontal overflow at the page level (body wider than viewport)
 *   - Code blocks (<pre>) that overflow without a scrollable ancestor
 *   - Tables that overflow without a scrollable ancestor
 *   - Images that overflow the viewport
 *   - Console errors / page errors
 *   - Sidebar layout: how much vertical space the top "blades" consume vs
 *     the scrollable sub-page list (the reported issue where root nav items
 *     crowd out sub-page links)
 *
 * Coverage:
 *   - ALL docs section pages (Docs, Architecture, Modernization, Runbooks,
 *     Accessibility, KnowledgeBase, Subject, Tools, HowTo, Design)
 *   - ALL component pages (same set as audit-gallery-full.mjs)
 *
 * Usage:
 *   node audit-gallery-handheld.mjs [baseUrl]
 *   node audit-gallery-handheld.mjs http://127.0.0.1:8082
 *
 * Output: audit-browser/(local|prod)/handheld/
 *   handheld-audit.json   — machine-readable full results
 *   handheld-audit.md     — human-readable report
 *   screenshots/<page>.png
 */

import { chromium } from 'playwright';
import { mkdirSync, writeFileSync } from 'fs';
import { join } from 'path';
import { discoverGalleryComponents } from './audit-gallery-components.mjs';
import { classifyForFullAudit } from './audit-gallery-classify.mjs';

const baseUrl = (process.argv[2] ?? 'http://127.0.0.1:8082').replace(/\/$/, '');
const isProd = baseUrl.includes('eggshell.dev');
const outDir = join(process.cwd(), 'audit-browser', isProd ? 'prod' : 'local', 'handheld');
const ssDir = join(outDir, 'screenshots');
mkdirSync(ssDir, { recursive: true });

// Phone viewport — iPhone 14 Pro
const VIEWPORT = { width: 390, height: 844 };

function enc(val) {
  return encodeURIComponent(JSON.stringify(val));
}

// ─── Docs section pages ────────────────────────────────────────────────────

const DOCS_PAGES = [
  // Docs
  { id: 'docs-index',        section: 'Docs',          url: 'index.md',                                         label: 'Docs — Introduction' },
  { id: 'docs-getting-started', section: 'Docs',       url: 'basics/getting-started.md',                        label: 'Docs — Getting Started' },
  { id: 'docs-dev-exp',      section: 'Docs',          url: 'basics/dev-experience.md',                         label: 'Docs — Dev Experience' },
  { id: 'docs-component',    section: 'Docs',          url: 'fsharp/component.md',                               label: 'Docs — F# Components' },
  { id: 'docs-styling',      section: 'Docs',          url: 'fsharp/styling.md',                                 label: 'Docs — Styling' },
  { id: 'docs-formatting',   section: 'Docs',          url: 'fsharp/formatting.md',                              label: 'Docs — Formatting' },
  { id: 'docs-legacy',       section: 'Docs',          url: 'fsharp/legacy.md',                                  label: 'Docs — Legacy Interop' },
  { id: 'docs-maintaining',  section: 'Docs',          url: 'maintaining-docs.md',                               label: 'Docs — Maintaining Docs' },

  // Architecture
  { id: 'arch-index',        section: 'Architecture',  url: 'architecture/index.md',                             label: 'Architecture — Overview' },
  { id: 'arch-lifecycles',   section: 'Architecture',  url: 'architecture/backend-lifecycles.md',                label: 'Architecture — Lifecycles' },
  { id: 'arch-hosting',      section: 'Architecture',  url: 'architecture/backend-hosting-persistence.md',       label: 'Architecture — Hosting & Persistence' },
  { id: 'arch-types',        section: 'Architecture',  url: 'architecture/shared-types-codecs.md',               label: 'Architecture — Shared Types' },
  { id: 'arch-testing',      section: 'Architecture',  url: 'architecture/testing-framework.md',                 label: 'Architecture — Testing Framework' },
  { id: 'arch-frontend',     section: 'Architecture',  url: 'architecture/frontend.md',                          label: 'Architecture — Frontend' },
  { id: 'arch-render-dsl',   section: 'Architecture',  url: 'architecture/render-dsl-and-toolchain.md',          label: 'Architecture — Render DSL & Toolchain' },
  { id: 'arch-file-map',     section: 'Architecture',  url: 'architecture/file-map.md',                          label: 'Architecture — Key File Map' },

  // Modernization
  { id: 'mod-index',         section: 'Modernization', url: 'modernization/index.md',                            label: 'Modernization — Current Status' },
  { id: 'mod-goals',         section: 'Modernization', url: 'modernization/goals-and-roadmap.md',                label: 'Modernization — Goals & Roadmap' },
  { id: 'mod-phased',        section: 'Modernization', url: 'modernization/phased-plan.md',                      label: 'Modernization — Phased Plan' },
  { id: 'mod-rnw',           section: 'Modernization', url: 'modernization/reactxp-to-rnw.md',                   label: 'Modernization — ReactXP to RNW' },
  { id: 'mod-render-dsl',    section: 'Modernization', url: 'modernization/render-dsl-retirement.md',            label: 'Modernization — Render DSL Retirement' },
  { id: 'mod-build',         section: 'Modernization', url: 'modernization/build-performance.md',                label: 'Modernization — Build Performance' },
  { id: 'mod-scaffolding',   section: 'Modernization', url: 'modernization/scaffolding.md',                      label: 'Modernization — Scaffolding' },
  { id: 'mod-security',      section: 'Modernization', url: 'modernization/security-review.md',                  label: 'Modernization — Security Review' },

  // Runbooks
  { id: 'run-index',         section: 'Runbooks',      url: 'runbooks/index.md',                                 label: 'Runbooks — Overview' },
  { id: 'run-dev-loop',      section: 'Runbooks',      url: 'runbooks/dev-loop.md',                              label: 'Runbooks — Dev Loop' },
  { id: 'run-android',       section: 'Runbooks',      url: 'runbooks/android.md',                               label: 'Runbooks — Android' },
  { id: 'run-ios',           section: 'Runbooks',      url: 'runbooks/ios.md',                                   label: 'Runbooks — iOS' },
  { id: 'run-web',           section: 'Runbooks',      url: 'runbooks/web.md',                                   label: 'Runbooks — Web' },
  { id: 'run-audit',         section: 'Runbooks',      url: 'runbooks/audit-toolkit.md',                         label: 'Runbooks — Audit Toolkit' },
  { id: 'run-build',         section: 'Runbooks',      url: 'runbooks/build-rebuild.md',                         label: 'Runbooks — Build & Rebuild' },
  { id: 'run-troubleshoot',  section: 'Runbooks',      url: 'runbooks/troubleshooting.md',                       label: 'Runbooks — Troubleshooting' },
  { id: 'run-migration',     section: 'Runbooks',      url: 'runbooks/migration-execution.md',                   label: 'Runbooks — Migration Execution' },

  // Accessibility
  { id: 'a11y-index',        section: 'Accessibility', url: 'accessibility/index.md',                            label: 'Accessibility — Overview' },
  { id: 'a11y-spectrum',     section: 'Accessibility', url: 'accessibility/spectrum.md',                         label: 'Accessibility — Full Spectrum' },
  { id: 'a11y-recipes',      section: 'Accessibility', url: 'accessibility/recipes.md',                          label: 'Accessibility — Recipes' },
  { id: 'a11y-platform',     section: 'Accessibility', url: 'accessibility/platform-settings.md',                label: 'Accessibility — Platform Settings' },
  { id: 'a11y-backlog',      section: 'Accessibility', url: 'accessibility/backlog.md',                          label: 'Accessibility — Backlog' },

  // KnowledgeBase
  { id: 'kb-index',          section: 'KnowledgeBase', url: 'knowledge-base/index.md',                           label: 'KnowledgeBase — Overview' },
  { id: 'kb-eng-log',        section: 'KnowledgeBase', url: 'knowledge-base/engineering-log.md',                 label: 'KnowledgeBase — Engineering Log' },
  { id: 'kb-app-structure',  section: 'KnowledgeBase', url: 'knowledge-base/app-structure.md',                   label: 'KnowledgeBase — App Structure' },
  { id: 'kb-deps',           section: 'KnowledgeBase', url: 'knowledge-base/dependencies.md',                    label: 'KnowledgeBase — Dependencies' },

  // Subject
  { id: 'subj-index',        section: 'Subject',       url: 'subject/index.md',                                  label: 'Subject — Introduction' },
  { id: 'subj-actions',      section: 'Subject',       url: 'subject/actions-and-transitions.md',                label: 'Subject — Actions & Transitions' },
  { id: 'subj-events',       section: 'Subject',       url: 'subject/events-and-subscriptions.md',               label: 'Subject — Events & Subscriptions' },
  { id: 'subj-construction', section: 'Subject',       url: 'subject/construction-and-id-generation.md',         label: 'Subject — Construction & ID' },
  { id: 'subj-indexing',     section: 'Subject',       url: 'subject/indexing-and-querying.md',                  label: 'Subject — Indexing & Querying' },
  { id: 'subj-testing',      section: 'Subject',       url: 'subject/testing.md',                                label: 'Subject — Testing' },
  { id: 'subj-simulator',    section: 'Subject',       url: 'subject/dev-host-simulator.md',                     label: 'Subject — Dev Host Simulator' },
  { id: 'subj-views',        section: 'Subject',       url: 'subject/views.md',                                  label: 'Subject — Views' },
  { id: 'subj-access',       section: 'Subject',       url: 'subject/access-control.md',                         label: 'Subject — Access Control' },
  { id: 'subj-consumption',  section: 'Subject',       url: 'subject/consumption.md',                            label: 'Subject — Consumption' },

  // Tools
  { id: 'tools-index',       section: 'Tools',         url: 'tools/index.md',                                    label: 'Tools — Introduction' },
  { id: 'tools-cli',         section: 'Tools',         url: 'tools/cli.md',                                      label: 'Tools — eggshell CLI' },
  { id: 'tools-snippets',    section: 'Tools',         url: 'tools/snippets.md',                                  label: 'Tools — Snippets' },
];

// HowTo uses a nested DU: HowTo (Markdown url) → /HowTo/Markdown/{enc(url)}
const HOWTO_PAGES = [
  { id: 'howto-index',       url: 'how-to/index.md',          label: 'HowTo — Introduction' },
  { id: 'howto-faq',         url: 'how-to/faq.md',            label: 'HowTo — FAQ' },
  { id: 'howto-projects',    url: 'how-to/projects.md',        label: 'HowTo — Where to find examples' },
  { id: 'howto-tap',         url: 'how-to/tap-capture.md',     label: 'HowTo — Taps & Clicks' },
  { id: 'howto-executors',   url: 'how-to/executors.md',       label: 'HowTo — Executors' },
  { id: 'howto-responsive',  url: 'how-to/responsive.md',      label: 'HowTo — Responsive Components' },
  { id: 'howto-scrolling',   url: 'how-to/scrolling.md',       label: 'HowTo — Scrolling' },
  { id: 'howto-refs',        url: 'how-to/refs.md',            label: 'HowTo — React Refs' },
  { id: 'howto-spinners',    url: 'how-to/spinners.md',        label: 'HowTo — Spinners' },
];

function docsPageUrl(section, markdownUrl) {
  return `${baseUrl}/${enc('Desktop')}/${section}/${enc(markdownUrl)}`;
}

function howToPageUrl(markdownUrl) {
  return `${baseUrl}/${enc('Desktop')}/HowTo/Markdown/${enc(markdownUrl)}`;
}

function componentPageUrl(name) {
  return `${baseUrl}/${enc('Desktop')}/Components/${enc(name)}`;
}

// ─── Layout checks ────────────────────────────────────────────────────────

async function checkPageLayout(page) {
  return page.evaluate(() => {
    const results = {
      bodyOverflow: null,
      codeBlockIssues: [],
      tableIssues: [],
      imageIssues: [],
    };

    // Page-level horizontal overflow
    const docW = document.documentElement.scrollWidth;
    const viewW = document.documentElement.clientWidth;
    results.bodyOverflow = {
      hasOverflow: docW > viewW + 4,
      docScrollWidth: docW,
      viewWidth: viewW,
      excess: docW - viewW,
    };

    // Helper: does element have a horizontally-scrollable ancestor?
    function hasScrollableAncestor(el) {
      let p = el.parentElement;
      while (p && p !== document.body && p !== document.documentElement) {
        const st = window.getComputedStyle(p);
        if (st.overflowX === 'auto' || st.overflowX === 'scroll') return true;
        p = p.parentElement;
      }
      return false;
    }

    // Code blocks (<pre>)
    for (const el of document.querySelectorAll('pre')) {
      if (el.scrollWidth > el.clientWidth + 4) {
        const scrollable = hasScrollableAncestor(el);
        results.codeBlockIssues.push({
          scrollWidth: el.scrollWidth,
          clientWidth: el.clientWidth,
          excess: el.scrollWidth - el.clientWidth,
          hasScrollableAncestor: scrollable,
          // not scrollable = content clipped / requires body scroll
          clipped: !scrollable,
          snippet: el.textContent?.slice(0, 80).replace(/\s+/g, ' ').trim() ?? '',
        });
      }
    }

    // Tables
    for (const el of document.querySelectorAll('table')) {
      if (el.scrollWidth > el.clientWidth + 4) {
        const scrollable = hasScrollableAncestor(el);
        results.tableIssues.push({
          scrollWidth: el.scrollWidth,
          clientWidth: el.clientWidth,
          excess: el.scrollWidth - el.clientWidth,
          hasScrollableAncestor: scrollable,
          clipped: !scrollable,
          // column count hint
          colCount: el.querySelector('tr')?.children?.length ?? 0,
        });
      }
    }

    // Images wider than viewport
    for (const el of document.querySelectorAll('img')) {
      const rect = el.getBoundingClientRect();
      if (rect.right > viewW + 4) {
        results.imageIssues.push({
          src: el.src?.slice(-60) ?? '',
          naturalWidth: el.naturalWidth,
          renderedWidth: Math.round(rect.width),
          overflow: Math.round(rect.right - viewW),
        });
      }
    }

    return results;
  });
}

// ─── Sidebar layout check ─────────────────────────────────────────────────

async function checkSidebarLayout(page, pageUrl) {
  const result = {
    pageUrl,
    opened: false,
    error: null,
    bladeHeight: null,
    scrollableMiddleHeight: null,
    sidebarTotalHeight: null,
    bladeSharePct: null,
    scrollableVisible: null,
    scrollableTopRatio: null,
    itemCount: { blades: 0, subItems: 0 },
    screenshotPath: null,
  };

  try {
    // Wait for sidebar toggle button
    const btn = page.locator('[data-testid="eggshell-sidebar-menu"]');
    if (!(await btn.count())) {
      result.error = 'hamburger button not found';
      return result;
    }

    await btn.click();
    await page.waitForTimeout(400);
    result.opened = true;

    // Screenshot of open sidebar
    const ssPath = join(ssDir, 'sidebar-open.png');
    await page.screenshot({ path: ssPath, fullPage: false });
    result.screenshotPath = ssPath;

    // Measure sidebar regions
    const metrics = await page.evaluate(() => {
      // The fixed-top blade items (section navigation)
      // They live in the sidebar's fixedTop region (LC.Sidebar.Base fixedTop=...)
      // In the DOM they are within the sidebar scroll container
      const sidebar = document.querySelector('[data-testid="eggshell-sidebar"]')
        ?? document.querySelector('.aesg-sidebar')
        ?? document.querySelector('[class*="Sidebar"]');

      // Try to find the fixed-top element (first child of sidebar content)
      // and the scrollable middle element
      const allSidebarItems = sidebar
        ? sidebar.querySelectorAll('[data-testid^="sidebar-blade-"]')
        : document.querySelectorAll('[data-testid^="sidebar-blade-"]');

      const subItems = sidebar
        ? sidebar.querySelectorAll('[data-testid^="sidebar-component-"], [data-testid^="sidebar-"]')
        : [];

      // Measure bounding boxes
      const bladeItems = [...document.querySelectorAll('[data-testid^="sidebar-blade-"]')];
      const bladeTop = bladeItems.length
        ? Math.min(...bladeItems.map(el => el.getBoundingClientRect().top))
        : null;
      const bladeBottom = bladeItems.length
        ? Math.max(...bladeItems.map(el => el.getBoundingClientRect().bottom))
        : null;
      const bladeHeight = bladeTop != null ? bladeBottom - bladeTop : null;

      // Find scrollable middle (the region after blades containing sub-page links)
      // It's typically in a ScrollView / overflow:auto container
      let scrollRegion = null;
      for (const el of document.querySelectorAll('*')) {
        const st = window.getComputedStyle(el);
        const rect = el.getBoundingClientRect();
        if (
          (st.overflowY === 'auto' || st.overflowY === 'scroll') &&
          rect.height > 50 &&
          rect.top > (bladeBottom ?? 0) - 20
        ) {
          scrollRegion = { top: rect.top, height: rect.height, bottom: rect.bottom };
          break;
        }
      }

      const viewHeight = window.innerHeight;
      const viewWidth = window.innerWidth;

      return {
        viewHeight,
        viewWidth,
        bladeCount: bladeItems.length,
        bladeHeight,
        bladeBottom,
        scrollRegion,
        subItemCount: sidebar?.querySelectorAll('[data-testid^="sidebar-"]').length ?? 0,
      };
    });

    result.bladeHeight = metrics.bladeHeight;
    result.sidebarTotalHeight = metrics.viewHeight;
    result.itemCount.blades = metrics.bladeCount;
    result.itemCount.subItems = metrics.subItemCount;

    if (metrics.scrollRegion) {
      result.scrollableMiddleHeight = metrics.scrollRegion.height;
      result.scrollableVisible = metrics.scrollRegion.top < metrics.viewHeight && metrics.scrollRegion.height > 0;
      result.scrollableTopRatio = metrics.scrollRegion.top / metrics.viewHeight;
    }

    if (metrics.bladeHeight != null && metrics.viewHeight) {
      result.bladeSharePct = Math.round((metrics.bladeHeight / metrics.viewHeight) * 100);
    }

    // Close sidebar
    await btn.click().catch(() => {});
    await page.waitForTimeout(200);
  } catch (e) {
    result.error = String(e.message ?? e);
  }

  return result;
}

// ─── Per-page audit ────────────────────────────────────────────────────────

async function auditPage(browser, { id, label, pageUrl, kind }) {
  const context = await browser.newContext({
    viewport: VIEWPORT,
    userAgent:
      'Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1',
  });
  const page = await context.newPage();

  const rawConsole = [];
  page.on('console', (msg) => {
    if (msg.type() === 'debug' || msg.type() === 'log') return;
    const text = msg.text();
    rawConsole.push({ type: msg.type(), text, ...classifyForFullAudit(text, msg.type()) });
  });
  page.on('pageerror', (err) => {
    rawConsole.push({ type: 'pageerror', text: err.message, ...classifyForFullAudit(err.message, 'pageerror') });
  });

  let httpStatus = 0;
  let loadError = null;
  let layout = null;
  let screenshotPath = null;

  try {
    const resp = await page.goto(pageUrl, { waitUntil: 'networkidle', timeout: 60000 });
    httpStatus = resp?.status() ?? 0;
    await page.waitForTimeout(1200);

    layout = await checkPageLayout(page);

    // Screenshot every page
    const ssFile = join(ssDir, `${id}.png`);
    await page.screenshot({ path: ssFile, fullPage: false });
    screenshotPath = ssFile;
  } catch (e) {
    loadError = String(e.message ?? e);
  }

  const actionable = rawConsole.filter((e) => e.bucket === 'actionable');

  await context.close();

  const issues = [];
  if (layout?.bodyOverflow?.hasOverflow) {
    issues.push({ kind: 'body-overflow', detail: `page body is ${layout.bodyOverflow.excess}px wider than viewport` });
  }
  for (const cb of layout?.codeBlockIssues ?? []) {
    issues.push({
      kind: cb.clipped ? 'code-block-clipped' : 'code-block-overflows-but-scrollable',
      detail: `pre overflows by ${cb.excess}px${cb.clipped ? ' with NO scroll container — content is cut off' : ' (has scroll container)'}`,
      snippet: cb.snippet,
    });
  }
  for (const tbl of layout?.tableIssues ?? []) {
    issues.push({
      kind: tbl.clipped ? 'table-clipped' : 'table-overflows-but-scrollable',
      detail: `table (${tbl.colCount} cols) overflows by ${tbl.excess}px${tbl.clipped ? ' with NO scroll container — cut off' : ' (has scroll container)'}`,
    });
  }
  for (const img of layout?.imageIssues ?? []) {
    issues.push({ kind: 'image-overflow', detail: `img overflows viewport by ${img.overflow}px (src: ...${img.src})` });
  }
  for (const ce of actionable) {
    issues.push({ kind: `console-${ce.kind ?? ce.type}`, detail: ce.summary ?? ce.text?.slice(0, 120) });
  }

  const severity = loadError ? 'load-fail'
    : issues.some((i) => i.kind.includes('clipped')) ? 'clipped'
    : issues.some((i) => i.kind === 'body-overflow') ? 'overflow'
    : issues.length > 0 ? 'minor'
    : 'ok';

  return { id, label, kind, pageUrl, httpStatus, loadError, severity, issues, layout, screenshotPath };
}

// ─── Main ─────────────────────────────────────────────────────────────────

const browser = await chromium.launch({ headless: true });

// ── 1. Docs pages ──────────────────────────────────────────────────────────
console.log(`\nAuditing ${DOCS_PAGES.length + HOWTO_PAGES.length} docs pages on ${baseUrl} at ${VIEWPORT.width}x${VIEWPORT.height}...\n`);

const docResults = [];
for (const p of DOCS_PAGES) {
  const r = await auditPage(browser, {
    id: p.id,
    label: p.label,
    kind: 'docs',
    pageUrl: docsPageUrl(p.section, p.url),
  });
  docResults.push(r);
  const flag = r.loadError ? 'LOAD FAIL'
    : r.issues.length ? r.issues.map((i) => i.kind).join(', ')
    : 'ok';
  console.log(`${p.label.padEnd(48)} ${flag}`);
}

for (const p of HOWTO_PAGES) {
  const r = await auditPage(browser, {
    id: p.id,
    label: p.label,
    kind: 'docs',
    pageUrl: howToPageUrl(p.url),
  });
  docResults.push(r);
  const flag = r.loadError ? 'LOAD FAIL'
    : r.issues.length ? r.issues.map((i) => i.kind).join(', ')
    : 'ok';
  console.log(`${p.label.padEnd(48)} ${flag}`);
}

// ── 2. Sidebar layout check ────────────────────────────────────────────────
console.log('\nChecking sidebar layout (opening hamburger menu on Architecture page)...');
let sidebarAudit = null;
{
  const ctx = await browser.newContext({
    viewport: VIEWPORT,
    userAgent:
      'Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1',
  });
  const pg = await ctx.newPage();
  const archUrl = docsPageUrl('Architecture', 'architecture/index.md');
  await pg.goto(archUrl, { waitUntil: 'networkidle', timeout: 60000 }).catch(() => {});
  await pg.waitForTimeout(1200);
  sidebarAudit = await checkSidebarLayout(pg, archUrl);
  await ctx.close();

  if (sidebarAudit.error) {
    console.log(`  sidebar check error: ${sidebarAudit.error}`);
  } else if (sidebarAudit.bladeSharePct != null) {
    console.log(
      `  blades: ${sidebarAudit.bladeHeight}px (${sidebarAudit.bladeSharePct}% of ${sidebarAudit.sidebarTotalHeight}px viewport), ` +
      `scrollable middle: ${sidebarAudit.scrollableMiddleHeight ?? '?'}px, ` +
      `scrollable visible: ${sidebarAudit.scrollableVisible}`
    );
  }
}

// ── 3. Component pages ─────────────────────────────────────────────────────
const componentNames = discoverGalleryComponents();
console.log(`\nAuditing ${componentNames.length} component pages...\n`);

const compResults = [];
for (const name of componentNames) {
  const r = await auditPage(browser, {
    id: `comp-${name.toLowerCase().replace(/[^a-z0-9]/g, '-')}`,
    label: `Component — ${name}`,
    kind: 'component',
    pageUrl: componentPageUrl(name),
  });
  compResults.push(r);
  const flag = r.loadError ? 'LOAD FAIL'
    : r.issues.length ? r.issues.map((i) => i.kind).join(', ')
    : 'ok';
  console.log(`${name.padEnd(30)} ${flag}`);
}

await browser.close();

// ─── Aggregate ────────────────────────────────────────────────────────────

const allResults = [...docResults, ...compResults];

const byKind = {};
for (const r of allResults) {
  for (const issue of r.issues) {
    if (!byKind[issue.kind]) byKind[issue.kind] = [];
    byKind[issue.kind].push({ page: r.label, detail: issue.detail });
  }
}

const pagesWithIssues = allResults.filter((r) => r.issues.length > 0 || r.loadError);
const clippedPages = allResults.filter((r) => r.severity === 'clipped');
const overflowPages = allResults.filter((r) => r.severity === 'overflow');

// ─── JSON report ──────────────────────────────────────────────────────────

const report = {
  baseUrl,
  viewport: VIEWPORT,
  auditedAt: new Date().toISOString(),
  summary: {
    totalPages: allResults.length,
    docPages: docResults.length,
    componentPages: compResults.length,
    pagesWithIssues: pagesWithIssues.length,
    clippedPages: clippedPages.length,
    overflowPages: overflowPages.length,
    cleanPages: allResults.filter((r) => r.severity === 'ok').length,
  },
  sidebar: sidebarAudit,
  issuesByKind: byKind,
  allResults: allResults.map((r) => ({
    id: r.id,
    label: r.label,
    kind: r.kind,
    httpStatus: r.httpStatus,
    severity: r.severity,
    loadError: r.loadError,
    issues: r.issues,
    screenshotPath: r.screenshotPath,
  })),
};

writeFileSync(join(outDir, 'handheld-audit.json'), JSON.stringify(report, null, 2));

// ─── Markdown report ──────────────────────────────────────────────────────

let md = `# Handheld Usability Audit — ${baseUrl}\n\n`;
md += `**Viewport:** ${VIEWPORT.width} x ${VIEWPORT.height} (iPhone 14 Pro)\n`;
md += `**Date:** ${report.auditedAt}\n`;
md += `**Pages audited:** ${allResults.length} (${docResults.length} docs + ${compResults.length} components)\n\n`;

md += `## Summary\n\n`;
md += `| Metric | Count |\n|--------|-------|\n`;
md += `| Pages with clipped content (no scroll) | ${clippedPages.length} |\n`;
md += `| Pages with body overflow | ${overflowPages.length} |\n`;
md += `| Pages with any issue | ${pagesWithIssues.length} |\n`;
md += `| Clean pages | ${report.summary.cleanPages} |\n\n`;

// Sidebar section
md += `## Sidebar Layout (handheld, hamburger open)\n\n`;
if (sidebarAudit?.error) {
  md += `> Error: ${sidebarAudit.error}\n\n`;
} else if (sidebarAudit) {
  const bladeShareNote = sidebarAudit.bladeSharePct != null && sidebarAudit.bladeSharePct > 50
    ? ` ⚠ Takes ${sidebarAudit.bladeSharePct}% of screen height — leaves little room for sub-pages`
    : sidebarAudit.bladeSharePct != null ? ` (${sidebarAudit.bladeSharePct}% of screen)` : '';
  md += `| Measurement | Value |\n|-------------|-------|\n`;
  md += `| Viewport height | ${sidebarAudit.sidebarTotalHeight}px |\n`;
  md += `| Blade (top sections) height | ${sidebarAudit.bladeHeight ?? '?'}px${bladeShareNote} |\n`;
  md += `| Scrollable sub-pages height | ${sidebarAudit.scrollableMiddleHeight ?? '?'}px |\n`;
  md += `| Scrollable region visible | ${sidebarAudit.scrollableVisible ?? '?'} |\n`;
  md += `| Blade items | ${sidebarAudit.itemCount.blades} |\n`;
  md += `\n`;
  if (sidebarAudit.bladeSharePct != null && sidebarAudit.bladeSharePct > 50) {
    md += `> **Issue:** The blade (section navigation) occupies ${sidebarAudit.bladeSharePct}% of the viewport height, leaving only ${100 - sidebarAudit.bladeSharePct}% for the sub-page list. Users must scroll just to reach the sub-pages. Consider collapsing the blade into an accordion or reducing item height.\n\n`;
  }
  if (sidebarAudit.screenshotPath) {
    md += `Screenshot: \`${sidebarAudit.screenshotPath}\`\n\n`;
  }
}

// Issues by kind
const issueKinds = Object.keys(byKind);
if (issueKinds.length) {
  md += `## Issues by Kind\n\n`;
  for (const kind of issueKinds.sort()) {
    const items = byKind[kind];
    md += `### ${kind} (${items.length} occurrence${items.length !== 1 ? 's' : ''})\n\n`;
    const shown = items.slice(0, 8);
    for (const item of shown) {
      md += `- **${item.page}**: ${item.detail}\n`;
    }
    if (items.length > 8) md += `- *(and ${items.length - 8} more)*\n`;
    md += '\n';
  }
}

// Per-page table
md += `## Per-Page Results\n\n`;
md += `### Docs pages\n\n`;
md += `| Page | HTTP | Severity | Issues |\n|------|------|----------|--------|\n`;
for (const r of docResults) {
  const issueList = r.issues.slice(0, 3).map((i) => i.kind).join(', ') || (r.loadError ? 'load-fail' : '—');
  md += `| ${r.label} | ${r.httpStatus} | ${r.severity} | ${issueList} |\n`;
}

md += `\n### Component pages\n\n`;
md += `| Component | HTTP | Severity | Issues |\n|-----------|------|----------|--------|\n`;
for (const r of compResults) {
  const issueList = r.issues.slice(0, 3).map((i) => i.kind).join(', ') || (r.loadError ? 'load-fail' : '—');
  const name = r.label.replace('Component — ', '');
  md += `| ${name} | ${r.httpStatus} | ${r.severity} | ${issueList} |\n`;
}

writeFileSync(join(outDir, 'handheld-audit.md'), md);

console.log(`\n--- Handheld audit summary ---\n`);
console.log(`Pages with clipped content: ${clippedPages.length}`);
console.log(`Pages with body overflow:   ${overflowPages.length}`);
console.log(`Pages with any issue:       ${pagesWithIssues.length} / ${allResults.length}`);
if (sidebarAudit?.bladeSharePct) {
  console.log(`Sidebar blade share:        ${sidebarAudit.bladeSharePct}% of ${VIEWPORT.height}px viewport`);
}
console.log(`\nWrote ${join(outDir, 'handheld-audit.json')}`);
console.log(`Wrote ${join(outDir, 'handheld-audit.md')}`);
console.log(`Screenshots: ${ssDir}/`);
