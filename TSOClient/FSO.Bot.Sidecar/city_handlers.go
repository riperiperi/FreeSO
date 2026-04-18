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

// RegisterCityHandlers (freesoexperiment-ded) wires the city verb family:
// view-bulletin, post-bulletin, vote, nominate, view-neighborhood. All five
// ride the city Aries socket (not the lot socket); all are thin argument-pass
// forwarding handlers like property/im. The C# bot owns correlation (FIFO for
// bulletin/nhood — they have no wire correlator; SendingAvatarID for
// DataServiceWrapperPDU). The sidecar has no city-side state.
//
// Election-gated ops (vote, nominate) deterministically refuse with
// ELECTION_OVER on workshop's current DB state (no active election_cycle_id).
// That IS the test's wire-level-effect verification lever.
func RegisterCityHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"view-bulletin":     simpleForwardingHandler(ipc, "view-bulletin", "neighborhood_id"),
		"post-bulletin":     simpleForwardingHandler(ipc, "post-bulletin", "subject", "body", "neighborhood_id", "lot_id"),
		"vote":              simpleForwardingHandler(ipc, "vote", "target_persist_id", "neighborhood_id"),
		"nominate":          simpleForwardingHandler(ipc, "nominate", "target_persist_id", "neighborhood_id"),
		"view-neighborhood": simpleForwardingHandler(ipc, "view-neighborhood", "neighborhood_id"),
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
		srv := convention.NewServer(cf.Client, decl)
		srv.RegisterHandler(op, handler)
		go func(op string, srv *convention.Server) {
			log.Printf("handler[%s]: serving", op)
			if err := srv.Serve(ctx, cf.ID); err != nil && err != context.Canceled {
				log.Printf("handler[%s]: serve err: %v", op, err)
			}
			log.Printf("handler[%s]: stopped", op)
		}(op, srv)
		started++
	}
	return started, nil
}
