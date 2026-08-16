// Vitaboy QA: does a real Sim body actually render?
//
// The only acceptable proof is a frame with a recognisable human in it, so this
// takes shots at the same beats as visual_qa.js but with ?vitaboy=1, and prints
// the layer's own status line so a blank frame can be told apart from a frame
// where the layer never ran.
// usage: node vitaboy_qa.js <baseUrl> <outPrefix>
const { chromium } = require('playwright');

(async () => {
  const base = process.argv[2] || 'http://127.0.0.1:5259';
  const out = process.argv[3] || 'shots/vitaboy';

  const browser = await chromium.launch({ args: ['--enable-unsafe-swiftshader'] });
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  const logs = [];
  page.on('console', (m) => logs.push(m.text()));
  page.on('pageerror', (e) => logs.push('[pageerror] ' + e.message));
  await page.goto(`${base}/?vm=1&vitaboy=1&name=vita`, { waitUntil: 'load', timeout: 90000 });

  const has = (pat) => logs.some((l) => l.includes(pat));
  const t0 = Date.now();
  while (Date.now() - t0 < 240000 && !has('vm ready')) await page.waitForTimeout(2000);
  console.log(has('vm ready') ? '[ok] vm ready' : '[FAIL] never became ready');

  // Wait for the sim to be inside the house — a body at the lot edge can sit
  // outside the viewport, which reads identically to "no body drawn".
  const t1 = Date.now();
  while (Date.now() - t1 < 120000 && !has('vm sim is inside the house')) await page.waitForTimeout(2000);

  for (const [label, wait] of [['settled', 3000], ['near', 0]]) {
    if (label === 'near') await page.keyboard.press('Digit1');
    await page.waitForTimeout(wait || 4000);
    await page.screenshot({ path: `${out}-${label}.png` });
    console.log(`[shot] ${out}-${label}.png`);
  }

  for (const pat of ['vitaboy:', 'Content.Init done', 'content boot']) {
    for (const l of logs.filter((l) => l.includes(pat))) console.log('   ' + l);
  }
  require('fs').writeFileSync(out + '-console.log', logs.join('\n'));
  await browser.close();
})().catch((e) => { console.error('[vitaboy] FAILED', e); process.exit(1); });
