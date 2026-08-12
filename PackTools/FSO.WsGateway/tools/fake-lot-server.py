#!/usr/bin/env python3
"""Fake lot server for gateway/browser testing (port 34101).

Lot path (unlike Archive city):
  connect → RequestClientSession (Aries type 22, empty body)
  ← type 21 (Unknown=39, 32-byte ASCII ticket) → HostOnlinePDU (0x001e)
  ← ClientOnlinePDU only (0x000a) → optional empty FSOVMTickBroadcast (Electron 7)

No Archive type 2000, no SetIgnoreList/SetInvincible.
"""
import socket
import struct
import sys
import threading


def aries_frame(packet_type: int, payload: bytes) -> bytes:
    return struct.pack("<III", packet_type, 0, len(payload)) + payload


def voltron_style_inner(subtype: int, body: bytes) -> bytes:
    return struct.pack(">HI", subtype, 6 + len(body)) + body


def voltron_frame(subtype: int, body: bytes) -> bytes:
    return aries_frame(0, voltron_style_inner(subtype, body))


def electron_frame(subtype: int, body: bytes) -> bytes:
    return aries_frame(1000, voltron_style_inner(subtype, body))


def request_client_session_frame() -> bytes:
    return aries_frame(22, b"")


def host_online_frame() -> bytes:
    body = struct.pack(">HHH", 0, 0x7FFF, 4096)
    return voltron_frame(0x001E, body)


def empty_tick_frame() -> bytes:
    """FSOVMTickBroadcast (7): Catchup bool + i32 Data length (BE), empty Data."""
    body = struct.pack(">Bi", 0, 0)  # Catchup=false, len=0
    return electron_frame(7, body)


def read_exact(conn, n: int) -> bytes:
    buf = bytearray()
    while len(buf) < n:
        chunk = conn.recv(n - len(buf))
        if not chunk:
            return bytes(buf)
        buf.extend(chunk)
    return bytes(buf)


def parse_inner_subtype(payload: bytes):
    if len(payload) < 6:
        return None
    subtype, _size = struct.unpack(">HI", payload[:6])
    return subtype


def handle_client(conn, addr):
    print(f"lot connect from {addr}, sending RequestClientSession (22)", flush=True)
    try:
        conn.sendall(request_client_session_frame())
        sent_host = False
        sent_tick = False
        while True:
            header = read_exact(conn, 12)
            if len(header) < 12:
                break
            ptype, _ts, size = struct.unpack("<III", header)
            payload = read_exact(conn, size) if size else b""
            if len(payload) < size:
                break
            print(f"  ← type {ptype}, {size} bytes", flush=True)

            if ptype == 21 and not sent_host:
                unknown = payload[318] if len(payload) > 318 else None
                # Password starts at 324 when Unknown!=40 (32 ASCII).
                ticket = payload[324:356].split(b"\x00", 1)[0].decode("ascii", "replace") if len(payload) >= 356 else ""
                user = payload[:112].split(b"\x00", 1)[0].decode("ascii", "replace") if len(payload) >= 112 else ""
                print(f"  type 21 Unknown={unknown} user={user!r} ticket={ticket!r} → HostOnline", flush=True)
                conn.sendall(host_online_frame())
                sent_host = True
            elif ptype == 0 and sent_host and not sent_tick:
                subtype = parse_inner_subtype(payload)
                print(f"  voltron 0x{subtype:04x}" if subtype is not None else "  short voltron", flush=True)
                if subtype == 0x000A:
                    print("  ClientOnline → empty FSOVMTickBroadcast", flush=True)
                    conn.sendall(empty_tick_frame())
                    sent_tick = True
    except OSError as e:
        print(f"  lot disconnect {addr}: {e}", flush=True)
    finally:
        try:
            conn.close()
        except OSError:
            pass


def serve(port: int):
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind(("127.0.0.1", port))
    server.listen(4)
    print(f"fake lot server on 127.0.0.1:{port}", flush=True)
    while True:
        conn, addr = server.accept()
        threading.Thread(target=handle_client, args=(conn, addr), daemon=True).start()


if __name__ == "__main__":
    serve(int(sys.argv[1]) if len(sys.argv) > 1 else 34101)
