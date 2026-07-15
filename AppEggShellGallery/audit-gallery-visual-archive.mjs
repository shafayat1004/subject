/**
 * Archival screenshots for human / AI aesthetic review (no pixel diff).
 * Captures before interaction, after interaction, and mid-animation frames.
 */

import { mkdirSync, writeFileSync, appendFileSync } from 'fs';
import { join } from 'path';
import { PLATFORM, sampleCellSelectorFor, WEB_SAMPLE_CELL_SELECTOR } from './audit-gallery-platform.mjs';

export const SAMPLE_CELL_SELECTOR = WEB_SAMPLE_CELL_SELECTOR;

/** Components where overlap / placement issues are more likely. */
export const OVERLAP_REVIEW_TAGS = new Set([
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
  'ThirdParty_MarkdownViewer',
  'ThirdParty_ImagePicker',
  'ThirdParty_ReCaptcha',
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

/** Gallery routes that run animation interaction recipes with mid-frame captures. */
export const ANIMATION_COMPONENTS = new Set([
  'AnimatableImage',
  'AnimatableText',
  'AnimatableTextInput',
  'AnimatableView',
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
  animation: 'before/after resting state plus mid-animation frames on Animatable* pages',
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

function sampleFileName(sampleIndex, phase, midIndex = null) {
  const idx = String(sampleIndex).padStart(2, '0');
  if (phase === 'before') return `sample-${idx}-before-interaction.png`;
  if (phase === 'after') return `sample-${idx}-after-interaction.png`;
  if (phase === 'mid') return `sample-${idx}-mid-${String(midIndex).padStart(2, '0')}-animation.png`;
  throw new Error(`Unknown visual archive phase: ${phase}`);
}

function viewportFileName(phase, midIndex = null) {
  if (phase === 'before') return 'viewport-before-interaction.png';
  if (phase === 'after') return 'viewport-after-interaction.png';
  if (phase === 'mid') return `viewport-mid-${String(midIndex).padStart(2, '0')}-animation.png`;
  throw new Error(`Unknown visual archive phase: ${phase}`);
}

async function resetSampleScroll(page, platform) {
  const isAndroid = platform === PLATFORM.ANDROID;
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
}

function shouldCaptureViewport(componentName, cellCount, phase) {
  if (phase === 'mid') return ANIMATION_COMPONENTS.has(componentName);
  return OVERLAP_REVIEW_TAGS.has(componentName) || cellCount === 0 || cellCount > 3;
}

/**
 * @param {import('playwright').Page} page
 * @param {string} componentName
 * @param {{ passDir: string, passIndex?: number, baseUrl?: string, url?: string, platform?: 'web' | 'android', log?: (msg: string) => void }} options
 */
export function createVisualArchiveSession(page, componentName, options) {
  const {
    passDir,
    passIndex = 1,
    baseUrl = '',
    url = '',
    platform = PLATFORM.WEB,
    log = () => {},
  } = options;
  const cellSelector = sampleCellSelectorFor(platform);
  const archiveRoot = join(passDir, 'visual-archive');
  const componentDir = join(archiveRoot, componentName);
  mkdirSync(componentDir, { recursive: true });

  const reviewFocus = reviewFocusFor(componentName);
  /** @type {Array<{ id: string, file: string, path: string, scope: string, phase: string, sampleIndex?: number, midIndex?: number, reviewFocus: string[] }>} */
  const images = [];
  let cellCount = 0;
  let capturedAt = new Date().toISOString();
  const viewportCaptured = { before: false, after: false, mid: new Set() };

  async function captureSampleCell(sampleIndex, phase, midIndex = null) {
    const cells = page.locator(cellSelector);
    const cell = cells.nth(sampleIndex);
    try {
      await cell.scrollIntoViewIfNeeded({ timeout: 8000 });
    } catch {
      /* best effort */
    }
    await page.waitForTimeout(phase === 'mid' ? 80 : 150);

    const fileName = sampleFileName(sampleIndex, phase, midIndex);
    const filePath = join(componentDir, fileName);
    try {
      await cell.screenshot({ path: filePath, timeout: 10000 });
      const id =
        phase === 'mid'
          ? `sample-${sampleIndex}-mid-${midIndex}`
          : `sample-${sampleIndex}-${phase}`;
      images.push({
        id,
        file: fileName,
        path: filePath,
        scope: 'sample-cell',
        phase,
        sampleIndex,
        ...(phase === 'mid' ? { midIndex } : {}),
        reviewFocus,
      });
      return true;
    } catch (e) {
      log(`visual archive: sample ${sampleIndex} ${phase} screenshot failed: ${e.message}`);
      return false;
    }
  }

  async function captureViewport(phase, midIndex = null) {
    const fileName = viewportFileName(phase, midIndex);
    const filePath = join(componentDir, fileName);
    try {
      await page.screenshot({ path: filePath, fullPage: false, timeout: 10000 });
      const id = phase === 'mid' ? `viewport-mid-${midIndex}` : `viewport-${phase}`;
      images.push({
        id,
        file: fileName,
        path: filePath,
        scope: 'viewport',
        phase,
        ...(phase === 'mid' ? { midIndex } : {}),
        reviewFocus: [...reviewFocus, phase === 'mid' ? 'animation' : 'overlap'],
      });
      return true;
    } catch (e) {
      log(`visual archive: viewport ${phase} screenshot failed: ${e.message}`);
      return false;
    }
  }

  async function captureAllSamples(phase, midIndex = null) {
    await resetSampleScroll(page, platform);
    const cells = page.locator(cellSelector);
    cellCount = await cells.count();

    for (let i = 0; i < cellCount; i++) {
      await captureSampleCell(i, phase, midIndex);
    }

    const wantViewport = shouldCaptureViewport(componentName, cellCount, phase);
    if (phase === 'before' && wantViewport && !viewportCaptured.before) {
      await captureViewport('before');
      viewportCaptured.before = true;
    } else if (phase === 'after' && wantViewport && !viewportCaptured.after) {
      await captureViewport('after');
      viewportCaptured.after = true;
    } else if (phase === 'mid' && wantViewport && !viewportCaptured.mid.has(midIndex)) {
      await captureViewport('mid', midIndex);
      viewportCaptured.mid.add(midIndex);
    }
  }

  return {
    componentName,
    componentDir,

    /** Capture resting state before component interactions. */
    async captureBefore() {
      capturedAt = new Date().toISOString();
      await captureAllSamples('before');
      log(`visual archive: before — ${images.length} image(s) so far`);
    },

    /**
     * Capture a mid-animation frame for one sample column (Animatable* recipes).
     * @param {number} sampleIndex
     * @param {1 | 2} midIndex
     */
    async captureAnimationMid(sampleIndex, midIndex) {
      await captureSampleCell(sampleIndex, 'mid', midIndex);
      const wantViewport = shouldCaptureViewport(componentName, cellCount, 'mid');
      if (wantViewport && !viewportCaptured.mid.has(midIndex)) {
        await captureViewport('mid', midIndex);
        viewportCaptured.mid.add(midIndex);
      }
      log(`visual archive: mid-${midIndex} sample ${sampleIndex}`);
    },

    /** Capture resting state after all interactions; writes manifest.json. */
    async captureAfter() {
      await captureAllSamples('after');

      const entry = {
        component: componentName,
        passIndex,
        capturedAt,
        baseUrl,
        url,
        sampleCellCount: cellCount,
        overlapReviewPriority: OVERLAP_REVIEW_TAGS.has(componentName),
        animationReview: ANIMATION_COMPONENTS.has(componentName),
        reviewFocus,
        reviewFocusDescriptions: Object.fromEntries(
          reviewFocus.map((k) => [k, REVIEW_FOCUS[k] ?? k])
        ),
        reviewPrompt:
          'Review for aesthetics and UI defects: overlapping elements, clipped content, misalignment, ' +
          'awkward spacing, poor visual hierarchy, and controls that look hard to use. ' +
          'Compare before vs after interaction; on Animatable* pages also inspect mid-animation frames. ' +
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
          animationReview: entry.animationReview,
        }) + '\n'
      );

      log(`visual archive: ${images.length} image(s) → visual-archive/${componentName}/`);
      return entry;
    },
  };
}

/**
 * @deprecated Prefer createVisualArchiveSession().captureBefore/captureAfter.
 */
export async function archiveVisualsForReview(page, componentName, options) {
  const session = createVisualArchiveSession(page, componentName, options);
  return session.captureAfter();
}

/**
 * Write pass-level README for reviewers (human or AI batch).
 */
export function writeVisualArchiveReadme(passDir, passIndex, baseUrl) {
  const archiveRoot = join(passDir, 'visual-archive');
  mkdirSync(archiveRoot, { recursive: true });
  const md = `# Visual archive — pass ${passIndex}

Base URL: ${baseUrl}

Screenshots on each gallery component page:
- **Before** interactions (resting state)
- **After** interactions (final state)
- **Mid-animation** (Animatable* only): two frames during the Animate action

**No pixel comparison** — stored for qualitative review (aesthetics, overlap, placement).

## Layout

\`\`\`
visual-archive/
  index.jsonl              # one line per component (machine-readable index)
  {Component}/
    manifest.json          # metadata + review focus tags
    sample-00-before-interaction.png
    sample-00-mid-01-animation.png   # Animatable* only
    sample-00-mid-02-animation.png   # Animatable* only
    sample-00-after-interaction.png
    viewport-before-interaction.png
    viewport-mid-01-animation.png    # Animatable* only
    viewport-after-interaction.png
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
| animation | Before/after resting state; mid frames for motion glitches |

## Priority components

Components tagged \`overlapReviewPriority: true\` in \`index.jsonl\`.
Animatable* pages also set \`animationReview: true\`.

## AI batch review

Load \`index.jsonl\`, open each \`manifest.json\`, attach PNGs, use \`reviewPrompt\` from manifest.
`;
  writeFileSync(join(archiveRoot, 'README.md'), md);
}
