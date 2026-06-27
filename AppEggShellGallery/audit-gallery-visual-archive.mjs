/**
 * Archival screenshots for human / AI aesthetic review (no pixel diff).
 * Captures sample visuals + viewport after interactions complete.
 */

import { mkdirSync, writeFileSync, appendFileSync } from 'fs';
import { join } from 'path';
import { PLATFORM, sampleCellSelectorFor, WEB_SAMPLE_CELL_SELECTOR } from './audit-gallery-platform.mjs';

export const SAMPLE_CELL_SELECTOR = WEB_SAMPLE_CELL_SELECTOR;

/** Components where overlap / placement issues are more likely. */
const OVERLAP_REVIEW_TAGS = new Set([
  'Layout_Row',
  'Layout_Column',
  'Layout_Sized',
  'Layout_Constrained',
  'Forms',
  'Grid',
  'QueryGrid',
  'Dialogs',
  'Carousel',
  'Tabs',
  'Nav_Top',
  'Nav_Bottom',
  'Sidebar',
  'ContextMenu',
  'Draggable',
  'AnimatableImage',
  'AnimatableText',
  'AnimatableTextInput',
  'AnimatableView',
  'ThirdParty_Map',
  'ThirdParty_Recharts',
  'Input_Picker',
  'Input_Date',
  'Input_Image',
  'Input_File',
  'Card',
  'ImageCard',
  'Thumbs',
  'Scrim',
  'ItemList',
  'WithSortAndFilter',
]);

const REVIEW_FOCUS = {
  layout: 'alignment, spacing, sizing, responsive layout',
  overlap: 'element overlap, clipping, z-index stacking, obscured controls',
  aesthetics: 'visual polish, typography, color, balance',
  controls: 'button/input placement, touch targets, label association',
  media: 'image crop, aspect ratio, loading placeholders',
  navigation: 'nav item spacing, active states, overflow',
  overlay: 'dialogs, scrims, menus, popovers vs page content',
  data: 'grid/table column alignment, pagination, empty states',
  chart: 'chart axes, legends, labels, responsive resize',
  animation: 'post-animation resting state (not motion verification)',
};

function reviewFocusFor(componentName) {
  const focus = ['aesthetics', 'overlap'];
  if (componentName.startsWith('Layout_')) focus.push('layout');
  if (componentName.startsWith('Input_') || componentName === 'Forms') focus.push('controls');
  if (componentName.startsWith('Animatable')) focus.push('animation', 'layout');
  if (componentName === 'Grid' || componentName === 'QueryGrid') focus.push('data', 'layout');
  if (componentName === 'Dialogs' || componentName === 'Scrim' || componentName === 'ContextMenu') {
    focus.push('overlay');
  }
  if (componentName.startsWith('Nav_') || componentName === 'Sidebar') focus.push('navigation');
  if (componentName === 'ThirdParty_Map' || componentName === 'ThirdParty_Recharts') focus.push('chart', 'media');
  if (['Thumb', 'Thumbs', 'ImageCard', 'Input_Image', 'Avatar'].includes(componentName)) focus.push('media');
  if (componentName === 'Carousel' || componentName === 'Draggable') focus.push('layout', 'media');
  return [...new Set(focus)];
}

/**
 * @param {import('playwright').Page} page
 * @param {string} componentName
 * @param {{ passDir: string, passIndex?: number, baseUrl?: string, url?: string, platform?: 'web' | 'android', log?: (msg: string) => void }} options
 */
export async function archiveVisualsForReview(page, componentName, options) {
  const { passDir, passIndex = 1, baseUrl = '', url = '', platform = PLATFORM.WEB, log = () => {} } = options;
  const isAndroid = platform === PLATFORM.ANDROID;
  const cellSelector = sampleCellSelectorFor(platform);
  const archiveRoot = join(passDir, 'visual-archive');
  const componentDir = join(archiveRoot, componentName);
  mkdirSync(componentDir, { recursive: true });

  if (isAndroid && typeof page.scrollSampleTable === 'function') {
    await page.scrollSampleTable().catch(() => {});
  } else {
    await page.evaluate(() => {
      const table = document.querySelector('.aesg-ContentComponent-table');
      if (!table) return;
      let el = table.parentElement;
      while (el) {
        if (el.scrollWidth > el.clientWidth + 4) el.scrollLeft = 0;
        el = el.parentElement;
      }
    }).catch(() => {});
  }

  await page.waitForTimeout(200);

  const cells = page.locator(cellSelector);
  const cellCount = await cells.count();
  const capturedAt = new Date().toISOString();
  const reviewFocus = reviewFocusFor(componentName);
  const images = [];

  for (let i = 0; i < cellCount; i++) {
    const cell = cells.nth(i);
    try {
      await cell.scrollIntoViewIfNeeded({ timeout: 8000 });
    } catch {
      /* best effort */
    }
    await page.waitForTimeout(150);
    const fileName = `sample-${String(i).padStart(2, '0')}-after-interaction.png`;
    const filePath = join(componentDir, fileName);
    try {
      await cell.screenshot({ path: filePath, timeout: 10000 });
      images.push({
        id: `sample-${i}-after`,
        file: fileName,
        path: filePath,
        scope: 'sample-cell',
        sampleIndex: i,
        reviewFocus,
      });
    } catch (e) {
      log(`visual archive: sample ${i} screenshot failed: ${e.message}`);
    }
  }

  const includeViewport =
    OVERLAP_REVIEW_TAGS.has(componentName) || cellCount === 0 || cellCount > 3;
  if (includeViewport) {
    const fileName = 'viewport-after-interaction.png';
    const filePath = join(componentDir, fileName);
    try {
      await page.screenshot({ path: filePath, fullPage: false, timeout: 10000 });
      images.push({
        id: 'viewport-after',
        file: fileName,
        path: filePath,
        scope: 'viewport',
        reviewFocus: [...reviewFocus, 'overlap'],
      });
    } catch (e) {
      log(`visual archive: viewport screenshot failed: ${e.message}`);
    }
  }

  const entry = {
    component: componentName,
    passIndex,
    capturedAt,
    baseUrl,
    url,
    sampleCellCount: cellCount,
    overlapReviewPriority: OVERLAP_REVIEW_TAGS.has(componentName),
    reviewFocus,
    reviewFocusDescriptions: Object.fromEntries(
      reviewFocus.map((k) => [k, REVIEW_FOCUS[k] ?? k])
    ),
    reviewPrompt:
      'Review for aesthetics and UI defects: overlapping elements, clipped content, misalignment, ' +
      'awkward spacing, poor visual hierarchy, and controls that look hard to use. ' +
      'No pixel baseline — qualitative judgment only.',
    images,
  };

  writeFileSync(join(componentDir, 'manifest.json'), JSON.stringify(entry, null, 2));
  appendFileSync(
    join(archiveRoot, 'index.jsonl'),
    JSON.stringify({
      component: componentName,
      capturedAt,
      imageCount: images.length,
      manifest: join('visual-archive', componentName, 'manifest.json'),
      overlapReviewPriority: entry.overlapReviewPriority,
    }) + '\n'
  );

  log(`visual archive: ${images.length} image(s) → visual-archive/${componentName}/`);
  return entry;
}

/**
 * Write pass-level README for reviewers (human or AI batch).
 */
export function writeVisualArchiveReadme(passDir, passIndex, baseUrl) {
  const archiveRoot = join(passDir, 'visual-archive');
  mkdirSync(archiveRoot, { recursive: true });
  const md = `# Visual archive — pass ${passIndex}

Base URL: ${baseUrl}

Screenshots captured **after interactions** on each gallery component page.
**No pixel comparison** — stored for qualitative review (aesthetics, overlap, placement).

## Layout

\`\`\`
visual-archive/
  index.jsonl              # one line per component (machine-readable index)
  {Component}/
    manifest.json          # metadata + review focus tags
    sample-00-after-interaction.png
    sample-01-after-interaction.png
    viewport-after-interaction.png   # overlap-prone / multi-sample pages
\`\`\`

## What to look for

| Tag | Check |
|-----|--------|
| overlap | Elements covering each other, clipped text/buttons |
| layout | Alignment, spacing, sizing, scroll regions |
| aesthetics | Typography, color, visual balance |
| controls | Labels, inputs, touch targets |
| media | Images thumbs/crops/aspect ratio |
| overlay | Dialogs, menus, scrims vs content |
| data | Grid columns, headers, pagination |
| navigation | Nav bars, sidebar items |
| animation | Resting state after animate (not frame-by-frame) |

## Priority components

Components tagged \`overlapReviewPriority: true\` in \`index.jsonl\`.

## AI batch review

Load \`index.jsonl\`, open each \`manifest.json\`, attach PNGs, use \`reviewPrompt\` from manifest.
`;
  writeFileSync(join(archiveRoot, 'README.md'), md);
}
