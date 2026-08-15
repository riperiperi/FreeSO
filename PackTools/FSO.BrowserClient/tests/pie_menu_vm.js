// Pie-menu acceptance in the browser VM: real canvas click on the pet rock →
// TTAB pie menu appears as a DOM overlay → click "Admire" → interaction lands
// in the avatar's queue in the shared VM.
// usage: node pie_menu_vm.js <baseUrl> <outPrefix> [rockX=36.5] [rockY=31.5]
const { chromium } = require('playwright');

(async () => {
  const base = process.argv[2] || 'http://127.0.0.1:5259';
  const out = process.argv[3] || 'shots/pie';
  const rockX = parseFloat(process.argv[4] || '36.5');
  const rockY = parseFloat(process.argv[5] || '31.5');

  const browser = await chromium.launch({ args: ['--enable-unsafe-swiftshader'] });
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  const logs = [];
  page.on('console', (m) => logs.push(m.text()));
  page.on('pageerror', (e) => logs.push('PAGEERROR ' + e.message));
  await page.goto(`${base}/?vm=1&name=pietab`, { waitUntil: 'load', timeout: 90000 });

  const t0 = Date.now();
  const has = (pat) => logs.some((l) => l.includes(pat));
  const waitFor = async (desc, cond, ms = 120000) => {
    while (Date.now() - t0 < ms) {
      if (cond()) { console.log('[ok]', desc, `(${((Date.now() - t0) / 1000).toFixed(0)}s)`); return true; }
      await page.waitForTimeout(500);
    }
    console.log('[FAIL timeout]', desc);
    return false;
  };

  let pass = true;
  pass &= await waitFor('vm ready', () => has('vm ready: SYNCED'));

  // The avatar is transiently Hidden during walk-in — poll the tile-space debug
  // hook until the pie menu is non-empty before attempting the real click path.
  let pie = '[]';
  const pieReady = await (async () => {
    while (Date.now() - t0 < 150000) {
      try {
        pie = await page.evaluate(([x, y]) => window.fsoDebug.pie(x, y), [rockX, rockY]);
        if (pie && pie !== '[]') return true;
      } catch (e) { /* instance not ready yet */ }
      await page.waitForTimeout(1000);
    }
    return false;
  })();
  console.log(pieReady ? '[ok]' : '[FAIL]', 'debug pie:', pie);
  pass &= pieReady;

  // Real path: project the rock to screen, click the canvas, expect the DOM menu.
  const posJson = await page.evaluate(([x, y]) => window.fsoDebug.screenPos(x, y), [rockX, rockY]);
  const pos = JSON.parse(posJson);
  console.log('[info] rock screen pos', posJson);
  await page.mouse.click(pos.x, pos.y);
  pass &= await waitFor('pie menu console line', () => has('pie menu on 0x'));
  const menuVisible = await page.waitForSelector('#fsoPieMenu', { timeout: 10000 }).then(() => true).catch(() => false);
  console.log(menuVisible ? '[ok]' : '[FAIL]', 'DOM pie menu visible');
  pass &= menuVisible;
  await page.screenshot({ path: out + '-menu.png' });

  if (menuVisible) {
    await page.click('#fsoPieMenu div:first-child');
    pass &= await waitFor('interaction sent', () => has('vm sent interaction'));
    pass &= await waitFor('interaction in queue', () => has('vm INTERACTION IN QUEUE'));
  }
  await page.waitForTimeout(3000);
  await page.screenshot({ path: out + '-after.png' });
  require('fs').writeFileSync(out + '-console.log', logs.join('\n'));
  console.log('[done]', pass ? 'PASS' : 'FAIL');
  await browser.close();
  process.exit(pass ? 0 : 1);
})().catch((e) => { console.error('[pie] FAILED', e); process.exit(2); });
