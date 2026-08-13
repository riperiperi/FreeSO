// Two-tab shared-VM acceptance: both tabs join LotHostLite through the gateway,
// both must sync, each must see the other's auto-chat, and neither may log a
// DESYNC. Screenshots of both at the end.
// usage: node two_tab_vm.js <baseUrl> <outPrefix> [waitMs=180000]
const { chromium } = require('playwright');

(async () => {
  const base = process.argv[2] || 'http://127.0.0.1:5259';
  const out = process.argv[3] || 'shots/twotab';
  const waitMs = parseInt(process.argv[4] || '180000', 10);

  const browser = await chromium.launch({ args: ['--enable-unsafe-swiftshader'] });
  const logs = { tab1: [], tab2: [] };
  const mkPage = async (name) => {
    const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
    page.on('console', (m) => logs[name].push(m.text()));
    page.on('pageerror', (e) => logs[name].push('PAGEERROR ' + e.message));
    await page.goto(`${base}/?vm=1&name=${name}`, { waitUntil: 'load', timeout: 90000 });
    return page;
  };

  const t0 = Date.now();
  const tab1 = await mkPage('tab1');
  // stagger so the host sees two separate joins
  await tab1.waitForTimeout(5000);
  const tab2 = await mkPage('tab2');

  const has = (name, pat) => logs[name].some((l) => l.includes(pat));
  const waitFor = async (desc, cond) => {
    while (Date.now() - t0 < waitMs) {
      if (cond()) { console.log('[ok]', desc, `(${((Date.now() - t0) / 1000).toFixed(0)}s)`); return true; }
      await tab1.waitForTimeout(1000);
    }
    console.log('[FAIL timeout]', desc);
    return false;
  };

  let pass = true;
  pass &= await waitFor('tab1 vm ready', () => has('tab1', 'vm ready: SYNCED'));
  pass &= await waitFor('tab2 vm ready', () => has('tab2', 'vm ready: SYNCED'));

  // Chat through the real overlay input, only after BOTH are in the world —
  // the auto-chat can fire before the other tab joins (its tick predates the
  // second join's state sync, so only a lucky tick-buffer replay carries it).
  await tab1.waitForSelector('#fsoChatInput', { timeout: 30000 });
  await tab2.waitForSelector('#fsoChatInput', { timeout: 30000 });
  await tab1.fill('#fsoChatInput', 'ui chat from tab1');
  await tab1.press('#fsoChatInput', 'Enter');
  await tab2.fill('#fsoChatInput', 'ui chat from tab2');
  await tab2.press('#fsoChatInput', 'Enter');
  pass &= await waitFor('tab2 sees tab1 chat', () => has('tab2', 'tab1 | ui chat from tab1'));
  pass &= await waitFor('tab1 sees tab2 chat', () => has('tab1', 'tab2 | ui chat from tab2'));

  // let them cohabit a while, then check for desyncs
  await tab1.waitForTimeout(20000);
  for (const name of ['tab1', 'tab2']) {
    const desyncs = logs[name].filter((l) => l.includes('DESYNC')).length;
    const errors = logs[name].filter((l) => l.startsWith('PAGEERROR')).length;
    console.log(`[${name}] desyncs=${desyncs} pageerrors=${errors}`);
    if (desyncs > 0 || errors > 0) pass = false;
    const hashes = logs[name].filter((l) => l.includes('vm tick='));
    console.log(`[${name}] last ticks:`, hashes.slice(-2).join(' | '));
  }

  await tab1.screenshot({ path: out + '-tab1.png' });
  await tab2.screenshot({ path: out + '-tab2.png' });
  require('fs').writeFileSync(out + '-tab1-console.log', logs.tab1.join('\n'));
  require('fs').writeFileSync(out + '-tab2-console.log', logs.tab2.join('\n'));
  console.log('[done]', pass ? 'PASS' : 'FAIL');
  await browser.close();
  process.exit(pass ? 0 : 1);
})().catch((e) => { console.error('[two_tab] FAILED', e); process.exit(2); });
