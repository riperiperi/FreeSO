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

// ErrPromotionRefusedDuringSession is the canonical typed error returned when
// an agent attempts runtime convention promotion against a live sidecar.
// The error text is the wire-observable value callers must match.
const ErrPromotionRefusedDuringSession = "error:promotion-refused-during-session"

// RegisterPromoteHandler wires the freeso:convention:promote convention.
//
// The handler ALWAYS returns error:promotion-refused-during-session.
//
// The sidecar's convention registry is frozen at boot (Component 10 invariant,
// automataisland-5bf). Declarations are loaded once from conventions/*.json
// when the process starts; the set of ops the Router knows about is fixed for
// the entire session. No agent — regardless of identity or permissions — can
// add, replace, or remove a convention at runtime.
//
// Why the verb exists at all: without this registration, a call to
// cf $BODY_CF convention:promote returns a generic "router: no handler
// registered for op" error, which is opaque to callers. With this
// registration the refusal is both discoverable (the op appears in the
// sidecar's tools/list) and typed (the error string is predictable, not an
// implementation detail of the router).
//
// To add a new convention to a live world:
//  1. Stop the sidecar (SIGTERM).
//  2. Add the declaration JSON to FSO.Bot.Sidecar/conventions/.
//  3. Rebuild: go build -o ./bin/freeso-sidecar .
//  4. Restart the sidecar — the new op appears in tools/list at boot.
func RegisterPromoteHandler(ctx context.Context, cf *Campfire) (int, error) {
	handler := func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		// Component 10 invariant: registry is frozen at boot. Refuse always.
		// Return a typed error so callers can match the specific condition
		// (rather than parsing a generic router error string).
		return nil, fmt.Errorf("%s", ErrPromotionRefusedDuringSession)
	}

	decls, err := LoadDeclarations(conventionFiles)
	if err != nil {
		return 0, fmt.Errorf("load declarations: %w", err)
	}
	for _, d := range decls {
		if d.Operation == "convention:promote" {
			cf.Router.Register(d, handler)
			return 1, nil
		}
	}
	return 0, fmt.Errorf("declaration for op %q missing (expected in conventions/convention-promote.json)", "convention:promote")
}
