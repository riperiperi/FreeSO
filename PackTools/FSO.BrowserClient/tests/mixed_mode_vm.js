// Does ?vitaboy=1 desync the shared VM?
//
// A browser with ?vitaboy=1 passes a real GraphicsDevice to Content.Init, which
// changes which providers are built — WorldObjectProvider.Init(withSprites) in
// particular. The headless host has no device. In a lockstep VM any divergence
// in the object set between participants is a desync, and desyncs are silent
// until entity hashes drift, so "it looked fine" proves nothing.
//
// This runs the two modes side by side against the same host and compares their
// entity hashes tick for tick. Tabs that agree on every shared tick are running
// the same simulation; one mismatch is a real desync and blocks defaulting
// Vitaboy on.
//
// usage: node mixed_mode_vm.js [baseUrl] [outPrefix] [waitMs]
const { chromium } = require('playwright');

(async () => {
  const base = process.argv[2] || 'http://127.0.0.1:5259';
  const out = process.argv[3] || 'shots/mixed';
  const waitMs = parseInt(process.argv[4] || '240000', 10);

  const browser = await chromium.launch({ args: ['--enable-unsafe-swiftshader'] });
  const logs = { vita: [], plain: [] };
  const mkPage = async (name, extra) => {
    const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
    page.on('console', (m) => logs[name].push(m.text()));
    page.on('pageerror', (e) => logs[name].push('PAGEERROR ' + e.message));
    await page.goto(`${base}/?vm=1&name=${name}${extra}`, { waitUntil: 'load', timeout: 90000 });
    return page;
  };

  const t0 = Date.now();
  const vita = await mkPage('vita', '&vitaboy=1');
  await vita.waitForTimeout(5000);
  const plain = await mkPage('plain', '');

  const has = (n, p) => logs[n].some((l) => l.includes(p));
  const waitFor = async (desc, cond) => {
    while (Date.now() - t0 < waitMs) {
      if (cond()) { console.log('[ok]', desc, `(${((Date.now() - t0) / 1000).toFixed(0)}s)`); return true; }
      await vita.waitForTimeout(1000);
    }
    console.log('[FAIL timeout]', desc);
    return false;
  };

  let pass = true;
  pass &= await waitFor('vitaboy tab synced', () => has('vita', 'vm ready: SYNCED'));
  pass &= await waitFor('plain tab synced', () => has('plain', 'vm ready: SYNCED'));

  // Both are in the world; let them simulate together long enough that a
  // divergence has somewhere to show up.
  await vita.waitForTimeout(60000);

  // "vm tick=300 synctick=1847 entities=86 hash=-5905952105886420511"
  //
  // Key on synctick, the real server-agreed TickID both clients replay — NOT
  // the local "tick=" call-counter. A client's own tick counter advances once
  // per frame regardless of how many buffered network ticks it actually ran
  // (VMClientDriver.Tick can run 0 to thousands per call while catching up),
  // so two independently-paced tabs printing "tick=300" are very likely
  // describing two different simulated moments. Comparing by that field
  // produced a false "6/6 ticks disagree" the first time this test ran; the
  // hashes were fine, the comparison was invalid.
  const ticks = (name) => {
    const m = new Map();
    for (const l of logs[name]) {
      const g = /vm tick=\d+ synctick=(\d+) entities=(\d+) hash=(-?\d+)/.exec(l);
      if (g) m.set(g[1], { entities: g[2], hash: g[3] });
    }
    return m;
  };
  const a = ticks('vita');
  const b = ticks('plain');
  const shared = [...a.keys()].filter((k) => b.has(k)).sort((x, y) => +x - +y);
  console.log(`[info] vitaboy logged ${a.size} tick samples, plain ${b.size}, ${shared.length} shared`);

  if (shared.length === 0) {
    console.log('[FAIL] no shared tick samples — cannot compare, so nothing is proven');
    pass = false;
  }
  let mismatched = 0;
  for (const t of shared) {
    if (a.get(t).hash !== b.get(t).hash || a.get(t).entities !== b.get(t).entities) {
      mismatched++;
      if (mismatched <= 5) {
        console.log(`[FAIL] tick ${t}: vitaboy entities=${a.get(t).entities} hash=${a.get(t).hash} ` +
          `| plain entities=${b.get(t).entities} hash=${b.get(t).hash}`);
      }
    }
  }
  if (mismatched === 0 && shared.length > 0) {
    console.log(`[ok] all ${shared.length} shared ticks agree ` +
      `(last: tick ${shared[shared.length - 1]}, ${a.get(shared[shared.length - 1]).entities} entities)`);
  } else if (mismatched > 0) {
    console.log(`[FAIL] ${mismatched}/${shared.length} shared ticks disagree`);
    pass = false;
  }

  for (const name of ['vita', 'plain']) {
    const desyncs = logs[name].filter((l) => l.includes('DESYNC')).length;
    const errors = logs[name].filter((l) => l.startsWith('PAGEERROR')).length;
    console.log(`[${name}] desyncs=${desyncs} pageerrors=${errors}`);
    if (desyncs > 0 || errors > 0) pass = false;
  }

  await vita.screenshot({ path: out + '-vita.png' });
  await plain.screenshot({ path: out + '-plain.png' });
  const fs = require('fs');
  fs.writeFileSync(out + '-vita-console.log', logs.vita.join('\n'));
  fs.writeFileSync(out + '-plain-console.log', logs.plain.join('\n'));
  console.log('[done]', pass ? 'PASS' : 'FAIL');
  await browser.close();
  process.exit(pass ? 0 : 1);
})().catch((e) => { console.error('[mixed_mode] FAILED', e); process.exit(2); });
