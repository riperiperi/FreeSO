# Catalog Sourcing — Licensing & Import Research

**Status: research, plus one experiment that partly overturned it. See "Correction" below before acting on the recommendation.**

Question asked: which permissive sources have furniture at usable quality, and **is importing meshes actually cheaper than tuning generator parameters?**

## Headline: the renderer's shape decides this, and it argues against importing

`FSO.PackCompiler/ArtGen/Mesh.cs` is a **flat list of faces, each carrying a single RGB colour**. There are no UVs, no texture sampling, and no vertex normals — `AddQuad`/`AddFace` compute one outward normal per face and shade flat.

That is the whole argument, and it isn't a licensing point:

- An imported mesh's quality advantage lives almost entirely in its **textures and smooth shading**. Our pipeline has neither. Importing a well-made sofa means flattening it to per-face colours, at which point it looks much like a parametric sofa with the same silhouette.
- So the import work isn't "load a mesh" — it's **load a mesh, triangulate it, and invent a per-face colour** (bake from texture, or read material colours, or average). That last step is where imported assets quietly become mediocre, and it's a step generators skip entirely because they assign colour at construction.
- The output is a TSO sprite: **a handful of small bitmaps at fixed zooms and rotations**. Detail beyond a certain point is destroyed by quantisation regardless of source fidelity.

**Provisional recommendation: extend generators, don't build an importer.** Not on licensing grounds — on the grounds that our renderer cannot express what an imported mesh is *for*.

Two things would change that verdict, and they're worth checking before anyone accepts it:
1. If we add texture/UV support to the renderer, imports become genuinely better and this flips.
2. If a target style is organically shaped (ornate, curved, sculptural), generators may not reach it at any parameter setting, and import becomes the only path.

## Correction — the experiment contradicts the section above

I built a throwaway OBJ loader and pushed a real Kenney chair through **our own `Renderer` at the real `SpriteAssembler` zooms**, beside a generated chair at matched height. Three of the four premises above did not survive.

**1. The "invent a per-face colour" cost is zero for Kenney.** The kit contains **no texture maps at all** — `grep map_ *.mtl` returns nothing across all 140 pieces. Every model is flat-shaded with solid `Kd` material colours drawn from **15 shared materials** (`wood`, `woodDark`, `metal`, `carpet`, `fur`, `glass`, …). `Kd` *is* the per-face colour. The expensive step I named as the hidden cost of importing does not exist for this source.

**2. So the headline argument, while true in general, does not apply to the source I recommended.** "An imported mesh's advantage lives in textures and smooth shading, and we can't consume either" is correct for a normal PBR asset. Kenney has neither to begin with — `chair.obj` carries **6 normals for 340 faces**, i.e. axis-aligned flat shading, which is exactly our renderer's model. The import is close to **lossless**, not lossy.

**3. The importer is small.** ~120 lines for OBJ + MTL, correct on the first run. I had implied a significant build; it isn't one.

**4. What imported geometry buys is silhouette, and silhouette survives quantisation.** The generated chair is **31 faces** — a slab seat, a slab back, four tapered legs. The Kenney chair is **340**, and spends them on a ladder-back with *gaps between the slats*, turned legs, and a separately-coloured cushion. Holes in an outline read at any resolution, so that detail is still visible at **Far**, the zoom I proposed as the honest test. Flat colour does not destroy it.

The generated chair here uses **default `ChairGenerator.Params`** and is not tuned, so this is not a fair fight on proportion. But it is a fair fight on *vocabulary*: no parameter setting adds back-slats, because the generator has no notion of them. Each new silhouette feature costs generator code; Kenney ships ~140 pieces' worth already made.

**Revised recommendation: import Kenney for silhouette variety, keep the generators for parameterised variants.** They are complementary, not alternatives — generators give per-object variation (dimensions, palette) that a fixed mesh cannot, and imports give shape detail that a generator would need new code for.

**Kenney's 15 shared materials are the coherent palette** I called the real bottleneck. A kit built against one palette delivers set-coherence for free, which strengthens the "only a kit, never an aggregator" rule rather than weakening it.

**Unresolved: colour space.** `Kd` is nominally linear, but treating it as linear and converting to sRGB washes the wood out; using `Kd * 255` directly matches our hand-picked generator colours much better. Kenney's exporter most likely wrote sRGB already. **Use raw `Kd * 255` until someone confirms this**, and confirm it before a bulk import — it shifts every imported piece.

Nothing about the **licence gate** changes: CC0 verified in the kit's own `License.txt`, and the CC-BY and Sketchfab rules below stand.

Reproduce: `scratchpad/compare/` (throwaway, not committed — links the real ArtGen sources so it renders through the shipping pipeline rather than a reimplementation).

## What already exists

Six generators — `chair`, `table`, `bed`, `lamp`, `storage`, plus a generic `parts` builder with `box` / `cylinder` / `cone` / `sphere` primitives — with a full pipeline behind them (`Renderer`, `Camera`, `SpriteAssembler`, `SimpleQuantizer`, `SymmetricAssembler`, `PngWriter`).

That is most of a furniture catalog's *shape* vocabulary already. The gap to a West Elm-quality catalog is much less "we lack meshes" than **"we lack a coherent style applied across the set"** — palettes, proportions, materials, leg profiles held consistent across every piece.

## Sources, if we import anyway

Judged on whether they can furnish a **matching set**, not on individual pieces.

| Source | License | Matching set? | Notes |
|---|---|---|---|
| [Kenney Furniture Kit](https://kenney.nl/assets/furniture-kit) | **CC0** | **Yes — best candidate** | ~140 assets built as one kit: chairs, sofas, tables, bookcases, kitchen, bathroom. Coherent by construction. Ships OBJ + PNG textures. CC0 means no attribution obligation and full relicensing freedom. |
| [Eclair Furniture Kit (140 GLB)](https://eclair-assets.itch.io/furniture-kit-glb-pack-140-free-cc0-3d-models) | CC0 (stated) | Likely | Similar scale, GLB. Verify the CC0 claim at source before use. |
| [Poly Pizza](https://poly.pizza/search/furniture) | Mixed (CC0 + CC-BY) | Partially | Aggregator, largely Kenney-derived. **Per-model licenses differ** — must be checked individually, which makes bulk import risky. |
| [OpenGameArt](https://opengameart.org/content/furniture-kit) | Mixed | Rarely | Per-submission licensing, wide quality variance. Fine for one-offs, poor for a coherent set. |
| [Sketchfab CC0 filter](https://sketchfab.com/blogs/community/refine-downloadable-model-searches-with-new-license-filters/) | Mixed, filterable | **Avoid for bulk** | See below. |

**Kenney is the only source I'd consider for a set**, because it's CC0 *and* designed as a kit — the coherence requirement is the hard one, and a kit satisfies it by construction where an aggregator cannot.

## License hygiene — hard gate

The reason we generate our own art is **owning what we ship**. Importing something we can't relicense undoes that and blocks browser distribution, so this is a gate rather than a preference.

- **CC0 or equivalently permissive only.** CC-BY is *not* equivalent — attribution obligations propagate to everyone who redistributes a pack, which conflicts with the remix model in MCP-DESIGN.md §4.
- **Record provenance per asset** — source URL, license, retrieval date, and the license text as retrieved. A license claim that can't be evidenced later is the same as no license.
- **Anything ambiguous is excluded.** Not flagged, excluded.
- **Sketchfab specifically: don't bulk-import from the CC0 filter.** Per-model licenses vary, the filter reflects uploader-declared metadata, and uploaders can mislabel — including re-uploads of assets they didn't make. A filter is a search convenience, not a provenance guarantee. Individually vetted assets only, if at all.
- **Verify at the source of truth.** Poly Pizza and OpenGameArt mirror upstream work; check the original author's terms, not the aggregator's summary.

## What I'd do instead

1. **Style system before geometry.** A shared palette and proportion set applied across the six existing generators buys catalog *coherence*, which is what "West Elm-quality" actually means. Individual mesh fidelity is not the bottleneck — consistency is.
2. **Name the gaps.** List the catalog pieces the six generators genuinely cannot approximate. If that list is short, importing isn't needed. If it's long and organic, revisit.
3. **Only then** consider Kenney CC0 as a bridge for the named gaps, with full provenance records.

## Open questions I could not settle

- **Nobody has confirmed a generated object renders in the client.** Every claim about generated art quality — including this document's — rests on the sprite pipeline being correct end to end, which is verified up to the `.iff` and no further. Another session is one click from that screenshot. **It should land before anyone commits to a sourcing approach**, because if the pipeline has a rendering defect, source choice is moot.
- ~~I have not visually compared a generated chair against a Kenney chair rendered through our own quantiser.~~ **Done — see Correction above. It did settle it, against the recommendation this document originally made.**
- The `Kd` colour-space question above is open and affects every imported asset.

Sources: [Kenney Furniture Kit](https://kenney.nl/assets/furniture-kit) · [Eclair CC0 Furniture Kit](https://eclair-assets.itch.io/furniture-kit-glb-pack-140-free-cc0-3d-models) · [Poly Pizza](https://poly.pizza/search/furniture) · [OpenGameArt Furniture Kit](https://opengameart.org/content/furniture-kit) · [Sketchfab license filters](https://sketchfab.com/blogs/community/refine-downloadable-model-searches-with-new-license-filters/) · [Sketchfab License Agreement](https://sketchfab.com/licenses)
