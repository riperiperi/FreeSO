#!/usr/bin/env python3
"""Fake Archive city server for gateway/browser testing without booting FreeSO.

On connect: sends RequestClientSessionArchive (Aries type 2000), like
CityServer.ArchiveHandshake.

On client RequestClientSessionResponse (type 21): accepts any well-formed frame
(does not validate RSA — that needs the live Archive private key) and replies
with a canned Voltron HostOnlinePDU (Aries type 0, subtype 0x001e).

On ClientOnlinePDU + SetIgnoreListPDU + SetInvinciblePDU (Voltron after HostOnline):
replies with SetIgnoreListResponsePDU (status 0, "OK", max 50), matching
SetPreferencesHandler on the real city.

Wire formats: AriesProtocolEncoder.cs, IoBufferUtils.cs (PascalVLC / PascalString),
HostOnlinePDU.cs, ClientOnlinePDU.cs, SetIgnoreListResponsePDU.cs.
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


def voltron_frame(subtype: int, body: bytes) -> bytes:
    """Aries type 0 + Voltron BE header (subtype u16, size u32 = 6+body)."""
    voltron = struct.pack(">HI", subtype, 6 + len(body)) + body
    return aries_frame(0, voltron)


def handshake_frame() -> bytes:
    # Non-PEM ServerKey on purpose: browser demo falls back to a placeholder
    # password; this server does not decrypt. Use a live Archive for real RSA.
    payload = (
        vlc("Kat's Archive City")      # Name
        + struct.pack("<i", 2)          # PlayerCount
        + vlc("archive-0.5.3-beta")     # VersionInfo
        + vlc("fake-server-key")        # ServerKey
        + vlc("fake-nonce")             # Nonce
        + struct.pack("<H", 0)          # ArchiveConfig (uint16 enum)
        + struct.pack("<I", 1)          # ShardId
        + vlc("San Francisco")          # ShardName
        + vlc("city_0900")              # ShardMap
    )
    return aries_frame(2000, payload)


def host_online_frame() -> bytes:
    """HostOnlinePDU 0x001e — Reserved=0, Version=0x7FFF, ClientBufSize=4096."""
    body = struct.pack(">HHH", 0, 0x7FFF, 4096)
    return voltron_frame(0x001E, body)


def ignore_list_response_frame() -> bytes:
    """SetIgnoreListResponsePDU 0x0035 — StatusCode=0, ReasonText=OK, Max=50."""
    body = struct.pack(">I", 0) + pascal_string("OK") + struct.pack(">I", 50)
    return voltron_frame(0x0035, body)


def read_exact(conn, n: int) -> bytes:
    buf = bytearray()
    while len(buf) < n:
        chunk = conn.recv(n - len(buf))
        if not chunk:
            return bytes(buf)
        buf.extend(chunk)
    return bytes(buf)


def parse_voltron_subtype(payload: bytes):
    if len(payload) < 6:
        return None
    subtype, size = struct.unpack(">HI", payload[:6])
    return subtype


def handle_client(conn, addr, handshake: bytes, host_online: bytes, ignore_resp: bytes):
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
                print(f"  accepted RequestClientSessionResponse (Unknown={unknown}), sending HostOnlinePDU", flush=True)
                conn.sendall(host_online)
                sent_host = True
            elif ptype == 0 and sent_host:
                subtype = parse_voltron_subtype(payload)
                print(f"  voltron subtype 0x{subtype:04x}" if subtype is not None else "  short voltron", flush=True)
                # Real client bursts ClientOnline (0x000a), SetIgnoreList (0x0034),
                # SetInvincible (0x0036). Reply once with SetIgnoreListResponse.
                if subtype == 0x0034 and not sent_ignore:
                    print("  sending SetIgnoreListResponsePDU", flush=True)
                    conn.sendall(ignore_resp)
                    sent_ignore = True
            # Keep draining so the browser can stay connected for inspection.
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
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind(("127.0.0.1", port))
    server.listen(4)
    print(f"fake city server on 127.0.0.1:{port}", flush=True)
    while True:
        conn, addr = server.accept()
        threading.Thread(
            target=handle_client,
            args=(conn, addr, handshake, host_online, ignore_resp),
            daemon=True,
        ).start()


if __name__ == "__main__":
    serve(int(sys.argv[1]) if len(sys.argv) > 1 else 33101)
