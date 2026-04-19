/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"fmt"
	"log"

	"github.com/campfire-net/campfire/pkg/convention"
)

// RegisterGoToHandler wires the go-to convention. Handler resolves --my_name
// against the sidecar's name map (built by the naming family on top of the
// shared MemoryStore), rewrites to one of the bot's supported selectors
// (target_object_id / target_sim_id / location), then forwards to the bot's
// go-to IPC op. If --my_name is absent, args pass through unchanged.
func RegisterGoToHandler(ctx context.Context, cf *Campfire, ipc *IPC, store *MemoryStore) (int, error) {
	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		return 0, fmt.Errorf("load declarations: %w", err)
	}
	var decl *convention.Declaration
	for _, d := range decls {
		if d.Operation == "go-to" {
			decl = d
			break
		}
	}
	if decl == nil {
		return 0, fmt.Errorf("declaration for op \"go-to\" missing")
	}
	srv := convention.NewServer(cf.Client, decl)
	srv.RegisterHandler("go-to", goToHandler(ipc, store))
	go func() {
		log.Printf("handler[go-to]: serving")
		if err := srv.Serve(ctx, cf.ID); err != nil && err != context.Canceled {
			log.Printf("handler[go-to]: serve err: %v", err)
		}
		log.Printf("handler[go-to]: stopped")
	}()
	return 1, nil
}

// goToHandler: resolve --my_name (if present) against the name store, rewrite
// to the appropriate lower-level selector, then forward to the bot. The bot
// does VM-side picking for object_name/target_object_id/location and dispatch.
func goToHandler(ipc *IPC, store *MemoryStore) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := map[string]any{}
		for _, k := range []string{"target_object_id", "target_sim_id", "object_name", "location", "interaction", "queue_mode", "max_distance_tiles"} {
			if v, ok := req.Args[k]; ok {
				args[k] = v
			}
		}

		if myName, ok := req.Args["my_name"].(string); ok && myName != "" {
			entry := lookupName(store, myName)
			if entry == nil {
				return &convention.Response{
					Payload: map[string]any{
						"ok":    false,
						"error": fmt.Sprintf("name %q is not bound (use list-names to see current bindings or 'name --as %q --target_object_id <id>' to bind it)", myName, myName),
					},
				}, nil
			}
			kind, _ := entry["kind"].(string)
			switch kind {
			case "object":
				if v, ok := numericArg(entry, "target_object_id"); ok {
					args["target_object_id"] = v
				}
			case "sim":
				if v, ok := numericArg(entry, "target_sim_id"); ok {
					args["target_sim_id"] = v
				}
			case "location":
				loc := map[string]any{}
				if v, ok := numericArg(entry, "x"); ok {
					loc["x"] = v
				}
				if v, ok := numericArg(entry, "y"); ok {
					loc["y"] = v
				}
				if v, ok := numericArg(entry, "level"); ok {
					loc["level"] = v
				} else {
					loc["level"] = int64(1)
				}
				args["location"] = loc
			default:
				return &convention.Response{
					Payload: map[string]any{
						"ok":    false,
						"error": fmt.Sprintf("name %q has unknown kind %q (store corruption?) — re-bind with the name verb", myName, kind),
					},
				}, nil
			}
		}

		return forwardIPC(ctx, ipc, "go-to", args)
	}
}
