using System.Collections.Generic;

namespace FSO.PackCompiler.ArtGen
{
    /// <summary>One flat-shaded quad face: 4 coplanar world-space verts, wound so Normal is outward.</summary>
    public class Face
    {
        public Vec3[] Verts; // 4 corners, CCW when viewed from outside along Normal
        public Vec3 Normal;
        public (byte r, byte g, byte b) Color;

        public Face(Vec3[] verts, Vec3 normal, (byte, byte, byte) color)
        {
            Verts = verts;
            Normal = normal;
            Color = color;
        }
    }

    public class Mesh
    {
        public List<Face> Faces = new List<Face>();

        /// <summary>Adds a quad face, computing its outward normal from vert winding.</summary>
        public void AddQuad(Vec3 a, Vec3 b, Vec3 c, Vec3 dd, (byte, byte, byte) color)
        {
            var normal = Vec3.Cross(b - a, dd - a).Normalized();
            Faces.Add(new Face(new[] { a, b, c, dd }, normal, color));
        }

        /// <summary>Adds an N-vertex planar face (e.g. a polygon cap), computing its outward
        /// normal the same way AddQuad does. Verts must be coplanar and wound so the normal
        /// points outward.</summary>
        public void AddFace(Vec3[] verts, (byte, byte, byte) color)
        {
            var normal = Vec3.Cross(verts[1] - verts[0], verts[verts.Length - 1] - verts[0]).Normalized();
            Faces.Add(new Face(verts, normal, color));
        }

        /// <summary>
        /// Adds a cylinder/frustum (possibly tapered: radiusBottom != radiusTop) as a
        /// segments-sided prism — side quads plus optional flat N-gon end caps. At TSO's
        /// render scale a 12-16 segment prism reads as a smooth round shape (surface facets
        /// are invisible at this size, per ART-PIPELINE-DESIGN.md's silhouette/proportion/color
        /// finding). Used for lamp stems/shades and round table pedestals.
        /// </summary>
        public void AddCylinder(Vec3 baseCenter, double height, double radiusBottom, double radiusTop,
            (byte, byte, byte) color, int segments = 14, bool capBottom = true, bool capTop = true)
        {
            var bottom = new Vec3[segments];
            var top = new Vec3[segments];
            for (int i = 0; i < segments; i++)
            {
                var a = 2 * System.Math.PI * i / segments;
                var cx = System.Math.Cos(a); var cz = System.Math.Sin(a);
                bottom[i] = new Vec3(baseCenter.X + cx * radiusBottom, baseCenter.Y, baseCenter.Z + cz * radiusBottom);
                top[i] = new Vec3(baseCenter.X + cx * radiusTop, baseCenter.Y + height, baseCenter.Z + cz * radiusTop);
            }
            for (int i = 0; i < segments; i++)
            {
                var j = (i + 1) % segments;
                AddQuad(bottom[i], top[i], top[j], bottom[j], color); // verified outward normal, see ArtGen cylinder derivation notes
            }
            if (capBottom)
                AddFace(bottom, color); // direct winding order yields -Y (down) normal
            if (capTop)
            {
                var rev = new Vec3[segments];
                for (int i = 0; i < segments; i++) rev[i] = top[segments - 1 - i]; // reversed winding yields +Y (up) normal
                AddFace(rev, color);
            }
        }

        /// <summary>
        /// Adds a lat/long-banded ellipsoid (sphere when radiusX==radiusY==radiusZ): body
        /// quads between adjacent rings, triangle fans at the two poles. Same faceted-is-fine
        /// reasoning as AddCylinder — a 12-16-sided band reads as round at TSO's render scale.
        /// Third primitive alongside box/cylinder for GENERIC-GENERATOR-DESIGN.md's
        /// primitive-composition generator (rounded masses: heads, rocks, beards, bodies).
        /// </summary>
        public void AddSphere(Vec3 center, double radiusX, double radiusY, double radiusZ,
            (byte, byte, byte) color, int latSegments = 8, int lonSegments = 12)
        {
            // Rings strictly between the poles (poles themselves are triangle fans below).
            var rings = new Vec3[latSegments - 1][];
            for (int i = 1; i < latSegments; i++)
            {
                var v = System.Math.PI * i / latSegments; // 0 (north pole) .. PI (south pole), exclusive
                var y = System.Math.Cos(v);
                var ringRadius = System.Math.Sin(v);
                var ring = new Vec3[lonSegments];
                for (int j = 0; j < lonSegments; j++)
                {
                    var u = 2 * System.Math.PI * j / lonSegments;
                    var x = ringRadius * System.Math.Cos(u);
                    var z = ringRadius * System.Math.Sin(u);
                    ring[j] = new Vec3(center.X + x * radiusX, center.Y + y * radiusY, center.Z + z * radiusZ);
                }
                rings[i - 1] = ring;
            }

            // Body quads between adjacent rings — verified outward normal (see ArtGen sphere
            // derivation notes: ringA is nearer the north pole, ringB nearer the south pole).
            for (int r = 0; r < rings.Length - 1; r++)
            {
                var ringA = rings[r]; var ringB = rings[r + 1];
                for (int j = 0; j < lonSegments; j++)
                {
                    var jn = (j + 1) % lonSegments;
                    AddQuad(ringA[j], ringA[jn], ringB[jn], ringB[j], color);
                }
            }

            // Poles: triangle fans, verified outward (mostly +Y at north, mostly -Y at south).
            var north = new Vec3(center.X, center.Y + radiusY, center.Z);
            var south = new Vec3(center.X, center.Y - radiusY, center.Z);
            var firstRing = rings[0];
            var lastRing = rings[rings.Length - 1];
            for (int j = 0; j < lonSegments; j++)
            {
                var jn = (j + 1) % lonSegments;
                AddFace(new[] { north, firstRing[jn], firstRing[j] }, color);
                AddFace(new[] { south, lastRing[j], lastRing[jn] }, color);
            }
        }

        /// <summary>
        /// Adds a full axis-aligned box (all 6 faces) spanning [center-size/2, center+size/2],
        /// with outward-facing normals and winding, in one call — the common case for
        /// box-like furniture parts (seat slabs, legs, backrests before tilting).
        /// </summary>
        public void AddBox(Vec3 center, Vec3 size, (byte, byte, byte) color)
        {
            var hx = size.X / 2; var hy = size.Y / 2; var hz = size.Z / 2;
            Vec3 P(double sx, double sy, double sz) => new Vec3(center.X + sx * hx, center.Y + sy * hy, center.Z + sz * hz);

            AddQuad(P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), P(-1, 1, -1), color);   // +Y top
            AddQuad(P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), color); // -Y bottom
            AddQuad(P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1), color);   // +X
            AddQuad(P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1), color); // -X
            AddQuad(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), color);   // +Z
            AddQuad(P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), color); // -Z
        }
    }
}
