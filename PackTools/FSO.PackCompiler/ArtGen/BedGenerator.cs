namespace FSO.PackCompiler.ArtGen
{
    /// <summary>
    /// Parametric bed: frame on tapered legs, mattress slab, headboard (always), optional
    /// low footboard. Generic platform-bed category — no specific branded design
    /// reproduced, no brand names anywhere in this file or its output.
    /// </summary>
    public static class BedGenerator
    {
        public class Params
        {
            public double MattressWidth = 1.9;   // full/queen-ish single-tile footprint
            public double MattressDepth = 2.4;
            public double MattressThickness = 0.28;
            public double FrameThickness = 0.14;  // frame slab under the mattress
            public double LegHeight = 0.22;
            public double LegWidth = 0.14;
            public double HeadboardHeight = 0.9;  // frame-top to top of headboard
            public double HeadboardThickness = 0.12;
            public bool Footboard = false;
            public double FootboardHeight = 0.35;

            public (byte, byte, byte) FrameColor = (96, 68, 42);
            public (byte, byte, byte) MattressColor = (232, 228, 216);
            public (byte, byte, byte) HeadboardColor = (140, 108, 70);
        }

        public static Mesh Build(Params p)
        {
            var mesh = new Mesh();

            var legInset = 0.10;
            var legX = p.MattressWidth / 2 - legInset;
            var legZ = p.MattressDepth / 2 - legInset;
            foreach (var sx in new[] { -1, 1 })
                foreach (var sz in new[] { -1, 1 })
                    AddLeg(mesh, new Vec3(sx * legX, 0, sz * legZ), p.LegHeight, p.LegWidth, p.FrameColor);

            var frameCenterY = p.LegHeight + p.FrameThickness / 2;
            mesh.AddBox(new Vec3(0, frameCenterY, 0), new Vec3(p.MattressWidth, p.FrameThickness, p.MattressDepth), p.FrameColor);

            var mattressCenterY = p.LegHeight + p.FrameThickness + p.MattressThickness / 2;
            mesh.AddBox(new Vec3(0, mattressCenterY, 0), new Vec3(p.MattressWidth * 0.98, p.MattressThickness, p.MattressDepth * 0.98), p.MattressColor);

            // Headboard: a vertical slab at the -Z (head) edge, rising from the floor.
            var headboardCenterY = p.LegHeight + p.FrameThickness + p.HeadboardHeight / 2;
            var headboardZ = -p.MattressDepth / 2 - p.HeadboardThickness / 2;
            mesh.AddBox(new Vec3(0, headboardCenterY, headboardZ), new Vec3(p.MattressWidth, p.HeadboardHeight, p.HeadboardThickness), p.HeadboardColor);

            if (p.Footboard)
            {
                var footboardCenterY = p.LegHeight + p.FrameThickness + p.FootboardHeight / 2;
                var footboardZ = p.MattressDepth / 2 + p.HeadboardThickness / 2;
                mesh.AddBox(new Vec3(0, footboardCenterY, footboardZ), new Vec3(p.MattressWidth, p.FootboardHeight, p.HeadboardThickness), p.HeadboardColor);
            }

            return mesh;
        }

        static void AddLeg(Mesh mesh, Vec3 baseCenterXZ, double height, double width, (byte, byte, byte) color)
        {
            mesh.AddBox(new Vec3(baseCenterXZ.X, height / 2, baseCenterXZ.Z), new Vec3(width, height, width), color);
        }
    }
}
