/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

// freeso-sidecar bridges the headless FSO C# bot (FSO.Bot.Headless) to a
// private campfire.
//
//   - Launches the bot as a child process, capturing its NDJSON stdout stream.
//   - Creates (or resumes) an invite-only campfire; prints a paste-ready
//     admission block to stdout so an operator can admit an agent identity.
//   - Publishes one convention.Declaration per conventions/*.json at startup.
//     Handlers are NOT registered in this scaffold — each verb-family child
//     (d87-d-*) wires its own handlers later. Declarations land now so agents
//     can discover the verb surface immediately.
//   - Bridges every perception/dialog/system event from the bot's stdout to
//     the campfire, tagged freeso:<kind> + sim:<avatar_id>.
//
// Environment (reused from FSO.Bot.Headless):
//
//	FSO_USER, FSO_PASS, FSO_SHARD, FSO_LOT_LOCATION, FSO_API_URL, FSO_VERSION,
//	FSO_HOLD_SECS, FSO_GAME_LOCATION, FSO_VM_TICK_HZ, FSO_PERCEPTION_HZ
//
// Credentials are forwarded via exec.Cmd.Env — never via CLI args, never via
// shell interpolation, so `ps auxe` on the bot child shows no FSO_PASS on the
// command line. See process_test.go.
//
// Flags:
//
//	--bot            path to the FSO.Bot.Headless executable (required unless
//	                 FREESO_BOT_CMD is set)
//	--bot-args       extra args for the bot (default --emit-perception)
//	--cf-home        campfire identity + store dir (default ./bot-data)
//	--conventions    conventions dir (default conventions/ embedded)
//	--campfire-id    reuse an existing campfire instead of creating one
//	                 (overrides FREESO_CF_CAMPFIRE)
//	--no-bot         skip launching the bot (campfire-only mode, for testing)
package main

import (
	"context"
	"embed"
	"flag"
	"fmt"
	"log"
	"os"
	"os/signal"
	"path/filepath"
	"syscall"
	"time"
)

//go:embed conventions/*.json
var conventionFiles embed.FS

func main() {
	botPath := flag.String("bot", "", "path to the FSO.Bot.Headless executable (or set FREESO_BOT_CMD)")
	botArgs := flag.String("bot-args", "--emit-perception", "extra args for the bot (space-separated)")
	cfHome := flag.String("cf-home", "./bot-data", "campfire identity + store dir")
	campfireID := flag.String("campfire-id", "", "reuse an existing campfire id (overrides FREESO_CF_CAMPFIRE)")
	noBot := flag.Bool("no-bot", false, "skip launching the bot (campfire-only mode for testing)")
	description := flag.String("description", "freeso.lot", "campfire description")
	flag.Parse()

	log.SetOutput(os.Stderr)
	log.SetFlags(log.Ltime | log.Lmicroseconds)
	log.SetPrefix("[sidecar] ")

	// Resolve campfire id source: flag beats env.
	if *campfireID == "" {
		*campfireID = os.Getenv("FREESO_CF_CAMPFIRE")
	}

	// Normalise cf-home to absolute so relative paths survive a cwd change.
	absHome, err := filepath.Abs(*cfHome)
	if err != nil {
		log.Fatalf("resolve cf-home: %v", err)
	}
	if err := os.MkdirAll(absHome, 0o700); err != nil {
		log.Fatalf("mkdir cf-home %s: %v", absHome, err)
	}

	// Resolve bot exec: --bot flag, then FREESO_BOT_CMD env.
	botExec := *botPath
	if botExec == "" {
		botExec = os.Getenv("FREESO_BOT_CMD")
	}
	if botExec == "" && !*noBot {
		log.Fatalf("no bot executable: set --bot or FREESO_BOT_CMD (or pass --no-bot for campfire-only mode)")
	}

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	// 1. Bring up the campfire: identity, create/resume, declarations, admission block.
	cf, err := StartCampfire(ctx, CampfireConfig{
		Home:        absHome,
		CampfireID:  *campfireID,
		Description: *description,
		Declarations: conventionFiles,
	})
	if err != nil {
		log.Fatalf("campfire bringup: %v", err)
	}
	defer cf.Close()

	// 2. Launch bot (unless --no-bot).
	var proc *BotProcess
	// ipc is declared at the outer scope so the supervisor loop can call
	// ipc.UpdateBot when relaunching. Nil in --no-bot mode.
	var ipc *IPC
	// botCmds is the bot-cmd envelope pump (freesoexperiment-0de): probe-lot and
	// bot-exit-request over the same stdin/stdout channel, but a different wire
	// shape. Declared at outer scope so UpdateBot is available in the supervisor loop.
	var botCmds *BotCmdPump
	// augmentor enriches perception ticks and caches mayor status for civic handlers
	// (freesoexperiment-ea0). Declared at outer scope so the supervisor loop can
	// pass it to new Bridges on bot relaunch. Nil in --no-bot mode.
	var augmentor *PerceptionAugmentor
	if !*noBot {
		proc, err = LaunchBot(ctx, BotConfig{
			Exec: botExec,
			Args: splitArgs(*botArgs),
			// Inherits parent env including FSO_*. Credentials are environmental
			// only — never placed on the CLI. FSO_HOME_LOT_LOCATION is injected
			// from owned-lots.json so go-home reflects the post-purchase home.
			Env: injectHomeLotEnv(os.Environ()),
		})
		if err != nil {
			log.Fatalf("bot launch: %v", err)
		}
		log.Printf("bot launched pid=%d", proc.Pid())

		// 3. IPC command channel (freesoexperiment-b9c): correlates stdin commands
		// sidecar→bot with response frames observed on bot stdout. Must be wired
		// before bridges start so response frames are routed instead of dropped.
		ipc = NewIPC(proc)

		// 3b. BotCmdPump (freesoexperiment-0de): city-socket ops over the bot-cmd
		// envelope. Shares the same stdin/stdout pipes but uses a different JSON
		// shape (kind=bot-cmd / kind=bot-cmd-reply) so the two channels don't collide.
		botCmds = NewBotCmdPump(proc)

		// 4. Start bridges with BotCmdPump wired so bot-cmd-reply frames are routed.
		// The augmentor is created once here and shared with convention handlers
		// that need mayor status (freesoexperiment-ea0).
		augmentor = NewPerceptionAugmentor()
		bridges := NewBridgesWithBotCmd(cf, proc, ipc, botCmds, augmentor)
		go bridges.Run(ctx)

		// 5. Register convention handlers for verb-family ops. Each op opens one
		// convention.Server on the campfire; Serve blocks in a goroutine until ctx
		// is cancelled.
		servers, err := RegisterMovementHandlers(ctx, cf, ipc)
		if err != nil {
			log.Fatalf("register movement handlers: %v", err)
		}
		log.Printf("convention handlers: %d movement-family ops serving", servers)

		// queries family (freesoexperiment-e9f) — local-VM introspection ops.
		qs, qerr := RegisterQueryHandlers(ctx, cf, ipc)
		if qerr != nil {
			log.Fatalf("register query handlers: %v", qerr)
		}
		log.Printf("convention handlers: %d query-family ops serving", qs)

		// Memory family (freesoexperiment-6a8): remember + recall. Sidecar-local
		// state only — no IPC to the bot, no FSO wire PDU. The store is process-
		// lifetime scoped; persistence is a follow-up item.
		memStore := NewMemoryStore()
		memServers, err := RegisterMemoryHandlers(ctx, cf, memStore)
		if err != nil {
			log.Fatalf("register memory handlers: %v", err)
		}
		log.Printf("convention handlers: %d memory-family ops serving", memServers)

		// Naming family (freesoexperiment-4fa): name, forget, list-names. Shares
		// the memory store — names are a keyed subset ("name:<x>"). Sidecar-local.
		namingServers, err := RegisterNamingHandlers(ctx, cf, memStore)
		if err != nil {
			log.Fatalf("register naming handlers: %v", err)
		}
		log.Printf("convention handlers: %d naming-family ops serving", namingServers)

		// go-to: high-level verb; sidecar resolves --my_name via memStore, bot resolves
		// object_name/target_*/location via local VM.
		gotoServers, err := RegisterGoToHandler(ctx, cf, ipc, memStore)
		if err != nil {
			log.Fatalf("register go-to handler: %v", err)
		}
		log.Printf("convention handlers: %d go-to-family ops serving", gotoServers)

		// Interaction family (freesoexperiment-2a8 / freesoexperiment-2ac): interact-with,
		// cancel-interaction, query-pie-menu — object/sim interaction dispatch + local
		// pie-menu introspection. botCmds is wired for the bulletin_board special path.
		iservers, err := RegisterInteractionHandlers(ctx, cf, ipc, botCmds)
		if err != nil {
			log.Fatalf("register interaction handlers: %v", err)
		}
		log.Printf("convention handlers: %d interaction-family ops serving", iservers)

		// Social family (freesoexperiment-9ae): speak + be-friendly/tell-joke/flirt/be-mean/give-gift.
		socialServers, err := RegisterSocialHandlers(ctx, cf, ipc)
		if err != nil {
			log.Fatalf("register social handlers: %v", err)
		}
		log.Printf("convention handlers: %d social-family ops serving", socialServers)

		// IM family (freesoexperiment-7d8): instant-message. City-socket PDU (not lot). Single op.
		imServers, err := RegisterIMHandlers(ctx, cf, ipc)
		if err != nil {
			log.Fatalf("register im handlers: %v", err)
		}
		log.Printf("convention handlers: %d im-family ops serving", imServers)

		// Avatar family (freesoexperiment-b88): change-outfit + change-description.
		avatarServers, err := RegisterAvatarHandlers(ctx, cf, ipc)
		if err != nil {
			log.Fatalf("register avatar handlers: %v", err)
		}
		log.Printf("convention handlers: %d avatar-family ops serving", avatarServers)

		// Navigation family (freesoexperiment-a61 / freesoexperiment-e66):
		// go-home (already-home only), visit-lot (name-primary + hex fallback,
		// real cross-lot transition via bot-cmd:probe-lot + bot-cmd:exit),
		// find-avatar.
		// find-lot DROPPED per verb-catalog.md (subsumed by visit-lot).
		navServers, err := RegisterNavigationHandlers(ctx, cf, ipc, botCmds, memStore)
		if err != nil {
			log.Fatalf("register navigation handlers: %v", err)
		}
		log.Printf("convention handlers: %d navigation-family ops serving", navServers)

		// Property family (freesoexperiment-9c5): add-roommate, evict-roommate, lock-lot,
		// unlock-lot. pay-bills dropped (catalog: no wire PDU). Owner gating enforced in
		// the bot handler (PropertyHandlers.cs CheckOwner) for a deterministic refuse path.
		propServers, err := RegisterPropertyHandlers(ctx, cf, ipc)
		if err != nil {
			log.Fatalf("register property handlers: %v", err)
		}
		log.Printf("convention handlers: %d property-family ops serving", propServers)

		// Purchase-lot family (freesoexperiment-eaa): purchase-lot — was scaffold, now
		// implemented. Road-bits pre-check via bot-cmd:probe-road, city-socket
		// PurchaseLotRequest via bot-cmd:purchase-lot, owned-lots.json update on success.
		purchaseServers, err := RegisterPurchaseLotHandlers(ctx, cf, botCmds)
		if err != nil {
			log.Fatalf("register purchase-lot handlers: %v", err)
		}
		log.Printf("convention handlers: %d purchase-lot-family ops serving", purchaseServers)

		// Admin family (freesoexperiment-3df).
		adminServers, err := RegisterAdminHandlers(ctx, cf, ipc)
		if err != nil {
			log.Fatalf("register admin handlers: %v", err)
		}
		log.Printf("convention handlers: %d admin-family ops serving", adminServers)

		// Mail family (freesoexperiment-bd2).
		mailServers, err := RegisterMailHandlers(ctx, cf, ipc)
		if err != nil {
			log.Fatalf("register mail handlers: %v", err)
		}
		log.Printf("convention handlers: %d mail-family ops serving", mailServers)

		// City family (freesoexperiment-ded).
		// memStore is passed so vote/nominate can resolve --target_avatar_name
		// via the naming store (freesoexperiment-17f, I0-5 name-primary invariant).
		cityServers, err := RegisterCityHandlers(ctx, cf, ipc, memStore)
		if err != nil {
			log.Fatalf("register city handlers: %v", err)
		}
		log.Printf("convention handlers: %d city-family ops serving", cityServers)

		// Build-buy-catalog family (freesoexperiment-304): buy-object, place-from-inventory,
		// move-object, delete-object, send-to-inventory, list-object-for-sale, buy-listed-object,
		// upgrade-object. Lot-socket VMNet*Cmd PDUs; owner/own-object gating in the bot handler.
		buyServers, err := RegisterBuyModeHandlers(ctx, cf, ipc)
		if err != nil {
			log.Fatalf("register buy-mode handlers: %v", err)
		}
		log.Printf("convention handlers: %d build-buy-catalog-family ops serving", buyServers)

		// Build-buy-architecture family (freesoexperiment-41d): place-wall, paint-wall,
		// paint-floor, paint-grass, flatten-terrain, raise-terrain, set-roof, change-environment,
		// change-lot-size, leave-build-buy. Lot-socket PDUs (six share VMNetArchitectureCmd,
		// four have dedicated types). Owner gating in the bot handler (handler-side deterministic
		// refuse; server silently drops unauthorized commands per OQ-8).
		buildServers, err := RegisterBuildModeHandlers(ctx, cf, ipc)
		if err != nil {
			log.Fatalf("register build-mode handlers: %v", err)
		}
		log.Printf("convention handlers: %d build-buy-architecture-family ops serving", buildServers)

		// Civic family (freesoexperiment-409): set-tax-rate, grant-community-access.
		// Mayor-only felt powers — each produces an observable effect on a body
		// (rate recorded, lot access change). set-zoning was dropped (c7f): engine
		// has no zoning concept and the hardcoded /home/baron/projects/freeso-civic/
		// path is non-portable.
		taxServers, err := RegisterTaxHandlers(ctx, cf)
		if err != nil {
			log.Fatalf("register tax handlers: %v", err)
		}
		log.Printf("convention handlers: %d tax-family ops serving", taxServers)

		civicAccessServers, err := RegisterCivicAccessHandlers(ctx, cf, augmentor)
		if err != nil {
			log.Fatalf("register civic-access handlers: %v", err)
		}
		log.Printf("convention handlers: %d civic-access-family ops serving", civicAccessServers)

		// Single-dispatcher: one Subscribe goroutine handles every registered op
		// instead of one Subscribe per op. Replaces the convention.Server fleet
		// (which saturated SQLite at 103 × 500ms polls + per-poll fs sync) with
		// one TagPrefixes:["freeso:"] subscription. See dispatcher.go.
		log.Printf("router: %d ops registered, starting single-dispatcher Serve", cf.Router.Count())
		go cf.Router.Serve(ctx, cf)
	} else {
		log.Printf("running in --no-bot mode (campfire-only)")

		// --no-bot mode still benefits from memory ops: they are sidecar-local
		// and don't require a bot subprocess. Useful for campfire-only tests.
		memStore := NewMemoryStore()
		memServers, err := RegisterMemoryHandlers(ctx, cf, memStore)
		if err != nil {
			log.Fatalf("register memory handlers: %v", err)
		}
		log.Printf("convention handlers: %d memory-family ops serving (no-bot mode)", memServers)

		log.Printf("router: %d ops registered, starting single-dispatcher Serve", cf.Router.Count())
		go cf.Router.Serve(ctx, cf)
	}

	// Signal handling for clean shutdown.
	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)

	// Supervisor loop (freesoexperiment-e5f): when proc != nil (bot mode) the
	// sidecar supervises the bot. Bot exit triggers a relaunch — the sidecar
	// does NOT exit. Only an OS signal or ctx cancellation stops the sidecar.
	//
	// On each bot exit the supervisor:
	//   1. Reads next-lot from persona state (written by the visit-lot handler
	//      before it issues a bot-cmd:exit). If present, override FSO_LOT_LOCATION.
	//   2. Clear next-lot so the subsequent launch uses the default location.
	//   3. Launch a fresh BotProcess with the (possibly overridden) env.
	//   4. Hot-swap IPC's bot reference (ipc.UpdateBot) so all registered
	//      handlers route to the new process without re-registration.
	//   5. Start a new Bridges goroutine over the new process stdout.
	//
	// Convention handlers and the campfire router run for the sidecar lifetime.
	// Only IPC.bot and the bridge goroutine are replaced on each bot relaunch.
	//
	// --no-bot mode: botExitCh(nil) returns a nil channel, so the bot-exit case
	// is never selected and the loop exits only on signal or ctx.
	for {
		select {
		case sig := <-sigCh:
			log.Printf("caught %s — shutting down", sig)
			cancel()
			if proc != nil {
				proc.Stop()
			}
			// Allow the bridge a beat to flush the bot-exited event.
			fmt.Fprintln(os.Stderr, "[sidecar] bye")
			return
		case <-ctx.Done():
			log.Printf("context cancelled")
			if proc != nil {
				proc.Stop()
			}
			fmt.Fprintln(os.Stderr, "[sidecar] bye")
			return
		case exitErr := <-botExitCh(proc):
			// proc is nil only in --no-bot mode; botExitCh returns nil for nil proc
			// so this case is never selected in --no-bot mode.
			if exitErr != nil {
				log.Printf("bot exited with error: %v — supervising relaunch", exitErr)
			} else {
				log.Printf("bot exited cleanly — supervising relaunch")
			}

			// Step 1: read next-lot (cross-lot transition target, if any).
			lotOverride := ""
			if nextLot, rErr := ReadNextLot(); rErr != nil {
				log.Printf("supervisor: read next-lot: %v (ignoring, using default lot)", rErr)
			} else if nextLot != "" {
				lotOverride = nextLot
				log.Printf("supervisor: next-lot=%s — will launch bot at target lot", lotOverride)
				// Step 2: clear next-lot so future launches don't reuse it.
				if cErr := ClearNextLot(); cErr != nil {
					log.Printf("supervisor: clear next-lot: %v (non-fatal)", cErr)
				}
			}

			// Step 3: build env for relaunch, overriding FSO_LOT_LOCATION when
			// a cross-lot transition was requested, and always refreshing
			// FSO_HOME_LOT_LOCATION from owned-lots.json (so go-home reflects
			// any lot purchased during the previous bot session).
			relaunchEnv := injectHomeLotEnv(os.Environ())
			if lotOverride != "" {
				// Replace FSO_LOT_LOCATION with the transition target. Strip
				// any existing value and append the new one.
				filtered := make([]string, 0, len(relaunchEnv))
				for _, e := range relaunchEnv {
					if len(e) > len("FSO_LOT_LOCATION=") &&
						e[:len("FSO_LOT_LOCATION=")] == "FSO_LOT_LOCATION=" {
						continue
					}
					filtered = append(filtered, e)
				}
				relaunchEnv = append(filtered, "FSO_LOT_LOCATION="+lotOverride)
			}

			// Brief pause so downstream consumers (campfire, bridges) can
			// flush the bot-exited event before the new bot connects. Also
			// avoids a tight restart loop on a persistently-crashing bot.
			time.Sleep(2 * time.Second)

			// Step 4: launch the new bot process (with up to 3 attempts).
			// proc's exitCh is already closed; set proc=nil while retrying so
			// botExitCh(proc) returns nil and blocks rather than spinning.
			proc = nil
			launchCfg := BotConfig{
				Exec: botExec,
				Args: splitArgs(*botArgs),
				Env:  relaunchEnv,
			}
			var newProc *BotProcess
			for attempt := 1; attempt <= 3; attempt++ {
				var lErr error
				newProc, lErr = LaunchBot(ctx, launchCfg)
				if lErr == nil {
					break
				}
				log.Printf("supervisor: relaunch attempt %d/3 failed: %v", attempt, lErr)
				if attempt < 3 {
					time.Sleep(5 * time.Second)
				}
			}
			if newProc == nil {
				log.Printf("ERROR: bot supervisor: all relaunch attempts failed, sidecar exiting")
				if mErr := WriteSidecarFailedMarker("bot supervisor: all relaunch attempts failed"); mErr != nil {
					log.Printf("supervisor: write failure marker: %v", mErr)
				}
				os.Exit(1)
			}
			log.Printf("supervisor: relaunched bot pid=%d", newProc.Pid())

			// Step 5: hot-swap IPC and BotCmdPump bot references so all registered
			// handler closures route to the new process immediately.
			// Both are declared at the outer scope so this assignment is visible
			// to all handler closures.
			if ipc != nil {
				ipc.UpdateBot(newProc)
			}
			if botCmds != nil {
				botCmds.UpdateBot(newProc)
			}
			proc = newProc

			// Start a new bridge goroutine over the new process stdout, with the
			// BotCmdPump wired so bot-cmd-reply frames are routed correctly.
			go NewBridgesWithBotCmd(cf, newProc, ipc, botCmds, augmentor).Run(ctx)
		}
	}
}

// injectHomeLotEnv reads the persona's first owned lot from owned-lots.json and
// injects FSO_HOME_LOT_LOCATION into env, replacing any existing value. If no
// owned lot is found (first purchase has not happened yet), the existing value
// (or absence) is preserved — the C# bot returns ok:false from go-home when
// FSO_HOME_LOT_LOCATION is 0 / absent.
//
// This is called on every bot launch (initial + relaunch) so go-home always
// reflects the post-purchase home lot without requiring a sidecar restart.
func injectHomeLotEnv(env []string) []string {
	homeLoc, err := ReadHomeLotFromOwnedLots()
	if err != nil {
		log.Printf("supervisor: read home lot from owned-lots.json: %v (FSO_HOME_LOT_LOCATION unchanged)", err)
		return env
	}
	if homeLoc == "" {
		// No owned lots yet — don't inject. The bot will return ok:false from go-home.
		log.Printf("supervisor: no owned lots — FSO_HOME_LOT_LOCATION not set")
		// Still strip any stale value from a previous session to avoid go-home
		// incorrectly navigating to a lot no longer owned.
		filtered := make([]string, 0, len(env))
		for _, e := range env {
			if len(e) >= len("FSO_HOME_LOT_LOCATION=") &&
				e[:len("FSO_HOME_LOT_LOCATION=")] == "FSO_HOME_LOT_LOCATION=" {
				continue
			}
			filtered = append(filtered, e)
		}
		return filtered
	}
	// Replace any existing FSO_HOME_LOT_LOCATION with the first owned lot.
	filtered := make([]string, 0, len(env))
	for _, e := range env {
		if len(e) >= len("FSO_HOME_LOT_LOCATION=") &&
			e[:len("FSO_HOME_LOT_LOCATION=")] == "FSO_HOME_LOT_LOCATION=" {
			continue
		}
		filtered = append(filtered, e)
	}
	filtered = append(filtered, "FSO_HOME_LOT_LOCATION="+homeLoc)
	log.Printf("supervisor: home lot=%s injected into FSO_HOME_LOT_LOCATION", homeLoc)
	return filtered
}

// botExitCh is a nil-safe helper: when proc is nil (--no-bot mode) it returns
// a nil channel (blocks forever in select).
func botExitCh(p *BotProcess) <-chan error {
	if p == nil {
		return nil
	}
	return p.ExitCh()
}

// splitArgs tokenises a space-separated flag value. Does not support quoting;
// bot args are simple enough not to need it.
func splitArgs(s string) []string {
	var out []string
	var cur []byte
	for i := 0; i < len(s); i++ {
		c := s[i]
		if c == ' ' || c == '\t' {
			if len(cur) > 0 {
				out = append(out, string(cur))
				cur = cur[:0]
			}
			continue
		}
		cur = append(cur, c)
	}
	if len(cur) > 0 {
		out = append(out, string(cur))
	}
	return out
}
