namespace FSO.PackCompiler.ArtGen
{
    /// <summary>
    /// Parametric lamp: tapered foot, stem, tapered shade. Fully rotationally symmetric —
    /// render with SymmetricAssembler (3 renders, not 12). Generic table/floor-lamp
    /// category — no specific branded design reproduced, no brand names anywhere in this
    /// file or its output.
    /// </summary>
    public static class LampGenerator
    {
        public class Params
        {
            public double BaseRadius = 0.28;
            public double BaseHeight = 0.09;
            public double StemRadius = 0.045;
            public double StemHeight = 0.95;
            public double ShadeBottomRadius = 0.32;
            public double ShadeTopRadius = 0.22;
            public double ShadeHeight = 0.38;

            public (byte, byte, byte) BaseColor = (70, 62, 54);
            public (byte, byte, byte) ShadeColor = (222, 208, 178);
        }

        public static Mesh Build(Params p)
        {
            var mesh = new Mesh();

            mesh.AddCylinder(new Vec3(0, 0, 0), p.BaseHeight, p.BaseRadius, p.BaseRadius * 0.6, p.BaseColor, 16);
            mesh.AddCylinder(new Vec3(0, p.BaseHeight, 0), p.StemHeight, p.StemRadius, p.StemRadius, p.BaseColor, 10, capBottom: false);

            var shadeBase = p.BaseHeight + p.StemHeight;
            mesh.AddCylinder(new Vec3(0, shadeBase, 0), p.ShadeHeight, p.ShadeBottomRadius, p.ShadeTopRadius, p.ShadeColor, 16, capBottom: false, capTop: true);

            return mesh;
        }
    }
}
