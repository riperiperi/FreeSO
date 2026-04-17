/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import "bytes"

// bytesReader is a one-liner wrapper used by bridges.extractPersistID to feed
// encoding/json a reader. Kept here so bridges.go stays focused on semantics.
func bytesReader(b []byte) *bytes.Reader { return bytes.NewReader(b) }
