/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"time"
	"context"
	"fmt"
	"log"

	"github.com/campfire-net/campfire/pkg/convention"
)

// RegisterMailHandlers (freesoexperiment-bd2) wires the mail verb family:
// poll-inbox, send-mail, delete-mail. All three are thin IPC forwarders; the
// bot holds the city Aries socket + the correlation state for the poll/send
// response PDUs.
//
// read-mail is NOT declared / NOT implemented. Catalog inspection
// (docs/design/verb-catalog.md:74-76) lists only poll-inbox / send-mail /
// delete-mail — MailRequestType has no READ variant, and fso_inbox.read_state
// is never transitioned server-side (the original client tracks read state
// locally). Per "drop ops with no wire PDU" rule, no convention JSON, no handler.
//
// The bot's MailHandlers.cs holds a single-slot correlator per response kind
// (one in-flight poll-inbox, one in-flight send-mail). Bursts from one agent
// will serialise at the bot; concurrent agents sharing one bot would contend.
// This is acceptable for the -bd2 research use case (one agent per bot).
func RegisterMailHandlers(ctx context.Context, cf *Campfire, ipc *IPC) (int, error) {
	ops := map[string]convention.HandlerFunc{
		"poll-inbox":  simpleForwardingHandler(ipc, "poll-inbox", "since_ticks"),
		"send-mail":   simpleForwardingHandler(ipc, "send-mail", "target_persist_id", "subject", "body"),
		"delete-mail": simpleForwardingHandler(ipc, "delete-mail", "mail_id"),
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
		op := op // capture

		srv := convention.NewServer(cf.Client, decl).WithErrorHandler(func(err error) {

			log.Printf("handler[%s]: errFn: %v", op, err)

		}).WithPollInterval(10 * time.Second)
		srv.RegisterHandler(op, handler)
		go func(op string, srv *convention.Server) {
			log.Printf("handler[%s]: serving", op)
			// retry-on-subscription-drop: Serve exits cleanly on sqlite BUSY or similar;
			// keep restarting until ctx is cancelled.
			for {
				err := srv.Serve(ctx, cf.ID)
				if ctx.Err() != nil {
					break
				}
				if err != nil && err != context.Canceled {
					log.Printf("handler[%s]: serve err: %v (restarting)", op, err)
				} else {
					log.Printf("handler[%s]: serve returned (restarting)", op)
				}
				// small backoff so we don't hot-spin on a persistent failure
				select {
				case <-ctx.Done():
					return
				case <-time.After(200 * time.Millisecond):
				}
			}
			log.Printf("handler[%s]: stopped", op)
		}(op, srv)
		started++
	}
	return started, nil
}
