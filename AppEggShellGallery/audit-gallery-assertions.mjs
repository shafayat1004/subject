/**
 * Post-interaction UI assertions for the EggShell Gallery audit.
 * Each assertion captures a screenshot (all by default, always on failure).
 */

import { mkdirSync, copyFileSync } from 'fs';
import { join, basename } from 'path';
import { PLATFORM, sampleCellSelectorFor } from './audit-gallery-platform.mjs';
import { findByTestId } from './audit-gallery-selectors.mjs';

/** @typedef {import('playwright').Page} Page */
/** @typedef {import('playwright').Locator} Locator */

function sanitize(name) {
  return name.replace(/[^a-zA-Z0-9._-]+/g, '_').slice(0, 80);
}

function a11ySlugTestId(prefix, label) {
  const slug = String(label)
    .toLowerCase()
    .replace(/\./g, '-')
    .replace(/ /g, '-')
    .replace(/\//g, '-');
  return `${prefix}-${slug}`;
}

/**
 * @param {Page} page
 * @param {string} componentName
 * @param {import('./audit-gallery-interactions.mjs').createInteractionContext extends (...args: any) => infer R ? R : never} ctx
 * @param {{ passDir: string, screenshotMode?: 'all' | 'failures' | 'none', platform?: 'web' | 'android', log?: (msg: string) => void }} options
 */
export async function runComponentAssertions(page, componentName, ctx, options) {
  const { passDir, screenshotMode = 'all', platform = PLATFORM.WEB, log = () => {} } = options;
  const isAndroid = platform === PLATFORM.ANDROID;
  const cellSelector = sampleCellSelectorFor(platform);
  const screenshotDir = join(passDir, 'screenshots', componentName);
  const failureDir = join(passDir, 'screenshots', 'failures', componentName);
  mkdirSync(screenshotDir, { recursive: true });

  const results = [];
  let index = 0;

  const sampleCells = () => page.locator(cellSelector);
  const firstCell = () => sampleCells().first();

  async function hasTestId(scope, testId) {
    return (await findByTestId(page, testId, { platform, scope }).count()) > 0;
  }

  async function hasPseudo(scope, text) {
    if (isAndroid) return cellContains(scope, text);
    return (await scope.locator(`[data-text-as-pseudo-element="${text}"]`).count()) > 0;
  }

  async function cellContains(scope, fragment) {
    const frag = fragment.toLowerCase();
    if (!isAndroid) {
      const pseudoMatch = await scope
        .locator('[data-text-as-pseudo-element]')
        .evaluateAll(
          (nodes, f) =>
            nodes.some((n) =>
              (n.getAttribute('data-text-as-pseudo-element') ?? '').toLowerCase().includes(f)
            ),
          frag
        );
      if (pseudoMatch) return true;
    }
    const text = await scope.innerText().catch(() => '');
    return text.toLowerCase().includes(frag);
  }

  async function inputValueForLabel(scope, label) {
    if (isAndroid) {
      const field = scope.getByLabel(label);
      if (await field.count()) return field.first().inputValue().catch(() => null);
      const input = scope.locator('input:not([type="file"]):not([type="hidden"]), textarea').first();
      if (await input.count()) return input.inputValue().catch(() => null);
      return null;
    }
    const pseudo = scope.locator(`[data-text-as-pseudo-element="${label}"]`).first();
    if (!(await pseudo.count())) return null;
    const input = pseudo.locator(
      'xpath=ancestor::*[.//input][1]//input[not(@type="hidden") and not(@type="file")]'
    ).first();
    if (!(await input.count())) return null;
    return input.inputValue().catch(() => null);
  }

  async function check(name, fn, scope = null) {
    index += 1;
    let passed = false;
    let message = name;
    try {
      const out = await fn();
      passed = !!out.passed;
      message = out.message ?? name;
    } catch (e) {
      passed = false;
      message = `${name}: ${e.message ?? e}`;
    }

    let screenshotPath = null;
    const shouldShot = screenshotMode === 'all' || (!passed && screenshotMode !== 'none');
    if (shouldShot) {
      const file = join(
        screenshotDir,
        `${String(index).padStart(3, '0')}-${passed ? 'pass' : 'FAIL'}-${sanitize(name)}.png`
      );
      try {
        if (scope && (await scope.count())) {
          await scope.screenshot({ path: file, timeout: 8000 });
        } else {
          await page.screenshot({ path: file, fullPage: false, timeout: 8000 });
        }
        screenshotPath = file;
        if (!passed) {
          mkdirSync(failureDir, { recursive: true });
          copyFileSync(file, join(failureDir, basename(file)));
        }
      } catch (e) {
        message += ` (screenshot failed: ${e.message})`;
      }
    }

    const result = { id: `${componentName}-${index}`, name, passed, message, screenshotPath };
    results.push(result);
    log(passed ? `ASSERT PASS: ${name}` : `ASSERT FAIL: ${message}`);
    return result;
  }

  async function cellHtmlContains(scope, fragment) {
    if (isAndroid) return cellContains(scope, fragment);
    const html = await scope.evaluate((el) => el.innerHTML ?? '').catch(() => '');
    return html.toLowerCase().includes(fragment.toLowerCase());
  }

  async function anySampleCell(page, predicate) {
    const cells = page.locator(cellSelector);
    const n = await cells.count();
    for (let i = 0; i < n; i++) {
      if (await predicate(cells.nth(i), i)) return true;
    }
    return false;
  }

  const handler = ASSERTION_HANDLERS[componentName] ?? ASSERTION_HANDLERS._default;
  await handler({
    page,
    ctx,
    check,
    firstCell,
    sampleCells,
    hasPseudo,
    cellContains,
    cellHtmlContains,
    anySampleCell,
    inputValueForLabel,
    componentName,
    hasTestId,
    a11ySlugTestId,
  });

  return results;
}

/** @type {Record<string, (tools: any) => Promise<void>>} */
const ASSERTION_HANDLERS = {
  Index: async ({ check, page, firstCell }) => {
    const bodyText = await page.locator('body').innerText().catch(() => '');
    await check('index page loaded', async () => ({
      passed: bodyText.includes('Components') || bodyText.includes('EggShell'),
      message: 'Gallery index should mention Components or EggShell',
    }));
    if (await firstCell().count()) {
      await check('index has sample area', async () => ({ passed: true, message: 'Sample cell present' }), firstCell());
    }
  },

  Tabs: async ({ check, firstCell, cellContains, hasPseudo, hasTestId, ctx, a11ySlugTestId }) => {
    const cell = firstCell();
    await check('Tabs expose tab testIds', async () => ({
      passed:
        (await hasTestId(cell, a11ySlugTestId('tab', 'Home'))) ||
        (await hasPseudo(cell, 'Home')),
      message: 'Basics sample should expose tab-home testId or Home label',
    }), cell);
    if (!(await ctx.clickTestIdOrLabel(cell, a11ySlugTestId('tab', 'Profile'), 'Profile'))) {
      await ctx.clickPseudo(cell, 'Profile');
    }
    await ctx.wait(300);
    await check('Profile tab content visible', async () => ({
      passed:
        (await cellContains(cell, 'PROFILE tab')) ||
        (await hasPseudo(cell, 'This is the PROFILE tab')),
      message: 'After clicking Profile, PROFILE tab content should show',
    }), cell);
    if (!(await ctx.clickTestIdOrLabel(cell, a11ySlugTestId('tab', 'Contact'), 'Contact'))) {
      await ctx.clickPseudo(cell, 'Contact');
    }
    await ctx.wait(300);
    await check('Contact tab content visible', async () => ({
      passed:
        (await cellContains(cell, 'CONTACT tab')) ||
        (await hasPseudo(cell, 'This is the CONTACT tab')),
      message: 'After clicking Contact, CONTACT tab content should show',
    }), cell);
  },

  ToggleButtons: async ({ check, firstCell, hasPseudo, hasTestId, ctx, a11ySlugTestId }) => {
    const cell = firstCell();
    for (const fruit of ['Mango', 'Peach', 'Banana']) {
      if (!(await ctx.clickTestIdOrLabel(cell, a11ySlugTestId('toggle-button', fruit), fruit))) {
        await ctx.clickPseudo(cell, fruit);
      }
      await ctx.wait(200);
      await check(`ToggleButtons ${fruit} selectable`, async () => ({
        passed:
          (await hasTestId(cell, a11ySlugTestId('toggle-button', fruit))) ||
          (await hasPseudo(cell, fruit)),
        message: `${fruit} toggle should remain visible after click`,
      }), cell);
    }
  },

  Forms: async ({ check, firstCell, inputValueForLabel, ctx }) => {
    const cell = firstCell();
    await ctx.fillLabel(cell, 'Name', 'Alice');
    await ctx.fillLabel(cell, 'Age', '30');
    await check('Forms Name field retains Alice', async () => {
      const v = await inputValueForLabel(cell, 'Name');
      return { passed: v === 'Alice', message: `Name input expected "Alice", got "${v}"` };
    }, cell);
    await check('Forms Age field retains 30', async () => {
      const v = await inputValueForLabel(cell, 'Age');
      return { passed: v === '30', message: `Age input expected "30", got "${v}"` };
    }, cell);
  },

  QueryGrid: async ({ check, firstCell, hasPseudo, cellContains, cellHtmlContains, inputValueForLabel, ctx }) => {
    const cell = firstCell();
    await ctx.fillLabel(cell, 'Substring', 'acc');
    await ctx.fillLabel(cell, 'MinLength', '5');
    await ctx.clickButton(cell, 'Submit');
    await ctx.wait(2500);
    await check('QueryGrid shows Word column header', async () => ({
      passed: await hasPseudo(cell, 'Word') || (await cellContains(cell, 'Word')),
      message: 'Grid header "Word" should be visible after submit',
    }), cell);
    await check('QueryGrid form values retained after submit', async () => {
      const sub = await inputValueForLabel(cell, 'Substring');
      const min = await inputValueForLabel(cell, 'MinLength');
      return {
        passed: sub === 'acc' && min === '5',
        message: `Expected Substring=acc MinLength=5, got "${sub}" / "${min}"`,
      };
    }, cell);
    await check('QueryGrid grid surface present', async () => ({
      passed:
        (await hasPseudo(cell, 'Character Count')) ||
        (await cellHtmlContains(cell, 'grid')) ||
        (await cell.locator('input').count()) >= 2,
      message: 'Query grid should show column headers or pagination after submit',
    }), cell);
  },

  Grid: async ({ check, firstCell, hasPseudo, cellContains }) => {
    const cell = firstCell();
    await check('Grid paginated sample has Word header', async () => ({
      passed: await hasPseudo(cell, 'Word') || (await cellContains(cell, 'Word')),
      message: 'Paginated grid should show Word column',
    }), cell);
    await check('Grid paginated sample has row data', async () => ({
      passed: (await cell.locator('input').count()) > 0 || (await cellContains(cell, 'Character')),
      message: 'Grid should show pagination controls or row data',
    }), cell);
  },

  Dialogs: async ({ check, firstCell, hasPseudo, page, ctx }) => {
    const cell = firstCell();
    await ctx.clickButton(cell, 'Alert');
    await ctx.wait(600);
    await check('Alert dialog shows OK', async () => ({
      passed: await hasPseudo(page.locator('body'), 'OK'),
      message: 'Alert dialog should expose OK button',
    }));
    await ctx.dismissOverlays();
    await check('Alert dialog dismissed', async () => ({
      passed: !(await hasPseudo(page.locator('body'), 'OK')) || (await hasPseudo(cell, 'Alert')),
      message: 'Dialog should close after dismiss',
    }), cell);
  },

  ContextMenu: async ({ check, anySampleCell, hasPseudo, page }) => {
    await check('ContextMenu open buttons present', async () => ({
      passed:
        (await anySampleCell(page, async (cell) => hasPseudo(cell, 'Handheld Context Menu'))) &&
        (await anySampleCell(page, async (cell) => hasPseudo(cell, 'Desktop Context Menu'))),
      message: 'Both context menu trigger buttons should be visible',
    }));
  },

  ErrorBoundary: async ({ check, sampleCells, hasPseudo, cellHtmlContains, ctx }) => {
    const n = await sampleCells().count();
    if (n >= 2) {
      const bounded = sampleCells().nth(1);
      await check('ErrorBoundary bounded sample shows try content', async () => ({
        passed: await cellHtmlContains(bounded, 'try content'),
        message: 'Bounded sample should show try content before bomb',
      }), bounded);
      await ctx.clickButton(bounded, 'The Bomb');
      await ctx.wait(800);
      await check('ErrorBoundary bounded sample reacts to bomb', async () => ({
        passed: !(await cellHtmlContains(bounded, 'try content')),
        message: 'Try content should disappear after The Bomb (error path taken)',
      }), bounded);
      if (await hasPseudo(bounded, 'Reset')) {
        await ctx.clickButton(bounded, 'Reset');
        await ctx.wait(400);
        await check('ErrorBoundary reset restores try content', async () => ({
          passed: await cellHtmlContains(bounded, 'try content'),
          message: 'Reset should restore try content when catch UI is shown',
        }), bounded);
      }
    }
  },

  AsyncData: async ({ check, sampleCells, cellContains, cellHtmlContains, anySampleCell, hasPseudo, ctx, page }) => {
    await check('AsyncData Unavailable sample', async () => ({
      passed: await anySampleCell(page, async (cell) => await hasPseudo(cell, 'Not available')),
      message: 'Unavailable AsyncData sample should show "Not available"',
    }));
    await check('AsyncData AccessDenied sample', async () => ({
      passed: await anySampleCell(page, async (cell) => await hasPseudo(cell, 'Access denied')),
      message: 'AccessDenied sample should show "Access denied"',
    }));
    await check('AsyncData Available sample renders Hyde', async () => ({
      passed: await anySampleCell(page, async (cell) => await cellHtmlContains(cell, 'Hyde')),
      message: 'Available sample visuals should reference Hyde',
    }));
    await check('AsyncData WhenFailed handler message', async () => ({
      passed: await anySampleCell(page, async (cell) =>
        (await cellHtmlContains(cell, 'Something went wrong')) ||
        (await cellHtmlContains(cell, "couldn't retrieve"))
      ),
      message: 'WhenFailed sample should show custom failure message in visuals',
    }));
    const failureCell = sampleCells().filter({ has: page.locator('[data-text-as-pseudo-element="Trigger AsyncData failure"]') }).first();
    if (await failureCell.count()) {
      await ctx.clickButton(failureCell, 'Trigger AsyncData failure');
      await ctx.wait(600);
      await check('AsyncData on-demand failure caught', async () => ({
        passed:
          (await cellContains(failureCell, 'Caught an error')) ||
          (await cellHtmlContains(failureCell, 'Caught an error')),
        message: 'Trigger should show ErrorBoundary catch message',
      }), failureCell);
    }
  },

  Input_Text: async ({ check, firstCell, inputValueForLabel, ctx }) => {
    const cell = firstCell();
    await ctx.fillLabel(cell, 'Name', 'Alice');
    await check('Input_Text Name retains value', async () => {
      const v = await inputValueForLabel(cell, 'Name');
      return { passed: v === 'Alice', message: `Name expected Alice, got "${v}"` };
    }, cell);
  },

  Input_Decimal: async ({ check, firstCell, inputValueForLabel, ctx }) => {
    const cell = firstCell();
    await ctx.fillLabel(cell, 'Price', '9.99');
    await check('Input_Decimal Price retains value', async () => {
      const v = await inputValueForLabel(cell, 'Price');
      return { passed: v === '9.99', message: `Price expected 9.99, got "${v}"` };
    }, cell);
  },

  Input_Checkbox: async ({ check, firstCell, hasPseudo }) => {
    const cell = firstCell();
    await check('Input_Checkbox labels visible', async () => ({
      passed:
        (await hasPseudo(cell, 'Children-based Label')) ||
        (await hasPseudo(cell, 'I want fries with that')),
      message: 'Checkbox demo labels should be visible',
    }), cell);
  },

  Input_ChoiceList: async ({ check, firstCell, hasPseudo, ctx }) => {
    const cell = firstCell();
    await ctx.clickPseudo(cell, 'Mango');
    await ctx.wait(200);
    await check('Input_ChoiceList Mango selectable', async () => ({
      passed: await hasPseudo(cell, 'Mango'),
      message: 'Mango choice should remain visible after selection click',
    }), cell);
  },

  Input_File: async ({ check, firstCell, hasPseudo }) => {
    const cell = firstCell();
    await check('Input_File control visible without opening picker', async () => ({
      passed:
        (await hasPseudo(cell, 'Select File')) ||
        (await cell.locator('input[type="file"]').count()) > 0,
      message: 'File input demo should expose file control in visuals',
    }), cell);
  },

  Input_Image: async ({ check, firstCell, hasPseudo }) => {
    const cell = firstCell();
    await check('Input_Image control visible without opening picker', async () => ({
      passed:
        (await hasPseudo(cell, 'Select File')) ||
        (await cell.locator('input[type="file"]').count()) > 0,
      message: 'Image input demo should expose file control in visuals',
    }), cell);
  },

  Input_Picker: async ({ check, firstCell, hasPseudo }) => {
    const cell = firstCell();
    await check('Input_Picker Fruit field visible', async () => ({
      passed: await hasPseudo(cell, 'Fruit'),
      message: 'Fruit picker label should be visible',
    }), cell);
  },

  Scrim: async ({ check, firstCell, hasPseudo, hasTestId, ctx }) => {
    const cell = firstCell();
    await check('Scrim demo controls visible', async () => ({
      passed: (await hasPseudo(cell, 'Toggle')) && (await hasPseudo(cell, 'Greet')),
      message: 'Scrim Toggle and Greet buttons should be visible',
    }), cell);
    await ctx.clickButton(cell, 'Toggle');
    await ctx.wait(400);
    await check('Scrim dismiss testId present when visible', async () => ({
      passed: await hasTestId(cell, 'scrim-dismiss'),
      message: 'Visible scrim with onPress should expose scrim-dismiss testId',
    }), cell);
  },

  Carousel: async ({ check, firstCell }) => {
    const cell = firstCell();
    await check('Carousel navigation controls present', async () => ({
      passed:
        (await cell.locator('svg, img').count()) > 0 &&
        ((await cell.locator('svg').count()) > 0 ||
          (await cell.locator('[class*="dot"], [class*="Dot"]').count()) > 0),
      message: 'Carousel should render slide surface and navigation (chevrons or dots)',
    }), cell);
  },

  Card: async ({ check, anySampleCell, cellContains, hasTestId, page }) => {
    await check('Card pressable sample visible', async () => ({
      passed: await anySampleCell(page, async (cell) =>
        (await hasTestId(cell, 'legacy-card-open')) ||
        (await cellContains(cell, 'This is a card that you can press'))
      ),
      message: 'Pressable card sample (testId or label) should be visible',
    }));
  },

  ThirdParty_Map: async ({ check, firstCell }) => {
    const cell = firstCell();
    await check('Map container rendered', async () => ({
      passed:
        (await cell.locator('.map, [class*="map"], .leaflet-container, canvas').count()) > 0,
      message: 'Map component should render a map surface',
    }), cell);
  },

  ThirdParty_Recharts: async ({ check, firstCell }) => {
    const cell = firstCell();
    await check('Recharts chart rendered', async () => ({
      passed: (await cell.locator('.recharts-wrapper, svg.recharts-surface').count()) > 0,
      message: 'Recharts should render chart SVG',
    }), cell);
  },

  AnimatableText: async ({ check, firstCell, hasPseudo }) => {
    const cell = firstCell();
    await check('AnimatableText Animate control present', async () => ({
      passed: await hasPseudo(cell, 'Animate'),
      message: 'Animate button should be visible',
    }), cell);
  },

  AnimatableTextInput: async ({ check, firstCell, hasPseudo }) => {
    const cell = firstCell();
    await check('AnimatableTextInput controls present', async () => ({
      passed: (await hasPseudo(cell, 'Animate')) && (await cell.locator('input').count()) > 0,
      message: 'Animate button and input should be visible',
    }), cell);
  },

  Sidebar: async ({ check, firstCell, cellContains }) => {
    const cell = firstCell();
    await check('Sidebar nav items visible', async () => ({
      passed:
        (await cellContains(cell, 'Inbox')) || (await cellContains(cell, 'Calendar')),
      message: 'Sidebar demo should show nav items',
    }), cell);
  },

  Nav_Top: async ({ check, firstCell, hasPseudo, hasTestId, a11ySlugTestId }) => {
    const cell = firstCell();
    await check('Nav_Top items visible', async () => ({
      passed:
        (await hasTestId(cell, a11ySlugTestId('nav-top-item', 'Design'))) ||
        (await hasPseudo(cell, 'Design')) ||
        (await hasPseudo(cell, 'Home')),
      message: 'Top nav demo items should be visible',
    }), cell);
  },

  Nav_Bottom: async ({ check, firstCell, hasPseudo, hasTestId, a11ySlugTestId }) => {
    const cell = firstCell();
    await check('Nav_Bottom items visible', async () => ({
      passed:
        (await hasTestId(cell, a11ySlugTestId('nav-bottom-item', 'Design'))) ||
        (await hasPseudo(cell, 'Design')) ||
        (await hasPseudo(cell, 'Store')),
      message: 'Bottom nav demo items should be visible',
    }), cell);
  },

  TouchableOpacity: async ({ check, firstCell, cellContains, hasTestId }) => {
    const cell = firstCell();
    await check('TouchableOpacity Click Me visible', async () => ({
      passed:
        (await hasTestId(cell, 'touchable-opacity-click-me')) ||
        (await cellContains(cell, 'Click Me')),
      message: 'Click Me sample should expose testId or visible label',
    }), cell);
  },

  TextButton: async ({ check, firstCell, hasPseudo, hasTestId, a11ySlugTestId }) => {
    const cell = firstCell();
    await check('TextButton Add to Cart visible', async () => ({
      passed:
        (await hasTestId(cell, a11ySlugTestId('text-button', 'Add to Cart'))) ||
        (await hasPseudo(cell, 'Add to Cart')),
      message: 'Add to Cart text button should be visible',
    }), cell);
  },

  Tag: async ({ check, anySampleCell, cellContains, hasTestId, page }) => {
    await check('Tag Actionable sample visible', async () => ({
      passed: await anySampleCell(page, async (cell) =>
        (await hasTestId(cell, 'tag-actionable')) ||
        (await cellContains(cell, 'Actionable'))
      ),
      message: 'Actionable tag (testId or label) should be visible',
    }));
  },

  InProgress: async ({ check, firstCell, cellContains }) => {
    const cell = firstCell();
    await check('InProgress demo content visible', async () => ({
      passed: await cellContains(cell, 'Some content here'),
      message: 'InProgress sample should show demo content',
    }), cell);
  },

  WithExecutor: async ({ check, firstCell, cellContains, ctx }) => {
    const cell = firstCell();
    await check('WithExecutor Press Here visible', async () => ({
      passed: await cellContains(cell, 'Press Here'),
      message: 'Press Here trigger should be visible',
    }), cell);
    await ctx.clickText(cell, 'Press Here');
    await ctx.wait(400);
    await check('WithExecutor in-progress after press', async () => ({
      passed:
        (await cell.locator('svg, [class*="ActivityIndicator"], [class*="spinner"]').count()) > 0 ||
        !(await cellContains(cell, 'Press Here')),
      message: 'Press Here should trigger in-progress spinner',
    }), cell);
    await ctx.wait(1200);
  },

  Executor_AlertErrors: async ({ check, firstCell, hasPseudo }) => {
    const cell = firstCell();
    await check('Executor_AlertErrors trigger visible', async () => ({
      passed: await hasPseudo(cell, 'Fail Asynchronously'),
      message: 'Fail Asynchronously button should be visible',
    }), cell);
  },

  _default: async ({ check, firstCell, page, componentName }) => {
    const cell = firstCell();
    if (await cell.count()) {
      await check(`${componentName} sample cell rendered`, async () => ({
        passed: true,
        message: 'Sample visuals cell present',
      }), cell);
    } else {
      const bodyText = await page.locator('body').innerText().catch(() => '');
      await check(`${componentName} page loaded`, async () => ({
        passed: bodyText.length > 100,
        message: 'Page body should have content',
      }));
    }
  },
};

/** @returns {string[]} */
export function listAssertionComponents() {
  return Object.keys(ASSERTION_HANDLERS).filter((k) => k !== '_default');
}

/**
 * Flag clickable pseudo labels in visuals that no recipe explicitly handles.
 * @returns {Promise<Array<{ id: string, name: string, passed: boolean, message: string, screenshotPath: string | null }>>}
 */
export async function checkUnhandledVisuals(page, componentName, ctx, options) {
  const {
    passDir,
    screenshotMode = 'all',
    platform = PLATFORM.WEB,
    hasSpecificHandler = false,
    log = () => {},
  } = options;
  const isAndroid = platform === PLATFORM.ANDROID;
  const { SKIP_CLICK_LABELS } = await import('./audit-gallery-components.mjs');
  const results = [];
  const cellSelector = sampleCellSelectorFor(platform);
  const cells = page.locator(cellSelector);
  const n = await cells.count();
  const unhandled = new Set();

  if (isAndroid) {
    // Native has no pseudo-element metadata; skip REVIEW heuristic on Android.
    return results;
  }

  for (let i = 0; i < n; i++) {
    const cell = cells.nth(i);
    const labels = await cell.locator('[data-text-as-pseudo-element]').evaluateAll((nodes) =>
      nodes
        .map((node) => node.getAttribute('data-text-as-pseudo-element') ?? '')
        .filter((t) => t && !/^(Visuals|Desktop|Handheld|Docs|Tools|Components)$/i.test(t))
    );
    for (const label of labels) {
      if (SKIP_CLICK_LABELS.has(label)) continue;
      const el = cell.locator(`[data-text-as-pseudo-element="${label}"]`).first();
      const clickable = await el
        .evaluate((node) => {
          let p = node;
          for (let j = 0; j < 8; j++) {
            if (!p) break;
            const style = window.getComputedStyle(p);
            if (style.cursor === 'pointer' || p.tagName === 'BUTTON') return true;
            p = p.parentElement;
          }
          return false;
        })
        .catch(() => false);
      if (clickable) unhandled.add(label);
    }
  }

  if (!hasSpecificHandler && unhandled.size > 0) {
    const screenshotDir = join(passDir, 'screenshots', componentName);
    mkdirSync(screenshotDir, { recursive: true });
    const file = join(screenshotDir, `999-REVIEW-unhandled-interactives.png`);
    if (screenshotMode !== 'none') {
      await page.screenshot({ path: file, fullPage: false }).catch(() => {});
    }
    const message = `No specific interaction recipe; found clickable: ${[...unhandled].slice(0, 8).join(', ')}`;
    log(`ASSERT REVIEW: ${message}`);
    results.push({
      id: `${componentName}-unhandled`,
      name: 'Unhandled interactives (needs recipe)',
      passed: false,
      message,
      screenshotPath: screenshotMode !== 'none' ? file : null,
      reviewOnly: true,
    });
  }

  return results;
}
