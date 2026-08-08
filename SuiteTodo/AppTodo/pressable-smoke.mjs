// Pressable press-path smoke test (PR #31 review item 3).
//
// LibClient/src/Components/Pressable.fs now fires the press from RN's `onPress` on BOTH web and
// native, replacing the web-only `onPressOut` + 5px movement guard. That is the activation path
// for every pressable in the repo, so it needs more than a compile.
//
// Asserts, against AppTodo's category pill row (a horizontal ScrollView full of pressables, i.e.
// exactly the RW8 defect-3 shape):
//   A  mouse click        -> press FIRES        (ordinary activation still works)
//   B  touch drag-scroll  -> press does NOT fire (the regression the change targets)
//   C  keyboard Enter     -> press FIRES        (RNW routes this through click)
//   D  keyboard Space     -> press FIRES
//   E  touch tap in place -> press FIRES        (scroll-cancel must not eat real taps)
//
// Side-effect probe, not vision. NOTE: the obvious probe (aria-checked, which the new
// radioSelected state is supposed to set) does NOT work here -- LC.TextButton drops
// accessibilityState, so the category pills render role="radio" with no aria-checked at all.
// Only SegmentedControl (the theme toggle) emits it. So selection is read off the visible pill's
// computed border instead: the selected pill gets palette.Accent (#458b8c -> rgb(69,139,140))
// plus an inset shadow, per Styles.categoryPill.
//
// usage: node pressable-smoke.mjs   (needs `../../eggshell dev-web` on :9080)

import { chromium } from 'playwright';

const URL = 'http://localhost:9080';
const results = [];
const record = (name, pass, detail) => {
  results.push({ name, pass, detail });
  console.log(`${pass ? 'PASS' : 'FAIL'}  ${name}${detail ? `  (${detail})` : ''}`);
};

const pillIds = ['todo-new-category-none', 'todo-new-category-work', 'todo-new-category-home'];

const browser = await chromium.launch();
const context = await browser.newContext({
  viewport: { width: 420, height: 900 },
  hasTouch: true,
  deviceScaleFactor: 2,
});
const page = await context.newPage();
await page.goto(URL, { waitUntil: 'domcontentloaded' });
await page.waitForSelector('[data-testid="todo-new-category-none"]', { timeout: 30000 });
await page.waitForTimeout(600);

// Which pills actually exist in this build (labels are i18n-driven).
const present = [];
for (const id of pillIds) {
  if (await page.locator(`[data-testid="${id}"]`).count()) present.push(id);
}
if (present.length < 2) {
  console.error(`Need >=2 category pills, found ${present.length}. Aborting.`);
  await browser.close();
  process.exit(2);
}
console.log(`Using pills: ${present.join(', ')}\n`);

// The testid node is LC.TextButton's absolutely-positioned tap-capture overlay (inset -12px); the
// styled pill is a few levels up. Identify it by its pill radius (999px), then read border width:
// Styles.categoryPill gives the selected pill a 2px accent border and unselected pills 1px.
// Do NOT key on border COLOR: in dark mode the unselected chip border and Accent coincide, and
// computed borderColor is reported even where borderWidth is 0, which makes it useless here.
const selectedId = async () => {
  for (const id of present) {
    const el = page.locator(`[data-testid="${id}"]`).first();
    const isSel = await el.evaluate((n) => {
      let e = n;
      for (let i = 0; i < 6 && e; i++) {
        const cs = getComputedStyle(e);
        if (cs.borderRadius.startsWith('999')) return parseFloat(cs.borderTopWidth) >= 2;
        e = e.parentElement;
      }
      return false;
    });
    if (isSel) return id;
  }
  return null;
};

const box = async (id) => page.locator(`[data-testid="${id}"]`).first().boundingBox();

const cdp = await context.newCDPSession(page);
await cdp.send('Emulation.setTouchEmulationEnabled', { enabled: true, maxTouchPoints: 10 });
// touchEnd must carry an EMPTY touchPoints array (it lists the points STILL down, and after
// lifting the only finger there are none). Passing the lifted point here instead makes CDP
// dispatch a malformed sequence that never completes a tap, so every press silently fails to
// fire -- which would turn assertion B into a false pass.
const touch = (type, x, y) =>
  cdp.send('Input.dispatchTouchEvent', {
    type,
    touchPoints: type === 'touchEnd' ? [] : [{ x, y, radiusX: 10, radiusY: 10, force: 1, id: 0 }],
  });

// --- A: mouse click fires -------------------------------------------------
{
  const before = await selectedId();
  const target = present.find((id) => id !== before) ?? present[1];
  await page.locator(`[data-testid="${target}"]`).first().click();
  await page.waitForTimeout(350);
  const after = await selectedId();
  record('A  mouse click fires the press', after === target, `selected ${before} -> ${after}, wanted ${target}`);
}

// --- B: touch drag-scroll must NOT fire -----------------------------------
{
  const before = await selectedId();
  const target = present.find((id) => id !== before) ?? present[0];
  const b = await box(target);
  const cx = b.x + b.width / 2;
  const cy = b.y + b.height / 2;

  // Touch down on the pill, then drag horizontally along the scroll axis and release well away.
  await touch('touchStart', cx, cy);
  for (let dx = 6; dx <= 150; dx += 6) {
    await touch('touchMove', cx - dx, cy);
    await page.waitForTimeout(8); // real time between moves; the browser needs it
  }
  await touch('touchEnd', cx - 150, cy);
  await page.waitForTimeout(450);

  const after = await selectedId();
  record('B  touch drag-scroll does NOT fire', after === before, `selected stayed ${after} (was ${before}); dragged over ${target}`);
}

// --- C/D: keyboard activation --------------------------------------------
for (const [key, label] of [['Enter', 'C  keyboard Enter fires'], ['Space', 'D  keyboard Space fires']]) {
  const before = await selectedId();
  const target = present.find((id) => id !== before) ?? present[0];
  const el = page.locator(`[data-testid="${target}"]`).first();

  const focused = await el.evaluate((n) => {
    const cand = n.matches('[tabindex],button,[role="radio"]') ? n : n.querySelector('[tabindex],button,[role="radio"]');
    if (!cand) return false;
    cand.focus();
    return document.activeElement === cand || n.contains(document.activeElement);
  });
  if (!focused) {
    record(label, false, 'could not focus the pill (no focusable node found)');
    continue;
  }
  await page.keyboard.press(key);
  await page.waitForTimeout(350);
  const after = await selectedId();
  record(label, after === target, `selected ${before} -> ${after}, wanted ${target}`);
}

// --- E: stationary touch tap still fires ----------------------------------
// Reload first: test B leaves the category ScrollView scrolled and still settling, and a tap
// landing on a moving scroller is legitimately cancelled. This isolates E from that.
{
  await page.reload({ waitUntil: 'domcontentloaded' });
  await page.waitForSelector(`[data-testid="${present[0]}"]`, { timeout: 30000 });
  await page.waitForTimeout(900);

  const before = await selectedId();
  const target = present.find((id) => id !== before) ?? present[0];
  const b = await box(target);
  const cx = b.x + b.width / 2;
  const cy = b.y + b.height / 2;
  await touch('touchStart', cx, cy);
  await page.waitForTimeout(150); // needs a realistic dwell; ~60ms is too short to register
  await touch('touchEnd', cx, cy);
  await page.waitForTimeout(500);
  const after = await selectedId();
  record('E  stationary touch tap fires', after === target, `selected ${before} -> ${after}, wanted ${target}`);
}

const failed = results.filter((r) => !r.pass);
console.log(`\n${results.length - failed.length}/${results.length} passed`);
await browser.close();
process.exit(failed.length ? 1 : 0);
