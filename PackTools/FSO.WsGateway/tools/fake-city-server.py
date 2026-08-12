#!/usr/bin/env python3
"""Fake Archive city server for gateway/browser testing without booting FreeSO.

Flow:
  connect → RequestClientSessionArchive (2000)
  ← type 21 → HostOnlinePDU (Voltron 0x001e)
  ← ClientOnline burst → SetIgnoreListResponsePDU (0x0035)
  ← Electron ArchiveAvatarSelectRequest (30) → Response Success (31)
  ← Electron FindLotRequest (5) → FindLotResponse FOUND (6) with address

RSA is not validated (live Archive private key required for real auth).
"""
import socket
import struct
import sys
import threading


def vlc(s: str) -> bytes:
    data = s.encode("utf-8")
    n = len(data)
    out = bytearray()
    first = True
    while n > 0 or first:
        out.append((0x80 if n > 127 else 0) | (n & 0x7F))
        n >>= 7
        first = False
    return bytes(out) + data


def pascal_string(s: str) -> bytes:
    """Classic Voltron PascalString: 4-byte BE length with high bit on first byte, then 1 byte/char."""
    data = s.encode("latin-1", errors="replace")
    n = len(data)
    hdr = bytes([(0x80 | ((n >> 24) & 0x7F)), (n >> 16) & 0xFF, (n >> 8) & 0xFF, n & 0xFF])
    return hdr + data


def aries_frame(packet_type: int, payload: bytes) -> bytes:
    return struct.pack("<III", packet_type, 0, len(payload)) + payload


def voltron_style_inner(subtype: int, body: bytes) -> bytes:
    return struct.pack(">HI", subtype, 6 + len(body)) + body


def voltron_frame(subtype: int, body: bytes) -> bytes:
    return aries_frame(0, voltron_style_inner(subtype, body))


def electron_frame(subtype: int, body: bytes) -> bytes:
    """Aries type 1000 + same BE inner header as Voltron."""
    return aries_frame(1000, voltron_style_inner(subtype, body))


def handshake_frame() -> bytes:
    payload = (
        vlc("Kat's Archive City")
        + struct.pack("<i", 2)
        + vlc("archive-0.5.3-beta")
        + vlc("fake-server-key")
        + vlc("fake-nonce")
        + struct.pack("<H", 0)
        + struct.pack("<I", 1)
        + vlc("San Francisco")
        + vlc("city_0900")
    )
    return aries_frame(2000, payload)


def host_online_frame() -> bytes:
    body = struct.pack(">HHH", 0, 0x7FFF, 4096)
    return voltron_frame(0x001E, body)


def ignore_list_response_frame() -> bytes:
    body = struct.pack(">I", 0) + pascal_string("OK") + struct.pack(">I", 50)
    return voltron_frame(0x0035, body)


def avatar_select_ok_frame() -> bytes:
    return electron_frame(31, struct.pack(">H", 0))  # Success


def find_lot_ok_frame(lot_id: int = 1, address: str = "127.0.0.1:34101") -> bytes:
    body = (
        struct.pack(">H", 0)  # FOUND
        + struct.pack(">I", lot_id)
        + vlc("demo-ticket")
        + vlc(address)
        + vlc("1")
    )
    return electron_frame(6, body)


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
        return None, b""
    subtype, size = struct.unpack(">HI", payload[:6])
    body = payload[6:size] if size >= 6 else payload[6:]
    return subtype, body


def handle_client(conn, addr, handshake, host_online, ignore_resp, select_ok):
    print(f"connect from {addr}, sending handshake", flush=True)
    try:
        conn.sendall(handshake)
        sent_host = False
        sent_ignore = False
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
                print(f"  accepted type 21 (Unknown={unknown}), HostOnlinePDU", flush=True)
                conn.sendall(host_online)
                sent_host = True
            elif ptype == 0 and sent_host:
                subtype, _body = parse_inner_subtype(payload)
                print(f"  voltron 0x{subtype:04x}" if subtype is not None else "  short voltron", flush=True)
                if subtype == 0x0034 and not sent_ignore:
                    print("  SetIgnoreListResponsePDU", flush=True)
                    conn.sendall(ignore_resp)
                    sent_ignore = True
            elif ptype == 1000:
                subtype, body = parse_inner_subtype(payload)
                print(f"  electron 0x{subtype:04x} ({len(body)} B body)", flush=True)
                if subtype == 30:
                    print("  ArchiveAvatarSelectResponse Success", flush=True)
                    conn.sendall(select_ok)
                elif subtype == 5:
                    lot_id = struct.unpack(">I", body[:4])[0] if len(body) >= 4 else 1
                    print(f"  FindLotResponse FOUND lot={lot_id} → 127.0.0.1:34101", flush=True)
                    conn.sendall(find_lot_ok_frame(lot_id))
    except OSError as e:
        print(f"  disconnect {addr}: {e}", flush=True)
    finally:
        try:
            conn.close()
        except OSError:
            pass


def serve(port: int):
    handshake = handshake_frame()
    host_online = host_online_frame()
    ignore_resp = ignore_list_response_frame()
    select_ok = avatar_select_ok_frame()
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind(("127.0.0.1", port))
    server.listen(4)
    print(f"fake city server on 127.0.0.1:{port}", flush=True)
    while True:
        conn, addr = server.accept()
        threading.Thread(
            target=handle_client,
            args=(conn, addr, handshake, host_online, ignore_resp, select_ok),
            daemon=True,
        ).start()


if __name__ == "__main__":
    serve(int(sys.argv[1]) if len(sys.argv) > 1 else 33101)
