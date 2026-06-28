/**
 * Per-component interaction recipes for the EggShell Gallery audit.
 * Scoped to demo visuals in the content table — derived from gallery .render / .fs sources.
 *
 * ReactXP web renders most labels/buttons as [data-text-as-pseudo-element], not DOM text nodes.
 */

import { archiveVisualsForReview } from './audit-gallery-visual-archive.mjs';
import { runComponentAssertions, checkUnhandledVisuals } from './audit-gallery-assertions.mjs';
import { SKIP_CLICK_LABELS } from './audit-gallery-components.mjs';
import { PLATFORM, sampleCellSelectorFor } from './audit-gallery-platform.mjs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const FIXTURE_SAMPLE = join(dirname(fileURLToPath(import.meta.url)), 'audit-browser/fixtures/sample.txt');

const PAUSE = 350;

export const SAMPLE_CELL_SELECTOR =
  '.aesg-ContentComponent-table td.vertical-align-middle, .aesg-ContentComponent-table td.vertical-align-top';

/** @typedef {import('playwright').Page} Page */
/** @typedef {import('playwright').Locator} Locator */

/**
 * @param {Page} page
 * @param {(msg: string) => void} log
 * @param {{ platform?: 'web' | 'android' }} [options]
 */
export function createInteractionContext(page, log, options = {}) {
  const platform = options.platform ?? PLATFORM.WEB;
  const isAndroid = platform === PLATFORM.ANDROID;
  const cellSelector = sampleCellSelectorFor(platform);
  const wait = (ms = PAUSE) => page.waitForTimeout(ms);

  const visualsCells = () => page.locator(cellSelector);

  async function scrollSampleTable() {
    if (isAndroid && typeof page.scrollSampleTable === 'function') {
      await page.scrollSampleTable();
      await wait(200);
      return;
    }
    await page.evaluate(() => {
      const table = document.querySelector('.aesg-ContentComponent-table');
      if (!table) return;
      let el = table.parentElement;
      while (el) {
        if (el.scrollWidth > el.clientWidth + 4) {
          for (const x of [0, 400, 800, 1200, 1600, 2000, 2400]) {
            el.scrollLeft = x;
          }
          return;
        }
        el = el.parentElement;
      }
      // Also scroll within wide sample cells (horizontal sample groups)
      for (const cell of table.querySelectorAll('td.vertical-align-middle, td.vertical-align-top')) {
        for (const inner of cell.querySelectorAll('*')) {
          if (inner.scrollWidth > inner.clientWidth + 4) {
            for (const x of [0, 300, 600, 900]) inner.scrollLeft = x;
          }
        }
      }
    });
    await wait(200);
  }

  async function forEachVisualCell(fn) {
    await scrollSampleTable();
    const cells = visualsCells();
    const n = await cells.count();
    log(`found ${n} visual sample cell(s)`);
    for (let i = 0; i < n; i++) {
      const cell = cells.nth(i);
      try {
        await cell.scrollIntoViewIfNeeded({ timeout: 8000 });
      } catch {
        /* horizontal scroll may still work */
      }
      await wait(150);
      await fn(cell, i);
    }
  }

  /** Click ReactXP pseudo-element label or fall back to role/text. */
  async function clickPseudo(scope, text, exact = true) {
    if (isAndroid) return clickPressable(scope, text, exact);
    const escaped = text.replace(/"/g, '\\"');
    const pseudo = scope.locator(`[data-text-as-pseudo-element="${escaped}"]`).first();
    if (await pseudo.count()) {
      await pseudo.click({ force: true, timeout: 5000 });
      log(`click pseudo "${text}"`);
      await wait();
      return true;
    }
    const loose = scope.locator(`[data-text-as-pseudo-element="${text}"]`).first();
    if (!exact && (await loose.count())) {
      await loose.click({ force: true, timeout: 5000 });
      log(`click pseudo "${text}"`);
      await wait();
      return true;
    }
    return false;
  }

  async function clickButton(scope, name, opts = {}) {
    if (typeof name === 'string' && (await clickPseudo(scope, name, opts.exact !== false))) {
      return true;
    }
    const btn = scope.getByRole('button', { name, ...opts });
    if (await btn.count()) {
      await btn.first().click({ timeout: 5000 });
      log(`click button "${name}"`);
      await wait();
      return true;
    }
    const textBtn = scope.getByText(String(name), { exact: opts.exact ?? false });
    if (await textBtn.count()) {
      await textBtn.first().click({ force: true, timeout: 5000 });
      log(`click text "${name}"`);
      await wait();
      return true;
    }
    return false;
  }

  /**
   * Click label text backed by LC.TapCapture (absolute RX.Button overlay on web).
   * Plain getByText clicks the visible div and fail when the empty button intercepts.
   */
  async function clickPressable(scope, text, exact = false) {
    if (isAndroid) {
      const label = scope.getByText(text, { exact });
      if (await label.count()) {
        await label.first().click({ force: true, timeout: 5000 });
        log(`click text "${text}"`);
        await wait();
        return true;
      }
      return false;
    }
    if (await clickPseudo(scope, text, exact)) return true;

    const label = scope.getByText(text, { exact });
    if (!(await label.count())) return false;

    const target = label.first();
    const button = target
      .locator('xpath=ancestor::*[.//button[@role="button"]][1]//button[@role="button"]')
      .first();
    if (await button.count()) {
      await button.click({ force: true, timeout: 5000 });
      log(`click pressable "${text}"`);
      await wait();
      return true;
    }

    await target.click({ force: true, timeout: 5000 });
    log(`click text "${text}"`);
    await wait();
    return true;
  }

  async function clickText(scope, text, exact = false) {
    return clickPressable(scope, text, exact);
  }

  async function fillLabel(scope, label, value) {
    if (isAndroid) {
      const field = scope.getByLabel(label);
      if (await field.count()) {
        await field.first().click({ timeout: 3000 }).catch(() => {});
        await field.first().fill(value, { timeout: 5000 });
        log(`fill "${label}" = "${value}"`);
        await wait();
        return true;
      }
      const inputs = scope.locator('input:not([type="file"]):not([type="hidden"]), textarea');
      if (await inputs.count()) {
        await inputs.first().click({ force: true, timeout: 3000 }).catch(() => {});
        await inputs.first().fill(value, { timeout: 5000 });
        log(`fill first input near "${label}" = "${value}"`);
        await wait();
        return true;
      }
      return false;
    }
    const pseudo = scope.locator(`[data-text-as-pseudo-element="${label}"]`).first();
    if (await pseudo.count()) {
      const input = pseudo.locator(
        'xpath=ancestor::*[.//input or .//textarea][1]//input[not(@type="hidden") and not(@type="file")] | ancestor::*[.//input or .//textarea][1]//textarea'
      ).first();
      if (await input.count()) {
        await input.click({ force: true, timeout: 3000 }).catch(() => {});
        await input.fill(value, { timeout: 5000 });
        log(`fill "${label}" = "${value}"`);
        await wait();
        return true;
      }
    }
    const field = scope.getByLabel(label);
    if (await field.count()) {
      await field.first().click({ timeout: 3000 }).catch(() => {});
      await field.first().fill(value, { timeout: 5000 });
      log(`fill "${label}" = "${value}"`);
      await wait();
      return true;
    }
    return false;
  }

  async function fillFirstInput(scope, value) {
    const input = scope.locator('input:not([type="file"]):not([type="hidden"]), textarea').first();
    if (await input.count()) {
      await input.click({ force: true, timeout: 3000 }).catch(() => {});
      await input.fill(value, { timeout: 5000 });
      log(`fill first input = "${value}"`);
      await wait();
      return true;
    }
    return false;
  }

  async function dismissOverlays() {
    await page.keyboard.press('Escape').catch(() => {});
    if (isAndroid) await page.keyboard.press('Escape').catch(() => {});
    await wait(200);
    // Prefer topmost overlay pseudo buttons (dialog/scrim), not code-panel text.
    const overlayRoot = page.locator('[class*="dialog"], [class*="Dialog"], [class*="modal"], [class*="scrim"], [class*="Scrim"]').last();
    const scope = (await overlayRoot.count()) ? overlayRoot : page.locator('body');
    for (const name of ['No', 'Close', 'Cancel', 'OK', 'Yes']) {
      const pseudo = scope.locator(`[data-text-as-pseudo-element="${name}"]`).last();
      if (await pseudo.count()) {
        await pseudo.click({ force: true, timeout: 2000 }).catch(() => {});
        log(`dismiss overlay pseudo: ${name}`);
        await wait(200);
        continue;
      }
      const btn = scope.getByRole('button', { name, exact: true });
      if (await btn.count()) {
        await btn.last().click({ force: true, timeout: 2000 }).catch(() => {});
        log(`dismiss overlay: ${name}`);
        await wait(200);
      }
    }
  }

  async function clickAllActionableButtons(scope, max = 12) {
    if (isAndroid) {
      const buttons = scope.locator('button:not([disabled])');
      const n = Math.min(await buttons.count(), max);
      for (let i = 0; i < n; i++) {
        await buttons.nth(i).click({ timeout: 3000 }).catch(() => {});
        log(`generic clickable[${i}]`);
        await wait(250);
        await dismissOverlays();
      }
      return;
    }
    const pseudoLabels = await scope
      .locator('[data-text-as-pseudo-element]')
      .evaluateAll((nodes) =>
        nodes
          .map((n) => n.getAttribute('data-text-as-pseudo-element') ?? '')
          .filter((t) => t && !/^(Visuals|Desktop|Handheld|Docs|Tools|Components)$/i.test(t))
      );
    const seen = new Set();
    let clicked = 0;
    for (const label of pseudoLabels) {
      if (seen.has(label) || clicked >= max) break;
      seen.add(label);
      if (SKIP_CLICK_LABELS.has(label)) continue;
      const el = scope.locator(`[data-text-as-pseudo-element="${label}"]`).first();
      const clickable = await el.evaluate((node) => {
        let p = node;
        for (let i = 0; i < 8; i++) {
          if (!p) break;
          const style = window.getComputedStyle(p);
          if (style.cursor === 'pointer' || p.tagName === 'BUTTON') return true;
          p = p.parentElement;
        }
        return false;
      }).catch(() => false);
      if (!clickable) continue;
      await el.click({ force: true, timeout: 2000 }).catch(() => {});
      log(`generic pseudo "${label}"`);
      clicked++;
      await wait(250);
      await dismissOverlays();
    }

    const buttons = scope.locator('button:not([disabled])');
    const n = Math.min(await buttons.count(), Math.max(0, max - clicked));
    for (let i = 0; i < n; i++) {
      const btn = buttons.nth(i);
      await btn.click({ timeout: 3000 }).catch(() => {});
      log(`generic native button[${i}]`);
      await wait(250);
      await dismissOverlays();
    }
  }

  async function interactInputsInCell(cell) {
    const inputs = cell.locator('input:not([type="file"]):not([type="hidden"]), textarea');
    const n = await inputs.count();
    for (let i = 0; i < Math.min(n, 6); i++) {
      const input = inputs.nth(i);
      const type = (await input.getAttribute('type')) ?? 'text';
      const placeholder = await input.getAttribute('placeholder');
      let value = '42';
      if (type === 'email') value = 'test@example.com';
      else if (placeholder?.toLowerCase().includes('name')) value = 'Alice';
      else if (type === 'tel') value = '+1 555 0100';
      await input.click({ force: true, timeout: 2000 }).catch(() => {});
      await input.fill(value, { timeout: 3000 }).catch(() => {});
      log(`input[${i}] type=${type} <- "${value}"`);
      await wait(200);
      await page.keyboard.press('Escape').catch(() => {});
    }
  }

  async function dragInCell(cell) {
    const target = cell.locator('img, [class*="ImageCard"], [class*="draggable"]').first();
    if (!(await target.count())) return;
    const box = await target.boundingBox().catch(() => null);
    if (!box) return;
    const cx = box.x + box.width / 2;
    const cy = box.y + box.height / 2;
    if (isAndroid && typeof page.performSwipe === 'function') {
      await page.performSwipe(cx, cy, cx + 50, cy + 30);
    } else {
      await page.mouse.move(cx, cy);
      await page.mouse.down();
      await page.mouse.move(cx + 50, cy + 30, { steps: 10 });
      await page.mouse.up();
    }
    log('drag gesture on image/card');
    await wait();
  }

  return {
    page,
    platform,
    log,
    wait,
    scrollSampleTable,
    forEachVisualCell,
    clickButton,
    clickText,
    clickPseudo,
    clickPressable,
    fillLabel,
    fillFirstInput,
    dismissOverlays,
    clickAllActionableButtons,
    interactInputsInCell,
    dragInCell,
    visualsCells,
  };
}

/** @type {Record<string, (ctx: ReturnType<typeof createInteractionContext>) => Promise<void>>} */
export const COMPONENT_HANDLERS = {
  Index: async (ctx) => {
    /* markdown only */
  },

  Layout_Row: async () => {},
  Layout_Column: async () => {},
  Layout_Sized: async () => {},
  Layout_Constrained: async () => {},

  Buttons: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickButton(cell, 'Home');
      await ctx.clickButton(cell, 'Submit');
    });
  },

  Button: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickButton(cell, 'Submit');
      await ctx.clickButton(cell, 'Cart');
    });
  },

  IconButton: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      const btn = cell.locator('button:visible').first();
      if (await btn.count()) {
        await btn.click({ timeout: 5000 });
        ctx.log('click icon button');
        await ctx.wait();
      }
    });
  },

  FloatingActionButton: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickButton(cell, 'Add Items');
      const btn = cell.locator('button:visible').first();
      if (await btn.count()) {
        await btn.click({ timeout: 5000 });
        ctx.log('click FAB');
        await ctx.wait();
      }
    });
  },

  TextButton: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickButton(cell, 'Add to Cart');
      await ctx.clickButton(cell, 'Special Add to Cart');
    });
  },

  ToggleButtons: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      for (const fruit of ['Mango', 'Peach', 'Banana']) {
        await ctx.clickButton(cell, fruit);
      }
    });
  },

  Forms: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.fillLabel(cell, 'Name', 'Alice');
      await ctx.fillLabel(cell, 'Age', '30');
      await ctx.clickText(cell, 'Subscribe to email');
      await ctx.clickButton(cell, 'Submit');
      await ctx.dismissOverlays();
    });
  },

  Input_Checkbox: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickText(cell, 'Children-based Label');
      await ctx.clickText(cell, 'I want fries with that');
    });
  },

  Input_ChoiceList: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      for (const item of ['Mango', 'Peach', 'Banana']) {
        await ctx.clickText(cell, item);
      }
    });
  },

  Input_Date: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.fillLabel(cell, 'Date', '2025-06-15');
      await ctx.fillFirstInput(cell, '2025-06-15');
      await ctx.page.keyboard.press('Escape');
    });
  },

  Input_DayOfTheWeek: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      for (const day of ['Mon', 'Wed', 'Fri']) {
        await ctx.clickText(cell, day);
      }
    });
  },

  Input_Decimal: async (ctx) => {
    await ctx.forEachVisualCell(async (cell, i) => {
      if (i === 0) await ctx.fillLabel(cell, 'Price', '9.99');
    });
  },

  Input_Duration: async (ctx) => {
    await ctx.forEachVisualCell(async (cell, i) => {
      if (i === 0) {
        await ctx.fillLabel(cell, 'Duration', '2');
        const inputs = cell.getByLabel('Duration').locator('input');
        if (await inputs.count()) {
          await inputs.nth(0).fill('1');
          if ((await inputs.count()) > 1) await inputs.nth(1).fill('30');
        }
      }
    });
  },

  Input_EmailAddress: async (ctx) => {
    await ctx.forEachVisualCell(async (cell, i) => {
      if (i === 0) await ctx.fillLabel(cell, 'Email Address', 'test@example.com');
    });
  },

  Input_LocalTime: async (ctx) => {
    await ctx.forEachVisualCell(async (cell, i) => {
      if (i === 0) {
        const group = cell.getByLabel('Start Time');
        const inputs = group.locator('input');
        if (await inputs.count()) {
          await inputs.nth(0).fill('10');
          if ((await inputs.count()) > 1) await inputs.nth(1).fill('30');
        }
      }
    });
  },

  Input_File: async (ctx) => {
    await ctx.forEachVisualCell(async (cell, i) => {
      if (i > 0) return;
      const input = cell.locator('input[type="file"]');
      if (await input.count()) {
        await input.setInputFiles(FIXTURE_SAMPLE).catch(() => {});
        ctx.log('setInputFiles on file input (no OS picker, first sample only)');
      } else {
        ctx.log('skip Select File (no file input in DOM)');
      }
    });
  },

  Input_Image: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      const input = cell.locator('input[type="file"]');
      if (await input.count()) {
        await input.setInputFiles(FIXTURE_SAMPLE).catch(() => {});
        ctx.log('setInputFiles on image input (no OS picker)');
      } else {
        ctx.log('skip Select File on image input');
      }
    });
  },

  Input_Picker: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickText(cell, 'Fruit', true);
      await ctx.fillLabel(cell, 'Fruit', 'App');
      await ctx.page.keyboard.press('ArrowDown').catch(() => {});
      await ctx.page.keyboard.press('Enter').catch(() => {});
      await ctx.fillLabel(cell, 'Many Choices', 'a');
      await ctx.wait(500);
      await ctx.page.keyboard.press('Escape');
    });
  },

  Input_PhoneNumber: async (ctx) => {
    await ctx.forEachVisualCell(async (cell, i) => {
      if (i === 0) await ctx.fillLabel(cell, 'Home Number', '+1 555 0100');
    });
  },

  Input_PositiveInteger: async (ctx) => {
    await ctx.forEachVisualCell(async (cell, i) => {
      if (i === 0) await ctx.fillLabel(cell, 'Price', '42');
    });
  },

  Input_PositiveDecimal: async (ctx) => {
    await ctx.forEachVisualCell(async (cell, i) => {
      if (i === 0) await ctx.fillLabel(cell, 'Price', '3.14');
    });
  },

  Input_Quantity: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      const buttons = cell.locator('button:visible');
      const n = await buttons.count();
      if (n >= 2) {
        await buttons.first().click().catch(() => {});
        await ctx.wait();
        await buttons.last().click().catch(() => {});
      }
    });
  },

  Input_Text: async (ctx) => {
    await ctx.forEachVisualCell(async (cell, i) => {
      if (i > 0) return;
      await ctx.fillLabel(cell, 'Name', 'Alice');
      await ctx.fillLabel(cell, 'Fruit', 'Mango');
      await ctx.fillLabel(cell, 'Title', 'Hello');
      await ctx.fillLabel(cell, 'Price', '9.99');
      await ctx.fillLabel(cell, 'Portion Size', '4');
    });
  },

  Input_UnsignedInteger: async (ctx) => {
    await ctx.forEachVisualCell(async (cell, i) => {
      if (i === 0) await ctx.fillLabel(cell, 'Quantity', '100');
    });
  },

  Input_UnsignedDecimal: async (ctx) => {
    await ctx.forEachVisualCell(async (cell, i) => {
      if (i === 0) await ctx.fillLabel(cell, 'Price', '2.50');
    });
  },

  Card: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickText(cell, 'This is a card that you can press');
      await ctx.dismissOverlays();
    });
  },

  Carousel: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      const buttons = cell.locator('button:visible');
      const n = await buttons.count();
      if (n >= 2) {
        await buttons.last().click();
        await ctx.wait();
        await buttons.first().click();
      }
      await ctx.page.keyboard.press('ArrowRight').catch(() => {});
    });
  },

  Dialogs: async (ctx) => {
    for (const label of ['Alert', 'Confirm', 'Custom Confirm', 'Image Viewer']) {
      await ctx.forEachVisualCell(async (cell) => {
        if (await ctx.clickButton(cell, label)) {
          await ctx.wait(700);
          await ctx.dismissOverlays();
        }
      });
    }
  },

  Draggable: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.dragInCell(cell);
      for (const label of ['Move Left', 'Reset', 'Move Right']) {
        await ctx.clickButton(cell, label);
      }
    });
  },

  ImageCard: async () => {},
  InfoMessage: async () => {},
  Section_Padded: async () => {},

  Tabs: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      for (const tab of ['Home', 'Profile', 'Contact']) {
        await ctx.clickText(cell, tab);
      }
    });
  },

  AnimatableImage: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickButton(cell, 'Animate');
      await ctx.wait(1200);
      await ctx.clickButton(cell, 'Animate');
    });
  },

  AnimatableText: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickButton(cell, 'Animate');
      await ctx.wait(1200);
    });
  },

  AnimatableTextInput: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.fillFirstInput(cell, 'Bob');
      await ctx.clickButton(cell, 'Animate');
      await ctx.wait(1200);
    });
  },

  AnimatableView: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      const animateBtns = cell.getByRole('button', { name: 'Animate' });
      const n = await animateBtns.count();
      for (let i = 0; i < n; i++) {
        await animateBtns.nth(i).click().catch(() => {});
        await ctx.wait(800);
      }
    });
  },

  Grid: async (ctx) => {
    await ctx.forEachVisualCell(async (cell, i) => {
      if (i === 0) {
        const pageBtn = cell.getByRole('button', { name: '2' });
        if (await pageBtn.count()) await pageBtn.first().click();
        await ctx.wait();
        const next = cell.locator('button:visible').last();
        if (await next.count()) await next.click();
        await ctx.wait();
        await ctx.fillFirstInput(cell, '1');
        await ctx.clickButton(cell, 'Go');
      }
    });
  },

  QueryGrid: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.fillLabel(cell, 'Substring', 'acc');
      await ctx.fillLabel(cell, 'MinLength', '5');
      await ctx.clickButton(cell, 'Submit');
      await ctx.wait(2500);
      const pagBtn = cell.locator('button:visible').last();
      if (await pagBtn.count()) await pagBtn.click().catch(() => {});
    });
  },

  Heading: async () => {},
  Pre: async () => {},
  Tag: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      if (await ctx.clickPressable(cell, 'Actionable')) {
        await ctx.dismissOverlays();
      }
    });
  },
  TimeSpan: async () => {},
  Timestamp: async () => {},
  Avatar: async () => {},
  Icon: async () => {},
  IconWithBadge: async () => {},
  Thumb: async () => {},

  Thumbs: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      const imgs = cell.locator('img:visible');
      const n = Math.min(await imgs.count(), 3);
      for (let i = 0; i < n; i++) {
        await imgs.nth(i).click().catch(() => {});
        await ctx.wait();
        await ctx.dismissOverlays();
      }
    });
  },

  Scrim: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickButton(cell, 'Toggle');
      await ctx.clickButton(cell, 'Greet');
      await ctx.clickButton(cell, 'Toggle');
    });
  },

  Stars: async () => {},

  ContextMenu: async (ctx) => {
    for (const openLabel of ['Handheld Context Menu', 'Desktop Context Menu']) {
      await ctx.forEachVisualCell(async (cell) => {
        if (await ctx.clickButton(cell, openLabel)) {
          await ctx.wait(400);
          for (const item of [
            'Continue shopping',
            'Continue shopping, please',
            'Buy more dammit!',
            'Save Cart',
            'Checkout',
            'Empty Cart',
          ]) {
            const menuItem = ctx.page.getByText(item, { exact: false });
            if (await menuItem.count()) {
              await menuItem.first().click({ timeout: 2000 }).catch(() => {});
              logMenu(ctx, item);
              break;
            }
          }
          await ctx.dismissOverlays();
        }
      });
    }
  },

  Sidebar: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      for (const item of ['Inbox', 'Calendar', 'Notifications', 'Log Out']) {
        await ctx.clickText(cell, item);
      }
    });
  },

  Nav_Top: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      for (const item of ['Design', 'Develop', 'Cart', 'Home']) {
        await ctx.clickText(cell, item);
      }
    });
  },

  Nav_Bottom: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      for (const item of ['Design', 'Develop', 'Store']) {
        await ctx.clickText(cell, item);
      }
      await ctx.clickButton(cell, 'Cart');
    });
  },

  ErrorBoundary: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      const bombs = cell.getByRole('button', { name: 'The Bomb' });
      const n = await bombs.count();
      for (let i = 0; i < n; i++) {
        await bombs.nth(i).click().catch(() => {});
        await ctx.wait(500);
        await ctx.clickButton(ctx.page.locator('body'), 'Reset');
        await ctx.dismissOverlays();
      }
    });
  },

  Executor_AlertErrors: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickButton(cell, 'Fail Asynchronously');
      await ctx.wait(1500);
      await ctx.dismissOverlays();
    });
  },

  AsyncData: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickButton(cell, 'Trigger AsyncData failure');
      await ctx.wait(600);
    });
  },

  WithContext: async () => {},
  TriStateful: async () => {},
  QuadStateful: async () => {},
  Responsive: async () => {},
  InProgress: async () => {},

  WithExecutor: async () => {
    /* click + in-progress assert live in audit-gallery-assertions.mjs (before generic sweep) */
  },

  WithDataFlowControl: async (ctx) => {
    await ctx.wait(1000);
    await ctx.clickButton(ctx.page.locator('body'), 'Confirm');
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickButton(cell, 'Tap to resolve');
    });
  },

  ThirdParty_Map: async (ctx) => {
    await ctx.wait(2000);
    await ctx.forEachVisualCell(async (cell) => {
      const map = cell.locator('.map, [class*="map"]').first();
      if (await map.count()) {
        const box = await map.boundingBox().catch(() => null);
        if (box) {
          await ctx.page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
          await ctx.page.mouse.down();
          await ctx.page.mouse.move(box.x + box.width / 2 - 60, box.y + box.height / 2 - 40, {
            steps: 8,
          });
          await ctx.page.mouse.up();
          ctx.log('map pan gesture');
        }
        await map.click({ timeout: 3000 }).catch(() => {});
      }
    });
  },

  ThirdParty_Recharts: async (ctx) => {
    await ctx.wait(1500);
    await ctx.forEachVisualCell(async (cell) => {
      const chart = cell.locator('.recharts-wrapper, svg.recharts-surface').first();
      if (await chart.count()) {
        await chart.hover({ timeout: 3000 }).catch(() => {});
        ctx.log('chart hover');
        await ctx.wait(500);
      }
    });
  },

  DateSelector: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      const trigger = cell.locator('button:visible, input:visible').first();
      if (await trigger.count()) {
        await trigger.click();
        await ctx.wait(500);
        await ctx.page.keyboard.press('Escape');
      }
    });
  },

  TouchableOpacity: async (ctx) => {
    await ctx.forEachVisualCell(async (cell) => {
      await ctx.clickText(cell, 'Click Me');
    });
  },

  _default: async () => {
    /* generic sweep runs after handler */
  },
};

function logMenu(ctx, item) {
  ctx.log(`context menu item "${item}"`);
}

/**
 * Run all interactions + post-interaction assertions for a gallery component page.
 * @param {import('playwright').Page} page
 * @param {string} componentName
 * @param {(msg: string) => void} logFn
 * @param {{ passDir?: string, screenshotMode?: 'all' | 'failures' | 'none', visualArchive?: boolean, passIndex?: number, baseUrl?: string, url?: string, platform?: 'web' | 'android' }} [options]
 * @returns {Promise<{ actionCount: number, assertions: Array<{ id: string, name: string, passed: boolean, message: string, screenshotPath: string | null }>, visualArchive: object | null }>}
 */
export async function interactWithComponent(page, componentName, logFn, options = {}) {
  let actionCount = 0;
  const log = (msg) => {
    actionCount++;
    logFn(msg);
  };

  const platform = options.platform ?? PLATFORM.WEB;
  const ctx = createInteractionContext(page, log, { platform });
  await ctx.scrollSampleTable();

  const handler = COMPONENT_HANDLERS[componentName] ?? COMPONENT_HANDLERS._default;
  await handler(ctx);

  let assertions = [];
  if (options.passDir) {
    assertions = await runComponentAssertions(page, componentName, ctx, {
      passDir: options.passDir,
      screenshotMode: options.screenshotMode ?? 'all',
      platform,
      log: (msg) => logFn(msg),
    });
    const unhandled = await checkUnhandledVisuals(page, componentName, ctx, {
      passDir: options.passDir,
      screenshotMode: options.screenshotMode ?? 'all',
      platform,
      hasSpecificHandler: Object.prototype.hasOwnProperty.call(COMPONENT_HANDLERS, componentName),
      log: (msg) => logFn(msg),
    });
    assertions = assertions.concat(unhandled);
  }

  // Generic sweep runs after targeted assertions so UI state checks stay meaningful.
  await ctx.forEachVisualCell(async (cell, index) => {
    await ctx.interactInputsInCell(cell);
    if (!['Dialogs', 'ErrorBoundary', 'Executor_AlertErrors', 'ThirdParty_Map'].includes(componentName)) {
      await ctx.clickAllActionableButtons(cell, 6);
    }
    log(`completed sample column ${index + 1}`);
  });

  let visualArchive = null;
  if (options.passDir && options.visualArchive !== false) {
    visualArchive = await archiveVisualsForReview(page, componentName, {
      passDir: options.passDir,
      passIndex: options.passIndex ?? 1,
      baseUrl: options.baseUrl ?? '',
      url: options.url ?? '',
      platform,
      log: (msg) => logFn(msg),
    });
  }

  return { actionCount, assertions, visualArchive };
}