using FSO.Common.Utils;
using FSO.Files;
using FSO.Files.RC;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Components.Geometry
{
    /// <summary>
    /// A model for a tile type, input from an OBJ file, with associated texture. 
    /// Pre-prepared for the quickest application to the ground tile. (split down the diagonals, already in TerrainVertex format)
    /// </summary>
    public class Modelled3DFloorTile
    {
        public TerrainParallaxVertex[] Vertices;
        public int[] Indices;

        public string TextureName;
        private Texture2D Texture;

        public Modelled3DFloorTile(OBJ model, string textureName)
        {
            TextureName = textureName;
            var indices = model.FacesByObjgroup.First(x => x.Value.Count > 0).Value;
            var outVerts = new List<TerrainParallaxVertex>();
            var outInds = new List<int>();
            var dict = new Dictionary<Tuple<int, int, int>, int>();

            var off = new Vector3(0.5f, 0, 0.5f);

            foreach (var ind in indices)
            {
                var tup = new Tuple<int, int, int>(ind[0], ind[1], ind[2]);
                int targ;
                if (!dict.TryGetValue(tup, out targ))
                {
                    //add a vertex
                    targ = outVerts.Count;
                    var tc = model.TextureCoords[ind[1] - 1];
                    tc.Y = 1 - tc.Y;
                    var vert = new TerrainParallaxVertex(model.Vertices[ind[0] - 1] + off, Vector4.One, tc, 0, model.Normals[ind[2] - 1]);
                    outVerts.Add(vert);
                    dict[tup] = targ;
                }
                outInds.Add(targ);
            }

            //todo: process using projector? (to cut at diagonals)

            Vertices = outVerts.ToArray();
            Indices = outInds.ToArray();
        }

        public Texture2D GetTexture(GraphicsDevice gd)
        {
            if (Texture != null) return Texture;
            Texture = TextureUtils.MipTextureFromFile(gd, $"Content/3D/floor/{TextureName}");
            return Texture;
        }
    }
}
