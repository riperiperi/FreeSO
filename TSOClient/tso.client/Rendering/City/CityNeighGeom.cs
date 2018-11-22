using FSO.Client.Rendering.City.Graph;
using FSO.Client.Rendering.City.Model;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels.Neighborhoods;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Files;
using FSO.Files.RC;
using MIConvexHull;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
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
        public Dictionary<int, int> NHoodToCell = new Dictionary<int, int>();
        public int HoverNHood = -1;

        public List<CityNeighbourhood> Data = new List<CityNeighbourhood>();
        private Terrain City;
        private Texture2D NhoodGrad;
        private Dictionary<int, float> HoverPct = new Dictionary<int, float>();
        public float TargetBannerPct = 0f;
        private float BannerPct = 0f;
        private CityCameraCenter MyCenter;

        private List<UINeighBanner> Banners = new List<UINeighBanner>();

        private float[] Gaussian5x5 = new float[] {
            1/256.0f, 4/256.0f, 6/256.0f, 4/256.0f, 1/256.0f,
            4/256.0f, 16/256.0f, 24/256.0f, 16/256.0f, 4/256.0f,
            6/256.0f, 24/256.0f, 36/256.0f, 24/256.0f, 6/256.0f,
            4/256.0f, 16/256.0f, 24/256.0f, 16/256.0f, 4/256.0f,
            1/256.0f, 4/256.0f, 6/256.0f, 4/256.0f, 1/256.0f,
        };

        public CityNeighGeom(Terrain city)
        {
            City = city;
            //RandomData();

            //gradient for when a neighbourhood is hovered.
            using (var file = File.Open(Path.Combine(FSOEnvironment.ContentDir, "Textures/nhoodGrad.png"), FileMode.Open, FileAccess.Read, FileShare.Read))
                NhoodGrad = ImageLoader.FromStream(GameFacade.GraphicsDevice, file);
        }

        public void RandomData()
        {
            var random = new Random();

            for (int i = 0; i < 100; i++)
            {
                Data.Add(new CityNeighbourhood()
                {
                    Name = "Rand" + i,
                    Location = new Point(random.Next(512), random.Next(512))
                } 
            );
            }

            CityNeighbourhood.Init(Data);
        }

        public void Generate(GraphicsDevice gd)
        {
            Dispose();

            var pts = Data.Select(x => x.Location.ToVector2() + new Vector2(0.5f, 0.5f)).ToList();
            pts.Add(new Vector2(-256, -256));
            pts.Add(new Vector2(768, 768));
            pts.Add(new Vector2(768, -256));
            pts.Add(new Vector2(-256, 768));

            pts.Add(new Vector2(256, -256));
            pts.Add(new Vector2(256, 768));
            pts.Add(new Vector2(768, 256));
            pts.Add(new Vector2(-256, 256));

            Cells = new VoronoiCellGraph(pts).Result;
            NHoodToCell.Clear();
            var index = 0;
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
                        new Vector3((float)vert.X, -1, (float)vert.Y),
                        Vector3.Zero,
                        new Vector2(0, ((-1)-City.InterpElevationAt(vert))/10)
                        ));

                    cV.Add(new DGRP3DVert(
                        new Vector3((float)vert.X, 100, (float)vert.Y),
                        Vector3.Zero,
                        new Vector2(0, (100 - City.InterpElevationAt(vert))/10)
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

                NHoodToCell.Add(cell.Ind, index++);
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

        public DepthStencilState StencilCompareLine = new DepthStencilState()
        {
            StencilFunction = CompareFunction.Equal,
            StencilEnable = true,
            DepthBufferEnable = true,
            DepthBufferFunction = CompareFunction.Greater,
            DepthBufferWriteEnable = false,
            StencilPass = StencilOperation.Zero,
            StencilFail = StencilOperation.Zero,
            StencilDepthBufferFail = StencilOperation.Zero
        };

        public void DrawHover(GraphicsDevice gd, SpriteBatch batch, Effect VertexShader, Effect PixelShader, CityContent content)
        {
            VertexShader.CurrentTechnique = VertexShader.Techniques[1];
            PixelShader.CurrentTechnique = PixelShader.Techniques[4];

            VertexShader.Parameters["ObjModel"].SetValue(Matrix.Identity);
            VertexShader.Parameters["DepthBias"].SetValue(0f);

            try
            {
                batch.Begin();
            }
            catch
            {

            }

            var toDraw = new HashSet<UINeighBanner>();
            var bannerContainer = (UIScreen.Current as UI.Screens.CoreGameScreen).CityFloatingContainer;

            foreach (var hover in HoverPct)
            {
                var id = hover.Key;
                var f = hover.Value;

                int cid;
                if (NHoodToCell.TryGetValue(id, out cid))
                {
                    var cell = Cells[cid];
                    var nhood = Data[id];
                    EdgeCell(gd, VertexShader, PixelShader, content, cell, (nhood.Color ?? Color.White) * f*0.6f);
                    FillCell(gd, VertexShader, PixelShader, content, cell, (nhood.Color ?? Color.White) * f*0.15f);
                    DrawCellBanner(cell, toDraw, bannerContainer, f);
                }
            }

            if (BannerPct > 0)
            {
                foreach (var cell in Cells)
                {
                    float oldOpacity = 0f;
                    HoverPct.TryGetValue(cell.Ind, out oldOpacity);
                    if (cell.Ind >= Data.Count) continue;
                    var nhood = Data[cell.Ind];
                    FillCell(gd, VertexShader, PixelShader, content, cell, (nhood.Color ?? Color.White) * BannerPct * 0.15f);
                    if (BannerPct > oldOpacity) DrawCellBanner(cell, toDraw, bannerContainer, BannerPct);
                }
                FillEdges(gd, VertexShader, PixelShader, content, Color.Black * 0.5f * BannerPct);
            }
            

            var toDelete = Banners.Except(toDraw).ToList();
            foreach (var del in toDelete)
            {
                Banners.Remove(del);
                bannerContainer.Remove(del);
            }

            try
            {
                batch.End();
            }
            catch
            {

            }
            /*
            for (int i = 0; i < Cells.Count; i++)
            {
                EdgeCell(gd, VertexShader, PixelShader, content, Cells[i], Colours[i % Colours.Length]);
            }
            */
        }

        private void DrawCellBanner(CompleteVCell cell, HashSet<UINeighBanner> toDraw, UIContainer bannerContainer, float f)
        {
            var screenCtr = City.transformSpr4(new Vector3(cell.Center.X, City.InterpElevationAt(cell.Center) + 5f, cell.Center.Y));

            if (screenCtr.Z > 0)
            {
                var banner = Banners.FirstOrDefault(x => x.DataID == cell.Ind);

                if (banner == null)
                {
                    var nhood = Data[cell.Ind];
                    banner = new UINeighBanner()
                    {
                        DataID = cell.Ind,
                        Caption = nhood.Name,
                        BannerColor = (nhood.Color ?? Color.White)
                    };
                    Banners.Add(banner);
                    bannerContainer.Add(banner);
                }

                var mulFactor = 120f;
                if (City.Camera is CityCamera2D) mulFactor = 0.5f;
                banner.ScaleX = banner.ScaleY = (mulFactor / screenCtr.W);
                banner.Position = new Vector2(screenCtr.X, screenCtr.Y);
                banner.Opacity = f;
                banner.Z = -screenCtr.Z;
                toDraw.Add(banner);
            }
        }

        private void EdgeCell(GraphicsDevice gd, Effect VertexShader, Effect PixelShader, CityContent content, CompleteVCell cell, Color color)
        {
            VertexShader.CurrentTechnique.Passes[2].Apply();

            gd.RasterizerState = RasterizerState.CullNone;
            gd.BlendState = BlendState.Additive;
            gd.DepthStencilState = DepthStencilState.DepthRead;

            PixelShader.Parameters["HighlightColor"].SetValue(color.ToVector4());
            PixelShader.Parameters["ObjTex"].SetValue(NhoodGrad);
            PixelShader.CurrentTechnique.Passes[0].Apply();

            gd.SetVertexBuffer(cell.Vertices);
            gd.Indices = cell.Indices;

            gd.SamplerStates[0] = SamplerState.LinearClamp;

            gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, cell.Indices.IndexCount / 3);
        }

        private void FillCell(GraphicsDevice gd, Effect VertexShader, Effect PixelShader, CityContent content, CompleteVCell cell, Color color)
        {
            VertexShader.CurrentTechnique.Passes[2].Apply();

            var frontDS = FrontDepthStencil;

            var bs = NoColor;

            gd.RasterizerState = RasterizerState.CullNone;
            gd.BlendState = bs;
            gd.DepthStencilState = frontDS;

            PixelShader.Parameters["ObjTex"].SetValue(TextureGenerator.GetPxWhite(gd));
            PixelShader.CurrentTechnique.Passes[0].Apply();

            gd.SetVertexBuffer(cell.Vertices);
            gd.Indices = cell.Indices;

            gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, cell.Indices.IndexCount / 3);

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

            PixelShader.Parameters["ObjTex"].SetValue(TextureGenerator.GetPxWhite(gd));// content.NeighTextures[i%3]);
            PixelShader.Parameters["HighlightColor"].SetValue(color.ToVector4());
            var bM = VertexShader.Parameters["BaseMatrix"].GetValueMatrix();
            VertexShader.Parameters["BaseMatrix"].SetValue(Matrix.CreateOrthographic(2, 2, -1, 1));
            VertexShader.CurrentTechnique.Passes[2].Apply();
            PixelShader.CurrentTechnique.Passes[0].Apply();
            gd.SamplerStates[0] = SamplerState.LinearWrap;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, dat, 0, 2);
            VertexShader.Parameters["BaseMatrix"].SetValue(bM);
        }

        public void FillEdges(GraphicsDevice gd, Effect VertexShader, Effect PixelShader, CityContent content, Color color)
        {
            VertexShader.CurrentTechnique.Passes[2].Apply();

            var frontDS = FrontDepthStencil;

            var bs = NoColor;

            gd.RasterizerState = RasterizerState.CullNone;
            gd.BlendState = bs;
            gd.DepthStencilState = frontDS;

            PixelShader.Parameters["ObjTex"].SetValue(TextureGenerator.GetPxWhite(gd));
            PixelShader.CurrentTechnique.Passes[0].Apply();

            foreach (var cell in Cells)
            {
                gd.SetVertexBuffer(cell.Vertices);
                gd.Indices = cell.Indices;

                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, cell.Indices.IndexCount / 3);
            }

            var so = StencilCompareLine;

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

            PixelShader.Parameters["ObjTex"].SetValue(TextureGenerator.GetPxWhite(gd));// content.NeighTextures[i%3]);
            PixelShader.Parameters["HighlightColor"].SetValue(color.ToVector4());
            var bM = VertexShader.Parameters["BaseMatrix"].GetValueMatrix();
            VertexShader.Parameters["BaseMatrix"].SetValue(Matrix.CreateOrthographic(2, 2, -1, 0.0001f));
            VertexShader.CurrentTechnique.Passes[2].Apply();
            PixelShader.CurrentTechnique.Passes[0].Apply();
            gd.SamplerStates[0] = SamplerState.LinearWrap;
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, dat, 0, 2);
            VertexShader.Parameters["BaseMatrix"].SetValue(bM);
        }

        public int NhoodNearestDB(int x, int y)
        {
            return ToDBID(NhoodNearest(x, y));
        }

        public int NhoodNearest(int x, int y)
        {
            return NhoodNearest(new Vector2(x + 0.5f, y + 0.5f));
        }

        public int NhoodNearest(Vector2 pos)
        {
            var bestDist = float.PositiveInfinity;
            var bestNhood = -1;

            var nhoodID = 0;
            foreach (var nhood in Data)
            {
                var dist = (nhood.Location.ToVector2() + new Vector2(0.5f, 0.5f) - pos).LengthSquared();
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestNhood = nhoodID;
                }
                nhoodID++;
            }
            return bestNhood;
        }

        public void Update(UpdateState state)
        {
            if (City.m_Zoomed == TerrainZoomMode.Far) {
                //find the nhood we're hovering
                var pos = City.EstTileAtPosWithScroll(state.MouseState.Position.ToVector2(), null);

                if (City.HandleMouse)
                {
                    HoverNHood = NhoodNearest(pos);
                    if (HoverNHood > -1 && !HoverPct.ContainsKey(HoverNHood))
                        HoverPct.Add(HoverNHood, 0f);
                } else
                {
                    HoverNHood = -1;
                }
            } else
            {
                HoverNHood = -1;
            }

            if (MyCenter != null)
            {
                if (City.Camera.CenterCam == null)
                {
                    MyCenter = null;
                    //if gizmo is present, we need to alert it that we've left center cam.
                }
                else
                {
                    HoverNHood = MyCenter.ID;
                    if (HoverNHood > -1 && !HoverPct.ContainsKey(HoverNHood))
                        HoverPct.Add(HoverNHood, 0f);
                }
            }

            if (City.m_Zoomed <= TerrainZoomMode.Near)
            {
                var list = HoverPct.ToList();
                var speed = 3f / FSOEnvironment.RefreshRate;
                foreach (var hover in list)
                {
                    var value = hover.Value + ((hover.Key == HoverNHood) ? speed : -speed);
                    if (value > 1) value = 1;
                    else if (value < 0)
                    {
                        HoverPct.Remove(hover.Key);
                        continue;
                    }
                    HoverPct[hover.Key] = value;
                }

                if (BannerPct != TargetBannerPct)
                {
                    var vel = ((TargetBannerPct > BannerPct) ? speed : -speed);
                    BannerPct = BannerPct + vel;
                    if (BannerPct > TargetBannerPct && vel > 0) BannerPct = TargetBannerPct;
                    else if (BannerPct < TargetBannerPct && vel < 0) BannerPct = TargetBannerPct;
                }

                var zoomBannerPct = City.Camera.FarUIFade;
                if (BannerPct > 1 - zoomBannerPct) BannerPct = 1 - zoomBannerPct;
            }
        }

        public int ToDBID(int nhoodID)
        {
            return Data[nhoodID].ID;
        }

        public int ToID(int nhoodDBID)
        {
            return Data.FindIndex(x => x.ID == nhoodDBID);
        }

        public void CenterNHood(int nhoodDBID)
        {
            var nhoodID = ToID(nhoodDBID);
            var nhood = Data[nhoodID];
            var cell = Cells[NHoodToCell[nhoodID]];
            var camSize = 2 - (float)Math.Sqrt(cell.Size) / 7;
            var center = new CityCameraCenter()
            {
                Center = cell.Center + new Vector2(0.5f, 0.5f),
                YAngle = 0,
                Dist = camSize,
                ID = nhoodID
            };
            MyCenter = center;
            City.Camera.CenterCamera(center);
        }

        public void Draw(GraphicsDevice gd, Effect VertexShader, Effect PixelShader, CityContent content)
        {
            VertexShader.CurrentTechnique = VertexShader.Techniques[1];
            PixelShader.CurrentTechnique = PixelShader.Techniques[4];

            VertexShader.Parameters["ObjModel"].SetValue(Matrix.Identity);
            VertexShader.Parameters["DepthBias"].SetValue(0f);

            for (int i=0; i<Cells.Count; i++)
            {
                var cell = Cells[i];
                var nhood = Data[cell.Ind];
                FillCell(gd, VertexShader, PixelShader, content, Cells[i], (nhood.Color ?? Color.White) * 0.2f);
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
