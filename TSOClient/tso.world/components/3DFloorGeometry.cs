using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Components
{
    /// <summary>
    /// Handles indices and groups floor geometry by texture for a single floor.
    /// 
    /// 
    /// </summary>
    /// 

    // indices for individaual tiles
    // w is ((width+1) * 2) - 1. this is a point for each tile
    //  0________2
    //   |\    /|
    //   | \  / |
    //   |  \/1 |
    //   |  /\  |
    //   | /  \ |
    // +w|/____\|+w+2

    public class _3DFloorGeometry
    {
        public int FloorNum;
        public Blueprint Bp;

        public Dictionary<ushort, FloorTextureGroup> IDToGeom = new Dictionary<ushort, FloorTextureGroup>();

        public void Regenerate(Rectangle bounds)
        {
            //find all meshes that are dirty:
            // - all floor textures with bounds intersecting these bounds (old)
            // - all floor textures present now (new, union)

            var floors = Bp.Floors[FloorNum];
            //get all 
            var set = new HashSet<ushort>();
            for (int y = bounds.Top; y < bounds.Bottom; y++)
            {
                var idx = y * Bp.Width + bounds.Left;
                for (int x = bounds.Left; x < bounds.Right; x++)
                {
                    set.Add(floors[idx++].Pattern);
                }
            }

            foreach (var geom in IDToGeom.Values)
            {
                if (bounds.Intersects(geom.Bounds) || set.Contains(geom.FloorID))
                {
                    set.Add(geom.FloorID);
                    bounds = Rectangle.Union(geom.Bounds, bounds);
                }
            }

            var idxwidth = (Bp.Width * 2) + 1;
            //build indices for all the geometry
            for (int y=bounds.Top; y<bounds.Bottom; y++)
            {
                var by = idxwidth * y;
                for (int x=bounds.Bottom; x<bounds.Right; x++)
                {
                    //

                    var idxs = new List<int>();
                    var basei = x*2 + by;

                    //top tri
                    idxs.Add(basei); //tl
                    idxs.Add(basei + 2); //tr
                    idxs.Add(basei + 1); //ctr
                    
                    //right tri
                    idxs.Add(basei + 2); //tr
                    idxs.Add(basei + idxwidth + 2); //br
                    idxs.Add(basei + 1); //ctr

                    //left tri
                    idxs.Add(basei); //tl
                    idxs.Add(basei + 1); //ctr
                    idxs.Add(basei + idxwidth); //bl

                    //bottom tri
                    idxs.Add(basei + 1); //ctr
                    idxs.Add(basei + idxwidth + 2); //br
                    idxs.Add(basei + idxwidth); //bl

                }
            }
        }
    }

    public class FloorTextureGroup
    {
        public Rectangle Bounds;
        public ushort FloorID;
    }
}
