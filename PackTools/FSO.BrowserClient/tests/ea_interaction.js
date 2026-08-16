// Do the real EA interactions actually run, or do they only appear in the menu?
//
// A pie menu entry proves the interaction table loaded. It does not prove the
// behaviour tree runs — that needs the option selected and the sim's queue
// watched. Sleep and Sit both start by routing, so a sim that accepts the
// interaction and then never moves is a routing failure, reported as such.
//
// usage: node ea_interaction.js [baseUrl] [tileX] [tileY] [optionName]
const { chromium } = require('playwright');

(async () => {
  const base = process.argv[2] || 'http://127.0.0.1:5259';
  const tx = parseFloat(process.argv[3] ?? '33.5');
  const ty = parseFloat(process.argv[4] ?? '28.5');
  const want = process.argv[5] || 'Sleep';

  const browser = await chromium.launch({ args: ['--enable-unsafe-swiftshader'] });
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  const logs = [];
  page.on('console', (m) => logs.push(m.text()));
  await page.goto(`${base}/?vm=1&vitaboy=1&name=ea`, { waitUntil: 'load', timeout: 90000 });

  const has = (p) => logs.some((l) => l.includes(p));
  let t0 = Date.now();
  const walkedIn = () => has('vm sim is inside the house') || has('walking into the house');
  while (Date.now() - t0 < 240000 && !walkedIn()) await page.waitForTimeout(2000);
  if (!walkedIn()) { console.log('[FAIL] sim never headed for the house'); process.exit(1); }
  // Let the walk finish: interacting from the lot edge routes across the map and
  // makes a routing failure look like an interaction failure.
  await page.waitForTimeout(15000);

  const pieJson = await page.evaluate(([x, y]) => window.fsoDebug.pie(x, y), [tx, ty]);
  const objJson = await page.evaluate(([x, y]) => window.fsoDebug.objectAt(x, y), [tx, ty]);
  const pie = JSON.parse(pieJson);
  const obj = JSON.parse(objJson);
  console.log(`[info] ${tx},${ty} -> ${obj.name} (${obj.guid}) objectID=${obj.objectID}`);
  console.log(`[info] menu: ${pie.map((p) => p.name).join(', ') || '(empty)'}`);

  const option = pie.find((p) => p.name === want);
  if (!option) { console.log(`[FAIL] "${want}" not offered`); process.exit(1); }

  const before = JSON.parse(await page.evaluate(() => window.fsoDebug.me()));
  await page.evaluate(([c, i]) => window.fsoDebug.interact(c, i), [obj.objectID, option.id]);

  // Accepted = it reaches the queue. Acted = the sim moves or its posture changes.
  let accepted = false, moved = false, after = before;
  t0 = Date.now();
  while (Date.now() - t0 < 60000) {
    await page.waitForTimeout(1500);
    after = JSON.parse(await page.evaluate(() => window.fsoDebug.me()));
    if ((after.queue || '').includes(want)) accepted = true;
    if (after.tileX !== before.tileX || after.tileY !== before.tileY) { moved = true; break; }
  }

  console.log(`[${accepted ? 'ok' : 'FAIL'}] "${want}" ${accepted ? 'entered the queue' : 'never reached the queue'}`);
  console.log(`[${moved ? 'ok' : 'warn'}] sim ${moved
    ? `routed ${before.tileX},${before.tileY} -> ${after.tileX},${after.tileY}`
    : `did not move from ${before.tileX},${before.tileY} (queue: ${after.queue || 'empty'})`}`);

  await page.screenshot({ path: `shots/ea-${want.toLowerCase().replace(/\W+/g, '')}.png` });
  require('fs').writeFileSync('shots/ea-interaction-console.log', logs.join('\n'));
  await browser.close();
  process.exit(accepted && moved ? 0 : 1);
})().catch((e) => { console.error('[ea] FAILED', e); process.exit(1); });
