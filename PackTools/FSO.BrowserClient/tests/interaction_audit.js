// What can the sims actually DO in this house?
//
// Walks every object in the furnish list and prints its real TTAB pie menu, so
// "the game feels thin" becomes a measurable list. Objects that only offer
// permission entries (Set Permissions / Building Tips / View Permissions) are
// counted as unusable — that is the engine's generic menu, not a behaviour.
//
// usage: node interaction_audit.js [baseUrl] [houseName]
const { chromium } = require('playwright');

const PERMISSION_ONLY = /Set Permissions|Building Tips|View Permissions|Roommates Only/;

(async () => {
  const base = process.argv[2] || 'http://127.0.0.1:5259';
  const house = process.argv[3] || 'grove';

  const browser = await chromium.launch({ args: ['--enable-unsafe-swiftshader'] });
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  const logs = [];
  page.on('console', (m) => logs.push(m.text()));
  await page.goto(`${base}/?vm=1&name=audit`, { waitUntil: 'load', timeout: 90000 });

  const t0 = Date.now();
  // Pie menus need our avatar present and unhidden; the walk-in log line is the
  // cheapest signal that the VM is fully live.
  while (Date.now() - t0 < 240000 &&
         !logs.some((l) => l.includes('vm sim is inside the house') || l.includes('walking into the house'))) {
    await page.waitForTimeout(1000);
  }
  await page.waitForTimeout(3000);

  const furnish = await page.evaluate(async (h) => {
    const r = await fetch(`houses/${h}-furnish.json`);
    return r.json();
  }, house);

  const rows = [];
  for (const f of furnish.furniture) {
    const json = await page.evaluate(([x, y]) => window.fsoDebug.pie(x, y), [f.x + 0.5, f.y + 0.5]);
    let options = [];
    try { options = JSON.parse(json).map((o) => o.name); } catch { /* leave empty */ }
    const real = options.filter((n) => !PERMISSION_ONLY.test(n));
    rows.push({ id: f.id, options, real });
  }

  const usable = rows.filter((r) => r.real.length > 0);
  console.log('object'.padEnd(26), 'interactions');
  console.log('-'.repeat(60));
  for (const r of rows) {
    console.log(r.id.padEnd(26), r.real.length ? r.real.join(', ')
      : (r.options.length ? '(permissions only)' : '(none)'));
  }
  console.log('-'.repeat(60));
  console.log(`${usable.length}/${rows.length} objects are usable`);

  require('fs').writeFileSync('interaction-audit.json', JSON.stringify(rows, null, 1));
  await browser.close();
})().catch((e) => { console.error('[audit] FAILED', e); process.exit(1); });
