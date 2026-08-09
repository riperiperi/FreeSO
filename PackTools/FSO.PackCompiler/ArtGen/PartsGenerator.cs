using System.Collections.Generic;

namespace FSO.PackCompiler.ArtGen
{
    /// <summary>
    /// Generic small-object generator: assembles a mesh from an author-supplied list of
    /// primitive parts (box/cylinder/cone/sphere), per GENERIC-GENERATOR-DESIGN.md. Complements
    /// the five named furniture generators rather than replacing them — this is for the long
    /// tail of one-off whimsical props (a gnome, a pet rock, a wishing well) that don't share
    /// enough construction logic to justify a dedicated generator of their own.
    ///
    /// Each part's Pos is its geometric CENTER regardless of type (including cylinder/cone,
    /// even though Mesh.AddCylinder itself takes a base center) — a single uniform contract
    /// is easier for an LLM author to reason about than per-type placement conventions.
    /// </summary>
    public static class PartsGenerator
    {
        public class Part
        {
            public string Type; // "box" | "cylinder" | "cone" | "sphere"
            public Vec3 Pos;    // center of the part, always — see class remarks
            public Vec3 Size;   // box: (width,height,depth). cylinder: (radiusBottom,height,radiusTop).
                                // cone: (radiusBottom,height,_unused, forced radiusTop=0). sphere: (radiusX,radiusY,radiusZ).
            public (byte, byte, byte) Color;
        }

        public class Params
        {
            public List<Part> Parts = new List<Part>();

            // Only valid when every part is centered on the vertical (Y) axis — the caller
            // (parser layer) is responsible for that check, same as it validates everything
            // else about a Params object before Build() is trusted with it. True means this
            // object renders 3 frames instead of 12 via SymmetricAssembler.
            public bool Symmetric = false;
        }

        /// <summary>
        /// Builds the mesh. Throws on the two ways a parts-list silently renders nothing
        /// instead of failing loud (the recurring bug class in this pipeline): an unknown
        /// part type, and a non-positive size component (a degenerate/zero-volume part).
        /// Does NOT enforce a part-count ceiling — that number should come from measuring
        /// what actually stays legible at render scale once this is wired up and exercised
        /// with real objects, not from a guess baked into this generator (see
        /// GENERIC-GENERATOR-DESIGN.md §5) — so a parser-layer cap, if any, belongs upstream
        /// of this call, not here.
        /// </summary>
        public static Mesh Build(Params p)
        {
            var mesh = new Mesh();

            for (int i = 0; i < p.Parts.Count; i++)
            {
                var part = p.Parts[i];

                // "cone" only uses Size.X/Size.Y (radiusBottom/height) — Size.Z is ignored
                // (radiusTop is forced to 0), so it's exempt from the positive-Z check the
                // other three types need.
                var checkZ = part.Type != "cone";
                if (part.Size.X <= 0 || part.Size.Y <= 0 || (checkZ && part.Size.Z <= 0))
                    throw new System.ArgumentException(
                        $"part {i} (\"{part.Type}\"): size must be > 0 in each dimension used by this type, got ({part.Size.X}, {part.Size.Y}, {part.Size.Z})");

                switch (part.Type)
                {
                    case "box":
                        mesh.AddBox(part.Pos, part.Size, part.Color);
                        break;

                    case "cylinder":
                    {
                        var radiusBottom = part.Size.X; var height = part.Size.Y; var radiusTop = part.Size.Z;
                        var baseCenter = new Vec3(part.Pos.X, part.Pos.Y - height / 2, part.Pos.Z);
                        mesh.AddCylinder(baseCenter, height, radiusBottom, radiusTop, part.Color);
                        break;
                    }

                    case "cone":
                    {
                        var radiusBottom = part.Size.X; var height = part.Size.Y;
                        var baseCenter = new Vec3(part.Pos.X, part.Pos.Y - height / 2, part.Pos.Z);
                        mesh.AddCylinder(baseCenter, height, radiusBottom, 0, part.Color);
                        break;
                    }

                    case "sphere":
                        mesh.AddSphere(part.Pos, part.Size.X, part.Size.Y, part.Size.Z, part.Color);
                        break;

                    default:
                        throw new System.ArgumentException(
                            $"part {i}: unknown part type \"{part.Type}\" (expected box, cylinder, cone, or sphere)");
                }
            }

            return mesh;
        }
    }
}
