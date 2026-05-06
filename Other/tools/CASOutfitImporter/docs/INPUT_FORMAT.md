# Input format

What a TS1 skin folder must contain for the importer to process it. The two
folders under `/srv/dev_projects/personal/FreeSO/test_skin_head/` are
working examples.

## Required files per skin folder

A skin folder must contain exactly one of each:

| File | Required | Purpose |
|---|---|---|
| `xskin-<name>-<BONE>-<GROUP>.skn` | yes | TS1 mesh in plain-text BMF format |
| `<name>.cmx` | optional | TS1 metadata; the importer reads it for sanity but doesn't use it directly |
| `*.bmp` | yes (≥ 1) | 8-bit indexed Windows BMP-3 textures, one per skin tone |

The base file `<name>` (without prefixes/suffixes) is just a label — pass
whatever short id you like via `--name` on the command line. The internal
references in `.skn` and `.cmx` use the literal name on the first line of the
`.skn` file.

## The `.skn` mesh

Plain text, CRLF-terminated. Layout (token-per-line in the simple case;
token streams are flattened across whitespace by the parser):

```
xskin-b076fafit_roundedflares-PELVIS-BODY    ← skinName line 1
x                                             ← textureName line 2 (always "x")
19                                             ← bone count
R_LEG                                          ← bone name 1
PELVIS                                         ← bone name 2
…
572                                            ← face count
4 3 2                                          ← face triangle (3 vertex indices)
3 16 2
…
19                                             ← bone-binding count
0 0 23 0 12                                    ← bonebinding: idx, firstReal, realCount, firstBlend, blendCount
…
406                                            ← real-vertex count
0.536488 0.453660                              ← UV pair, one per real vertex
…
106                                            ← blend-vertex count
44 9937                                        ← blend datum: otherVertex, weight*0x8000
…
512                                            ← realVertexCount2 = real + blend totals
0.280432 -0.204246 -0.22161 0.667084 -0.721269 -0.186465   ← position(xyz) + normal(xyz)
…                                              ← then realVertexCount blend pos+normals
```

The importer reads this through a self-contained parser
(`Formats/SknReader.cs`) that mirrors `FSO.Vitaboy.Mesh.Read(io, bmf=true)`
in the FreeSO source.

### Filename convention dictates the bone binding

The `<BONE>` segment in `xskin-<name>-<BONE>-<GROUP>.skn` is the bone the
mesh attaches to. The importer parses it out and stores it on every binding
record it generates:

| Filename | Primary bone | Use |
|---|---|---|
| `xskin-c704fa_realeris-HEAD-HEAD.skn` | `HEAD` | head outfit |
| `xskin-b076fafit_roundedflares-PELVIS-BODY.skn` | `PELVIS` | body outfit |

If the filename pattern is missing the `-BONE-GROUP` suffix, the importer
falls back to `HEAD` for `--type head` and `PELVIS` for `--type body`.

## The `.cmx` metadata

Plain text, version 300 in our test inputs. The importer doesn't use this
directly — it's a sibling reference in the original TS1 distribution. Safe
to omit if you only have the `.skn`+`.bmp`. Keeping it doesn't hurt.

## The `.bmp` textures

All texture files in the folder are scanned. Selection is by filename
substring matching:

| Token in filename | Slot |
|---|---|
| `lgt` (and not `pale`) | light skin tone |
| `med` | medium |
| `drk` | dark |

Bodies typically ship all three; heads often ship only `lgt`. If `med` or
`drk` is missing, the importer **synthesizes** them by multiplying the
light-tone RGB by 0.78 (medium) and 0.55 (dark). See
`Imaging/TextureBuilder.cs:SynthesizeTone`. Replacing the synthesized
`_med.png`/`_drk.png` files in the staged output with hand-painted versions
gives much better results.

### Format requirements

- Windows BMP-3 with `BITMAPINFOHEADER` (40-byte header)
- 8-bit indexed colour (one of 24-bit or 32-bit also accepted)
- No RLE compression — only `BI_RGB` (compression field = 0)
- Any size — head textures are typically 128×128, body textures 256×256

The importer's reader at `Imaging/BmpReader.cs` handles 8/24/32-bit BMPs and
will reject compressed variants explicitly.

### Magenta as the transparent key

Body textures use `(R=255, G=0, B=255)` — full magenta — as a chroma key for
transparency. The importer applies this automatically for `--type body` and
emits PNG alpha=0 for matching pixels. Heads do not key magenta (rendering
issue with eyes/teeth that happen to use related colors); the alpha channel
is solid for head PNGs.

## Two reference inputs in the repo

```
test_skin_head/
├── b076fa_k8groupie/                  # body outfit
│   ├── xskin-b076fafit_roundedflares-PELVIS-BODY.skn
│   ├── b076fafit_roundedflares.cmx
│   ├── b076fafitlgt_k8groupie.bmp     # light  (256×256)
│   ├── b076fafitmed_k8groupie.bmp     # medium (256×256)
│   ├── b076fafitdrk_k8groupie.bmp     # dark   (256×256)
│   └── b076fafitlgt_k8groupie_pale.bmp  # ignored (the `pale` substring excludes it)
└── c704fa_k8suzalgt/                  # head
    ├── xskin-c704fa_realeris-HEAD-HEAD.skn
    ├── c704fa_realeris.cmx
    └── c704falgt_k8suzalgt.bmp        # light only (128×128)
```

Both are processable end-to-end in their current form. The head triggers the
medium/dark synthesis fallback because no `med`/`drk` BMPs are present.

## What's NOT supported in inputs today

- **Compressed BMPs** (RLE-encoded) — explicit error from `BmpReader`
- **TGA / DDS / PSD textures** — only BMP is read
- **`.bmf` (binary BMF) meshes** — only the text-format `.skn` variant
- **Multiple meshes per outfit** (e.g., separate hat + body meshes glued
  together) — one `.skn` per folder; outfits expect a single mesh group
- **Animation `.cfp` / `.bcf` clips** — out of scope; this tool is for
  static-skin import, not animation packs
- **Skeletons other than the stock TSO adult skeleton** — the bones the
  `.skn` references must exist in the engine's loaded skeleton (PELVIS,
  R_LEG, HEAD, NECK, SPINE, etc.)