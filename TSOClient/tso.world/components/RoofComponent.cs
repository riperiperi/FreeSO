using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using FSO.LotView.Utils;
using FSO.Common.Utils;
using FSO.LotView.LMap;
using System.IO;
using FSO.LotView.Effects;

namespace FSO.LotView.Components
{
    public class RoofComponent : WorldComponent, IDisposable
    {
        public Blueprint blueprint;
        private List<RoofRect>[] RoofRects;
        private RoofDrawGroup[] Drawgroups;
        private GrassEffect Effect;
        public Texture2D Texture;
        public Texture2D ParallaxTexture;
        public Texture2D NormalMap;
        public Texture2D EdgeTexture;
        public Color RoofAvgColor;

        public uint RoofStyle;
        public float RoofPitch;

        public bool StyleDirty = false;
        public bool ShapeDirty = true;
        public float TexRescale = 1f;

        public void SetStylePitch(uint style, float pitch)
        {
            RoofStyle = style;
            RoofPitch = pitch;
            blueprint.Changes.SetFlag(BlueprintGlobalChanges.ROOF_STYLE_CHANGED);
        }

        public RoofComponent(Blueprint bp)
        {
            blueprint = bp;
            RoofRects = new List<RoofRect>[bp.Stories];
            Drawgroups = new RoofDrawGroup[bp.Stories];
            this.Effect = WorldContent.GrassEffect;
        }

        private Texture2D GenMips(GraphicsDevice device, Texture2D texture)
        {
            var data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            texture.Dispose();
            texture = new Texture2D(device, texture.Width, texture.Height, true, SurfaceFormat.Color);
            TextureUtils.UploadWithAvgMips(texture, device, data);
            return texture;
        }

        private void PrepTextures(GraphicsDevice device)
        {
            ParallaxTexture?.Dispose();
            NormalMap?.Dispose();
            EdgeTexture?.Dispose();
            var roofs = Content.Content.Get().WorldRoofs;
            var name = roofs.IDToName((int)RoofStyle);

            var indu = Array.FindIndex(name.ToCharArray(), (char c) => { return c == '_' || char.IsDigit(c) || c == '.'; });
            if (name.Contains("drk")) indu -= 3;

            var searchName = (indu == -1) ? name : name.Substring(0, indu);

            try
            {
                TexRescale = 0.2f;
                using (var strm = File.OpenRead($"Content/Textures/roof/{searchName}.png"))
                {
                    Texture = Texture2D.FromStream(device, strm);
                }
            }
            catch
            {
                TexRescale = 1f;
                Texture = roofs.Get(name).Get(device);
            }

            ParallaxTexture = null;
            NormalMap = null;
            try
            {
                using (var strm = File.OpenRead($"Content/Textures/roof/{searchName}_h.png"))
                {
                    ParallaxTexture = Texture2D.FromStream(device, strm);
                }

                using (var strm = File.OpenRead($"Content/Textures/roof/{searchName}_n.png"))
                {
                    NormalMap = GenMips(device, Texture2D.FromStream(device, strm));
                }
            } catch (Exception)
            {

            }

            using (var strm = File.OpenRead($"Content/Textures/roof/default_edge.png"))
            {
                EdgeTexture = GenMips(device, Files.ImageLoader.FromStream(device, strm));
            }

            var color = new Vector4();
            var data = new Color[Texture.Width * Texture.Height];
            Texture.GetData(data);
            foreach (var col in data)
            {
                color += col.ToVector4();
            }
            color /= data.Length;
            RoofAvgColor = new Color(color);
        }

        public void RegenRoof(GraphicsDevice device)
        {
            if (Texture == null) PrepTextures(device);

            for (int i = 1; i <= blueprint.Stories; i++)
            {
                RegenRoof((sbyte)(i + 1), device);
            }
        }

        public void RemeshRoof(GraphicsDevice device)
        {
            PrepTextures(device);

            for (int i = 1; i <= blueprint.Stories; i++)
            {
                MeshRects((sbyte)(i + 1), device);
            }
        }

        public void RegenRoof(sbyte level, GraphicsDevice device)
        {
            //algorithm overview:
            // 1. divide each tile into 4.
            // 2. find tile that IsRoofable
            // 3. place a starting rectangle on the tile
            // 4. Expand the rectangle along x and y axis one by one
            //    (for current axis)
            //    4a. if the expansion would be entirely contained by an existing rectangle (other axis), expand to the end of that rectangle.
            //    4b. if any tiles we're expanding into are not roofable, stop expanding this rectangle and add new ones that start from the same baseline (current axis), but split into the regions (other axis)
            //    4c. if all tiles are not roofable, rectangle is complete.

            var width = blueprint.Width * 2;
            var height = blueprint.Height * 2;

            var evaluated = new bool[width * height];

            var result = new List<RoofRect>();
            for (int y = 2; y < height; y++)
            {
                for (int x = 2; x < width; x++)
                {
                    var off = x + y * width;
                    if (!evaluated[off])
                    {
                        evaluated[off] = true;
                        var tilePos = new LotTilePos((short)(x * 8), (short)(y * 8), level);
                        if (IsRoofable(tilePos))
                        {
                            //bingo. try expand a rectangle here.
                            RoofSpread(tilePos, evaluated, width, height, level, result);
                        }
                    }
                }
            }
            RoofRects[level - 2] = result;
            MeshRects(level, device);
        }

        private Vector2 PosToTexCRim(Vector3 pos, float ypos)
        {
            return new Vector2((pos.X + pos.Z) * 0.66f, ypos);
        }

        public RoofData MeshRectData(int level)
        {
            var rects = RoofRects[level - 2];
            if (rects == null) return null;
            var advRoof = true;

            var numQuads = rects.Count * 4; //4 sides for each roof rectangle
            TerrainParallaxVertex[] Vertices = new TerrainParallaxVertex[numQuads * 4];
            int[] Indices = new int[numQuads * 6];

            TerrainParallaxVertex[] AdvVertices = null;
            int[] AdvIndices = null;
            int advNumPrimitives = 0;
            if (advRoof)
            {
                //advanced roof: add down facing edges at each side,
                //and edge guards
                var advQuads = numQuads * (1 + 3); //1 quad for the rim, then 3 for the edge
                advQuads += rects.Count; //bottom
                AdvVertices = new TerrainParallaxVertex[advQuads * 4];
                AdvIndices = new int[advQuads * 6];
                advNumPrimitives = advQuads * 2;
            }

            var numPrimitives = (numQuads * 2);
            int geomOffset = 0;
            int indexOffset = 0;

            int advGeomOffset = 0;
            int advIndexOffset = 0;

            var mat3 = Matrix.CreateRotationZ((float)(Math.PI / -2f)); //good
            var mat1 = Matrix.CreateRotationZ((float)(Math.PI / -2f));
            var mat4 = Matrix.CreateRotationX((float)(Math.PI / 2f)) * Matrix.CreateRotationZ((float)(Math.PI / -2f)); //good
            var mat2 = Matrix.CreateRotationX((float)(Math.PI / 2f)) * Matrix.CreateRotationZ((float)(Math.PI / 2f));

            var edgeT = 0.7f;
            var edgeO = new Vector3(0, 0.005f, 0);

            var advCol = RoofAvgColor.ToVector4();

            foreach (var rect in rects)
            {
                //determine roof height of the smallest edge. This height will be used on all edges
                var height = Math.Min(rect.x2 - rect.x1, rect.y2 - rect.y1) / 2;
                //    /    \
                //   /      \
                //  /________\  Draw 4 segments like this. two segments will have the top middle section so short it will not appear.

                var heightMod = height / 400f;
                var pitch = RoofPitch;
                var tl = ToWorldPos(rect.x1, rect.y1, 0, level, pitch) + new Vector3(0, heightMod, 0);
                var tr = ToWorldPos(rect.x2, rect.y1, 0, level, pitch) + new Vector3(0, heightMod, 0);
                var bl = ToWorldPos(rect.x1, rect.y2, 0, level, pitch) + new Vector3(0, heightMod, 0);
                var br = ToWorldPos(rect.x2, rect.y2, 0, level, pitch) + new Vector3(0, heightMod, 0);
                
                var m_tl = ToWorldPos(rect.x1 + height, rect.y1 + height, height, level, pitch) + new Vector3(0, heightMod, 0);
                var m_tr = ToWorldPos(rect.x2 - height, rect.y1 + height, height, level, pitch) + new Vector3(0, heightMod, 0);
                var m_bl = ToWorldPos(rect.x1 + height, rect.y2 - height, height, level, pitch) + new Vector3(0, heightMod, 0);
                var m_br = ToWorldPos(rect.x2 - height, rect.y2 - height, height, level, pitch) + new Vector3(0, heightMod, 0);

                var thickness = new Vector3(0, -0.5f, 0);

                var b_tl = tl+thickness;
                var b_tr = tr+thickness;
                var b_bl = bl+thickness;
                var b_br = br+thickness;

                Color topCol = Color.White; //Color.Lerp(Color.White, new Color(175, 175, 175), pitch);
                Color rightCol = Color.White; //Color.White;
                Color btmCol = Color.White; //Color.Lerp(Color.White, new Color(200, 200, 200), pitch);
                Color leftCol = Color.White; //Color.Lerp(Color.White, new Color(150, 150, 150), pitch);
                Vector4 darken = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);

                //quad as two tris
                for (int j = 0; j < 16; j += 4)
                {
                    Indices[indexOffset++] = (geomOffset + 2) + j;
                    Indices[indexOffset++] = (geomOffset + 1) + j;
                    Indices[indexOffset++] = geomOffset + j;

                    Indices[indexOffset++] = geomOffset + j;
                    Indices[indexOffset++] = (geomOffset + 3) + j;
                    Indices[indexOffset++] = (geomOffset + 2) + j;
                }

                var n1 = -Vector3.Normalize(Vector3.Cross(tl - tr, tr - m_tr));
                Vector2 texScale = new Vector2(2 / 3f, 1f) * TexRescale;
                Vertices[geomOffset++] = new TerrainParallaxVertex(tl, topCol.ToVector4(), new Vector2(tl.X, tl.Z * -1) * texScale, 0, n1, mat1, true);
                Vertices[geomOffset++] = new TerrainParallaxVertex(tr, topCol.ToVector4(), new Vector2(tr.X, tr.Z * -1) * texScale, 0, n1, mat1, true);
                Vertices[geomOffset++] = new TerrainParallaxVertex(m_tr, topCol.ToVector4()*1.25f, new Vector2(m_tr.X, m_tr.Z * -1) * texScale, 0, n1, mat1, true);
                Vertices[geomOffset++] = new TerrainParallaxVertex(m_tl, topCol.ToVector4() * 1.25f, new Vector2(m_tl.X, m_tl.Z * -1) * texScale, 0, n1, mat1, true);

                var n2 = -Vector3.Normalize(Vector3.Cross(tr - br, br - m_br));
                Vertices[geomOffset++] = new TerrainParallaxVertex(tr, rightCol.ToVector4(), new Vector2(tr.Z, tr.X) * texScale, 0, n2, mat2, true);
                Vertices[geomOffset++] = new TerrainParallaxVertex(br, rightCol.ToVector4(), new Vector2(br.Z, br.X) * texScale, 0, n2, mat2, true);
                Vertices[geomOffset++] = new TerrainParallaxVertex(m_br, rightCol.ToVector4() * 1.25f, new Vector2(m_br.Z, m_br.X) * texScale, 0, n2, mat2, true);
                Vertices[geomOffset++] = new TerrainParallaxVertex(m_tr, rightCol.ToVector4() * 1.25f, new Vector2(m_tr.Z, m_tr.X) * texScale, 0, n2, mat2, true);

                var n3 = -Vector3.Normalize(Vector3.Cross(br - bl, bl - m_bl));
                Vertices[geomOffset++] = new TerrainParallaxVertex(br, btmCol.ToVector4(), new Vector2(br.X, br.Z) * texScale, 0, n3, mat3, false);
                Vertices[geomOffset++] = new TerrainParallaxVertex(bl, btmCol.ToVector4(), new Vector2(bl.X, bl.Z) * texScale, 0, n3, mat3, false);
                Vertices[geomOffset++] = new TerrainParallaxVertex(m_bl, btmCol.ToVector4() * 1.25f, new Vector2(m_bl.X, m_bl.Z) * texScale, 0, n3, mat3, false);
                Vertices[geomOffset++] = new TerrainParallaxVertex(m_br, btmCol.ToVector4() * 1.25f, new Vector2(m_br.X, m_br.Z) * texScale, 0, n3, mat3, false);

                var n4 = -Vector3.Normalize(Vector3.Cross(bl - tl, tl - m_tl));
                Vertices[geomOffset++] = new TerrainParallaxVertex(bl, leftCol.ToVector4(), new Vector2(bl.Z, bl.X * -1) * texScale, 0, n4, mat4, false);
                Vertices[geomOffset++] = new TerrainParallaxVertex(tl, leftCol.ToVector4(), new Vector2(tl.Z, tl.X * -1) * texScale, 0, n4, mat4, false);
                Vertices[geomOffset++] = new TerrainParallaxVertex(m_tl, leftCol.ToVector4() * 1.25f, new Vector2(m_tl.Z, m_tl.X * -1) * texScale, 0, n4, mat4, false);
                Vertices[geomOffset++] = new TerrainParallaxVertex(m_bl, leftCol.ToVector4() * 1.25f, new Vector2(m_bl.Z, m_bl.X * -1) * texScale, 0, n4, mat4, false);

                if (advRoof)
                {
                    for (int j = 0; j < 20; j += 4)
                    {
                        AdvIndices[advIndexOffset++] = (advGeomOffset + 2) + j;
                        AdvIndices[advIndexOffset++] = (advGeomOffset + 1) + j;
                        AdvIndices[advIndexOffset++] = advGeomOffset + j;

                        AdvIndices[advIndexOffset++] = advGeomOffset + j;
                        AdvIndices[advIndexOffset++] = (advGeomOffset + 3) + j;
                        AdvIndices[advIndexOffset++] = (advGeomOffset + 2) + j;
                    }

                    //thickness
                    var n5 = Vector3.Normalize(Vector3.Cross(tl - tr, tr - b_tr));
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(b_tr, advCol, PosToTexCRim(b_tr, 0.73f), 0, n5, mat1, true);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(tr, advCol, PosToTexCRim(tr, 0.4f), 0, n5, mat1, true);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(tl, advCol, PosToTexCRim(tl, 0.4f), 0, n5, mat1, true);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(b_tl, advCol, PosToTexCRim(b_tl, 0.73f), 0, n5, mat1, true);

                    n5 = Vector3.Normalize(Vector3.Cross(tr - br, br - b_br));
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(b_br, advCol, PosToTexCRim(b_br, 0.73f), 0, n5, mat2, true);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(br, advCol, PosToTexCRim(br, 0.4f), 0, n5, mat2, true);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(tr, advCol, PosToTexCRim(tr, 0.4f), 0, n5, mat2, true);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(b_tr, advCol, PosToTexCRim(b_tr, 0.73f), 0, n5, mat2, true);

                    n5 = Vector3.Normalize(Vector3.Cross(br - bl, bl - b_bl));
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(b_bl, advCol, PosToTexCRim(b_bl, 0.73f), 0, n5, mat3, false);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(bl, advCol, PosToTexCRim(bl, 0.4f), 0, n5, mat3, false);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(br, advCol, PosToTexCRim(br, 0.4f), 0, n5, mat3, false);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(b_br, advCol, PosToTexCRim(b_br, 0.73f), 0, n5, mat3, false);

                    n5 = Vector3.Normalize(Vector3.Cross(bl - tl, tl - b_tl));
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(b_tl, advCol, PosToTexCRim(b_tl, 0.73f), 0, n5, mat4, false);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(tl, advCol, PosToTexCRim(tl, 0.4f), 0, n5, mat4, false);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(bl, advCol, PosToTexCRim(bl, 0.4f), 0, n5, mat4, false);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(b_bl, advCol, PosToTexCRim(b_bl, 0.73f), 0, n5, mat4, false);

                    //bottom (solid black)

                    n5 = Vector3.Normalize(Vector3.Cross(b_tl - b_br, b_br - b_bl));
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(b_bl, advCol, new Vector2(0, 0.4f+0.33f), 0, n5, mat4, false);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(b_br, advCol, new Vector2(0, 0.4f + 0.33f), 0, n5, mat4, false);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(b_tr, advCol, new Vector2(0, 0.4f + 0.33f), 0, n5, mat4, false);
                    AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(b_tl, advCol, new Vector2(0, 0.4f + 0.33f), 0, n5, mat4, false);

                    //outer edge: 3 quads for top, left and right edges (on each side!)

                    Action<Vector3, Vector3, Vector3, Vector3, Vector3, Matrix, bool> addEdge = (l, r, m_l, m_r, normal, mat, flip) =>
                    {
                        var lToR = r - l;
                        lToR.Normalize();
                        var lInner = l + lToR * edgeT;
                        var rInner = r - lToR * edgeT;

                        var mlToL = l - m_l;
                        var diagLength = mlToL.Length();
                        mlToL.Normalize();
                        var mlInner = m_l + (mlToL + lToR) * edgeT;
                        var innerDiagLength = (mlInner - lInner).Length();

                        var mrToR = r - m_r;
                        mrToR.Normalize();
                        var mrInner = m_r + (mrToR - lToR) * edgeT;

                        for (int j = 0; j < 12; j += 4)
                        {
                            AdvIndices[advIndexOffset++] = (advGeomOffset + 2) + j;
                            AdvIndices[advIndexOffset++] = (advGeomOffset + 1) + j;
                            AdvIndices[advIndexOffset++] = advGeomOffset + j;

                            AdvIndices[advIndexOffset++] = advGeomOffset + j;
                            AdvIndices[advIndexOffset++] = (advGeomOffset + 3) + j;
                            AdvIndices[advIndexOffset++] = (advGeomOffset + 2) + j;
                        }

                        var idOff = (diagLength - innerDiagLength) * 0.1f;
                        var hmul = 0.5f;
                        AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(mlInner + edgeO, advCol, new Vector2(-idOff * hmul, 0.33f), 0, normal, mat, flip);
                        AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(m_l + edgeO, advCol, new Vector2(0, 0), 0, normal, mat, flip);
                        AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(l + edgeO, advCol, new Vector2(diagLength * -hmul, 0), 0, normal, mat, flip);
                        AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(lInner + edgeO, advCol, new Vector2((idOff + innerDiagLength) * -hmul, 0.33f), 0, normal, mat, flip);

                        var topLength = (m_r - m_l).Length();
                        var innerTopLength = (mrInner - mlInner).Length();
                        var itOff = (topLength - innerTopLength) / 2;

                        AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(mrInner + edgeO, advCol, new Vector2((itOff + innerTopLength) * hmul, 0.33f), 0, normal, mat, flip);
                        AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(m_r + edgeO, advCol, new Vector2(topLength* hmul, 0), 0, normal, mat, flip);
                        AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(m_l + edgeO, advCol, new Vector2(0, 0), 0, normal, mat, flip);
                        AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(mlInner + edgeO, advCol, new Vector2(itOff * hmul, 0.33f), 0, normal, mat, flip);

                        AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(mrInner + edgeO, advCol, new Vector2((topLength + idOff) * hmul, 0.33f), 0, normal, mat, flip);
                        AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(rInner + edgeO, advCol, new Vector2((topLength + idOff + innerDiagLength) * hmul, 0.33f), 0, normal, mat, flip);
                        AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(r + edgeO, advCol, new Vector2((topLength + diagLength) * hmul, 0f), 0, normal, mat, flip);
                        AdvVertices[advGeomOffset++] = new TerrainParallaxVertex(m_r + edgeO, advCol, new Vector2(topLength * hmul, 0f), 0, normal, mat, flip);
                    };

                    addEdge(tl, tr, m_tl, m_tr, n1, mat1, true);
                    addEdge(tr, br, m_tr, m_br, n2, mat2, true);
                    addEdge(br, bl, m_br, m_bl, n3, mat3, false);
                    addEdge(bl, tl, m_bl, m_tl, n4, mat4, false);
                }
            }

            var result = new RoofData()
            {
                Vertices = Vertices,
                Indices = Indices,
                NumPrimitives = numPrimitives,

                AdvVertices = AdvVertices,
                AdvIndices = AdvIndices,
                AdvNumPrimitives = advNumPrimitives
            };

            return result;
        }

        public void MeshRects(int level, GraphicsDevice device)
        {
            var data = MeshRectData(level);
            if (data == null) return;
            
            if (Drawgroups[level - 2] != null && Drawgroups[level - 2].NumPrimitives > 0)
            {
                Drawgroups[level - 2].VertexBuffer.Dispose();
                Drawgroups[level - 2].IndexBuffer.Dispose();

                Drawgroups[level - 2].AdvVertexBuffer?.Dispose();
                Drawgroups[level - 2].AdvIndexBuffer?.Dispose();
            }

            var result = new RoofDrawGroup();
            if (data.NumPrimitives > 0)
            {
                result.VertexBuffer = new VertexBuffer(device, typeof(TerrainParallaxVertex), data.Vertices.Length, BufferUsage.None);
                if (data.Vertices.Length > 0) result.VertexBuffer.SetData(data.Vertices);

                result.IndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, sizeof(int) * data.Indices.Length, BufferUsage.None);
                if (data.Vertices.Length > 0) result.IndexBuffer.SetData(data.Indices);
            }

            if (data.AdvNumPrimitives > 0)
            {
                result.AdvVertexBuffer = new VertexBuffer(device, typeof(TerrainParallaxVertex), data.AdvVertices.Length, BufferUsage.None);
                if (data.AdvVertices.Length > 0) result.AdvVertexBuffer.SetData(data.AdvVertices);

                result.AdvIndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, sizeof(int) * data.AdvIndices.Length, BufferUsage.None);
                if (data.AdvVertices.Length > 0) result.AdvIndexBuffer.SetData(data.AdvIndices);
            }

            result.NumPrimitives = data.NumPrimitives;
            result.AdvNumPrimitives = data.AdvNumPrimitives;

            Drawgroups[level - 2] = result;
        }

        private Vector3 ToWorldPos(int x, int y, int z, int level, float pitch)
        {
            return new Vector3((x / 16f) * 3f, (z * pitch / 16f) * 3f + ((level - 1) * 2.95f * 3f) + blueprint.GetAltitude(x / 16, y / 16) * 3, (y / 16f) * 3f);
        }

        private static Point[] advanceByDir = new Point[]
        {
            new Point(8, 0),
            new Point(0, 8),
            new Point(-8, 0),
            new Point(0, -8)
        };

        private Point StartLocation(RoofRect rect, int dir)
        {
            switch (dir)
            {
                case 0:
                    return new Point(rect.x2, rect.y1);
                case 1:
                    return new Point(rect.x2 - 8, rect.y2);
                case 2:
                    return new Point(rect.x1 - 8, rect.y2 - 8);
                case 3:
                    return new Point(rect.x1, rect.y1 - 8);
            }
            return new Point();
        }

        private bool RangeCheck(RoofRect me, RoofRect into, int dir)
        {
            switch (dir % 2)
            {
                case 0:
                    return (me.y1 > into.y1 && me.y2 < into.y2);
                case 1:
                    return (me.x1 > into.x1 && me.x2 < into.x2);
            }
            return false;
        }

        private static int[] ExpandOrder = new int[]
        {
            2, 3, 1, -1
        };

        private void RoofSpread(LotTilePos start, bool[] evaluated, int width, int height, sbyte level, List<RoofRect> result)
        {
            var rect = new RoofRect(start.x, start.y, start.x + 8, start.y + 8);
            var toCtr = new Point(4, 4);

            while (rect.ExpandDir != -1)
            {
                //still have to expand in a direction
                //order: 0, 2, 1, 3, (xpos,xneg,ypos,yneg)
                var dir = rect.ExpandDir;
                var startPt = StartLocation(rect, dir);
                var testPt = startPt;
                var inc = advanceByDir[(dir + 1) % 4];
                var count = Math.Abs(rect.GetByDir((dir + 1) % 4) - rect.GetByDir((dir + 3) % 4)) / 8;

                bool canExpand = true;
                for (int i = 0; i < count; i++)
                {
                    var tile = new LotTilePos((short)testPt.X, (short)testPt.Y, level);
                    if (!IsRoofable(tile))
                    {
                        canExpand = false;
                        break;
                    }
                    testPt += inc;
                }

                if (!canExpand) rect.ExpandDir = ExpandOrder[rect.ExpandDir];
                else
                {
                    //mark as complete - new roof rects cannot START on these tiles..
                    testPt = startPt;
                    for (int i = 0; i < count; i++)
                    {
                        evaluated[(testPt.X / 8) + (testPt.Y / 8) * width] = true;
                        testPt += inc;
                    }

                    //SPEEDUP: if expansion is within an existing roof rectangle skip to the other end of it
                    var midPt = startPt + new Point(inc.X * count / 2, inc.Y * count / 2) + toCtr;

                    var expandInto = result.FirstOrDefault(x => x.Contains(midPt) && RangeCheck(rect, x, dir));
                    if (expandInto != null)
                    {
                        rect.SetByDir(dir, expandInto.GetByDir(dir));
                    }
                    else
                    {
                        //on no detection, expand by 1
                        rect.SetByDir(dir, rect.GetByDir(dir) + ((dir > 1) ? -8 : 8));
                    }
                }
            }

            result.Add(rect);
        }

        public class RoofRect
        {
            public int ExpandDir;

            public int x1;
            public int x2;
            public int y1;
            public int y2;

            public int GetByDir(int dir)
            {
                switch (dir)
                {
                    case 0: return x2;
                    case 1: return y2;
                    case 2: return x1;
                    case 3: return y1;
                }
                return 0;
            }

            public void SetByDir(int dir, int value)
            {
                switch (dir)
                {
                    case 0: x2 = value; break;
                    case 1: y2 = value; break;
                    case 2: x1 = value; break;
                    case 3: y1 = value; break;
                }
            }

            public RoofRect(int x1, int y1, int x2, int y2)
            {
                if (x1 > x2) { var temp = x1; x1 = x2; x2 = temp; }
                if (y1 > y2) { var temp = y1; y1 = y2; y2 = temp; }

                this.x1 = x1;
                this.y1 = y1;
                this.x2 = x2;
                this.y2 = y2;
            }

            public bool Contains(Point pt)
            {
                return (pt.X >= x1 && pt.X <= x2) && (pt.Y >= y1 && pt.Y <= y2);
            }

            public bool HardContains(Point pt)
            {
                return (pt.X > x1 && pt.X < x2) && (pt.Y > y1 && pt.Y < y2);
            }

            public Point Closest(int x, int y)
            {
                return new Point(Math.Max(Math.Min(x2, x), x1), Math.Max(Math.Min(y2, y), y1));
            }

            public bool Intersects(RoofRect other)
            {
                return !((other.x1 >= x2 || other.x2 <= x1) || (other.y1 >= y2 || other.y2 <= y1));
            }
        }

        public bool TileIndoors(int x, int y, int level)
        {
            var room = blueprint.RoomMap[level - 1][x + y * blueprint.Width];
            var room1 = room & 0xFFFF;
            var room2 = (room >> 16) & 0x7FFF;
            if (room1 < blueprint.Rooms.Count && !blueprint.Rooms[(int)room1].IsOutside) return true;
            if (room2 > 0 && room2 < blueprint.Rooms.Count && !blueprint.Rooms[(int)room2].IsOutside) return true;
            return false;
        }

        public bool IndoorsOrFloor(int x, int y, int level)
        {
            if (level <= blueprint.Stories)
            {
                if (TileIndoors(x, y, level)) return true;
                if (blueprint.GetFloor((short)x, (short)y, (sbyte)level).Pattern != 0) return true;
                var wall = blueprint.GetWall((short)x, (short)y, (sbyte)level);
                if ((wall.Segments & WallSegments.AnyDiag) > 0) return true;
            }
            return false;
        }

        public bool IsRoofable(LotTilePos pos)
        {
            if (pos.Level == 1) return false;
            var tileX = pos.TileX;
            var tileY = pos.TileY;
            var level = pos.Level;
            if (tileX <= 0 || tileX >= blueprint.Width - 1 || tileY <= 0 || tileY >= blueprint.Height - 1) return false;
            var fDiag = false;
            //must be over indoors
            var halftile = false;
            if (!TileIndoors(tileX, tileY, level - 1))
            {
                //are a half tile away from indoors?
                bool found = false;
                if (pos.x % 16 == 8)
                {
                    if (TileIndoors(tileX + 1, tileY, level - 1)) found = true;
                }
                else
                {
                    if (TileIndoors(tileX - 1, tileY, level - 1)) found = true;
                }

                if (pos.y % 16 == 8)
                {
                    if (TileIndoors(tileX, tileY + 1, level - 1)) found = true;
                }
                else
                {
                    if (TileIndoors(tileX, tileY - 1, level - 1)) found = true;
                }

                if (TileIndoors(tileX + ((pos.x % 16 == 8) ? 1 : -1), tileY + ((pos.y % 16 == 8) ? 1 : -1), level - 1))
                {
                    if (!found) fDiag = true;
                    found = true;
                }
                if (!found) return false;
                halftile = true;
            }
            //on our level, the tile must not be indoors or floored
            if (IndoorsOrFloor(tileX, tileY, level)) return false;

            if (halftile)
            {
                if (pos.x % 16 == 8)
                {
                    if (IndoorsOrFloor(tileX + 1, tileY, level)) return false;
                }
                else
                {
                    if (IndoorsOrFloor(tileX - 1, tileY, level)) return false;
                }

                if (pos.y % 16 == 8)
                {
                    if (IndoorsOrFloor(tileX, tileY + 1, level)) return false;
                }
                else
                {
                    if (IndoorsOrFloor(tileX, tileY - 1, level)) return false;
                }
                
                if (fDiag && IndoorsOrFloor(tileX + ((pos.x % 16 == 8) ? 1 : -1), tileY + ((pos.y % 16 == 8) ? 1 : -1), level)) return false;
            }

            return true;
        }

        public void Dispose()
        {
            foreach (var buf in Drawgroups)
            {
                if (buf != null && buf.NumPrimitives > 0)
                {
                    buf.IndexBuffer.Dispose();
                    buf.VertexBuffer.Dispose();
                    
                    buf.AdvIndexBuffer?.Dispose();
                    buf.AdvVertexBuffer?.Dispose();
                }
            }
        }

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            var enableParallax = WorldConfig.Current.Complex && ParallaxTexture != null;
            device.RasterizerState = RasterizerState.CullNone;
            if (ShapeDirty)
            {
                RegenRoof(device);
                ShapeDirty = false;
                StyleDirty = false;
            }
            else if (StyleDirty)
            {
                RemeshRoof(device);
                StyleDirty = false;
            }

            device.RasterizerState = RasterizerState.CullClockwise;
            for (int i = 0; i < Drawgroups.Length; i++)
            {
                if (i > world.Level - 1) return;
                Effect.Level = (float)i + 1.0001f;
                if (Drawgroups[i] != null)
                {
                    var dg = Drawgroups[i];
                    if (dg.NumPrimitives == 0) continue;
                    PPXDepthEngine.RenderPPXDepth(Effect, true, (depthMode) =>
                    {
                        world._3D.ApplyCamera(Effect);
                        Effect.World = Matrix.Identity;
                        Effect.DiffuseColor = new Vector4(world.OutsideColor.R / 255f, world.OutsideColor.G / 255f, world.OutsideColor.B / 255f, 1.0f);
                        Effect.UseTexture = true;
                        Effect.BaseTex = Texture;
                        Effect.Alpha = 1f;
                        Effect.IgnoreColor = false;
                        Effect.TexOffset = Vector2.Zero;
                        Effect.TexMatrix = new Vector4(1, 0, 0, 1);

                        Effect.ParallaxUVTexMat = new Vector4(1, 0, 1, 0);
                        Effect.ParallaxHeight = 0.2f * TexRescale;
                        Effect.ParallaxTex = ParallaxTexture;
                        Effect.NormalMapTex = NormalMap;

                        Effect.GrassShininess = 0.01f;//005f);

                        var baseShad = (enableParallax) ? 3 : 2;

                        device.SetVertexBuffer(dg.VertexBuffer);
                        device.Indices = dg.IndexBuffer;

                        Effect.SetTechnique(GrassTechniques.DrawBase);
                        var pass = Effect.CurrentTechnique.Passes[(world.CameraMode == CameraRenderMode._3D)?baseShad:WorldConfig.Current.PassOffset];
                        pass.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, dg.NumPrimitives);

                        if (dg.AdvNumPrimitives > 0)
                        {
                            device.SetVertexBuffer(dg.AdvVertexBuffer);
                            device.Indices = dg.AdvIndexBuffer;
                            Effect.BaseTex = EdgeTexture;
                            Effect.GrassShininess = 0.003f;
                            pass = Effect.CurrentTechnique.Passes[(world.CameraMode == CameraRenderMode._3D) ? 2 : WorldConfig.Current.PassOffset];
                            pass.Apply();
                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, dg.AdvNumPrimitives);
                        }
                    });
                }
            }
            device.RasterizerState = RasterizerState.CullNone;
        }

        public void DrawOne(GraphicsDevice device, Matrix view, Matrix projection, WorldState world, int i)
        {
            device.RasterizerState = RasterizerState.CullNone;
            if (ShapeDirty)
            {
                RegenRoof(device);
                ShapeDirty = false;
                StyleDirty = false;
            }
            else if (StyleDirty)
            {
                RemeshRoof(device);
                StyleDirty = false;
            }
            if (i > world.Level - 1) return;
            Effect.Level = (float)i + 1.0001f;
            if (Drawgroups[i] != null)
            {
                var dg = Drawgroups[i];
                if (dg.NumPrimitives == 0) return;
                Effect.View = view;
                Effect.Projection = projection;
                Effect.World = Matrix.Identity;
                Effect.DiffuseColor = new Vector4(world.OutsideColor.R / 255f, world.OutsideColor.G / 255f, world.OutsideColor.B / 255f, 1.0f);
                Effect.UseTexture = true;
                Effect.BaseTex = Texture;
                Effect.Alpha = 1f;
                Effect.IgnoreColor = false;
                Effect.TexOffset = Vector2.Zero;
                Effect.TexMatrix = new Vector4(1, 0, 0, 1);

                Effect.FadeRectangle = new Vector4(-1000, -1000, 2000, 2000);
                Effect.FadeWidth = 35f * 3;

                device.SetVertexBuffer(dg.VertexBuffer);
                device.Indices = dg.IndexBuffer;

                Effect.SetTechnique(GrassTechniques.DrawBase);
                var pass = Effect.CurrentTechnique.Passes[2];
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, dg.NumPrimitives);

                if (dg.AdvNumPrimitives > 0)
                {
                    device.SetVertexBuffer(dg.AdvVertexBuffer);
                    device.Indices = dg.AdvIndexBuffer;
                    Effect.BaseTex = EdgeTexture;
                    Effect.GrassShininess = 0.003f;
                    pass = Effect.CurrentTechnique.Passes[2];
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, dg.AdvNumPrimitives);
                }
            }
        }

        public void DrawLMap(GraphicsDevice gd, LightData light, Matrix projection, Matrix lightTransform)
        {
            if (ShapeDirty)
            {
                RegenRoof(gd);
                ShapeDirty = false;
                StyleDirty = false;
            }
            else if (StyleDirty)
            {
                RemeshRoof(gd);
                StyleDirty = false;
            }

            var s = Matrix.Identity;
            s.M22 = 0;
            s.M33 = 0;
            s.M23 = 1;
            s.M32 = 1;

            for (int i = light.Level; i < Drawgroups.Length; i++)
            {
                Effect.Level = (float)i + 1;
                if (Drawgroups[i] != null)
                {
                    var dg = Drawgroups[i];
                    if (dg.NumPrimitives == 0) continue;

                    Effect.UseTexture = false;
                    Effect.Projection = projection;
                    var view = Matrix.Identity;
                    Effect.View = view;

                    var worldmat = Matrix.CreateScale(1 / 3f, 0, 1 / 3f) * Matrix.CreateTranslation(0, 1f * (i - (light.Level-1)), 0) * s * lightTransform;

                    Effect.World = worldmat;
                    Effect.DiffuseColor = new Vector4(1, 1, 1, 1) * (float)(4 - (i - (light.Level))) / 5f;

                    gd.SetVertexBuffer(dg.VertexBuffer);
                    gd.Indices = dg.IndexBuffer;

                    Effect.SetTechnique(GrassTechniques.DrawLMap);
                    foreach (var pass in Effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, dg.NumPrimitives);
                    }
                }
            }
        }
    }

    public class RoofData
    {
        public int[] Indices;
        public TerrainParallaxVertex[] Vertices;
        public int NumPrimitives;

        public int[] AdvIndices;
        public TerrainParallaxVertex[] AdvVertices;
        public int AdvNumPrimitives;
    }

    public class RoofDrawGroup
    {
        public IndexBuffer IndexBuffer;
        public VertexBuffer VertexBuffer;
        public int NumPrimitives;
        
        public IndexBuffer AdvIndexBuffer;
        public VertexBuffer AdvVertexBuffer;
        public int AdvNumPrimitives;
    }
}
