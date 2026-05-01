/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log"

	"github.com/campfire-net/campfire/pkg/convention"
)

// RegisterTaxHandlers (freesoexperiment-409) wires the set-tax-rate civic op.
//
// Design: set-tax-rate is a "felt power" — it must produce an observable budget
// delta on residents. Sidecar delegates the debit to the C# bot via bot-cmd:tax-debit
// because the C# bot owns the Avatars.Transaction IPC (the only way to mutate
// avatar balances). The bot iterates all residents in the target neighborhood,
// issues a transaction debit per resident, and returns a per-resident delta list.
//
// Authorization: the C# bot checks whether the calling avatar has mayor_nhood > 0
// (i.e. is mayor of the target neighborhood) and returns NO_AUTH if not. The sidecar
// surfaces this refusal verbatim — no re-check here.
//
// Bulletin: after a successful tax cycle, the sidecar posts a tax-announcement
// bulletin via IPC (same pattern as post-bulletin) so residents see a notification.
// Bulletin failure is non-fatal — the tax was collected; log and continue.
//
// Escalation risk (Risk #6): if any single tax cycle takes > 5s wall time
// (50+ residents × Transaction round-trip latency), surface a warning in the
// response and escalate. The spec cap is 5 souls at research scale — safe today.
func RegisterTaxHandlers(ctx context.Context, cf *Campfire, botCmds *BotCmdPump, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"set-tax-rate": setTaxRateHandler(botCmds, ipc),
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

// TaxResidentResult is one resident's tax outcome, returned in the response.
type TaxResidentResult struct {
	PersistID  int64  `json:"persist_id"`
	AvatarName string `json:"avatar_name,omitempty"`
	Debit      int64  `json:"debit"`      // amount debited (positive = money removed)
	NewBalance int64  `json:"new_balance"` // balance after tax
}

// setTaxRateHandler implements the set-tax-rate convention.
//
// Flow:
//  1. Validate tax_rate_percent (0–100).
//  2. Delegate to bot-cmd:tax-debit with {tax_rate_percent, neighborhood_id}.
//  3. The C# bot checks mayor_nhood, collects residents, issues per-resident
//     Avatars.Transaction, and returns {ok, residents:[{persist_id, avatar_name, debit, new_balance}]}.
//  4. On success, post a bulletin announcing the tax cycle (non-fatal on failure).
//  5. Return the per-resident delta list so the agent can observe the effect.
func setTaxRateHandler(botCmds *BotCmdPump, ipc *IPC) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := req.Args

		// --- Step 1: validate tax_rate_percent ---
		rawRate, ok := args["tax_rate_percent"]
		if !ok || rawRate == nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "tax_rate_percent required (0.0–100.0)"},
			}, nil
		}
		taxRate, err := coerceFloat64(rawRate)
		if err != nil || taxRate < 0 || taxRate > 100 {
			return &convention.Response{
				Payload: map[string]any{
					"ok":     false,
					"error":  "INVALID_TAX_RATE",
					"reason": "INVALID_TAX_RATE",
					"hint":   "tax_rate_percent must be 0.0–100.0",
				},
			}, nil
		}

		// --- Step 2: optional neighborhood_id (default 1) ---
		neighborhoodID := uint64(1)
		if v, ok := args["neighborhood_id"]; ok && v != nil {
			if n, err := coerceUint64(v); err == nil {
				neighborhoodID = n
			}
		}

		// --- Step 3: require bot-cmd pump ---
		if botCmds == nil {
			return &convention.Response{
				Payload: map[string]any{
					"ok":       false,
					"error":    "set-tax-rate: bot-cmd channel not available (--no-bot mode)",
					"deferred": true,
				},
			}, nil
		}

		// --- Step 4: delegate to C# bot via bot-cmd:tax-debit ---
		taxReply, taxErr := botCmds.Send(ctx, "tax-debit", map[string]any{
			"tax_rate_percent": taxRate,
			"neighborhood_id":  neighborhoodID,
		})
		if taxErr != nil {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("tax-debit failed: %v", taxErr)},
			}, nil
		}
		if !taxReply.Ok {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": fmt.Sprintf("tax-debit refused: %s", taxReply.Error)},
			}, nil
		}

		// Parse tax-debit reply: {ok, reason, residents:[{persist_id, avatar_name, debit, new_balance}]}
		var taxData struct {
			Ok        bool                `json:"ok"`
			Reason    string              `json:"reason,omitempty"`
			Residents []TaxResidentResult `json:"residents"`
		}
		if len(taxReply.Data) > 0 {
			if perr := json.Unmarshal(taxReply.Data, &taxData); perr != nil {
				log.Printf("set-tax-rate: tax-debit data parse error: %v", perr)
			}
		}

		// Surface mayor-auth failure from the bot handler.
		if taxData.Reason == "NO_AUTH" {
			return &convention.Response{
				Payload: map[string]any{
					"ok":     false,
					"error":  "NO_AUTH",
					"reason": "NO_AUTH",
					"hint":   "Only the neighborhood mayor can set the tax rate. Your avatar must have mayor_nhood > 0.",
				},
			}, nil
		}

		// --- Step 5: post bulletin announcing the tax cycle (non-fatal) ---
		if ipc != nil && len(taxData.Residents) > 0 {
			totalDebited := int64(0)
			for _, r := range taxData.Residents {
				totalDebited += r.Debit
			}
			bulletinBody := fmt.Sprintf(
				"Tax cycle complete: %.1f%% collected from %d residents. Total collected: §%d.",
				taxRate, len(taxData.Residents), totalDebited,
			)
			_, bulletinErr := ipc.Send(ctx, "post-bulletin", map[string]any{
				"subject":         fmt.Sprintf("Tax Notice: %.1f%% Collected", taxRate),
				"body":            bulletinBody,
				"neighborhood_id": neighborhoodID,
				"lot_id":          uint64(0),
			})
			if bulletinErr != nil {
				log.Printf("set-tax-rate: bulletin post failed (non-fatal): %v", bulletinErr)
			}
		}

		return &convention.Response{
			Payload: map[string]any{
				"ok":              true,
				"tax_rate_percent": taxRate,
				"neighborhood_id": neighborhoodID,
				"residents":       taxData.Residents,
				"resident_count":  len(taxData.Residents),
			},
		}, nil
	}
}

// coerceFloat64 converts a value from convention args (JSON numbers arrive as
// float64 from campfire's JSON unmarshalling, but callers may also pass strings).
func coerceFloat64(v any) (float64, error) {
	switch val := v.(type) {
	case float64:
		return val, nil
	case int:
		return float64(val), nil
	case int64:
		return float64(val), nil
	case string:
		var f float64
		if _, err := fmt.Sscanf(val, "%f", &f); err != nil {
			return 0, fmt.Errorf("cannot parse %q as float64: %w", val, err)
		}
		return f, nil
	default:
		return 0, fmt.Errorf("cannot convert %T to float64", v)
	}
}

// coerceUint64 converts a convention arg value to uint64.
func coerceUint64(v any) (uint64, error) {
	switch val := v.(type) {
	case float64:
		if val < 0 {
			return 0, fmt.Errorf("negative value %v cannot be uint64", val)
		}
		return uint64(val), nil
	case int:
		if val < 0 {
			return 0, fmt.Errorf("negative value %v cannot be uint64", val)
		}
		return uint64(val), nil
	case int64:
		if val < 0 {
			return 0, fmt.Errorf("negative value %v cannot be uint64", val)
		}
		return uint64(val), nil
	case uint64:
		return val, nil
	case string:
		var u uint64
		if _, err := fmt.Sscanf(val, "%d", &u); err != nil {
			return 0, fmt.Errorf("cannot parse %q as uint64: %w", val, err)
		}
		return u, nil
	default:
		return 0, fmt.Errorf("cannot convert %T to uint64", v)
	}
}
