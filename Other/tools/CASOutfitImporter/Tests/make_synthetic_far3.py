#!/usr/bin/env python3
"""
Build a tiny FAR3 archive containing a hand-crafted wedding_female.col with
several fake "vanilla" entries. Used to test --source-collection round-tripping
without needing the user's actual installed game.

FAR3 v3 layout (little-endian throughout):
  8B "FAR!byAZ"
  u32 version=3
  u32 manifestOffset
  ... raw entry payloads ...
  manifestOffset:
    u32 numFiles
    per entry: u32 decompSize, u24 compSize, u8 dataType, u32 dataOffset,
               u8 isCompressed, u8 accessNumber, u16 filenameLength,
               u32 typeId, u32 fileId, ASCII filename

Collection (.col) v1 (big-endian):
  i32 count
  count × (i32 index, u32 fileId, u32 typeId)
"""

import struct
import sys
from pathlib import Path

VANILLA = [
    # (index, fileId, typeId) — fake vanilla wedding outfits.
    (0, 0xAAAA0001, 0x504F5446),
    (1, 0xAAAA0002, 0x504F5446),
    (2, 0xAAAA0003, 0x504F5446),
    (3, 0xAAAA0004, 0x504F5446),
    (4, 0xAAAA0005, 0x504F5446),
]


def build_collection(entries):
    """Big-endian Collection format."""
    buf = struct.pack('>i', len(entries))
    for idx, fid, tid in entries:
        buf += struct.pack('>iII', idx, fid, tid)
    return buf


def build_far3(entries):
    """Wraps a list of (filename, payload bytes) into an uncompressed FAR3."""
    HEADER_SIZE = 8 + 4 + 4
    out = bytearray()
    out += b'FAR!byAZ'
    out += struct.pack('<I', 3)            # version
    out += struct.pack('<I', 0)            # placeholder for manifestOffset

    # Write each payload, recording its offset.
    placed = []
    for name, payload in entries:
        offset = len(out)
        out += payload
        placed.append((name, payload, offset))

    # Manifest starts here.
    manifest_offset = len(out)
    out += struct.pack('<I', len(placed))
    for name, payload, offset in placed:
        decomp_size = len(payload)
        comp_size = decomp_size & 0xFFFFFF       # 24-bit
        out += struct.pack('<I', decomp_size)
        out += bytes([comp_size & 0xFF, (comp_size >> 8) & 0xFF, (comp_size >> 16) & 0xFF])
        out += bytes([0x00])                     # dataType = raw
        out += struct.pack('<I', offset)
        out += bytes([0x00])                     # isCompressed = no
        out += bytes([0x00])                     # accessNumber
        name_b = name.encode('ascii')
        out += struct.pack('<H', len(name_b))
        out += struct.pack('<I', 0x504F5446)     # typeId of the .col entry — arbitrary
        out += struct.pack('<I', 0xC0110001)     # fileId — arbitrary
        out += name_b

    # Patch manifestOffset.
    out[12:16] = struct.pack('<I', manifest_offset)
    return bytes(out)


def main():
    out_path = Path(sys.argv[1] if len(sys.argv) > 1 else 'wedding_synthetic.dat')
    col = build_collection(VANILLA)
    far = build_far3([('wedding_female.col', col)])
    out_path.write_bytes(far)
    print(f"wrote {out_path} ({len(far)} bytes; {len(VANILLA)} vanilla entries inside)")


if __name__ == '__main__':
    main()