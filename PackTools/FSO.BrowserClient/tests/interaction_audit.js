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
  // Match by GUID, not proximity: the probe returns the nearest object, and in a
  // furnished room that is regularly a neighbour — which reported objects as
  // having no interactions when the audit was simply reading the wrong one.
  const manifest = await page.evaluate(async () => (await fetch('packs/manifest.json')).json());
  const guidOf = Object.fromEntries(manifest.map((m) => [m.id, (m.guid || '').toUpperCase()]));

  const rows = [];
  for (const f of furnish.furniture) {
    const [json, objJson] = await Promise.all([
      page.evaluate(([x, y]) => window.fsoDebug.pie(x, y), [f.x + 0.5, f.y + 0.5]),
      page.evaluate(([x, y]) => window.fsoDebug.objectAt(x, y), [f.x + 0.5, f.y + 0.5]),
    ]);
    let options = [];
    try { options = JSON.parse(json).map((o) => o.name); } catch { /* leave empty */ }
    let obj = {};
    try { obj = JSON.parse(objJson); } catch { /* leave empty */ }
    const real = options.filter((n) => !PERMISSION_ONLY.test(n));
    const want = guidOf[f.id];
    const here = obj.found && (!want || (obj.guid || '').toUpperCase() === want);
    rows.push({ id: f.id, x: f.x, y: f.y, options, real, present: !!here, hit: obj, want });
  }

  const usable = rows.filter((r) => r.real.length > 0 && r.present);
  const missing = rows.filter((r) => !r.present);
  const silent = rows.filter((r) => r.present && r.real.length === 0);

  console.log('object'.padEnd(26), 'state');
  console.log('-'.repeat(64));
  for (const r of rows) {
    const state = !r.present ? `probe hit ${r.hit.guid || 'nothing'} instead of ${r.want} — inconclusive`
      : r.real.length ? r.real.join(', ')
      : 'no interactions';
    console.log(r.id.padEnd(26), state);
  }
  console.log('-'.repeat(64));
  console.log(`${usable.length}/${rows.length} usable · ${silent.length} placed but inert · ${missing.length} never placed`);

  require('fs').writeFileSync('interaction-audit.json', JSON.stringify(rows, null, 1));
  await browser.close();
})().catch((e) => { console.error('[audit] FAILED', e); process.exit(1); });
