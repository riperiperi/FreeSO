# Browser client acceptance tests

Playwright scripts run against a live demo stack (`PackTools/tools/run_browser_demo.sh`):

- `pie_menu_vm.js` — canvas click on the pet rock → TTAB pie menu → Admire lands in the avatar's queue.
- `two_tab_vm.js` — two tabs share the lockstep VM: both sync, typed chat crosses, zero desyncs.
- `probe.js` — generic console-echo + screenshot probe for any URL.

Needs `playwright` resolvable from Node (`npm i playwright` somewhere on `NODE_PATH`)
and a Chromium (headless containers: launched with `--enable-unsafe-swiftshader`).

```sh
./PackTools/tools/run_browser_demo.sh &
node tests/pie_menu_vm.js http://127.0.0.1:5259 /tmp/pie
node tests/two_tab_vm.js http://127.0.0.1:5259 /tmp/twotab
```
