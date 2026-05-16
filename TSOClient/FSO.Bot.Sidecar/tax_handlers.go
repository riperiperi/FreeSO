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

// RegisterTaxHandlers (freesoexperiment-409) wires the set-tax-rate civic op.
//
// Design (freesoexperiment-ea0, operator decision 99d=B): set-tax-rate is a
// stub. It validates the rate and records intent but does NOT collect tax.
// The engine-side TaxCycleHandler (analogous to MoneyClock in FSO.Server/Domain/)
// is the correct place for budget mutations — implementing it is tracked as a
// follow-up item (M2/M3 milestone). The bot-cmd:tax-debit delegation path has
// been removed as part of the ea0 architectural cleanup.
func RegisterTaxHandlers(ctx context.Context, cf *Campfire) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"set-tax-rate": setTaxRateHandler(),
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

// setTaxRateHandler implements the set-tax-rate convention as a validated stub.
//
// freesoexperiment-ea0 (operator decision 99d=B): tax collection is deferred.
// The engine-side TaxCycleHandler (M2/M3 milestone, analogous to MoneyClock
// in FSO.Server/Domain/) is the correct place for avatar budget mutations.
// This stub validates the rate and records intent so the agent sees a
// round-trip response, but no §simoleons are moved.
//
// TODO(M2/M3): replace with engine-side TaxCycleHandler that calls
// Avatars.Transaction for each resident in the neighborhood.
func setTaxRateHandler() convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		args := req.Args

		// Validate tax_rate_percent (0–100).
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

		// Optional neighborhood_id (default 1).
		neighborhoodID := uint64(1)
		if v, ok2 := args["neighborhood_id"]; ok2 && v != nil {
			if n, err2 := coerceUint64(v); err2 == nil {
				neighborhoodID = n
			}
		}

		// Stub: rate recorded, no collection performed.
		// Engine-side TaxCycleHandler (M2/M3) will perform the actual debit.
		return &convention.Response{
			Payload: map[string]any{
				"ok":               true,
				"tax_rate_percent": taxRate,
				"neighborhood_id":  neighborhoodID,
				"deferred":         true,
				"note":             "Tax rate recorded. Collection is deferred to engine-side TaxCycleHandler (M2/M3).",
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

// coerceUint64 is declared in helpers.go (shared across all handler families).
