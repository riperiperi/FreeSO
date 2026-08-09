namespace FSO.PackCompiler.ArtGen
{
    /// <summary>
    /// Parametric sofa: seat slab on short legs, angled backrest, boxy arms at both ends,
    /// cushion-seam bands dividing the seat/back into CushionCount sections. Generic
    /// mid-century-adjacent sofa *category* — no specific branded design reproduced, no
    /// brand names anywhere in this file or its output.
    ///
    /// The thing a chair generator can't fake: overall width scales independently of seat/
    /// back/arm proportions, so the same Params shape can express a loveseat or a 3-seat
    /// sofa just by changing Width and CushionCount — construction knowledge (where the arms
    /// sit, how the seam bands space themselves, that seams need real thickness/contrast to
    /// read at render scale) lives here once instead of being re-derived per pack.
    /// </summary>
    public static class SofaGenerator
    {
        public class Params
        {
            public double Width = 6.5;        // overall floor footprint, arm outside to arm outside
            public double SeatDepth = 3.0;
            public double SeatHeight = 1.45;  // floor to top of seat cushion
            public double SeatThickness = 0.35;
            public int CushionCount = 3;

            // Back height and thickness are deliberately exaggerated well past a literal
            // real-furniture ratio (a real sofa back rises roughly seat-depth's-worth above
            // the seat; this defaults higher still) — a first pass at the literal real ratio
            // read as a low lip around an open rectangle, a tray, not something you sit in.
            // Silhouette read at TSO's render scale needed the back to dominate as a solid
            // mass, not just be dimensionally correct.
            public double BackHeight = 2.6;   // seat top to top of backrest
            public double BackThickness = 0.55; // real front-to-back mass, so it casts its own
                                                 // shading instead of reading as a thin card/edge
            public double BackAngleDeg = 8.0; // slight recline
            public double BackCapHeight = 0.2; // wood trim cap along the top of the backrest

            public double ArmWidth = 1.05;    // each arm's mass, eating into Width from both ends —
                                               // arms want mass (footprint), not just height
            public double ArmHeight = 1.5;    // seat top to top of arm — comparable to the back,
                                               // or the arm reads as part of the seat mass, not a
                                               // separate raised element
            public double ArmCapHeight = 0.16; // wood trim cap along the top of each arm

            public double LegHeight = 0.2;
            public double LegWidth = 0.14;

            public (byte, byte, byte) WoodColor = (90, 62, 38);
            public (byte, byte, byte) UpholsteryColor = (150, 130, 108);
            public (byte, byte, byte) SeamColor = (110, 92, 74); // cushion divider bands

            // Arm and back caps are WoodColor, not a separate field — this is a real
            // mid-century motif (exposed wood arm rails/back trim over upholstery), and it's
            // also what makes the back and arms register as distinct silhouette elements
            // instead of merging into the seat cushion's color mass at render scale.

            // This uses ArmWidth on both ends plus a minimum usable seat span, so it's the
            // one generator-specific rule worth naming: Width has to leave room for both arms
            // and at least a sliver of seat, or the arms would overlap in the middle.
        }

        public static Mesh Build(Params p)
        {
            var mesh = new Mesh();

            var legInset = 0.12;
            var legX = p.Width / 2 - legInset;
            var legZ = p.SeatDepth / 2 - legInset;
            foreach (var sx in new[] { -1, 1 })
                foreach (var sz in new[] { -1, 1 })
                    mesh.AddBox(new Vec3(sx * legX, p.LegHeight / 2, sz * legZ), new Vec3(p.LegWidth, p.LegHeight, p.LegWidth), p.WoodColor);

            var innerWidth = System.Math.Max(0.2, p.Width - 2 * p.ArmWidth);
            var seatCenterY = p.LegHeight + p.SeatThickness / 2;
            mesh.AddBox(new Vec3(0, seatCenterY, 0), new Vec3(innerWidth, p.SeatThickness, p.SeatDepth), p.UpholsteryColor);

            // Cushion seams: thin proud accent strips across the seat, same lesson as
            // StorageGenerator's drawer bands — needs real width/contrast to read, not just
            // a subtle color shift, at TSO's render scale.
            var seatTopY = p.LegHeight + p.SeatThickness;
            if (p.CushionCount > 1)
            {
                var seamWidth = 0.04;
                for (int i = 1; i < p.CushionCount; i++)
                {
                    var x = -innerWidth / 2 + innerWidth * i / p.CushionCount;
                    mesh.AddBox(new Vec3(x, seatTopY - 0.01, 0), new Vec3(seamWidth, 0.02, p.SeatDepth * 0.94), p.SeamColor);
                }
            }

            AddAngledBackrest(mesh,
                hingeCenter: new Vec3(0, seatTopY, -p.SeatDepth / 2 + p.BackThickness / 2),
                width: innerWidth,
                height: p.BackHeight,
                thickness: p.BackThickness,
                angleDeg: p.BackAngleDeg,
                color: p.UpholsteryColor,
                capHeight: p.BackCapHeight,
                capColor: p.WoodColor);

            var armBodyHeight = p.ArmHeight - p.ArmCapHeight;
            var armCapCenterY = p.LegHeight + p.ArmHeight - p.ArmCapHeight / 2;
            foreach (var sx in new[] { -1, 1 })
            {
                var armX = sx * (p.Width / 2 - p.ArmWidth / 2);
                var armBodyCenterY = p.LegHeight + armBodyHeight / 2;
                mesh.AddBox(new Vec3(armX, armBodyCenterY, 0), new Vec3(p.ArmWidth, armBodyHeight, p.SeatDepth), p.UpholsteryColor);
                mesh.AddBox(new Vec3(armX, armCapCenterY, 0), new Vec3(p.ArmWidth, p.ArmCapHeight, p.SeatDepth), p.WoodColor);
            }

            return mesh;
        }

        /// <summary>
        /// Backrest as two stacked angled slabs sharing one hinge line: an upholstery body
        /// plus a thin wood-colored cap along the top — the cap is what makes the back read
        /// as a distinct element from the seat cushion at render scale, not just a taller
        /// slab of the same color.
        /// </summary>
        static void AddAngledBackrest(Mesh mesh, Vec3 hingeCenter, double width, double height, double thickness, double angleDeg, (byte, byte, byte) color, double capHeight, (byte, byte, byte) capColor)
        {
            var rad = angleDeg * System.Math.PI / 180.0;
            var sin = System.Math.Sin(rad); var cos = System.Math.Cos(rad);

            Vec3 Rot(double lx, double ly, double lz) =>
                new Vec3(lx, ly * cos - lz * sin, ly * sin + lz * cos) + hingeCenter;

            var hw = width / 2; var ht2 = thickness / 2;
            var bodyHeight = height - capHeight;

            Vec3 T(int sx, int sz) => Rot(sx * hw, bodyHeight, sz * ht2);
            Vec3 B(int sx, int sz) => Rot(sx * hw, 0, sz * ht2);

            mesh.AddQuad(B(1, -1), T(1, -1), T(1, 1), B(1, 1), color);
            mesh.AddQuad(B(-1, 1), T(-1, 1), T(-1, -1), B(-1, -1), color);
            mesh.AddQuad(B(-1, -1), T(-1, -1), T(1, -1), B(1, -1), color);
            mesh.AddQuad(B(1, 1), T(1, 1), T(-1, 1), B(-1, 1), color);

            if (capHeight > 0)
            {
                Vec3 CT(int sx, int sz) => Rot(sx * hw, height, sz * ht2);
                mesh.AddQuad(T(1, -1), CT(1, -1), CT(1, 1), T(1, 1), capColor);
                mesh.AddQuad(T(-1, 1), CT(-1, 1), CT(-1, -1), T(-1, -1), capColor);
                mesh.AddQuad(T(-1, -1), CT(-1, -1), CT(1, -1), T(1, -1), capColor);
                mesh.AddQuad(T(1, 1), CT(1, 1), CT(-1, 1), T(-1, 1), capColor);
                mesh.AddQuad(CT(1, -1), CT(-1, -1), CT(-1, 1), CT(1, 1), capColor);
            }
            else
            {
                mesh.AddQuad(T(1, -1), T(-1, -1), T(-1, 1), T(1, 1), color);
            }
        }
    }
}
