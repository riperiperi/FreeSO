namespace FSO.PackCompiler.ArtGen
{
    /// <summary>
    /// Parametric table: a slab or round top on either four tapered legs or a single
    /// pedestal base. Generic mid-century-adjacent categories (tapered-leg slab table,
    /// round pedestal table) — no specific branded design reproduced, no brand names
    /// anywhere in this file or its output.
    /// </summary>
    public static class TableGenerator
    {
        public enum TopShapeType { Rectangular, Round }
        public enum BaseStyleType { FourLeg, Pedestal, Tripod }

        public class Params
        {
            public TopShapeType TopShape = TopShapeType.Rectangular;
            public BaseStyleType BaseStyle = BaseStyleType.FourLeg;

            public double TopWidth = 2.4;   // rectangular top only
            public double TopDepth = 1.2;   // rectangular top only
            public double TopDiameter = 1.6; // round top only
            public double TopThickness = 0.12;
            public double Height = 1.15;    // floor to top surface

            public double LegTopWidth = 0.16;   // FourLeg and Tripod
            public double LegBottomWidth = 0.10; // FourLeg and Tripod
            public double PedestalTopRadius = 0.10; // Pedestal only, radius just under the top
            public double PedestalBaseRadius = 0.32; // Pedestal only, radius of the floor foot
            public double TripodTopRadius = 0.06;   // Tripod only — where the 3 legs converge, near center
            public double TripodBottomRadius = 0.5; // Tripod only — how far the splayed feet sit from center

            public (byte, byte, byte) WoodColor = (110, 74, 44);
            public (byte, byte, byte) TopColor = (168, 140, 92);
        }

        /// <summary>True when the mesh has no directional feature — a round top on a
        /// pedestal reads identically from all 4 yaw directions, so callers can use
        /// SymmetricAssembler instead of rendering all 12 frames. A 3-legged Tripod base is
        /// NOT included even with a round top — 3-fold symmetry doesn't repeat at the
        /// engine's 4 canonical 90-degree-apart directions the way pedestal's 4-fold (i.e.
        /// no directional feature at all) does.</summary>
        public static bool IsRotationallySymmetric(Params p) =>
            p.TopShape == TopShapeType.Round && p.BaseStyle == BaseStyleType.Pedestal;

        public static Mesh Build(Params p)
        {
            var mesh = new Mesh();
            var baseHeight = p.Height - p.TopThickness;

            if (p.BaseStyle == BaseStyleType.FourLeg)
            {
                var legInset = 0.18;
                var legX = (p.TopShape == TopShapeType.Round ? p.TopDiameter / 2 : p.TopWidth / 2) - legInset;
                var legZ = (p.TopShape == TopShapeType.Round ? p.TopDiameter / 2 : p.TopDepth / 2) - legInset;
                foreach (var sx in new[] { -1, 1 })
                    foreach (var sz in new[] { -1, 1 })
                        AddTaperedLeg(mesh, new Vec3(sx * legX, 0, sz * legZ), baseHeight, p.LegTopWidth, p.LegBottomWidth, p.WoodColor);
            }
            else if (p.BaseStyle == BaseStyleType.Pedestal)
            {
                // Foot -> stem -> narrow collar under the top, one continuous tapered column.
                var footHeight = baseHeight * 0.08;
                mesh.AddCylinder(new Vec3(0, 0, 0), footHeight, p.PedestalBaseRadius, p.PedestalBaseRadius * 0.6, p.WoodColor, 16);
                mesh.AddCylinder(new Vec3(0, footHeight, 0), baseHeight - footHeight, p.PedestalBaseRadius * 0.6, p.PedestalTopRadius, p.WoodColor, 16, capBottom: false);
            }
            else // Tripod
            {
                // 3 legs, splayed outward as they descend, converging near (not at) the
                // underside of the top — the classic mid-century tripod-table silhouette.
                // Deliberately NOT a pedestal: a single thin central column at this render
                // scale reads as a mushroom stalk, not a table base; 3 legs anchoring the
                // top from multiple points breaks that read.
                var topY = baseHeight * 0.92;
                for (int i = 0; i < 3; i++)
                {
                    var theta = i * 2 * System.Math.PI / 3;
                    var topC = new Vec3(p.TripodTopRadius * System.Math.Cos(theta), topY, p.TripodTopRadius * System.Math.Sin(theta));
                    var botC = new Vec3(p.TripodBottomRadius * System.Math.Cos(theta), 0, p.TripodBottomRadius * System.Math.Sin(theta));
                    AddSlantedLeg(mesh, topC, botC, p.LegTopWidth, p.LegBottomWidth, p.WoodColor);
                }
                // Small hub where the 3 legs converge under the top, for a clean junction.
                mesh.AddCylinder(new Vec3(0, topY, 0), baseHeight - topY, p.TripodTopRadius * 1.4, p.TripodTopRadius * 1.4, p.WoodColor, 12);
            }

            var topCenter = new Vec3(0, baseHeight + p.TopThickness / 2, 0);
            if (p.TopShape == TopShapeType.Round)
                mesh.AddCylinder(new Vec3(0, baseHeight, 0), p.TopThickness, p.TopDiameter / 2, p.TopDiameter / 2, p.TopColor, 20);
            else
                mesh.AddBox(topCenter, new Vec3(p.TopWidth, p.TopThickness, p.TopDepth), p.TopColor);

            return mesh;
        }

        static void AddTaperedLeg(Mesh mesh, Vec3 baseCenterXZ, double height, double topWidth, double bottomWidth, (byte, byte, byte) color)
        {
            var top = height; var bot = 0.0;
            var ht = topWidth / 2; var hb = bottomWidth / 2;
            var cx = baseCenterXZ.X; var cz = baseCenterXZ.Z;

            Vec3 T(int sx, int sz) => new Vec3(cx + sx * ht, top, cz + sz * ht);
            Vec3 B(int sx, int sz) => new Vec3(cx + sx * hb, bot, cz + sz * hb);

            mesh.AddQuad(B(1, -1), T(1, -1), T(1, 1), B(1, 1), color);
            mesh.AddQuad(B(-1, 1), T(-1, 1), T(-1, -1), B(-1, -1), color);
            mesh.AddQuad(B(-1, -1), T(-1, -1), T(1, -1), B(1, -1), color);
            mesh.AddQuad(B(1, 1), T(1, 1), T(-1, 1), B(-1, 1), color);
            mesh.AddQuad(B(1, -1), B(1, 1), B(-1, 1), B(-1, -1), color);
        }

        /// <summary>Same tapered-frustum shape as AddTaperedLeg, but the top and bottom
        /// cross-sections sit at independent XZ centers — a straight leg whose ends don't
        /// share a vertical axis, i.e. a splayed/angled leg (tripod bases). Top/bottom
        /// rectangles stay axis-aligned rather than rotated to follow the slant precisely —
        /// invisible at TSO's render scale, same as the tapered leg's faceted roundness.</summary>
        static void AddSlantedLeg(Mesh mesh, Vec3 topCenter, Vec3 bottomCenter, double topWidth, double bottomWidth, (byte, byte, byte) color)
        {
            var ht = topWidth / 2; var hb = bottomWidth / 2;

            Vec3 T(int sx, int sz) => new Vec3(topCenter.X + sx * ht, topCenter.Y, topCenter.Z + sz * ht);
            Vec3 B(int sx, int sz) => new Vec3(bottomCenter.X + sx * hb, bottomCenter.Y, bottomCenter.Z + sz * hb);

            mesh.AddQuad(B(1, -1), T(1, -1), T(1, 1), B(1, 1), color);
            mesh.AddQuad(B(-1, 1), T(-1, 1), T(-1, -1), B(-1, -1), color);
            mesh.AddQuad(B(-1, -1), T(-1, -1), T(1, -1), B(1, -1), color);
            mesh.AddQuad(B(1, 1), T(1, 1), T(-1, 1), B(-1, 1), color);
            mesh.AddQuad(B(1, -1), B(1, 1), B(-1, 1), B(-1, -1), color);
        }
    }
}
