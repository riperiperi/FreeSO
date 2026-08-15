// Headless probe: load a URL, echo browser console, screenshot after a delay.
// usage: node probe.js <url> <out.png> [waitMs=30000]
const { chromium } = require('playwright');

(async () => {
  const url = process.argv[2];
  const out = process.argv[3];
  const waitMs = parseInt(process.argv[4] || '30000', 10);

  // Headless container has no GPU: opt in to SwiftShader software WebGL2.
  const browser = await chromium.launch({ args: ['--enable-unsafe-swiftshader'] });
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  page.on('console', (m) => console.log('[console]', m.text()));
  page.on('pageerror', (e) => console.log('[pageerror]', e.message));
  page.on('requestfailed', (r) => console.log('[reqfail]', r.url(), r.failure()?.errorText));

  await page.goto(url, { waitUntil: 'load', timeout: 90000 });
  await page.waitForTimeout(waitMs);
  await page.screenshot({ path: out });
  console.log('[probe] screenshot ->', out);
  await browser.close();
})().catch((e) => { console.error('[probe] FAILED', e); process.exit(1); });
