namespace FSO.PackCompiler.ArtGen
{
    /// <summary>
    /// Parametric chair: seat slab, angled backrest, four tapered legs, optional arms.
    /// Generic mid-century-lounge-chair *category* (tapered legs, slab seat, angled back) —
    /// not a reproduction of any specific branded design. No brand names anywhere in this
    /// file or its output.
    /// </summary>
    public static class ChairGenerator
    {
        public class Params
        {
            public double SeatWidth = 1.6;
            public double SeatDepth = 1.5;
            public double SeatHeight = 1.1;   // floor to top of seat
            public double SeatThickness = 0.18;
            public double BackHeight = 1.7;   // seat top to top of backrest
            public double BackThickness = 0.15;
            public double BackAngleDeg = 12.0; // tilt from vertical, backrest leans away from seat
            public double LegTopWidth = 0.22;
            public double LegBottomWidth = 0.12; // tapered legs: bottom narrower than top
            public bool Arms = false;
            public double ArmHeight = 0.6; // seat-top to top of arm
            public double ArmThickness = 0.14;

            public (byte, byte, byte) WoodColor = (120, 82, 48);
            public (byte, byte, byte) UpholsteryColor = (168, 140, 92);
        }

        public static Mesh Build(Params p)
        {
            var mesh = new Mesh();

            var legInset = 0.12;
            var legHeight = p.SeatHeight - p.SeatThickness / 2;
            var legX = p.SeatWidth / 2 - legInset;
            var legZ = p.SeatDepth / 2 - legInset;

            foreach (var sx in new[] { -1, 1 })
                foreach (var sz in new[] { -1, 1 })
                    AddTaperedLeg(mesh, new Vec3(sx * legX, 0, sz * legZ), legHeight, p.LegTopWidth, p.LegBottomWidth, p.WoodColor);

            // Seat slab, sitting on top of the legs.
            var seatCenter = new Vec3(0, legHeight + p.SeatThickness / 2, 0);
            mesh.AddBox(seatCenter, new Vec3(p.SeatWidth, p.SeatThickness, p.SeatDepth), p.UpholsteryColor);

            // Backrest: a slab tilted back by BackAngleDeg, hinged at the rear edge of the seat.
            AddAngledBackrest(mesh,
                hingeCenter: new Vec3(0, legHeight + p.SeatThickness, -p.SeatDepth / 2 + p.BackThickness / 2),
                width: p.SeatWidth,
                height: p.BackHeight,
                thickness: p.BackThickness,
                angleDeg: p.BackAngleDeg,
                color: p.UpholsteryColor);

            if (p.Arms)
            {
                var armY = legHeight + p.SeatThickness + p.ArmHeight / 2;
                foreach (var sx in new[] { -1, 1 })
                    mesh.AddBox(new Vec3(sx * (p.SeatWidth / 2 - p.ArmThickness / 2), armY, 0),
                        new Vec3(p.ArmThickness, p.ArmHeight, p.SeatDepth * 0.8), p.WoodColor);
            }

            return mesh;
        }

        static void AddTaperedLeg(Mesh mesh, Vec3 baseCenterXZ, double height, double topWidth, double bottomWidth, (byte, byte, byte) color)
        {
            // A tapered leg as a 4-sided frustum: 8 verts (4 top, 4 bottom), 5 faces (4 sides + bottom;
            // top is covered by the seat, omitted).
            var top = height; var bot = 0.0;
            var ht = topWidth / 2; var hb = bottomWidth / 2;
            var cx = baseCenterXZ.X; var cz = baseCenterXZ.Z;

            Vec3 T(int sx, int sz) => new Vec3(cx + sx * ht, top, cz + sz * ht);
            Vec3 B(int sx, int sz) => new Vec3(cx + sx * hb, bot, cz + sz * hb);

            mesh.AddQuad(B(1, -1), T(1, -1), T(1, 1), B(1, 1), color);    // +X
            mesh.AddQuad(B(-1, 1), T(-1, 1), T(-1, -1), B(-1, -1), color); // -X
            mesh.AddQuad(B(-1, -1), T(-1, -1), T(1, -1), B(1, -1), color); // -Z
            mesh.AddQuad(B(1, 1), T(1, 1), T(-1, 1), B(-1, 1), color);    // +Z
            mesh.AddQuad(B(1, -1), B(1, 1), B(-1, 1), B(-1, -1), color);  // bottom
        }

        static void AddAngledBackrest(Mesh mesh, Vec3 hingeCenter, double width, double height, double thickness, double angleDeg, (byte, byte, byte) color)
        {
            var rad = angleDeg * System.Math.PI / 180.0;
            var sin = System.Math.Sin(rad); var cos = System.Math.Cos(rad);

            // Local box corners (before tilt), hinge at local origin, extends up (+Y) and
            // slightly forward-to-back in Z as it tilts.
            Vec3 Rot(double lx, double ly, double lz) =>
                new Vec3(lx, ly * cos - lz * sin, ly * sin + lz * cos) + hingeCenter;

            var hw = width / 2; var ht2 = thickness / 2;

            Vec3 T(int sx, int sz) => Rot(sx * hw, height, sz * ht2);
            Vec3 B(int sx, int sz) => Rot(sx * hw, 0, sz * ht2);

            mesh.AddQuad(B(1, -1), T(1, -1), T(1, 1), B(1, 1), color);    // +X
            mesh.AddQuad(B(-1, 1), T(-1, 1), T(-1, -1), B(-1, -1), color); // -X
            mesh.AddQuad(B(-1, -1), T(-1, -1), T(1, -1), B(1, -1), color); // front (-Z-ish)
            mesh.AddQuad(B(1, 1), T(1, 1), T(-1, 1), B(-1, 1), color);    // back (+Z-ish)
            mesh.AddQuad(T(1, -1), T(-1, -1), T(-1, 1), T(1, 1), color);  // top cap
        }
    }
}
