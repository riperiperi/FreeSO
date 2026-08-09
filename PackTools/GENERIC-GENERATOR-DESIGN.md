# Generic Small-Object Generator — Design Draft

Status: design only, no code. Companion to `ART-PIPELINE-DESIGN.md`/`ART-PIPELINE-CALIBRATION.md` (the render pipeline these findings all depend on) and the five furniture generators in `PackTools/FSO.PackCompiler/ArtGen/` (chair, table, bed, lamp, storage).

## 0. The gap this addresses

`PackTools/examples/` has five sample objects: gossip gnome, fortune cat, mood lamp, pet rock, wishing well. Only the mood lamp fits a furniture category. The other four are exactly the kind of whimsical one-off prop this product is *for* — and none of them can be generated today. Anything outside the five named categories falls back to `clone_from_guid`, which means borrowed EA art, which means it can't ship in a browser build. Five named generators don't cover the actual request distribution, and they never will by adding more names one at a time — gnomes and teapots and wishing wells are each their own category with N=1.

This doc asks: what's the generator for *everything else*, and is a generator even the right answer.

## 1. Prior art

Searched before designing, per house rule (never reinvent the wheel, applies across domains). Six things worth naming, three worth taking from directly.

### 1a. CGA shape grammars (Müller et al., "Procedural Modeling of Buildings," SIGGRAPH 2006)
The canonical academic "shape grammar" reference — rule-based recursive subdivision/extrusion of a mass into architectural detail (facades, windows, roofs), used to generate whole cities and reconstruct sites like Pompeii at scale. **Wrong domain for us**: it's built around subdividing a large extruded volume into repeated architectural elements, not composing a small handful of primitives into a single tabletop-scale prop. Noting it because it's the thing "shape grammar" usually means, and it's not what we need — ruling it out is itself useful.

### 1b. Spore's Creature Creator (rigblocks + metaballs)
The closest real precedent for *this* problem. Spore's editor used two techniques together: metaballs (implicit surfaces that blend smoothly where they overlap) for the organic body/limb mass, and hand-authored "rigblocks" — individual pre-made parts (hands, mouths, spikes) that snap onto the metaball skeleton and get scaled/stretched within designer-defined limits. **Directly relevant**: it's a working example of "assemble a recognizable creature from a small vocabulary of parts placed by a non-artist," which is structurally our problem (an LLM standing in for the player). Two things to take: (1) a small fixed vocabulary of part *kinds* goes further than expected when combined with freeform placement/scale, and (2) purely organic continuous blending (true metaballs) is expensive and not obviously necessary — Spore needed it for smooth creature skin; our objects render at 20-100px and are flat-shaded, so a joint between two primitives just needs to not have a visible gap, not blend seamlessly.

### 1c. Kit-bashing / parametric prop-kit packs (industry-standard, not academic)
Kenney.nl's prop packs and the Unreal Marketplace's "Procedural Asset Creator" (which ships individual parametric generators for arrows, books, bows, buckets, chests, clocks, columns, fences, hourglasses, lamps, potions, scrolls, shields, swords, vases, wells) are the working industry pattern for exactly the five-generators-and-counting approach we already have. **This validates, not replaces, the current approach**: named per-category generators are how the industry actually solves "I need a well" — a hand-tuned generator per common prop category is not a stopgap waiting to be obsoleted by something more general, it's a legitimate end state for categories that recur often enough to justify the tuning (proportion rules, construction logic like the chair's tapered-leg math). The gap is specifically the *long tail* — categories that will each appear once.

### 1d. Part-based primitive shape representation (2024-2026 research: PASTA, PartCrafter, OmniPart, and the broader superquadrics-as-parts literature)
Recent shape-generation research has converged on representing an arbitrary 3D object as a short *sequence* of simple parametric primitives (cuboids, superquadrics, cylinders — typically 5-20 of them) with per-part pose, scale, and sometimes color, generated autoregressively by a trained transformer. **This is the load-bearing citation for this design**: it's independent, recent, mainstream confirmation that "represent an arbitrary small object as a handful of primitive parts with position/size/color" is not a naive simplification — it's the representation the field currently prefers, specifically *because* it's compact and controllable. The only substitution we're making is using an LLM prompted for structured JSON instead of a trained shape-transformer to produce the part sequence — which the next item confirms is itself an established substitution, not a stretch.

### 1e. LLM-driven procedural content generation (survey: "Procedural Content Generation in Games: A Survey with Insights on Emerging LLM Integration," 2024; general finding repeated across current PCG-with-LLM work)
Current PCG research explicitly treats "modern LLMs reliably output valid JSON, making them drop-in replacements for procedural generation systems that expect structured data" as a working assumption, not a novel claim needing its own validation. **Directly applicable**: our authoring agent is already an LLM turning plain language into pack JSON (that's what `FSO.AgentBridge` does for the whole pack format, not just appearance). Asking it to also emit a small parts-list for `appearance.generated` is the same mechanism it already uses for everything else in the pack, not a new capability.

### 1f. Ruled out explicitly
- **L-systems** (branching/recursive grammars, used for plants and trees) — our objects aren't organic branching structures; a wishing well and a gossip gnome have nothing in common with a fern.
- **Wave Function Collapse** — operates at the tile/texture level (constraint-propagation over a grid), used for level layouts and textures, not single small-object silhouette generation. Not the right tool for "what does one object look like."

## 2. Recommended approach

Add a fifth kind of `appearance.generated`: `generator: "primitives"`. Where the four furniture generators each expose a *fixed, named* parameter set (`seat_width`, `back_angle_deg`, ...), this one exposes a small, general parts-list:

```json
{
  "generated": {
    "generator": "primitives",
    "params": {
      "parts": [
        { "type": "cone",     "pos": [0, 0.85, 0], "size": [0.35, 0.4, 0.35], "color": [180, 40, 40] },
        { "type": "sphere",   "pos": [0, 0.55, 0], "size": [0.3, 0.3, 0.3],   "color": [230, 195, 150] },
        { "type": "cylinder", "pos": [0, 0, 0],    "size": [0.28, 0.55, 0.22], "color": [40, 80, 160] },
        { "type": "sphere",   "pos": [0, 0.35, 0.22], "size": [0.22, 0.18, 0.15], "color": [245, 245, 245] }
      ],
      "symmetric": false
    }
  }
}
```

(That example: cone hat, sphere head, tapered cylinder body, ellipsoid beard — a garden gnome, in four parts.)

### What's new in ArtGen
- **`PartsGenerator.cs`** — new file, same shape as the four existing generators: takes a validated `Params` (a list of parts), builds a `Mesh`. No new rendering machinery — reuses `Mesh.AddBox`/`AddCylinder` as-is.
- **One new primitive: `Mesh.AddSphere`/ellipsoid.** Box and tapered-cylinder already exist (added this session for the lamp/pedestal-table work); sphere is the third leg of a minimal-but-sufficient vocabulary (box = rectilinear mass, cylinder/cone = tapered rotational mass, sphere/ellipsoid = rounded mass — between them, most small everyday objects decompose into a handful of these). Implementation is the same lat/long-banded-quads-plus-polar-cap pattern already used for the cylinder's end caps, so it's a small, low-risk addition consistent with what's there.
- **Validation** follows the existing generators' pattern exactly (`PackParser.ParseGeneratedAppearance`'s per-generator branch, `ReqString`/`OptDouble`/positive-dimension checks, `Done()` rejecting unknown fields) — plus generator-specific rules: unknown `type` per part is a loud error (mirrors `UnknownGenerator_IsError`), a part count cap (recommend 16 — see §3 on why more doesn't help), and `symmetric: true` only permitted when every part is centered on the vertical axis (else it's a silent lie about the render).
- **Assembly**: `symmetric: false` (the default — most whimsical props face a direction, unlike the round pedestal table or lamp) uses the existing `SpriteAssembler.BuildIff`; `symmetric: true` (a barrel, a rock, a well viewed from directly above) uses `SymmetricAssembler.BuildIff`, exactly the choice `TableGenerator`/`LampGenerator` already make internally — here it's just author-declared instead of shape-inferred, since inferring true rotational symmetry from an arbitrary part list reliably is a harder problem than asking the author (LLM) to say so.

### Description → parameters
No new ML/inference component. The authoring agent already converts plain language into full pack JSON (behavior trees, interactions, strings — see `gossip-gnome.json`); this adds one more thing it emits directly as structured JSON, exactly per §1e. What it needs that it doesn't have yet: a **small prompted vocabulary and a handful of few-shot examples** (gnome → cone+sphere+cylinder+sphere as above; pet rock → single irregular sphere; wishing well → squat cylinder base + two thin cylinder posts + cone roof) so the agent has a mental model of "prop = 3-8 primitives, not a mesh description." This is a prompt-engineering deliverable, not a code deliverable — out of scope for this doc but flagged as the actual hard part.

## 3. Honest assessment: what this can and can't cover

Render-scale ground truth from `ART-PIPELINE-CALIBRATION.md` applies here even harder than to furniture: **silhouette, proportion, and color are everything; nothing else survives.** Walking the four example objects against that:

- **Pet rock**: trivial. One deformed/irregular sphere, one earth-tone color. A generic composer handles this better than a dedicated generator would — there's nothing category-specific to tune.
- **Wishing well**: good fit. Squat cylinder + two support posts + cone/wedge roof is exactly a small primitive composition, and it's rotationally near-symmetric (`symmetric: true` candidate) — cheaper to render, too.
- **Gossip gnome**: workable, with a caveat. A pointy-hat + round-head + body silhouette in the right colors (red hat, white beard, blue body — the real-world garden-gnome color code) is very achievable and will likely read as "a gnome-shaped garden ornament" even at Far zoom, because color is doing most of the identification work here, which is exactly what the render scale rewards. It will **not** carry fine gnome-specific detail (a face, a distinct beard texture) — but per our own finding, that detail wouldn't survive rendering anyway, named-generator or not.
- **Fortune cat**: the honest limit case. Its recognizability depends partly on a *specific small gesture* (one raised paw) and iconographic color/detail (gold coin, red collar) that are meaningful at human-eye scale but marginal at 20-40px. A primitive composition (sphere head, two cone ears, ellipsoid body, small raised-paw stub) will very plausibly read as "a seated animal figurine" at Near zoom and probably won't specifically read as "beckoning cat" at Far zoom. This is not a limitation of the composer design — a hand-tuned `FortuneCatGenerator` would hit the exact same ceiling, because the ceiling is the render resolution, not the generation method.

**Net assessment**: build it. The primitive-composer approach is well-supported by prior art (not a novel risky idea), reuses infrastructure that already exists, and covers the pet-rock/wishing-well end of the distribution cleanly and the gnome/fortune-cat end at "recognizable as the right *kind* of thing" rather than "recognizable as *that specific* thing" — which matches what the render pipeline can deliver regardless of generation method. The alternative framing this doc was asked to consider honestly — "generation should stay furniture-only, whimsical objects should keep cloning base art" — is not the right call: it would permanently block exactly the object category (whimsical one-offs) that's the actual product, for a fidelity ceiling that a hand-authored generator per object would hit too.

## 4. Relationship to the five existing generators

Keep them separate; don't collapse chair/table/bed/lamp/storage into the primitive composer as a common code path. A named generator encodes construction knowledge a flat parts-list would otherwise push onto every individual call site — the chair's tapered-leg math, the table's pedestal-vs-four-leg branching, the storage generator's "shelf boards need to be several pixels thick or they vanish" lesson from this session. That knowledge belongs in code once, not in every LLM-authored parts-list. The primitive composer is for the long tail where no such reusable construction knowledge exists yet (because each object is its own category) — the two approaches serve different parts of the request distribution, not a maturity ladder from one to the other.

## 5. Open questions

- **A profile-revolve ("lathe") primitive** — rotate a 2D radius-vs-height curve around the vertical axis — would cover teapots, vases, urns, mushroom caps, and the well's own roof far more naturally than faceted boxes/cylinders/cones, and is the single highest-leverage addition beyond the three primitives proposed here. Deliberately **not** proposed as v1: it needs a curve input shape (an array of `[height, radius]` control points) that's a bigger surface than a box/cylinder/sphere's fixed dimensions, and this doc is scoped to "design first, don't build." Worth a follow-up design pass once the three-primitive version has real mileage.
- **Part count ceiling** — recommended 16 above as a starting guess (comfortably more than any of the four example objects need), not derived from a render-scale measurement the way the furniture proportions were. Should get an empirical check once a few dozen real generated objects exist: does anything past ~10 parts actually change the silhouette, or does it just add invisible-at-render-scale complexity for no benefit (same lesson as the storage generator's sub-pixel shelf boards, one level up)?
- **Shared palette coherence** — every generator so far (including this one) picks colors per-object, freely. Fine for one-off props; if enough generated objects ship into one world, a shared palette constraint might matter for visual coherence the way `ART-PIPELINE-DESIGN.md` §7 flagged for furniture. Not blocking, not designed here.
- **Attachment detail that isn't a rigid primitive** (the well's bucket-on-a-rope, a raised paw with articulated fingers) — v1 answer is "omit it, per the render-scale finding that fine detail doesn't survive anyway" (§3). Worth revisiting only if real generated objects come back reading as noticeably incomplete rather than just simplified.
- **Where this plugs into `appearance.generated`** — same dispatch mechanism as the four furniture generators (`PackParser.ParseGeneratedAppearance`'s per-`generator`-name branch, `PackBuilder`'s sibling `else if`), so it doesn't need new plumbing — just a fifth name once someone builds `PartsGenerator.cs` and `Mesh.AddSphere`.

## Sources

- [Procedural Modeling of Buildings (CGA shape) — ACM Transactions on Graphics](https://dl.acm.org/doi/10.1145/1141911.1141931)
- [CGA Shape grammar lecture notes, UPC](https://www.cs.upc.edu/~virtual/SGI/docs/1.%20Theory/Unit%2011.%20Procedural%20modeling/CGA%20shape%20grammar.pdf)
- [How The Spore Creature Creator Works — RemptonGames](https://remptongames.com/2022/08/07/how-the-spore-creature-creator-works/)
- [Metaballs for mesh generation? Spore Creature Creator — Godot Forum](https://forum.godotengine.org/t/metaballs-for-mesh-generation-spore-creature-creator/92992)
- [Development of Spore — Wikipedia](https://en.wikipedia.org/wiki/Development_of_Spore)
- [Procedural Asset Creator — Unreal Marketplace / Fab](https://www.unrealengine.com/marketplace/en-US/product/procedural-asset-creator)
- [Rethinking 3D Shape Generation: Diffusion over Superquadrics](https://arxiv.org/html/2606.08957)
- [OmniPart: Part-Aware 3D Generation with Semantic Decoupling and Structural Cohesion](https://arxiv.org/html/2507.06165v1)
- [Procedural Content Generation in Games: A Survey with Insights on Emerging LLM Integration](https://www.researchgate.net/publication/385107356_Procedural_Content_Generation_in_Games_A_Survey_with_Insights_on_Emerging_LLM_Integration)
