using FSO.Client.Rendering.City.Graph;
using FSO.Client.UI.Framework;
using FSO.Common.Utils;
using FSO.Files.RC;
using MIConvexHull;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Rendering.City
{
    /// <summary>
    /// Creates and manages voronoi tesselation of the city for neighbourhood rendering. 
    /// </summary>
    public class CityNeighGeom
    {
        public List<CompleteVCell> Cells;

        public CityNeighGeom()
        {
        }

        private Color[] Colours = new Color[] {
            new Color(255, 255, 255),
            new Color(125, 255, 255),
            new Color(255, 125, 255),
            new Color(255, 255, 125),
            new Color(125, 125, 255),
            new Color(255, 125, 125),
            new Color(125, 255, 125),
            new Color(0, 255, 255),
            new Color(255, 255, 0)
        };

        public void Generate(GraphicsDevice gd)
        {
            Dispose();
            var pts = new List<Vector2>();
            var random = new Random();

            for (int i = 0; i < 100; i++)
            {
                pts.Add(new Vector2((float)random.NextDouble() * 512, (float)random.NextDouble() * 512));
            }

            Cells = new VoronoiCellGraph(pts).Result;
            foreach (var cell in Cells)
            {
                //follow cell vertices, making them into a mesh.
                var cV = new List<DGRP3DVert>();
                var cI = new List<int>();

                var i = 0;
                foreach (var vert in cell.Cycle)
                {
                    //add a part of the edge loop.
                    cV.Add(new DGRP3DVert(
                        new Vector3((float)vert.X, -200, (float)vert.Y),
                        Vector3.Zero,
                        Vector2.Zero
                        ));

                    cV.Add(new DGRP3DVert(
                        new Vector3((float)vert.X, 400, (float)vert.Y),
                        Vector3.Zero,
                        Vector2.Zero
                        ));

                    //i mod
                    //0   1
                    //_____
                    //|\  |
                    //| \ |
                    //|__\|

                    // lo, hi, lo, hi
                    if (i > 0)
                    {
                        cI.Add(i - 2);
                        cI.Add(i - 1);
                        cI.Add(i);

                        cI.Add(i - 1);
                        cI.Add(i + 1);
                        cI.Add(i);
                    }
                    i += 2;
                }

                cI.Add(i - 2);
                cI.Add(i - 1);
                cI.Add(0);

                cI.Add(i - 1);
                cI.Add(1);
                cI.Add(0);

                //add top and bottom tri fans

                
                i = 0;
                foreach (var vert in cell.Cycle)
                {
                    if (i > 3) {
                        cI.Add(i-2); //bottom cap
                        cI.Add(i);
                        cI.Add(0);

                        /*
                        cI.Add(i-1); //top cap
                        cI.Add(1);
                        cI.Add(i+1);
                        */
                    }
                    i += 2;
                }
                

                DGRP3DVert.GenerateNormals(false, cV, cI);
                cV = cV.Select(x => new DGRP3DVert(x.Position + x.Normal * 0.25f, x.Normal, x.TextureCoordinate)).ToList();

                var verts = new VertexBuffer(gd, typeof(DGRP3DVert), cV.Count, BufferUsage.None);
                var inds = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, cI.Count, BufferUsage.None);
                verts.SetData(cV.ToArray());
                inds.SetData(cI.ToArray());

                cell.Vertices = verts;
                cell.Indices = inds;
            }
        }

        public BlendState NoColor = new BlendState()
        {
            ColorWriteChannels = ColorWriteChannels.None
        };

        public DepthStencilState FrontDepthStencil = new DepthStencilState()
        {
            DepthBufferWriteEnable = false,
            StencilDepthBufferFail = StencilOperation.Increment,
            TwoSidedStencilMode = true,
            StencilEnable = true,
            CounterClockwiseStencilDepthBufferFail = StencilOperation.Decrement
        };

        public DepthStencilState StencilCompare = new DepthStencilState()
        {
            StencilFunction = CompareFunction.NotEqual,
            StencilEnable = true,
            DepthBufferEnable = false,
            DepthBufferWriteEnable = false,
            StencilPass = StencilOperation.Zero,
            StencilFail = StencilOperation.Zero
        };

        public void Draw(GraphicsDevice gd, Effect VertexShader, Effect PixelShader, int passIndex, CityContent content)
        {
            VertexShader.CurrentTechnique = VertexShader.Techniques[1];
            PixelShader.CurrentTechnique = PixelShader.Techniques[4];

            VertexShader.Parameters["ObjModel"].SetValue(Matrix.Identity);
            VertexShader.Parameters["DepthBias"].SetValue(0f);

            var bM = VertexShader.Parameters["BaseMatrix"].GetValueMatrix();

            for (int i=0; i<Cells.Count; i++)
            {
                //draw each cell
                VertexShader.CurrentTechnique.Passes[2].Apply();

                //var col = (new Vector4(m_TintColor.R / 255.0f, m_TintColor.G / 255.0f, m_TintColor.B / 255.0f, 1) * 1.25f) / fsof.NightLightColor.ToVector4();
                //PixelShader.Parameters["LightCol"].SetValue(col);

                var frontDS = FrontDepthStencil;

                var bs = NoColor;

                gd.RasterizerState = RasterizerState.CullNone;
                gd.BlendState = bs;
                gd.DepthStencilState = frontDS;

                PixelShader.Parameters["ObjTex"].SetValue(TextureGenerator.GetPxWhite(gd));
                PixelShader.CurrentTechnique.Passes[0].Apply();

                gd.SetVertexBuffer(Cells[i].Vertices);
                gd.Indices = Cells[i].Indices;

                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, Cells[i].Indices.IndexCount / 3);

                var so = StencilCompare;

                gd.DepthStencilState = so;
                gd.BlendState = BlendState.AlphaBlend;

                var screen = UIScreen.Current;
                var aspect = screen.ScreenWidth / (float)(screen.ScreenHeight);
                var dat = new DGRP3DVert[]
                {
                new DGRP3DVert(new Vector3(-1, -1, 0f), Vector3.Zero, new Vector2(0, 0)),
                new DGRP3DVert(new Vector3(1, -1, 0f), Vector3.Zero, new Vector2(20*aspect, 0)),
                new DGRP3DVert(new Vector3(-1, 1, 0f), Vector3.Zero, new Vector2(0, 20)),
                new DGRP3DVert(new Vector3(1, 1, 0f), Vector3.Zero, new Vector2(20*aspect, 20)),
                };

                PixelShader.Parameters["ObjTex"].SetValue(content.NeighTextures[i%3]);
                PixelShader.Parameters["HighlightColor"].SetValue((Colours[i%Colours.Length]*0.5f).ToVector4());
                VertexShader.Parameters["BaseMatrix"].SetValue(Matrix.CreateOrthographic(2, 2, -1, 1));
                VertexShader.CurrentTechnique.Passes[2].Apply();
                PixelShader.CurrentTechnique.Passes[0].Apply();
                gd.SamplerStates[0] = SamplerState.LinearWrap;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, dat, 0, 2);
                VertexShader.Parameters["BaseMatrix"].SetValue(bM);
            }

            VertexShader.CurrentTechnique = VertexShader.Techniques[2];
            PixelShader.CurrentTechnique = PixelShader.Techniques[2];
        }

        public void Dispose()
        {
            if (Cells != null) {
                foreach (var cell in Cells)
                {
                    cell.Dispose();
                }
                Cells.Clear();
            }
        }
    }

    public class Vtx : IVertex
    {
        public double[] Position
        {
            get;set;
        }
        public int Index;
        public Vtx(double[] d)
        {
            Position = d;
        }

        public Vtx(Vector2 v, int index)
        {
            Position = new double[] { v.X, v.Y };
            Index = index;
        }
    }
}
