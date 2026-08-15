// Visual QA: play the game like a person and capture what they'd see.
// Console-line assertions miss what actually matters — a sim stuck at the lot
// edge, mis-tinted furniture, a menu off-screen. This takes timed screenshots
// through a session so the frames can be examined.
// usage: node visual_qa.js <baseUrl> <outPrefix>
const { chromium } = require('playwright');

(async () => {
  const base = process.argv[2] || 'http://127.0.0.1:5259';
  const out = process.argv[3] || 'shots/qa';

  const browser = await chromium.launch({ args: ['--enable-unsafe-swiftshader'] });
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  const logs = [];
  page.on('console', (m) => logs.push(m.text()));
  await page.goto(`${base}/?vm=1&name=qa`, { waitUntil: 'load', timeout: 90000 });

  const has = (pat) => logs.some((l) => l.includes(pat));
  const t0 = Date.now();
  while (Date.now() - t0 < 150000 && !has('vm ready')) await page.waitForTimeout(1000);
  console.log(has('vm ready') ? '[ok] vm ready' : '[FAIL] never became ready');

  // Frames: arrival, mid-walk, settled — a sim that never leaves the lot edge
  // and furniture that renders wrong are both visible across these.
  for (const [label, wait] of [['arrive', 3000], ['walk', 12000], ['settled', 20000]]) {
    await page.waitForTimeout(wait);
    await page.screenshot({ path: `${out}-${label}.png` });
    console.log(`[shot] ${out}-${label}.png`);
  }

  // Zoom in (key 1 = near) for a close look at furniture and the sim.
  await page.keyboard.press('Digit1');
  await page.waitForTimeout(4000);
  await page.screenshot({ path: `${out}-near.png` });
  console.log(`[shot] ${out}-near.png`);

  const walked = logs.find((l) => l.includes('walking into the house'));
  console.log(walked ? '[ok] ' + walked : '[warn] no walk-in command sent');
  require('fs').writeFileSync(out + '-console.log', logs.join('\n'));
  await browser.close();
})().catch((e) => { console.error('[qa] FAILED', e); process.exit(1); });
