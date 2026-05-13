/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

package main

import (
	"bufio"
	"context"
	"errors"
	"fmt"
	"io"
	"log"
	"os"
	"os/exec"
	"sync"
)

// BotConfig parameterises bot subprocess launch.
type BotConfig struct {
	Exec string
	Args []string
	// Env is the full environment for the bot. Credentials live here only —
	// never on the CLI, never in a shell string. This is the hygiene guarantee
	// tested by TestFSOPassNotOnCLI.
	Env []string
	// Dir is an optional working directory for the child.
	Dir string
}

// BotProcess owns the bot subprocess. It exposes a stdout line channel for
// bridges to consume, pipes stderr straight through with a prefix, and surfaces
// exit status on ExitCh. Stdin is owned by the IPC layer (see ipc.go) —
// commands sidecar→bot travel here as NDJSON lines.
type BotProcess struct {
	cmd *exec.Cmd

	stdoutLines chan []byte
	stdin       io.WriteCloser
	stdinMu     sync.Mutex
	exitCh      chan error

	// stdoutDone is closed by the scan goroutine once it has finished reading
	// all stdout data. The Wait goroutine blocks on this before calling
	// cmd.Wait() to satisfy the StdoutPipe contract: "it is incorrect to call
	// Wait before all reads from the pipe have completed."
	stdoutDone chan struct{}

	stopOnce sync.Once
}

// LaunchBot starts the bot subprocess and returns a handle. The caller should
// range over Lines() to consume stdout, or let a Bridges instance own it.
func LaunchBot(ctx context.Context, cfg BotConfig) (*BotProcess, error) {
	if cfg.Exec == "" {
		return nil, errors.New("BotConfig.Exec is empty")
	}
	// CommandContext — when ctx is cancelled, the child gets SIGKILL. Our
	// Stop() also sends SIGTERM first for a clean bye.
	cmd := exec.CommandContext(ctx, cfg.Exec, cfg.Args...)
	cmd.Env = cfg.Env
	cmd.Dir = cfg.Dir

	stdout, err := cmd.StdoutPipe()
	if err != nil {
		return nil, fmt.Errorf("stdout pipe: %w", err)
	}
	stderr, err := cmd.StderrPipe()
	if err != nil {
		return nil, fmt.Errorf("stderr pipe: %w", err)
	}
	stdin, err := cmd.StdinPipe()
	if err != nil {
		return nil, fmt.Errorf("stdin pipe: %w", err)
	}

	if err := cmd.Start(); err != nil {
		return nil, fmt.Errorf("start: %w", err)
	}

	p := &BotProcess{
		cmd:         cmd,
		stdoutLines: make(chan []byte, 64),
		stdin:       stdin,
		exitCh:      make(chan error, 1),
		stdoutDone:  make(chan struct{}),
	}

	// stderr → sidecar stderr, with a prefix so bot log lines are visibly
	// tagged when interleaved with sidecar log lines.
	go tee(stderr, os.Stderr, "[bot] ")

	// stdout → channel. Buffer size accepts larger-than-default lines so a full
	// perception frame (which can be a few KB with many nearby objects) does
	// not trip the scanner's token-too-long error.
	//
	// The scan goroutine MUST drain stdout to completion before returning —
	// cmd.Wait() closes the pipe after the process exits, so returning early
	// (e.g. on ctx.Done()) while stdout still has unread bytes causes a
	// "file already closed" scan error and drops lines. When ctx is cancelled,
	// exec.CommandContext kills the subprocess, which closes its stdout write
	// end; sc.Scan() then returns false naturally — no explicit ctx.Done()
	// select case needed here.
	go func() {
		defer close(p.stdoutDone) // signal Wait goroutine: safe to call cmd.Wait()
		defer close(p.stdoutLines)
		sc := bufio.NewScanner(stdout)
		sc.Buffer(make([]byte, 4096), 1<<20) // 1 MiB cap per line
		for sc.Scan() {
			line := make([]byte, len(sc.Bytes()))
			copy(line, sc.Bytes())
			// Send line to consumer, or discard if context is done.
			// Using ctx.Done() here ONLY as a discard path — NOT as an early
			// return. We continue scanning until sc.Scan() returns false (EOF
			// or error). This ensures all stdout is consumed before stdoutDone
			// closes, satisfying the cmd.Wait() ordering constraint.
			select {
			case p.stdoutLines <- line:
			case <-ctx.Done():
				// Consumer is gone; drain remaining stdout to unblock the
				// subprocess and reach EOF — then fall through to the loop.
			}
		}
		if err := sc.Err(); err != nil && !errors.Is(err, io.EOF) {
			log.Printf("bot stdout scan error: %v", err)
		}
	}()

	// Wait for process exit; surface on ExitCh.
	// We must not call cmd.Wait() until the scan goroutine has finished
	// reading stdout — cmd.Wait() closes the StdoutPipe, which would
	// truncate any buffered-but-unread output. Block on stdoutDone first.
	go func() {
		<-p.stdoutDone
		err := cmd.Wait()
		select {
		case p.exitCh <- err:
		default:
		}
		close(p.exitCh)
	}()

	return p, nil
}

// Pid returns the bot's OS pid.
func (p *BotProcess) Pid() int {
	if p == nil || p.cmd == nil || p.cmd.Process == nil {
		return 0
	}
	return p.cmd.Process.Pid
}

// Lines returns the stdout-lines channel. Each value is one NDJSON line from
// the bot (minus the trailing newline).
func (p *BotProcess) Lines() <-chan []byte { return p.stdoutLines }

// WriteStdin writes a single NDJSON line (with trailing newline) to the bot's
// stdin. Serialized across callers so concurrent IPC commands don't interleave
// bytes on the pipe. Returns io.ErrClosedPipe if Stop was called or the child
// has exited.
func (p *BotProcess) WriteStdin(line []byte) error {
	if p == nil {
		return fmt.Errorf("bot process is nil")
	}
	p.stdinMu.Lock()
	defer p.stdinMu.Unlock()
	if p.stdin == nil {
		return io.ErrClosedPipe
	}
	if len(line) == 0 || line[len(line)-1] != '\n' {
		line = append(line, '\n')
	}
	_, err := p.stdin.Write(line)
	return err
}

// ExitCh returns the bot exit channel. Fires once with the Wait() error.
func (p *BotProcess) ExitCh() <-chan error { return p.exitCh }

// Stop best-effort terminates the bot. Idempotent.
func (p *BotProcess) Stop() {
	p.stopOnce.Do(func() {
		// Close stdin first so the bot's IPC reader loop sees EOF and shuts
		// down cleanly before SIGINT reaches the runtime.
		p.stdinMu.Lock()
		if p.stdin != nil {
			_ = p.stdin.Close()
			p.stdin = nil
		}
		p.stdinMu.Unlock()
		if p.cmd == nil || p.cmd.Process == nil {
			return
		}
		_ = p.cmd.Process.Signal(os.Interrupt)
	})
}

// tee copies src to dst, prefixing each line. Used for stderr pass-through.
func tee(src io.Reader, dst io.Writer, prefix string) {
	sc := bufio.NewScanner(src)
	sc.Buffer(make([]byte, 4096), 1<<20)
	for sc.Scan() {
		fmt.Fprintf(dst, "%s%s\n", prefix, sc.Text())
	}
}
