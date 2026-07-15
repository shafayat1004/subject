import { remote } from 'webdriverio';
import { ANDROID_APP } from './audit-gallery-platform.mjs';

const caps = {
  platformName: 'Android',
  'appium:automationName': 'UiAutomator2',
  'appium:udid': 'bf08f3ed',
  'appium:deviceName': 'bf08f3ed',
  'appium:appPackage': ANDROID_APP.package,
  'appium:appActivity': ANDROID_APP.activity,
  'appium:noReset': true,
  'appium:autoGrantPermissions': true,
  'appium:newCommandTimeout': 240,
  'appium:disableWindowAnimation': true,
  'appium:appWaitDuration': 60000,
  'wdio:enforceWebDriverClassic': true,
};

console.log('Connecting to Appium...');
const driver = await remote({
  hostname: '127.0.0.1',
  port: 4723,
  path: '/',
  capabilities: caps,
  logLevel: 'error',
});
console.log('Session created');

async function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

// Wait for app UI
console.log('Waiting for gallery UI...');
for (let i = 0; i < 30; i++) {
  const menu = await driver.$$('android=new UiSelector().resourceId("eggshell-sidebar-menu")');
  if (menu.length > 0) break;
  await sleep(1000);
}

// Find the menu element and get its bounds
const menuEls = await driver.$$('android=new UiSelector().resourceId("eggshell-sidebar-menu")');
if (!menuEls.length) {
  console.error('Could not find eggshell-sidebar-menu');
  await driver.deleteSession();
  process.exit(1);
}
const menu = menuEls[0];
const loc = await menu.getLocation();
const size = await menu.getSize();
const cx = Math.round(loc.x + size.width / 2);
const cy = Math.round(loc.y + size.height / 2);
console.log(`Menu element bounds: x=${loc.x} y=${loc.y} w=${size.width} h=${size.height}`);
console.log(`Center: (${cx}, ${cy})`);

// --- Attempt 1: W3C Actions API (pointer touch down → pause → up) ---
console.log('\n=== Attempt 1: W3C Actions (pointer touch) ===');
await driver.action('pointer', { type: 'touch', id: 'finger1' })
  .move({ x: cx, y: cy })
  .down()
  .pause(100)
  .up()
  .perform();
console.log('W3C action performed');
await sleep(1000);

// Check if drawer opened
let blade = await driver.$$('android=new UiSelector().resourceId("sidebar-blade-components")');
console.log(`Drawer open after W3C action? ${blade.length > 0}`);
if (blade.length > 0) {
  console.log('SUCCESS: W3C Actions opened the drawer');
  await driver.deleteSession();
  process.exit(0);
}

// --- Attempt 2: el.click() (baseline) ---
console.log('\n=== Attempt 2: el.click() ===');
await menu.click();
await sleep(1000);
blade = await driver.$$('android=new UiSelector().resourceId("sidebar-blade-components")');
console.log(`Drawer open after el.click()? ${blade.length > 0}`);

// --- Attempt 3: adb shell input tap ---
console.log('\n=== Attempt 3: adb shell input tap ===');
const { execSync } = await import('child_process');
execSync(`adb -s bf08f3ed shell input tap ${cx} ${cy}`, { stdio: 'inherit' });
await sleep(1000);
blade = await driver.$$('android=new UiSelector().resourceId("sidebar-blade-components")');
console.log(`Drawer open after adb tap? ${blade.length > 0}`);

// --- Attempt 4: adb shell input swipe (tap via swipe 0 distance) ---
console.log('\n=== Attempt 4: adb input swipe (down→up at same point) ===');
execSync(`adb -s bf08f3ed shell input swipe ${cx} ${cy} ${cx} ${cy} 100`, { stdio: 'inherit' });
await sleep(1000);
blade = await driver.$$('android=new UiSelector().resourceId("sidebar-blade-components")');
console.log(`Drawer open after adb swipe-tap? ${blade.length > 0}`);

// --- Attempt 5: find clickable child and use W3C actions on it ---
console.log('\n=== Attempt 5: clickable child + W3C actions ===');
const clickables = await menu.$$('android=new UiSelector().clickable(true)');
console.log(`Found ${clickables.length} clickable children`);
if (clickables.length > 0) {
  const child = clickables[0];
  const cloc = await child.getLocation();
  const csize = await child.getSize();
  const ccx = Math.round(cloc.x + csize.width / 2);
  const ccy = Math.round(cloc.y + csize.height / 2);
  console.log(`Clickable child center: (${ccx}, ${ccy})`);
  await driver.action('pointer', { type: 'touch', id: 'finger2' })
    .move({ x: ccx, y: ccy })
    .down()
    .pause(100)
    .up()
    .perform();
  await sleep(1000);
  blade = await driver.$$('android=new UiSelector().resourceId("sidebar-blade-components")');
  console.log(`Drawer open after child W3C action? ${blade.length > 0}`);
}

// --- Report ---
console.log('\n=== Summary ===');
blade = await driver.$$('android=new UiSelector().resourceId("sidebar-blade-components")');
if (blade.length > 0) {
  console.log('Drawer IS open — one of the attempts worked');
} else {
  console.log('ALL attempts failed — GestureView swallows all synthetic touches');
  // Dump the UI hierarchy to see what's actually on screen
  const dump = await driver.getPageSource();
  console.log('UI hierarchy (first 2000 chars):');
  console.log(dump.substring(0, 2000));
}

await driver.deleteSession();
