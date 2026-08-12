#!/usr/bin/env python3
"""Fake Archive city server for gateway/browser testing without booting FreeSO.

Sends a RequestClientSessionArchive (Aries type 2000) on every connect, exactly
like CityServer.ArchiveHandshake, then keeps the socket open. Wire format per
AriesProtocolEncoder.cs and IoBufferUtils.cs (PascalVLC = varint len + UTF-8).
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


def handshake_frame() -> bytes:
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
    header = struct.pack("<III", 2000, 0, len(payload))
    return header + payload


def serve(port: int):
    frame = handshake_frame()
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind(("127.0.0.1", port))
    server.listen(4)
    print(f"fake city server on 127.0.0.1:{port}", flush=True)
    while True:
        conn, addr = server.accept()
        print(f"connect from {addr}, sending handshake", flush=True)
        conn.sendall(frame)
        # Hold the connection open like a real server; drain client bytes.
        threading.Thread(target=drain, args=(conn,), daemon=True).start()


def drain(conn):
    try:
        while conn.recv(4096):
            pass
    except OSError:
        pass


if __name__ == "__main__":
    serve(int(sys.argv[1]) if len(sys.argv) > 1 else 33101)
