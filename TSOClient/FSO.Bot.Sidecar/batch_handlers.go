/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"fmt"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// RegisterBatchHandlers wires the batch-build verb (freesoexperiment-e5e).
//
// batch-build folds N build-buy ops into ONE cf round-trip. The engine's PDU
// channel is still 1-at-a-time — each op goes out as its own VMNet*Cmd — but
// the cf round-trip floor (~2.5s per call against fs-transport at the time of
// writing, fs.Transport.ListMessages walks the whole messages dir) gets paid
// once instead of N times. For a furnished-room build (~50 ops), that's the
// difference between ~2 min of cf overhead and ~2.5s.
//
// Composition with freesoexperiment-fe1: each entry's response carries the
// full structured verdict (placed/silent-drop/bot-rejected, hints,
// balance_before/after) from the per-op verifying handler — agents can read
// precisely what happened to each placement without a follow-up
// query-lot-objects.
//
// Atomicity model: NOT transactional. The engine has no reverse-op for these
// commands, so a halt-on-failure leaves earlier successful ops in place. The
// declaration documents this loudly. continue_on_failure=true opts in to
// best-effort mode for use cases like "demolish everything, ignore the
// individual failures".
func RegisterBatchHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"batch-build": batchBuildHandler(ipc),
	}

	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		return 0, fmt.Errorf("load declarations: %w", err)
	}
	byOp := make(map[string]*convention.Declaration, len(decls))
	for _, d := range decls {
		byOp[d.Operation] = d
	}

	started := 0
	for op, handler := range ops {
		decl, ok := byOp[op]
		if !ok {
			return started, fmt.Errorf("declaration for op %q missing (expected in conventions/%s.json)", op, op)
		}
		cf.Router.Register(decl, handler)
		started++
	}
	return started, nil
}

// batchBuildHandler returns the convention HandlerFunc for batch-build. It
// composes the BuyMode + BuildMode op maps into one allowlist and dispatches
// each entry through the same handler the single-op call would use, so the
// fe1 structured-verdict shape carries through untouched.
//
// We intentionally exclude read-only ops (search-catalog, list-* etc) — those
// don't belong in a "build" batch and including them invites confusion. The
// allowed set is exactly the mutating build-buy verbs.
func batchBuildHandler(ipc *IPC) convention.HandlerFunc {
	return batchBuildHandlerWithMap(buildBatchOps(ipc))
}

// buildBatchOps assembles the merged op handler map: all mutating build-buy
// verbs from BuyModeOps and BuildModeOps, minus the read-only ones.
func buildBatchOps(ipc *IPC) map[string]convention.HandlerFunc {
	buy := BuyModeOps(ipc)
	build := BuildModeOps(ipc)
	merged := make(map[string]convention.HandlerFunc, len(buy)+len(build))
	// BuyMode mutating ops (excludes search-catalog, list-catalog-categories).
	for _, op := range []string{
		"buy-object", "place-from-inventory", "move-object", "delete-object",
		"send-to-inventory", "list-object-for-sale", "buy-listed-object", "upgrade-object",
	} {
		if h, ok := buy[op]; ok {
			merged[op] = h
		}
	}
	// BuildMode mutating ops (excludes list-architecture-styles, leave-build-buy).
	for _, op := range []string{
		"place-wall", "paint-wall", "paint-floor", "paint-grass",
		"flatten-terrain", "raise-terrain", "set-roof",
		"change-environment", "change-lot-size",
	} {
		if h, ok := build[op]; ok {
			merged[op] = h
		}
	}
	return merged
}

// batchBuildHandlerWithMap is the testable seam — accepts the dispatch map
// directly so unit tests can inject mock handlers without spinning up IPC.
//
// Success verdict whitelist: the per-op verifying handlers (verifying_handler.go)
// return verdict="placed" for buy-object/place-from-inventory and
// verdict="deleted" for delete-object. Other verbs (move-object, simple ops
// without a verifying wrapper) return ok=true without a verdict field; we
// treat absent-verdict-but-ok=true as success.
func batchBuildHandlerWithMap(ops map[string]convention.HandlerFunc) convention.HandlerFunc {
	successVerdicts := map[string]bool{
		"placed":  true, // buy-object, place-from-inventory
		"deleted": true, // delete-object
	}

	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		opsRaw, ok := req.Args["ops"].([]any)
		if !ok {
			return batchErrResp("batch-build requires 'ops' array"), nil
		}
		if len(opsRaw) == 0 {
			return batchErrResp("batch-build requires non-empty 'ops' array"), nil
		}

		continueOnFailure, _ := req.Args["continue_on_failure"].(bool)

		// Pre-validate every entry — bad op name in the middle of a batch should fail
		// the whole batch up front, not halfway through after we've placed three things.
		for i, opAny := range opsRaw {
			opMap, isMap := opAny.(map[string]any)
			if !isMap {
				return batchErrResp(fmt.Sprintf("batch-build[%d]: entry is not an object", i)), nil
			}
			opName, _ := opMap["op"].(string)
			if opName == "" {
				return batchErrResp(fmt.Sprintf("batch-build[%d]: missing 'op' field", i)), nil
			}
			if _, allowed := ops[opName]; !allowed {
				return batchErrResp(fmt.Sprintf("batch-build[%d]: op %q not in build-buy family", i, opName)), nil
			}
		}

		verdicts := make([]any, 0, len(opsRaw))
		for i, opAny := range opsRaw {
			opMap := opAny.(map[string]any)
			opName := opMap["op"].(string)

			// Strip 'op' from forwarded args.
			forwardArgs := make(map[string]any, len(opMap))
			for k, v := range opMap {
				if k == "op" {
					continue
				}
				forwardArgs[k] = v
			}

			subReq := &convention.Request{Args: forwardArgs}
			resp, err := ops[opName](ctx, subReq)

			entry := map[string]any{
				"index": i,
				"op":    opName,
			}
			isSuccess := false
			if err != nil {
				entry["ok"] = false
				entry["error"] = err.Error()
			} else if resp == nil {
				entry["ok"] = false
				entry["error"] = "nil response from handler"
			} else {
				payload, _ := resp.Payload.(map[string]any)
				for k, v := range payload {
					entry[k] = v
				}
				okFlag, _ := payload["ok"].(bool)
				verdict, _ := payload["verdict"].(string)
				// Success: ok=true AND (verdict matches successVerdicts OR verdict absent
				// for ops that don't carry a verifying-handler verdict).
				if okFlag {
					if verdict == "" || successVerdicts[verdict] {
						isSuccess = true
					}
				}
			}
			verdicts = append(verdicts, entry)

			if !isSuccess && !continueOnFailure {
				haltReason, _ := entry["verdict"].(string)
				if haltReason == "" {
					if e, ok := entry["error"].(string); ok && e != "" {
						haltReason = e
					} else {
						haltReason = "non-success"
					}
				}
				return &convention.Response{Payload: map[string]any{
					"ok":          true,
					"count":       len(verdicts),
					"verdicts":    verdicts,
					"halted_at":   i,
					"halt_reason": haltReason,
				}}, nil
			}
		}

		return &convention.Response{Payload: map[string]any{
			"ok":       true,
			"count":    len(verdicts),
			"verdicts": verdicts,
		}}, nil
	}
}

// batchErrResp returns a fulfilment-error response with ok:false. Mirrors the
// shape forwardIPC uses for bot-side failures so agents see a consistent
// error envelope across single-op and batch ops. (Distinct from helpers.go's
// errResp which has a different two-arg signature.)
func batchErrResp(msg string) *convention.Response {
	return &convention.Response{
		Payload: map[string]any{
			"ok":    false,
			"error": msg,
		},
	}
}
