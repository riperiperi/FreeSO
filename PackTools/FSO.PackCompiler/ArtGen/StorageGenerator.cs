namespace FSO.PackCompiler.ArtGen
{
    /// <summary>
    /// Parametric storage piece: bookshelf (tall, open shelf cavities with a recessed dark
    /// back panel) or dresser (low, wide, solid carcass with proud drawer-front bands).
    /// Surface detail is invisible at TSO's render scale, so the two kinds differ by
    /// proportion and silhouette (open shelf gaps vs a solid stacked-band front), not by
    /// fine geometry. Generic storage-furniture categories — no specific branded design
    /// reproduced, no brand names anywhere in this file or its output.
    /// </summary>
    public static class StorageGenerator
    {
        public enum KindType { Bookshelf, Dresser }

        public class Params
        {
            public KindType Kind = KindType.Bookshelf;
            public double Width = 0.9;
            public double Depth = 0.35;
            public double Height = 1.8;      // bookshelf default; dressers should pass ~0.9
            public int Sections = 4;         // shelf cavities (bookshelf) or drawer bands (dresser)
            public double PanelThickness = 0.055; // thin, but must stay a few px at Near zoom to read at all
            public double LegHeight = 0.06;

            public (byte, byte, byte) CarcassColor = (108, 76, 46);
            public (byte, byte, byte) AccentColor = (58, 50, 40); // shelf-back / drawer-front accent — dark but not near-black, or it reads as a flat void
        }

        public static Mesh Build(Params p)
        {
            return p.Kind == KindType.Bookshelf ? BuildBookshelf(p) : BuildDresser(p);
        }

        static Mesh BuildBookshelf(Params p)
        {
            var mesh = new Mesh();
            var hw = p.Width / 2; var hd = p.Depth / 2; var pt = p.PanelThickness;
            var bodyBottom = p.LegHeight;
            var bodyHeight = p.Height - p.LegHeight;

            foreach (var sx in new[] { -1, 1 })
                mesh.AddBox(new Vec3(sx * (hw - pt / 2), bodyBottom + bodyHeight / 2, 0), new Vec3(pt, bodyHeight, p.Depth), p.CarcassColor);

            mesh.AddBox(new Vec3(0, bodyBottom + bodyHeight - pt / 2, 0), new Vec3(p.Width, pt, p.Depth), p.CarcassColor);
            mesh.AddBox(new Vec3(0, bodyBottom + pt / 2, 0), new Vec3(p.Width, pt, p.Depth), p.CarcassColor);

            // Recessed dark back panel — reads as the shelf cavity's shadow at render scale.
            mesh.AddBox(new Vec3(0, bodyBottom + bodyHeight / 2, hd - pt / 2), new Vec3(p.Width - 2 * pt, bodyHeight - 2 * pt, pt), p.AccentColor);

            // Shelf boards, evenly spaced between top and bottom.
            var innerHeight = bodyHeight - 2 * pt;
            var step = innerHeight / p.Sections;
            for (int i = 1; i < p.Sections; i++)
            {
                var y = bodyBottom + pt + step * i;
                mesh.AddBox(new Vec3(0, y, 0), new Vec3(p.Width - 2 * pt, pt, p.Depth - pt), p.CarcassColor);
            }

            AddFeet(mesh, hw, hd, p.LegHeight, p.CarcassColor);
            return mesh;
        }

        static Mesh BuildDresser(Params p)
        {
            var mesh = new Mesh();
            var hd = p.Depth / 2;
            var bodyBottom = p.LegHeight;
            var bodyHeight = p.Height - p.LegHeight;

            mesh.AddBox(new Vec3(0, bodyBottom + bodyHeight / 2, 0), new Vec3(p.Width, bodyHeight, p.Depth), p.CarcassColor);

            // Drawer fronts: thin accent-colored slabs proud of the carcass front face,
            // stacked band by band — a color/silhouette cue standing in for real drawers.
            var bandHeight = bodyHeight / p.Sections;
            var frontProud = 0.02;
            // This from-scratch rasterizer has no anti-aliasing, so a diagonal seam under
            // ~2px wide breaks up into a dotted/aliased line instead of reading as continuous
            // at Near zoom — wide enough here to stay a solid seam at typical Sections counts.
            var gap = bandHeight * 0.28;
            for (int i = 0; i < p.Sections; i++)
            {
                var y = bodyBottom + bandHeight * i + bandHeight / 2;
                mesh.AddBox(new Vec3(0, y, hd + frontProud / 2), new Vec3(p.Width * 0.92, bandHeight - gap, frontProud), p.AccentColor);
            }

            AddFeet(mesh, p.Width / 2, hd, p.LegHeight, p.CarcassColor);
            return mesh;
        }

        static void AddFeet(Mesh mesh, double hw, double hd, double legHeight, (byte, byte, byte) color)
        {
            if (legHeight <= 0) return;
            var footSize = legHeight * 0.9;
            var inset = footSize / 2 + 0.02;
            foreach (var sx in new[] { -1, 1 })
                foreach (var sz in new[] { -1, 1 })
                    mesh.AddBox(new Vec3(sx * (hw - inset), legHeight / 2, sz * (hd - inset)), new Vec3(footSize, legHeight, footSize), color);
        }
    }
}
