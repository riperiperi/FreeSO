#!/usr/bin/env python3
"""Patch KNI-built XNB effects for GLSL ES.

KNI's effect compiler injects a `#if GL_ARB_shader_texture_lod` preprocessor
block into the embedded GLSL. Desktop GL tolerates it; GLSL ES 3.0 rejects the
bare extension-name condition and the whole shader fails to compile, which in
the browser means Effect load throws and the lot view falls back to diamonds.

The directive comes from KNI's compiler, not our .fx source, so a CI rebuild
cannot remove it at source. This script neutralizes it in place with a
length-preserving rewrite (`#if GL_ARB_shader_texture_lod` -> `#if 0` padded
with spaces), so XNB internal offsets stay valid. Patched XNBs are committed;
re-run this only after rebuilding effects (see kni-effects-blazor.yml).

The injected block ends in an `#else` with correct GLSL ES fallbacks
(`#define texture2DGrad(a,b,c,d) texture(a,b)` …), so killing both the `#if`
and the `#elif` conditions routes every ES compile to the branch KNI intended
for ES anyway. Patching only the `#if` (the original hand patch) works on
ANGLE but not SwiftShader, whose preprocessor also rejects the undefined
macro in the `#elif`.

Usage: patch_glsl_es.py <file.xnb> [more.xnb ...]
Exit code 0 if all files end up patched (already-patched files are fine).
"""
import sys

PATCHES = [
    (b"#if GL_ARB_shader_texture_lod", b"#if 0"),
    (b"#elif GL_EXT_gpu_shader4", b"#elif 0"),
]


def patch(path: str) -> int:
    with open(path, "rb") as f:
        data = f.read()
    total = 0
    for needle, stub in PATCHES:
        replacement = stub + b" " * (len(needle) - len(stub))
        assert len(replacement) == len(needle)
        count = data.count(needle)
        if count:
            data = data.replace(needle, replacement)
            total += count
    if total == 0:
        print(f"{path}: already clean")
        return 0
    with open(path, "wb") as f:
        f.write(data)
    print(f"{path}: patched {total} occurrence(s)")
    return total


def main(argv):
    if len(argv) < 2:
        print(__doc__)
        return 2
    for path in argv[1:]:
        patch(path)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
