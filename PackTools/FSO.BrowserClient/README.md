# FSO.BrowserClient — The Sims Online in a browser tab

Runs the **full SimAntics VM in WebAssembly**, in lockstep with a headless lot host
(`../FSO.LotHostLite`) over FreeSO's sandbox protocol. Two browser tabs are two
players in one live world: same house, same furniture, same simulation, chat and
pie-menu interactions crossing between them.

Not a mockup and not a replay — each tab boots the real content system, runs the
real VM, and stays in sync tick for tick (verified by matching entity hashes).

Sims render as real skinned Vitaboy bodies by default, and the demo house is
furnished with real base-game EA objects (placed by GUID, not just CC0 packs) —
click one for its real pie menu.

## Play

**You need:** .NET 9 SDK, Python 3, Chrome, and a TSO install (the folder that
contains `objectdata/` — the same files desktop FreeSO uses).

```sh
cd <repo>

# 0. dotnet on PATH (macOS installs it outside the default PATH)
export PATH="$PATH:/usr/local/share/dotnet:$HOME/.dotnet"

# 1. point at your TSO install
TSO_DIR=$(dirname "$(find ~ -maxdepth 6 -type d -name objectdata 2>/dev/null | head -1)")
echo "$TSO_DIR"          # sanity-check this looks right

# 2. compile the furniture — once, ~10 min
for j in PackTools/examples/*.json; do
  dotnet run --project PackTools/FSO.PackCompiler -- build "$j" -o ~/packs-out --tso-dir "$TSO_DIR" || true
done

# 3. start everything
TSO_DIR="$TSO_DIR" ./PackTools/tools/run_browser_demo.sh
```

Wait for **`Demo is up`**, then open in Chrome:

```
http://127.0.0.1:5259/?vm=1&name=kat
```

Open it a second time with a different `name=` — that's player two, in the same
house.

### Controls

| | |
|---|---|
| **Click furniture** | its real pie menu — pick an option and your sim performs it |
| **Chat box** (bottom-left) | Enter to send; everyone in the lot sees it |
| **Arrows / WASD** | pan (this also stops the camera following your sim) |
| **1 / 2 / 3** | zoom near / medium / far |
| **Q / E** | rotate the lot |

Your sim is the one with the **yellow arrow** over its head; it walks into the
house by itself when you join, and the camera follows it until you pan away.

## What the script does, and how long it takes

| Phase | First run | Later runs |
|---|---|---|
| preflight | instant | instant |
| content bundle (`tools/make_browser_content.py`) | ~5 min | skipped |
| build lot host + gateway | ~1 min | seconds (incremental) |
| publish the browser app | ~3 min | skipped unless code changed |
| start + readiness checks | ~30 s | ~30 s |

Then each browser tab downloads the ~200 MB bundle once and boots the content
system in-tab (~20 s), which is what the status banner at the top is counting.

The script **owns** ports 5259 / 8087 / 37564: it stops its own leftovers from
previous runs, and refuses to start if something else holds them.

## When something goes wrong

The terminal running the script is **busy** — anything you type into it is
queued, not run. Use a second terminal tab, and start with:

```sh
./PackTools/tools/run_browser_demo.sh --doctor
```

which prints the repo revision, furniture count, bundle, published build, which
ports are listening, and the last line of each service log.

| Symptom | Cause | Fix |
|---|---|---|
| `✗ dotnet not found on PATH` | macOS installs .NET outside the default PATH | run the `export PATH=…` line above |
| `✗ no compiled furniture in …` | step 2 skipped, or it ran without dotnet on PATH | run step 2; expect ~60 `.iff` files |
| `✗ no content bundle … and TSO_DIR is not set` | first run needs the game files | set `TSO_DIR` as in step 1 |
| `FATAL: :8087 is held by something that is not part of this demo` | another program owns the port | quit it, or `lsof -ti :8087 \| xargs kill -9` |
| `FATAL: gateway died` | usually a stale binary or a taken port | the log tail is printed; rerun after freeing ports |
| `FATAL: the game content bundle is not being served` | bundle missing or unreadable | delete `~/browser-content` and rerun with `TSO_DIR` set |
| Page loads but no banner, no `build …` tag | browser cached an old build | hard reload (**Cmd+Shift+R**) |
| Banner sticks on `game server not reachable` | tab opened before the host was ready | it retries by itself; wait for `Demo is up` |
| `content boot failed: …` in the banner | the tab could not fetch/extract the bundle | `--doctor`, then rerun |

Reset everything and start clean:

```sh
rm -rf ~/browser-publish ~/browser-content    # keeps ~/packs-out (the slow part)
```

## Tests

`tests/` runs the game headlessly through Playwright (`npm i playwright`):

```sh
node tests/pie_menu_vm.js       http://127.0.0.1:5259 /tmp/pie      # click → pie menu → interaction runs
node tests/two_tab_vm.js        http://127.0.0.1:5259 /tmp/twotab   # two tabs, chat crosses, no desync
node tests/visual_qa.js         http://127.0.0.1:5259 /tmp/qa       # screenshots through a session
node tests/mixed_mode_vm.js     http://127.0.0.1:5259               # ?vitaboy=1 vs plain tab, entity-hash desync check
node tests/interaction_audit.js http://127.0.0.1:5259 grove         # every furnished object's real pie menu, usable count
node tests/ea_interaction.js    http://127.0.0.1:5259 <x> <y> "<Option Name>"  # queue → route → arrive, not just menu presence
```

`visual_qa.js` exists because console assertions passed while the game was
visibly broken — look at its frames before believing a green test.

## How it fits together

```
browser tab  ──ws──►  FSO.WsGateway  ──tcp──►  FSO.LotHostLite
 (SimAntics VM)        (/sandbox route)         (SimAntics VM, authoritative ticks)
```

- `BrowserContentBoot.cs` — fetches `content.tar.gz`, extracts it into MEMFS (a
  hand-rolled ustar reader; `System.Formats.Tar` is unsupported on wasm), then
  runs the stock `Content.Init` in SERVER mode
- `BrowserSandboxClient.cs` — `ClientWebSocket` speaking the sandbox protocol's
  9-byte framing, with auto-retry until the host is up
- `VmLotClient.cs` — `VMClientDriver` + local `VM`, pie menus, chat, the walk-in,
  and the entity billboards drawn from live VM state
- `tools/make_browser_content.py` — builds the trimmed content bundle (the file
  list is `tools/browser-content-files.txt`, derived from what a real session
  actually opens)

### URL parameters

| | |
|---|---|
| `?vm=1&name=…` | the game: shared VM, pie menus, chat |
| `?vitaboy=0` | capsule sims instead of the default real skinned bodies |
| `?house=grove` | arch-only view, no VM (older path, still works) |
| `?lot=real` | terrain only |
| `?furnish=png\|real` | billboard vs DGRP furniture in the no-VM path |
| `?zoom=near\|medium\|far`, `?rot=0..3` | pin the camera (used by tests) |

## Known gaps

- **Furniture art is billboards.** True DGRP sprites don't rasterise through the KNI
  batch — a real anomaly, documented in `../docs/SESSION-LANES.md`. Billboards are
  fed from live VM positions, so behaviour is correct even though the art is flat.
- **Sleep on EA beds queues, then silently reverts to Idle.** Diagnosed, not fixed:
  `VM.OnDialog` is now wired up (browser + `FSO.LotHostLite`), so a real SimAntics
  exception would be visible — none fires. Neither `VMMemory.GetSlot` returning null
  nor `VMSlotParser` finding zero routing candidates fires either (both are now
  logged). Whatever rejects the interaction is earlier in the bed's own behavior tree
  than any routing/slot primitive; pinning down the exact primitive needs BHAV
  bytecode disassembly. Sitting (chairs) and every other tested interaction route and
  complete normally.
- **No depth against walls** — draw order only.
- **The lot host stands in for the archive server**; no accounts, no city.
