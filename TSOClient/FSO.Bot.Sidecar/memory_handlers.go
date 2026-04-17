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
	"sync"

	"github.com/campfire-net/campfire/pkg/convention"
)

// MemoryStore is a thread-safe in-memory key→value store scoped to a single
// sidecar session. Values are persisted only for the lifetime of this
// sidecar process — survives neither bot restart nor sidecar restart.
// Persistence to disk is a separate follow-up item (see freesoexperiment-6a8
// close reason).
//
// Concurrency: sync.Map is the right choice here because the access pattern
// is "many readers, occasional writers on disjoint keys" (each avatar has its
// own namespaces of keys; stress is typically reads). Values are stored as
// json.RawMessage so structured values round-trip without re-encoding on
// every recall.
type MemoryStore struct {
	m sync.Map // map[string]json.RawMessage
}

// NewMemoryStore returns an empty store ready for use.
func NewMemoryStore() *MemoryStore {
	return &MemoryStore{}
}

// Store atomically writes key=value. value may be nil (represented as the JSON
// literal `null`) or any json.RawMessage value. A second Store on the same
// key overwrites the prior value.
func (s *MemoryStore) Store(key string, value json.RawMessage) {
	if value == nil {
		value = json.RawMessage("null")
	}
	// Defensive copy: the caller's slice may be reused.
	buf := make(json.RawMessage, len(value))
	copy(buf, value)
	s.m.Store(key, buf)
}

// Load returns the value stored at key, plus found=true if the key was ever
// stored. A found=false value is an empty RawMessage.
func (s *MemoryStore) Load(key string) (json.RawMessage, bool) {
	v, ok := s.m.Load(key)
	if !ok {
		return nil, false
	}
	raw, _ := v.(json.RawMessage)
	// Defensive copy on the way out to keep the store's slice immutable.
	out := make(json.RawMessage, len(raw))
	copy(out, raw)
	return out, true
}

// RegisterMemoryHandlers wires remember + recall as convention.Server
// instances on cf. This family is sidecar-local only: no IPC to the bot, no
// FSO wire PDU. The store is shared across both handlers for obvious reasons.
// Returns the count of opened servers; errors hard if either declaration is
// missing (the conventions are freesoexperiment-6a8 deliverables and MUST
// ship together).
func RegisterMemoryHandlers(ctx context.Context, cf *Campfire, store *MemoryStore) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"remember": rememberHandler(store),
		"recall":   recallHandler(store),
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

// rememberHandler stores a key=value pair in the sidecar's in-memory store.
// Args:
//   key   (string, required) — the key to store under.
//   value (any JSON value, required) — the value to store. Stored verbatim as
//         raw JSON; recall returns exactly this shape.
//
// Returns: {ok: true}.
// Errors: missing/empty key → ok=false with error "key is required".
//
// No wire PDU — this is sidecar-only state. A follow-up item will add
// persistence if experiments show the lack of durability bites.
func rememberHandler(store *MemoryStore) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		key, _ := req.Args["key"].(string)
		if key == "" {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "key is required"},
				Tags:    []string{"freeso:remember"},
			}, nil
		}
		rawVal, ok := req.Args["value"]
		if !ok {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "value is required"},
				Tags:    []string{"freeso:remember"},
			}, nil
		}
		// Convention arg type is `json`, which means the framework delivers the
		// value as a validated JSON string (see campfire executor.go case
		// "json"). Parse it to its native shape so recall returns an un-
		// escaped object tree — otherwise the agent receives a string of a
		// string. If the caller happens to pass a non-string (e.g. the Go
		// unit tests send a map directly), fall back to re-marshaling it.
		var encoded json.RawMessage
		switch v := rawVal.(type) {
		case string:
			if !json.Valid([]byte(v)) {
				return &convention.Response{
					Payload: map[string]any{"ok": false, "error": "value is not valid JSON"},
					Tags:    []string{"freeso:remember"},
				}, nil
			}
			encoded = json.RawMessage(v)
		default:
			b, err := json.Marshal(v)
			if err != nil {
				return &convention.Response{
					Payload: map[string]any{"ok": false, "error": "encode value: " + err.Error()},
					Tags:    []string{"freeso:remember"},
				}, nil
			}
			encoded = b
		}
		store.Store(key, encoded)
		return &convention.Response{
			Payload: map[string]any{"ok": true, "key": key},
			Tags:    []string{"freeso:remember"},
		}, nil
	}
}

// recallHandler fetches a previously-stored value by key.
// Args:
//   key (string, required) — the key to look up.
//
// Returns: {ok: true, found: bool, value: <whatever was stored> | null}.
// Errors: missing/empty key → ok=false with error "key is required".
//
// found=false when the key was never stored or was cleared (clear is not yet
// supported — future item).
func recallHandler(store *MemoryStore) convention.HandlerFunc {
	return func(ctx context.Context, req *convention.Request) (*convention.Response, error) {
		key, _ := req.Args["key"].(string)
		if key == "" {
			return &convention.Response{
				Payload: map[string]any{"ok": false, "error": "key is required"},
				Tags:    []string{"freeso:recall"},
			}, nil
		}
		raw, found := store.Load(key)
		out := map[string]any{"ok": true, "found": found}
		if found {
			// Decode back to native any so the convention framework's outbound
			// marshal produces a natural JSON tree (not a double-encoded string).
			var decoded any
			if err := json.Unmarshal(raw, &decoded); err != nil {
				// Shouldn't happen — we stored it via json.Marshal — but surface
				// rather than panic.
				out["ok"] = false
				out["error"] = "decode stored value: " + err.Error()
				delete(out, "found")
			} else {
				out["value"] = decoded
			}
		}
		return &convention.Response{
			Payload: out,
			Tags:    []string{"freeso:recall"},
		}, nil
	}
}
