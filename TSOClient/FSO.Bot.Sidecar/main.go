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
	if !*noBot {
		proc, err = LaunchBot(ctx, BotConfig{
			Exec: botExec,
			Args: splitArgs(*botArgs),
			// Inherits parent env including FSO_*. Credentials are environmental
			// only — never placed on the CLI.
			Env: os.Environ(),
		})
		if err != nil {
			log.Fatalf("bot launch: %v", err)
		}
		log.Printf("bot launched pid=%d", proc.Pid())

		// 3. IPC command channel (freesoexperiment-b9c): correlates stdin commands
		// sidecar→bot with response frames observed on bot stdout. Must be wired
		// before bridges start so response frames are routed instead of dropped.
		ipc := NewIPC(proc)

		// 4. Start bridges.
		bridges := NewBridges(cf, proc, ipc)
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

		// Interaction family (freesoexperiment-2a8): interact-with, cancel-interaction,
		// query-pie-menu — object/sim interaction dispatch + local pie-menu introspection.
		iservers, err := RegisterInteractionHandlers(ctx, cf, ipc)
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

		// Navigation family (freesoexperiment-a61): go-home (already-home only), find-avatar.
		// visit-lot ships with deferred:true marker — cross-lot transition needs -ca0.
		// find-lot DROPPED per verb-catalog.md (subsumed by visit-lot).
		navServers, err := RegisterNavigationHandlers(ctx, cf, ipc)
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
		cityServers, err := RegisterCityHandlers(ctx, cf, ipc)
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
	}

	// Signal handling for clean shutdown.
	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, syscall.SIGINT, syscall.SIGTERM)

	select {
	case sig := <-sigCh:
		log.Printf("caught %s — shutting down", sig)
	case <-ctx.Done():
		log.Printf("context cancelled")
	case err := <-botExitCh(proc):
		// Bridge already broadcasts bot-exited system event before this fires.
		if err != nil {
			log.Printf("bot exited: %v", err)
		} else {
			log.Printf("bot exited cleanly")
		}
	}

	cancel()
	if proc != nil {
		proc.Stop()
	}
	// Allow the bridge a beat to flush the bot-exited event.
	fmt.Fprintln(os.Stderr, "[sidecar] bye")
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
