/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"testing"

	"github.com/campfire-net/campfire/cf-conventions/cf-convention"
)

// Tests for the idempotent-declaration-publish path. Sidecars used to
// accumulate one full set of declarations per restart in the campfire
// (340+ ops listed for 113 real declarations at peak — 3x duplication).
// The fix uses cf's canonical dedup key — (convention, operation, version)
// — same shape `cf convention promote` uses (cmd/cf/cmd/
// convention_promote.go:155). We query the campfire for our own prior
// publications (Sender-filtered Read), build a key set, and skip
// publication for declarations already published under the same key.

// TestDeclarationKey_Canonical: the dedup key format MUST match cf's own
// convention_promote.go format ("conv:op@version") so a declaration
// published by our sidecar and a declaration promoted via `cf convention
// promote` collapse to the same identity. If anyone in the toolchain
// changes the separator, dedup silently misfires and we accumulate
// duplicates again.
func TestDeclarationKey_Canonical(t *testing.T) {
	d := &convention.Declaration{
		Convention: "freeso-embodiment",
		Operation:  "buy-object",
		Version:    "1.0",
	}
	got := declarationKey(d)
	want := "freeso-embodiment:buy-object@1.0"
	if got != want {
		t.Errorf("declarationKey() = %q; want %q (must match cf convention_promote.go conflictKey)", got, want)
	}
}

// TestDeclarationKey_VersionedDedup: bumping version produces a distinct
// key, so a content-changed declaration (with bumped version) republishes
// cleanly. This is the documented cf upgrade path — `cf convention
// promote --force` overwrites by promote-time check; we accept it because
// version bumped.
func TestDeclarationKey_VersionedDedup(t *testing.T) {
	a := &convention.Declaration{Convention: "freeso-embodiment", Operation: "buy-object", Version: "1.0"}
	b := &convention.Declaration{Convention: "freeso-embodiment", Operation: "buy-object", Version: "1.1"}
	if declarationKey(a) == declarationKey(b) {
		t.Error("v1.0 and v1.1 of same op collapsed to the same key — version bump must be a distinct identity")
	}
}

// TestDeclarationKey_ConventionScoping: two conventions defining the
// same operation name must NOT collide. Per cf, the dedup namespace is
// convention-scoped — a future "social:speak" and "freeso:speak" can
// coexist.
func TestDeclarationKey_ConventionScoping(t *testing.T) {
	a := &convention.Declaration{Convention: "freeso-embodiment", Operation: "speak", Version: "1.0"}
	b := &convention.Declaration{Convention: "social", Operation: "speak", Version: "1.0"}
	if declarationKey(a) == declarationKey(b) {
		t.Error("speak in two conventions collapsed to the same key — conventions must namespace ops")
	}
}
