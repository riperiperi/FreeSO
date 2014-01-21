using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CityRenderer
{
    public class Terrain
    {
        GraphicsDevice m_GraphicsDevice;

        public bool ShadowsEnabled = true;
        public int ShadowRes = 2048;

        private CityDataRetriever m_CityData;
        private Dictionary<Vector2, LotTileEntry> m_CityLookup;
        private Dictionary<int, Texture2D> m_HouseGraphics;
        private Texture2D m_Elevation, m_VertexColor, m_TerrainType, m_ForestType, m_ForestDensity, m_RoadMap;
        private Color m_TintColor;

        public Texture2D Atlas, TransAtlas, RoadAtlas, RoadCAtlas;
        public Texture2D[] m_Roads = new Texture2D[16], m_RoadCorners = new Texture2D[16];
        public Effect Shader2D;
        private Color[] m_TerrainTypeColorData;
        private Texture2D[] m_TransA = new Texture2D[30], TransB = new Texture2D[30];
        private Texture2D m_Ground, m_Rock, m_Snow, m_Water, m_Sand, m_Forest, m_DefaultHouse, m_LotOnline, m_LotOffline;
        private Vector3 m_LightPosition;

        private MeshVertex[] m_Verts;
        private ArrayList m_2DVerts;

        private Dictionary<Color, int> m_ToBlendPrio = new Dictionary<Color, int>();
        private Dictionary<Color, double[]> m_AtlasOff = new Dictionary<Color, double[]>();
        private Dictionary<Color, int> m_ForestTypes = new Dictionary<Color, int>();
        private Dictionary<int, double> m_Prio2Map = new Dictionary<int, double>();
        private Dictionary<string, double[]> m_EdgeBLookup = new Dictionary<string, double[]>();
        private double[][] m_AtlasOffPrio = new double[5][];

        private byte[] m_ElevationData, m_ForestDensityData;
            
        private Color[] m_ForestTypeData;

        private MouseState m_MouseState, m_LastMouseState;
        private bool m_MouseMove = false, m_Zoomed = false;
        private Vector2 m_MouseStart;
        private float m_ScrollSpeed;
        private float m_ViewOffX, m_ViewOffY, m_TargVOffX, m_TargVOffY;
        private float m_ZoomProgress = 0;
        private float m_SpotOsc = 0;
        private float m_ShadowMult = 1;
        private double m_DayNightCycle = 0.0;
        private int[] m_SelTile = new int[] { -1, -1 };
        private Matrix m_MovMatrix;
        private Texture2D m_WhiteLine;
        private Texture2D m_stpWhiteLine;
        private VertexBuffer vertBuf;
        private int[][] m_SurTileOffs = new int[][] {
            new int[] {0, -1},
            new int[] {1, -1},
            new int[] {1, 0},
            new int[] {1, 1},
            new int[] {0, 1},
            new int[] {-1, 1},
            new int[] {-1, 0},
            new int[] {-1, -1},
        };

        private Color[] m_TimeColors = new Color[] {
            new Color(50, 70, 122),
            new Color(60, 80, 132),
            new Color(60, 80, 132),
            new Color(217, 109, 0),
            new Color(235, 235, 235),
            new Color(255, 255, 255),
            new Color(235, 235, 235),
            new Color(217, 109, 0),
            new Color(60, 80, 80),
            new Color(60, 80, 132),
            new Color(50, 70, 122)
        };

        private int m_Width, m_Height;

        public Terrain(GraphicsDevice GfxDevice, int CityNumber, CityDataRetriever cityData)
        {
            m_CityData = cityData;
            string CityStr = (CityNumber >= 10) ? "city_00" + CityNumber.ToString() : "city_000" + CityNumber.ToString();

            m_GraphicsDevice = GfxDevice;
            m_Elevation = Texture2D.FromFile(GfxDevice, CityStr + "\\elevation.bmp");
            m_VertexColor = Texture2D.FromFile(GfxDevice, CityStr + "\\vertexcolor.bmp");
            m_TerrainType = Texture2D.FromFile(GfxDevice, CityStr + "\\terraintype.bmp");
            m_ForestType = Texture2D.FromFile(GfxDevice, CityStr + "\\foresttype.bmp");
            m_ForestDensity = Texture2D.FromFile(GfxDevice, CityStr + "\\forestdensity.bmp");
            m_RoadMap = Texture2D.FromFile(GfxDevice, CityStr + "\\roadmap.bmp");

            m_Ground = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\gr.tga");
            m_Rock = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\rk.tga");
            m_Water = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\wt.tga");
            m_Sand = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\sd.tga");
            m_Snow = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\sn.tga");
            m_Forest = Texture2D.FromFile(GfxDevice, "gamedata\\farzoom\\forest00a.tga");
            m_DefaultHouse = Texture2D.FromFile(GfxDevice, "userdata\\houses\\defaulthouse.bmp", new TextureCreationParameters(128, 64, 24, 0, SurfaceFormat.Rgba32, TextureUsage.Linear, Color.Black, FilterOptions.None, FilterOptions.None));


            //LOAD THESE FROM MATCHMAKER.FAR IN REAL GAME!!! also remember the TextureCreationParameters when implementing that loading as it handles making the magenta transparent.
            m_LotOnline = Texture2D.FromFile(GfxDevice, "farzoomlotonline.bmp", new TextureCreationParameters(4, 3, 24, 0, SurfaceFormat.Rgba32, TextureUsage.Linear, new Color(255, 0, 255, 255), FilterOptions.None, FilterOptions.None));
            m_LotOffline = Texture2D.FromFile(GfxDevice, "farzoomlotreserved.bmp", new TextureCreationParameters(4, 3, 24, 0, SurfaceFormat.Rgba32, TextureUsage.Linear, new Color(255, 0, 255, 255), FilterOptions.None, FilterOptions.None));

            //fills used for line drawing

            m_WhiteLine = new Texture2D(m_GraphicsDevice, 1, 1);
            m_WhiteLine.SetData<Color>(new Color[] { Color.White });

            m_stpWhiteLine = new Texture2D(m_GraphicsDevice, 1, 1);
            m_stpWhiteLine.SetData<Color>(new Color[] { new Color(255, 255, 255, 128) });

            string Num;

            for (int x = 0; x < 30; x = x + 2)
            {
                Num = ZeroPad((x / 2).ToString(), 2);
                m_TransA[x] = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\transa" + Num + "a.tga");
                m_TransA[x + 1] = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\transa" + Num + "b.tga");
                //Debug.Write(x / 2 + "\r\n");
            }

            for (int x = 0; x < 30; x = x + 2)
            {
                Num = ZeroPad((x / 2).ToString(), 2);
                TransB[x] = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\transb" + Num + "a.tga");
                TransB[x + 1] = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\newformat\\transb" + Num + "b.tga");
            }

            for (int x = 0; x < 16; x++)
            {
                Num = ZeroPad((x).ToString(), 2);
                m_Roads[x] = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\road" + Num + ".tga");
            }

            for (int x = 0; x < 16; x++)
            {
                Num = ZeroPad((x).ToString(), 2);
                m_RoadCorners[x] = Texture2D.FromFile(GfxDevice, "gamedata\\terrain\\roadcorner" + Num + ".tga");
            }

            m_Width = m_Elevation.Width;
            m_Height = m_Elevation.Height;

            m_Verts = new MeshVertex[m_Width * m_Height * 6];
        }

        private string ZeroPad(string Str, int NumZeroes)
        {
            while (Str.Length < NumZeroes)
                Str = "0" + Str;

            return Str;
        }

        private void HandleGFXReset(object gfx, EventArgs x)
        {
            int size = MeshVertex.SizeInBytes * m_Verts.Length;
            vertBuf = new VertexBuffer((GraphicsDevice)gfx, size, BufferUsage.WriteOnly);
            vertBuf.SetData(m_Verts);
        }

        public void Initialize()
        {
            m_ToBlendPrio.Add(new Color(0, 255, 0), 0);     //grass
            m_ToBlendPrio.Add(new Color(12, 0, 255), 4);    //water
            m_ToBlendPrio.Add(new Color(255, 255, 255), 3); //snow
            m_ToBlendPrio.Add(new Color(255, 0, 0), 2);     //rock
            m_ToBlendPrio.Add(new Color(255, 255, 0), 1);   //sand
            m_ToBlendPrio.Add(new Color(0, 0, 0), -1);      //nothing, don't blend into this

            m_AtlasOff.Add(new Color(0, 255, 0), new double[] {0.0, 0.0});     //grass
            m_AtlasOff.Add(new Color(12, 0, 255), new double[] {0.5, 0.0});    //water
            m_AtlasOff.Add(new Color(255, 0, 0), new double[] {0.0, 0.25});     //rock
            m_AtlasOff.Add(new Color(255, 255, 255), new double[] {0.5, 0.25}); //snow
            m_AtlasOff.Add(new Color(255, 255, 0), new double[] {0.0, 0.5});   //sand
            m_AtlasOff.Add(new Color(0, 0, 0), new double[] {0.0, 0.0});      //nothing, don't blend into this


            m_Prio2Map.Add(0, 0);
            m_Prio2Map.Add(1, 0.5);
            m_Prio2Map.Add(2, 0.75);
            m_Prio2Map.Add(3, 0.25);
            m_Prio2Map.Add(4, 0);

            m_EdgeBLookup.Add("0000", new double[] { 15.0 / 16.0, 0 }); //none

            m_EdgeBLookup.Add("1000", new double[] { 13.0 / 16.0, 0 }); //top
            m_EdgeBLookup.Add("0100", new double[] { 14.0 / 16.0, 0 }); //right
            m_EdgeBLookup.Add("0010", new double[] { 7.0 / 16.0, 0 }); //bottom
            m_EdgeBLookup.Add("0001", new double[] { 11.0 / 16.0, 0 }); //left

            m_EdgeBLookup.Add("1010", new double[] { 5.0 / 16.0, 0 }); //top + bottom
            m_EdgeBLookup.Add("0101", new double[] { 10.0 / 16.0, 0 }); //left + right

            m_EdgeBLookup.Add("1001", new double[] { 9.0 / 16.0, 0 }); //left + top
            m_EdgeBLookup.Add("1100", new double[] { 12.0 / 16.0, 0 }); //right + top
            m_EdgeBLookup.Add("0110", new double[] { 6.0 / 16.0, 0 }); //right + bottom
            m_EdgeBLookup.Add("0011", new double[] { 3.0 / 16.0, 0 }); //left + bottom

            m_EdgeBLookup.Add("1101", new double[] { 8.0 / 16.0, 0 }); //all but bottom
            m_EdgeBLookup.Add("1110", new double[] { 4.0 / 16.0, 0 }); //all but left
            m_EdgeBLookup.Add("0111", new double[] { 2.0 / 16.0, 0 }); //all but top
            m_EdgeBLookup.Add("1011", new double[] { 1.0 / 16.0, 0 }); //all but right

            m_EdgeBLookup.Add("1111", new double[] { 0.0 / 15.0, 0 }); //all 

            m_AtlasOffPrio[0] = new double[] {0, 0};
            m_AtlasOffPrio[1] = new double[] {0, 0.5};
            m_AtlasOffPrio[2] = new double[] {0, 0.25};
            m_AtlasOffPrio[3] = new double[] {0.5, 0.25};
            m_AtlasOffPrio[4] = new double[] {0.5, 0};

            m_ForestTypes.Add(new Color(0, 0x6A, 0x28), 0);   //heavy forest
            m_ForestTypes.Add(new Color(0, 0xEB, 0x42), 1);   //light forest
            m_ForestTypes.Add(new Color(255, 0, 0), 2);   //cacti
            m_ForestTypes.Add(new Color(255, 0xFC, 0), 3);   //palm
            m_ForestTypes.Add(new Color(0, 0, 0), -1);  //nothing, don't blend into this

            m_HouseGraphics = new Dictionary<int,Texture2D>();
            populateCityLookup();
            m_GraphicsDevice.DeviceReset += new EventHandler(HandleGFXReset);
        }

        private void populateCityLookup()
        {
            LotTileEntry[] data = m_CityData.LotTileData;
            m_CityLookup = new Dictionary<Vector2, LotTileEntry>();
            for (int i = 0; i < data.Length; i++)
            {
                m_CityLookup[new Vector2 ( data[i].x, data[i].y )] = data[i];
            }
        }

        private void ApplyTransparentColor(Texture2D tex, Color tcolor)
        {
            Color[] ColorData = new Color[tex.Width * tex.Height];
            tex.GetData(ColorData);
            for (var i = 0; i < ColorData.Length; i++)
            {
                if (ColorData[i].Equals(tcolor)) ColorData[i] = Color.TransparentBlack;
            }
            tex.SetData(ColorData);
        }

        private void DrawLine(Texture2D Fill, Vector2 Start, Vector2 End, SpriteBatch spriteBatch, int lineWidth, float opacity)
        {
            double length = Math.Sqrt(Math.Pow(End.X - Start.X, 2) + Math.Pow(End.Y - Start.Y, 2));
            float direction = (float)Math.Atan2(End.Y - Start.Y, End.X - Start.X);
            Color tint = new Color(1, 1, 1, opacity);
            spriteBatch.Draw(Fill, new Rectangle((int)Start.X, (int)Start.Y-(int)(lineWidth/2), (int)length, lineWidth), null, tint, direction, new Vector2(0, 0.5f), SpriteEffects.None, 0); //
        }

        private Blend GetBlend(Color[] TerrainTypeData, int i, int j)
        {
            int[] edges;
            int sample;
            int t;

            edges = new int[] { -1, -1, -1, -1 };
            sample = m_ToBlendPrio[TerrainTypeData[i * 512 + j]];
            t = m_ToBlendPrio[TerrainTypeData[Positivize((i - 1) * 512 + j)] ];

            if ((i - 1 >= 0) && (t > sample)) edges[0] = t;
            t = m_ToBlendPrio[TerrainTypeData[i * 512 + j + 1] ];
            if ((j + 1 < 512) && (t > sample)) edges[1] = t;
            t = m_ToBlendPrio[TerrainTypeData[Math.Min((i + 1), 511) * 512 + j] ];
            if ((i + 1 < 512) && (t > sample)) edges[2] = t;
            t = m_ToBlendPrio[TerrainTypeData[i * 512 + j - 1] ];
            if ((j - 1 >= 0) && (t > sample)) edges[3] = t;

            var binary = new int[] {
		    (edges[0]>-1) ? 1:0,
		    (edges[1]>-1) ? 1:0,
		    (edges[2]>-1) ? 1:0,
		    (edges[3]>-1) ? 1:0};
            //Construct a string of the format "1011" to look up an edge.
            double[] temp = m_EdgeBLookup[ToBinaryString(binary)];
            double[] atlasPos = new double[] {temp[0], temp[1]};

            int maxEdge = 4;

            for (int x = 0; x < 4; x++)
                if (edges[x] < maxEdge && edges[x] != -1) maxEdge = edges[x];

            atlasPos[1] = m_Prio2Map[maxEdge];

            Blend ReturnBlend = new Blend();
            ReturnBlend.AtlasPosition = atlasPos;
            ReturnBlend.MaxEdge = maxEdge;

            return ReturnBlend;
        }

        /// <summary>
        /// Creates a texture atlas with which to texture the terrain.
        /// </summary>
        /// <param name="spriteBatch">A spritebatch to draw with.</param>
        public void CreateTextureAtlas(SpriteBatch spriteBatch)
        {
            DepthStencilBuffer OldDSBuffer = m_GraphicsDevice.DepthStencilBuffer;

            RenderTarget2D RTarget = new RenderTarget2D(m_GraphicsDevice, 512, 1024, 0, SurfaceFormat.Color,
                RenderTargetUsage.PreserveContents);
            //For some reason, we have to create a new depth stencil buffer with the same size
            //as the rendertarget in order to set the rendertarget on the GraphicsDevice.
            DepthStencilBuffer DSBuffer = new DepthStencilBuffer(m_GraphicsDevice, 512, 1024,
                OldDSBuffer.Format);

            m_GraphicsDevice.DepthStencilBuffer = DSBuffer;
            m_GraphicsDevice.SetRenderTarget(0, RTarget);

            m_GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            spriteBatch.Draw(m_Ground, new Rectangle(0, 0, m_Ground.Width, m_Ground.Height), Color.White);
            spriteBatch.Draw(m_Water, new Rectangle(256, 0, m_Water.Width, m_Water.Height), Color.White);
            spriteBatch.Draw(m_Rock, new Rectangle(0, 256, m_Rock.Width, m_Rock.Height), Color.White);
            spriteBatch.Draw(m_Snow, new Rectangle(256, 256, m_Snow.Width, m_Snow.Height), Color.White);
            spriteBatch.Draw(m_Sand, new Rectangle(0, 512, m_Sand.Width, m_Sand.Height), Color.White);
            spriteBatch.End();

            m_GraphicsDevice.SetRenderTarget(0, null);
            m_GraphicsDevice.DepthStencilBuffer = OldDSBuffer;

            Atlas = RTarget.GetTexture();
        }

        /// <summary>
        /// Creates a transparency atlas with which to texture the terrain.
        /// </summary>
        /// <param name="spriteBatch">A spritebatch to draw with.</param>
        public void CreateTransparencyAtlas(SpriteBatch spriteBatch)
        {
            DepthStencilBuffer OldDSBuffer = m_GraphicsDevice.DepthStencilBuffer;

            RenderTarget2D RTarget = new RenderTarget2D(m_GraphicsDevice, 1024, 256, 0, SurfaceFormat.Color,
                RenderTargetUsage.PreserveContents);
            //For some reason, we have to create a new depth stencil buffer with the same size
            //as the rendertarget in order to set the rendertarget on the GraphicsDevice.
            DepthStencilBuffer DSBuffer = new DepthStencilBuffer(m_GraphicsDevice, 1024, 256,
                OldDSBuffer.Format);

            m_GraphicsDevice.DepthStencilBuffer = DSBuffer;
            m_GraphicsDevice.SetRenderTarget(0, RTarget);

            m_GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            for (int i = 0; i < 30; i = i + 2)
            {
                spriteBatch.Draw(m_TransA[i], new Rectangle(i*32, 0, m_TransA[i].Width, m_TransA[i].Height), Color.White);
                spriteBatch.Draw(m_TransA[i + 1], new Rectangle(i*32, 64, m_TransA[i + 1].Width, m_TransA[i + 1].Height), Color.White);
            }

            for (int i = 0; i < 30; i = i + 2)
            {
                spriteBatch.Draw(TransB[i], new Rectangle(i*32, 128, m_TransA[i].Width, m_TransA[i].Height), Color.White);
                spriteBatch.Draw(TransB[i + 1], new Rectangle(i*32, 192, m_TransA[i + 1].Width, m_TransA[i + 1].Height), Color.White);
            }

            Texture2D black = new Texture2D(m_GraphicsDevice, 1, 1);
            black.SetData<Color>(new Color[] { Color.Black });
            spriteBatch.Draw(black, new Rectangle(1024-64, 0, 64, 256), Color.Black);
            //fill far end with black to cause no blend if adjacency bitmask is "0000"

            spriteBatch.End();

            m_GraphicsDevice.SetRenderTarget(0, null);
            m_GraphicsDevice.DepthStencilBuffer = OldDSBuffer;

            TransAtlas = RTarget.GetTexture();
        }

        public Texture2D CreateRoadAtlas(Texture2D[] input, SpriteBatch spriteBatch)
        {
            DepthStencilBuffer OldDSBuffer = m_GraphicsDevice.DepthStencilBuffer;

            RenderTarget2D RTarget = new RenderTarget2D(m_GraphicsDevice, 512, 512, 0, SurfaceFormat.Color,
                RenderTargetUsage.PreserveContents);
            //For some reason, we have to create a new depth stencil buffer with the same size
            //as the rendertarget in order to set the rendertarget on the GraphicsDevice.
            DepthStencilBuffer DSBuffer = new DepthStencilBuffer(m_GraphicsDevice, 512, 512,
                OldDSBuffer.Format);

            m_GraphicsDevice.DepthStencilBuffer = DSBuffer;
            m_GraphicsDevice.SetRenderTarget(0, RTarget);

            m_GraphicsDevice.Clear(Color.TransparentBlack);

            spriteBatch.Begin();

            for (int i = 0; i < 16; i++)
            {
                spriteBatch.Draw(input[i], new Rectangle((i%4) * 128, (int)(i/4.0)*128, 128, 128), Color.White);
            }

            spriteBatch.End();

            m_GraphicsDevice.SetRenderTarget(0, null);
            m_GraphicsDevice.DepthStencilBuffer = OldDSBuffer;

            return RTarget.GetTexture();
        }



        private string ToBinaryString(int[] Array)
        {
            StringBuilder StrBuilder = new StringBuilder();

            for (int i = 0; i < Array.Length; i++)
                StrBuilder.Append(Array[i].ToString());

            return StrBuilder.ToString();
        }

        /// <summary>
        /// Takes a number that might be negative, and returns the positive.
        /// </summary>
        /// <param name="PossibleNegative">Number that might be negative.</param>
        /// <returns>A positive, or the number passed in if it was positive.</returns>
        private int Positivize(int PossibleNegative)
        {

            if (PossibleNegative < 0)
                return PossibleNegative * -1;
            else
                return PossibleNegative;
        }
        public void GenerateCityMesh(GraphicsDevice GfxDevice)
        {
            int xStart, xEnd;

            Color[] ColorData = new Color[m_Width * m_Height];
            m_TerrainTypeColorData = new Color[m_TerrainType.Width * m_TerrainType.Height];
            Color[] ForestDensityData = new Color[m_ForestDensity.Width * m_ForestDensity.Height];
            m_ForestTypeData = new Color[m_ForestType.Width * m_ForestType.Height];

            Color[] RoadMapData = new Color[m_RoadMap.Width * m_RoadMap.Height];

            m_Elevation.GetData(ColorData);
            m_TerrainType.GetData(m_TerrainTypeColorData);
            m_ForestDensity.GetData(ForestDensityData);
            m_ForestType.GetData(m_ForestTypeData);
            m_RoadMap.GetData(RoadMapData);

            byte[] RoadData = ConvertToBinaryArray(RoadMapData);
            m_ElevationData = ConvertToBinaryArray(ColorData);
            m_ForestDensityData = ConvertToBinaryArray(ForestDensityData);

            int index = 0;

            for (int i = 0; i < 512; i++)
            {
                if (i < 306)
                    xStart = 306 - i;
                else
                    xStart = i - 306;
                if (i < 205)
                    xEnd = 307 + i;
                else
                    xEnd = 512 - (i - 205);
                for (var j = xStart; j < xEnd; j++)
                { //where the magic happens
                    var blendData = GetBlend(m_TerrainTypeColorData, i, j);

                    var bOff = blendData.AtlasPosition;
                    double[] temp = m_AtlasOff[m_TerrainTypeColorData[((i * 512) + j)]];
                    double[] off = new double[] { temp[0], temp[1] };
                    off[0] += 0.125 * (j % 4);
                    off[1] += (0.125 / 2.0) * (i % 4); //vertically 2 times as large
                    double[] temp2 = m_AtlasOffPrio[blendData.MaxEdge];
                    double[] off2 = new double[] { temp2[0], temp2[1] };
                    off2[0] += 0.125 * (j % 4);
                    off2[1] += (0.125 / 2.0) * (i % 4);

                    var toX = 0; //vertex colour offset
				    var toY = 0;

                    byte roadByte = RoadData[(i * 512 + j) * 4];
                    double[] off3 = new double[] { ((roadByte & 15) % 4) * 0.25, ((int)((roadByte & 15) / 4)) * 0.25 };
                    double[] off4 = new double[] { ((roadByte >> 4) % 4) * 0.25, ((int)((roadByte >> 4) / 4)) * 0.25 };

                    m_Verts[index].Coord.X = j;
                    m_Verts[index].Coord.Y = m_ElevationData[(i * 512 + j) * 4] / 12.0f; //elevation
                    m_Verts[index].Coord.Z = i;
                    m_Verts[index].TextureCoord.X = (j + toX) / 512.0f;
                    m_Verts[index].TextureCoord.Y = (i + toY) / 512.0f;
                    m_Verts[index].Texture2Coord.X = (float)off[0];
                    m_Verts[index].Texture2Coord.Y = (float)off[1];
                    m_Verts[index].Texture3Coord.X = (float)off2[0];
                    m_Verts[index].Texture3Coord.Y = (float)off2[1];
                    m_Verts[index].UVBCoord.X = (float)bOff[0];
                    m_Verts[index].UVBCoord.Y = (float)bOff[1];
                    m_Verts[index].RoadCoord.X = (float)(off3[0] + 0.25);
                    m_Verts[index].RoadCoord.Y = (float)off3[1];
                    m_Verts[index].RoadCCoord.X = (float)(off4[0] + 0.25);
                    m_Verts[index].RoadCCoord.Y = (float)off4[1];

                    index++;

                    m_Verts[index].Coord.X = j + 1;
                    m_Verts[index].Coord.Y = m_ElevationData[(i * 512 + Math.Min(511, j + 1)) * 4] / 12.0f; //elevation
                    m_Verts[index].Coord.Z = i;
                    m_Verts[index].TextureCoord.X = (j + toX + 1) / 512.0f;
                    m_Verts[index].TextureCoord.Y = (i + toY) / 512.0f;
                    m_Verts[index].Texture2Coord.X = (float)(off[0] + 0.125);
                    m_Verts[index].Texture2Coord.Y = (float)(off[1]);
                    m_Verts[index].Texture3Coord.X = (float)(off2[0] + 0.125);
                    m_Verts[index].Texture3Coord.Y = (float)(off2[1]);
                    m_Verts[index].UVBCoord.X = (float)(bOff[0] + 0.0625);
                    m_Verts[index].UVBCoord.Y = (float)bOff[1];
                    m_Verts[index].RoadCoord.X = (float)off3[0];
                    m_Verts[index].RoadCoord.Y = (float)off3[1];
                    m_Verts[index].RoadCCoord.X = (float)off4[0];
                    m_Verts[index].RoadCCoord.Y = (float)off4[1];

                    index++;

                    m_Verts[index].Coord.X = j + 1;
                    m_Verts[index].Coord.Y = m_ElevationData[(Math.Min(511, i + 1) * 512 + Math.Min(511, j + 1)) * 4] / 12.0f; //elevation
                    m_Verts[index].Coord.Z = i + 1;
                    m_Verts[index].TextureCoord.X = (j + toX + 1) / 512.0f;
                    m_Verts[index].TextureCoord.Y = (i + toY + 1) / 512.0f;
                    m_Verts[index].Texture2Coord.X = (float)(off[0] + 0.125);
                    m_Verts[index].Texture2Coord.Y = (float)(off[1] + 0.125 / 2.0);
                    m_Verts[index].Texture3Coord.X = (float)(off2[0] + 0.125);
                    m_Verts[index].Texture3Coord.Y = (float)(off2[1] + 0.125 / 2.0);
                    m_Verts[index].UVBCoord.X = (float)(bOff[0] + 0.0625);
                    m_Verts[index].UVBCoord.Y = (float)(bOff[1] + 0.25);
                    m_Verts[index].RoadCoord.X = (float)(off3[0]);
                    m_Verts[index].RoadCoord.Y = (float)(off3[1] + 0.25);
                    m_Verts[index].RoadCCoord.X = (float)(off4[0]);
                    m_Verts[index].RoadCCoord.Y = (float)(off4[1] + 0.25);

                    index++;

                    //tri 2

                    m_Verts[index].Coord.X = j;
                    m_Verts[index].Coord.Y = m_ElevationData[(i * 512 + j) * 4] / 12.0f; //elevation
                    m_Verts[index].Coord.Z = i;
                    m_Verts[index].TextureCoord.X = (j + toX) / 512.0f;
                    m_Verts[index].TextureCoord.Y = (i + toY) / 512.0f;
                    m_Verts[index].Texture2Coord.X = (float)(off[0]);
                    m_Verts[index].Texture2Coord.Y = (float)(off[1]);
                    m_Verts[index].Texture3Coord.X = (float)off2[0];
                    m_Verts[index].Texture3Coord.Y = (float)off2[1];
                    m_Verts[index].UVBCoord.X = (float)bOff[0];
                    m_Verts[index].UVBCoord.Y = (float)bOff[1];
                    m_Verts[index].RoadCoord.X = (float)(off3[0] + 0.25);
                    m_Verts[index].RoadCoord.Y = (float)off3[1];
                    m_Verts[index].RoadCCoord.X = (float)(off4[0] + 0.25);
                    m_Verts[index].RoadCCoord.Y = (float)off4[1];

                    index++;

                    m_Verts[index].Coord.X = j + 1;
                    m_Verts[index].Coord.Y = m_ElevationData[(Math.Min(511, i + 1) * 512 + Math.Min(511, j + 1)) * 4] / 12.0f; //elevation
                    m_Verts[index].Coord.Z = i + 1;
                    m_Verts[index].TextureCoord.X = (j + toX + 1) / 512.0f;
                    m_Verts[index].TextureCoord.Y = (i + toY + 1) / 512.0f;
                    m_Verts[index].Texture2Coord.X = (float)(off[0] + 0.125);
                    m_Verts[index].Texture2Coord.Y = (float)(off[1] + 0.125 / 2.0);
                    m_Verts[index].Texture3Coord.X = (float)(off2[0] + 0.125);
                    m_Verts[index].Texture3Coord.Y = (float)(off2[1] + 0.125 / 2.0);
                    m_Verts[index].UVBCoord.X = (float)(bOff[0] + 0.0625);
                    m_Verts[index].UVBCoord.Y = (float)(bOff[1] + 0.25);
                    m_Verts[index].RoadCoord.X = (float)(off3[0]);
                    m_Verts[index].RoadCoord.Y = (float)(off3[1] + 0.25);
                    m_Verts[index].RoadCCoord.X = (float)(off4[0]);
                    m_Verts[index].RoadCCoord.Y = (float)(off4[1] + 0.25);

                    index++;

                    m_Verts[index].Coord.X = j;
                    m_Verts[index].Coord.Y = m_ElevationData[(Math.Min(511, i + 1) * 512 + j) * 4] / 12.0f; //elevation
                    m_Verts[index].Coord.Z = i + 1;
                    m_Verts[index].TextureCoord.X = (j + toX) / 512.0f;
                    m_Verts[index].TextureCoord.Y = (i + toY + 1) / 512.0f;
                    m_Verts[index].Texture2Coord.X = (float)(off[0]);
                    m_Verts[index].Texture2Coord.Y = (float)(off[1] + 0.125 / 2.0);
                    m_Verts[index].Texture3Coord.X = (float)off2[0];
                    m_Verts[index].Texture3Coord.Y = (float)(off2[1] + 0.125 / 2.0);
                    m_Verts[index].UVBCoord.X = (float)bOff[0];
                    m_Verts[index].UVBCoord.Y = (float)(bOff[1] + 0.25);
                    m_Verts[index].RoadCoord.X = (float)(off3[0] + 0.25);
                    m_Verts[index].RoadCoord.Y = (float)(off3[1] + 0.25);
                    m_Verts[index].RoadCCoord.X = (float)(off4[0] + 0.25);
                    m_Verts[index].RoadCCoord.Y = (float)(off4[1] + 0.25);

                    index++;
                }
            }
            int size = MeshVertex.SizeInBytes * m_Verts.Length;
            vertBuf = new VertexBuffer(m_GraphicsDevice, size, BufferUsage.WriteOnly);
            vertBuf.SetData(m_Verts);
        }

        private byte[] ConvertToBinaryArray(Color[] ColorArray)
        {
            byte[] BinArray = new byte[ColorArray.Length * 4];

            for(int i = 0; i < ColorArray.Length; i++)
            {
                BinArray[i*4] = ColorArray[i].R;
                BinArray[i*4 + 1] = ColorArray[i].G;
                BinArray[i*4 + 2] = ColorArray[i].B;
                BinArray[i*4 + 3] = ColorArray[i].A;
            }

            return BinArray;
        }

        private uint[] ConvertToPackedArray(Color[] ColorArray)
        {
            uint[] PackedArray = new uint[ColorArray.Length];

            for (int i = 0; i < ColorArray.Length; i++)
            {
                PackedArray[i] = ColorArray[i].PackedValue;
            }

            return PackedArray;
        }

        private int[] GetHoverSquare()
        {
            double fisoScale = Math.Sqrt(0.5*0.5*2)/5.10; // is 5.10 on far zoom
            double zisoScale = Math.Sqrt(0.5*0.5*2)/144.0; // currently set 144 to near zoom
            double isoScale = (1-m_ZoomProgress)*fisoScale + (m_ZoomProgress)*zisoScale;
            double width = m_GraphicsDevice.Viewport.Width;
            float iScale = (float)(width/(width*isoScale*2));
            
            var mid = CalculateR(new Vector2(m_ViewOffX, -m_ViewOffY));
            mid.X -= 6;
            mid.Y += 6;
            var bounds = new double[] {Math.Round(mid.X-19), Math.Round(mid.Y-19), Math.Round(mid.X+19), Math.Round(mid.Y+19)};
            var pos = new double[] { m_MouseState.X, m_MouseState.Y };

            for (int y=(int)bounds[1]; y<bounds[3]; y++) {
                if (y < 0 || y > 511) continue;
                for (int x=(int)bounds[0]; x<bounds[2]; x++) {
                    if (x < 0 || x > 511) continue;
                    var xy = transformSpr(iScale, new Vector3(x+0, m_ElevationData[(y*512+x)*4]/12.0f, y+0));
                    var xy2 = transformSpr(iScale, new Vector3(x + 1, m_ElevationData[(y * 512 + Math.Min(x + 1, 511)) * 4] / 12.0f, y + 0));
                    var xy3 = transformSpr(iScale, new Vector3(x + 1, m_ElevationData[(Math.Min(y + 1, 511) * 512 + Math.Min(x + 1, 511)) * 4] / 12.0f, y + 1));
                    var xy4 = transformSpr(iScale, new Vector3(x + 0, m_ElevationData[(Math.Min(y + 1, 511) * 512 + x) * 4] / 12.0f, y + 1));
                    if (IsInsidePoly(new double[] {xy.X, xy.Y, xy2.X, xy2.Y, xy3.X, xy3.Y, xy4.X, xy4.Y}, pos)) return new int[] {x, y};
                }
            }
            return new int[] {-1, -1};
        }

        private bool IsInsidePoly(double[] Poly, double[] Pos)
        {
            if (Poly.Length % 2 != 0) return false; //invalid polygon
		    var n = Poly.Length / 2;
		    var result = false;
		    
            for (var i=0; i<n; i++)
            {
			    var x1 = Poly[i*2];
			    var y1 = Poly[i*2+1];
			    var x2 = Poly[((i+1)*2)%Poly.Length];
			    var y2 = Poly[((i+1)*2+1)%Poly.Length];
			    var slope = (y2-y1)/(x2-x1);
			    var c = y1-(slope*x1);
                if ((Pos[1] < (slope * Pos[0]) + c) && (Pos[0] >= Math.Min(x1, x2)) && (Pos[0] < Math.Max(x1, x2))) 
                    result = !(result);
		    }

		    return result;
        }

        private Vector2 CalculateR(Vector2 m)
        {
		    Vector2 ReturnM = new Vector2(m.X, m.Y);
		    ReturnM.Y = 2.0f * m.Y;
            float temp = ReturnM.X;
            double cos = Math.Cos((-45.0 / 180.0) * Math.PI);
            double sin = Math.Sin((-45.0 / 180.0) * Math.PI);
            ReturnM.X = (float)(cos * ReturnM.X + sin * ReturnM.Y);
            ReturnM.Y = (float)(cos * ReturnM.Y - sin * temp);
		    ReturnM.X += 254.55844122715712f;
		    ReturnM.Y += 254.55844122715712f;
		    return ReturnM;
	    }

        private void drawBorderSide(Vector2 xy, Vector2 xy2, Vector2 xy3, Vector2 xy4, SpriteBatch spriteBatch, float opacity)
        {
            double o = (17.0/144.0); //used for border segments
            double p = (1-o);

            double[] int1 = new double[] { xy.X * p + xy2.X * o, xy.Y * p + xy2.Y * o };
            double[] int2 = new double[] { xy4.X * p + xy3.X * o, xy4.Y * p + xy3.Y * o };
            double[] int3 = new double[] { xy.X * o + xy2.X * p, xy.Y * o + xy2.Y * p };
            double[] int4 = new double[] { xy4.X * o + xy3.X * p, xy4.Y * o + xy3.Y * p };

            DrawLine(m_stpWhiteLine, new Vector2((float)(int1[0]), (float)(int1[1])), new Vector2((float)(int1[0] * p + int2[0] * o), (float)(int1[1] * p + int2[1] * o)), spriteBatch, 2, opacity);
            DrawLine(m_stpWhiteLine, new Vector2((float)(int1[0] * p + int2[0] * o), (float)(int1[1] * p + int2[1] * o)), new Vector2((float)(int3[0] * p + int4[0] * o), (float)(int3[1] * p + int4[1] * o)), spriteBatch, 2,opacity);
            DrawLine(m_stpWhiteLine, new Vector2((float)(int3[0] * p + int4[0] * o), (float)(int3[1] * p + int4[1] * o)), new Vector2((float)(int3[0]), (float)(int3[1])), spriteBatch, 2, opacity);
        }

        private void drawPartLine(Vector2 xy, Vector2 xy2, SpriteBatch spriteBatch, float opacity)
        {
            double o = (17.0/144.0); //used for border segments
            double p = (1-o);

            DrawLine(m_stpWhiteLine, new Vector2((float)(xy.X * p + xy2.X * o), (float)(xy.Y * p + xy2.Y * o)), new Vector2((float)(xy2.X * p + xy.X * o), (float)(xy2.Y * p + xy.Y * o)), spriteBatch, 2, opacity);
        }

        private void drawTileCorner(Vector2 xy, Vector2 xy2, Vector2 xy3, SpriteBatch spriteBatch, float opacity)
        {
		    double o = (17.0/144.0); //used for border segments
		    double p = (1-o);
            DrawLine(m_stpWhiteLine, new Vector2((float)(xy2.X * p + xy.X * o), (float)(xy2.Y * p + xy.Y * o)), new Vector2((float)(xy2.X), (float)(xy2.Y)), spriteBatch, 2, opacity);
            DrawLine(m_stpWhiteLine, new Vector2((float)(xy2.X), (float)(xy2.Y)), new Vector2((float)(xy2.X * p + xy3.X * o), (float)(xy2.Y * p + xy3.Y * o)), spriteBatch, 2, opacity);
	    }

        private void DrawTileBorders(float iScale, SpriteBatch spriteBatch) {

            Vector2 offset = new Vector2(0, 0);

            if (m_SelTile[0] != -1)
            {
                for (int x = m_SelTile[0] - 3; x < m_SelTile[0] + 4; x++)
                {
                    if (x < 0 || x > 511) continue;
                    for (int y = m_SelTile[1] - 3; y < m_SelTile[1] + 4; y++)
                    {
                        if (y < 0 || y > 511) continue;
                        
                        Vector2 xy = transformSpr(iScale, new Vector3(x+0, m_ElevationData[(y * 512 + x) * 4] / 12.0f, y + 0)) + offset;
                        Vector2 xy2 = transformSpr(iScale, new Vector3(x + 1, m_ElevationData[(y * 512 + Math.Min(x + 1, 511)) * 4] / 12.0f, y + 0)) + offset;
                        Vector2 xy3 = transformSpr(iScale, new Vector3(x + 1, m_ElevationData[(Math.Min(y + 1, 511) * 512 + Math.Min(x + 1, 511)) * 4] / 12.0f, y + 1)) + offset;
                        Vector2 xy4 = transformSpr(iScale, new Vector3(x + 0, m_ElevationData[(Math.Min(y + 1, 511) * 512 + x) * 4] / 12.0f, y + 1)) + offset;

                        Vector2 mousedist = ((xy + xy2 + xy3 + xy4) / 4.0f - new Vector2(m_MouseState.X, m_MouseState.Y));

                        bool[] surTile = new bool[8];
                        for (var i=0; i<m_SurTileOffs.Length; i++) {
                            surTile[i] = (isLandBuildable(x + m_SurTileOffs[i][0], y + m_SurTileOffs[i][1]));
                        }

                        float opacity = (float)(1.0 - (mousedist.Length() / 200.0));

                        if (isLandBuildable(x, y))
                        {

                            if (surTile[0]) drawBorderSide(xy, xy2, xy3, xy4, spriteBatch, opacity);
                            else drawPartLine(xy, xy2, spriteBatch, opacity);
                            if (surTile[2]) drawBorderSide(xy2, xy3, xy4, xy, spriteBatch, opacity);
                            else drawPartLine(xy2, xy3, spriteBatch, opacity);
                            if (surTile[4]) drawBorderSide(xy3, xy4, xy, xy2, spriteBatch, opacity);
                            else drawPartLine(xy3, xy4, spriteBatch, opacity);
                            if (surTile[6]) drawBorderSide(xy4, xy, xy2, xy3, spriteBatch, opacity);
                            else drawPartLine(xy4, xy, spriteBatch, opacity);

                            if (!(surTile[0] && surTile[1] && surTile[2])) drawTileCorner(xy, xy2, xy3, spriteBatch, opacity);
                            if (!(surTile[2] && surTile[3] && surTile[4])) drawTileCorner(xy2, xy3, xy4, spriteBatch, opacity);
                            if (!(surTile[4] && surTile[5] && surTile[6])) drawTileCorner(xy3, xy4, xy, spriteBatch, opacity);
                            if (!(surTile[6] && surTile[7] && surTile[0])) drawTileCorner(xy4, xy, xy2, spriteBatch, opacity);
                        }
                        else
                        {
                            DrawLine(m_stpWhiteLine, xy, xy2, spriteBatch, 2, opacity);
                            DrawLine(m_stpWhiteLine, xy2, xy3, spriteBatch, 2, opacity);
                        }

                        double o = (17.0/144.0); //used for border segments
                        double p = (1-o);

                        if (x == m_SelTile[0] && y == m_SelTile[1])
                        {
                            DrawLine(m_WhiteLine, xy, xy2, spriteBatch, 2, 1);
                            DrawLine(m_WhiteLine, xy2, xy3, spriteBatch, 2, 1);
                            DrawLine(m_WhiteLine, xy3, xy4, spriteBatch, 2, 1);
                            DrawLine(m_WhiteLine, xy4, xy, spriteBatch, 2, 1);
                        }
                    }
                }
            }
        }

        private bool isLandBuildable(int x, int y) {
            if (x < 0 || x > 510 || y < 0 || y > 510) return false; //because of +1s, use 510 as bound rather than 511. People won't see those tiles at near view anyways.

            if (m_TerrainTypeColorData[y * 512 + x] == new Color(0x0C, 0, 255)) return false; //if on water, not buildable

            //gets max and min elevation of the 4 verts of this tile, and compares them against a threshold. This threshold should be EXACTLY THE SAME ON THE SERVER! 
            //This is so that the game and the server have the same ideas on what is buildable and what is not.

            int max = Math.Max(m_ElevationData[(y*512+x)*4], Math.Max(m_ElevationData[(y*512+x+1)*4], Math.Max(m_ElevationData[((y+1)*512+x+1)*4], m_ElevationData[((y+1)*512+x)*4])));
            int min = Math.Min(m_ElevationData[(y*512+x)*4], Math.Min(m_ElevationData[(y*512+x+1)*4], Math.Min(m_ElevationData[((y+1)*512+x+1)*4], m_ElevationData[((y+1)*512+x)*4])));

            return (max-min < 10); //10 is the threshold for now
        }

        private void DrawSpotlights(float HB)
        {
            float iScale = (float)m_GraphicsDevice.Viewport.Width/(HB*2.0f);
		
            float spotlightScale = (float)(iScale*(2.0*Math.Sqrt(0.5*0.5*2)/5.10));
            LotTileEntry[] lots = m_CityData.LotTileData;

            for (var i = 0; i < lots.Length; i++)
            {
                if ((lots[i].flags & 2) > 0)
                {
                    Vector2 pos = new Vector2(lots[i].x, lots[i].y);
                    Vector2 xy = transformSpr(iScale, new Vector3(pos.X + 0.5f, m_ElevationData[((int)pos.Y * 512 + (int)pos.X) * 4] / 12.0f, pos.Y + 0.5f));
                    Vector3 xyz = new Vector3(xy.X, xy.Y, 1);

                    Matrix trans = Matrix.Identity;
                    trans = Matrix.CreateRotationZ((float)(0.33 * Math.Sin(2.0 * Math.PI * ((m_SpotOsc + i * 0.43) % 1))));

                    m_2DVerts.Add(new VertexPositionColor(xyz, new Color(1, 1, 1, 0.5f)));
                    m_2DVerts.Add(new VertexPositionColor((xyz + (Vector3.Transform(new Vector3(-12, -100, 0), trans) * spotlightScale)), new Color(1, 1, 1, 0.0f)));
                    m_2DVerts.Add(new VertexPositionColor((xyz + (Vector3.Transform(new Vector3(12, -100, 0), trans) * spotlightScale)), new Color(1, 1, 1, 0.0f)));
                }
            }
        }

        private void DrawHouses(float HB)
        {
            SpriteBatch spriteBatch = new SpriteBatch(m_GraphicsDevice);
            spriteBatch.Begin();
            float iScale = (float)m_GraphicsDevice.Viewport.Width / (HB * 2);
            LotTileEntry[] lots = m_CityData.LotTileData;
            for (int i=0; i<lots.Length; i++) {
				short x = lots[i].x;
				short y = lots[i].y;
				Vector2 xy = transformSpr(iScale, new Vector3(x+0.5f, m_ElevationData[(y*512+x)*4]/12.0f, y+0.5f));
				bool online = ((lots[i].flags & 1) == 1);
                Texture2D img = (online) ? m_LotOnline : m_LotOffline;
				double alpha = online?(0.5+Math.Sin(4*Math.PI*(m_SpotOsc%1))/2.0):1;
				spriteBatch.Draw(img, new Rectangle((int)Math.Round(xy.X-1), (int)Math.Round(xy.Y-2), 4, 3), new Color(1.0f, 1.0f, 1.0f, (float)alpha));
			}
            spriteBatch.End();
        }

        private void PathTile(int x, int y, float iScale, float opacity) {
            Vector2 xy = transformSpr(iScale, new Vector3(x + 0, m_ElevationData[(y * 512 + x) * 4] / 12.0f, y + 0));
            Vector2 xy2 = transformSpr(iScale, new Vector3(x + 1, m_ElevationData[(y * 512 + Math.Min(x + 1, 511)) * 4] / 12.0f, y + 0));
            Vector2 xy3 = transformSpr(iScale, new Vector3(x + 1, m_ElevationData[(Math.Min(y + 1, 511) * 512 + Math.Min(x + 1, 511)) * 4] / 12.0f, y + 1));
            Vector2 xy4 = transformSpr(iScale, new Vector3(x + 0, m_ElevationData[(Math.Min(y + 1, 511) * 512 + x) * 4] / 12.0f, y + 1));
					
            m_2DVerts.Add(new VertexPositionColor(new Vector3(xy, 1), new Color(1.0f, 1.0f, 1.0f, opacity)));
            m_2DVerts.Add(new VertexPositionColor(new Vector3(xy2, 1), new Color(1.0f, 1.0f, 1.0f, opacity)));
            m_2DVerts.Add(new VertexPositionColor(new Vector3(xy3, 1), new Color(1.0f, 1.0f, 1.0f, opacity)));

            m_2DVerts.Add(new VertexPositionColor(new Vector3(xy, 1), new Color(1.0f, 1.0f, 1.0f, opacity)));
            m_2DVerts.Add(new VertexPositionColor(new Vector3(xy3, 1), new Color(1.0f, 1.0f, 1.0f, opacity)));
            m_2DVerts.Add(new VertexPositionColor(new Vector3(xy4, 1), new Color(1.0f, 1.0f, 1.0f, opacity)));
	    }

        private void DrawSprites(float HB, float VB)
        {
            SpriteBatch spriteBatch = new SpriteBatch(m_GraphicsDevice);
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend);
            if (m_ZoomProgress < 0.5)
            {
                spriteBatch.End();
                return;
            }

            float iScale = (float)m_GraphicsDevice.Viewport.Width / (HB * 2);

		    float treeWidth = (float)(Math.Sqrt(2)*(128.0/144.0));
		    float treeHeight = treeWidth*(80/128);

		    Vector2 mid = CalculateR(new Vector2(m_ViewOffX, -m_ViewOffY));
		    mid.X -= 6;
		    mid.Y += 6;
            float[] bounds = new float[] { (float)Math.Round(mid.X - 19), (float)Math.Round(mid.Y - 19), (float)Math.Round(mid.X + 19), (float)Math.Round(mid.Y + 19) };
    		
		    var img = m_Forest;
		    float fade = Math.Max(0, Math.Min(1, (m_ZoomProgress - 0.4f) * 2));

            DrawTileBorders(iScale, spriteBatch);

            for (short y = (short)bounds[1]; y < bounds[3]; y++)
            {
                if (y < 0 || y > 511) continue;
                for(short x = (short)bounds[0]; x < bounds[2]; x++)
                {
                    if (x < 0 || x > 511) continue;

                    float elev = (m_ElevationData[(y * 512 + x) * 4] + m_ElevationData[(y * 512 + Math.Min(x + 1, 511)) * 4] + 
                        m_ElevationData[(Math.Min(y + 1, 511) * 512 + Math.Min(x + 1, 511)) * 4] + 
                        m_ElevationData[(Math.Min(y + 1, 511) * 512 + x) * 4]) / 4;
				    double fType = m_ForestTypes[m_ForestTypeData[(y * 512 + x)]];
				    double fDens = Math.Round((double)(m_ForestDensityData[(y * 512 + x) * 4] * 4 / 255));

                    var xy = transformSpr(iScale, new Vector3((float)(x + 0.5), elev / 12.0f, (float)(y + 0.5)));

                    if (xy.X > -64 && xy.X < m_GraphicsDevice.Viewport.Width + 64 && xy.Y > -40 && xy.Y < m_GraphicsDevice.Viewport.Height + 40)
                    {

                        Vector2 loc = new Vector2( x, y );
                        LotTileEntry house;

                        if (m_CityLookup.ContainsKey(loc))
                        {
                            house = m_CityLookup[loc];
                        }
                        else
                        {
                            house = null;
                        }
                        if (house != null)
                        {
                            if ((house.flags & 1) > 0) {
							    PathTile(x, y, iScale, (float)(0.3+Math.Sin(4*Math.PI*(m_SpotOsc%1))*0.15));
						    }

                            double scale = treeWidth * iScale / 128.0;
                            if (!m_HouseGraphics.ContainsKey(house.lotid)) {
							    //no house graphic found - request one!
                                m_HouseGraphics[house.lotid] = m_DefaultHouse;
                                m_CityData.RetrieveHouseGFX(house.lotid, m_HouseGraphics, m_GraphicsDevice);
						    }
                            Texture2D lotImg = m_HouseGraphics[house.lotid];
                            spriteBatch.Draw(lotImg, new Rectangle((int)(xy.X - 64.0 * scale), (int)(xy.Y - 32.0 * scale), (int)(scale * 128), (int)(scale * 64)), m_TintColor);
                        }
                        else
                        {
                            if (!(fType == -1 || fDens == 0))
                            {

                                double scale = treeWidth * iScale / 128.0;
                                spriteBatch.Draw(m_Forest, new Rectangle((int)(xy.X - 64.0 * scale), (int)(xy.Y - 56.0 * scale), (int)(scale * 128), (int)(scale * 80)), new Rectangle((int)(128 * (fDens - 1)), (int)(80 * fType), 128, 80), m_TintColor);

                            }
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            Draw2DPoly();
            spriteBatch.End();
        }

        public Vector2 transformSpr(float iScale, Vector3 pos) {
            Vector3 temp = Vector3.Transform(pos, m_MovMatrix);
            int width = m_GraphicsDevice.Viewport.Width;
            int height = m_GraphicsDevice.Viewport.Height;
            return new Vector2((temp.X-m_ViewOffX)*iScale+width/2, (-(temp.Y-m_ViewOffY)*iScale)+height/2);
        }

        public void Update()
        {
            m_LastMouseState = m_MouseState;
            m_MouseState = Mouse.GetState();

            m_MouseMove = (m_MouseState.MiddleButton == ButtonState.Pressed);

            if (m_Zoomed)
            {
                m_SelTile = GetHoverSquare();
            }

            if (m_MouseState.MiddleButton == ButtonState.Pressed && m_LastMouseState.MiddleButton == ButtonState.Released)
            {
                m_MouseStart = new Vector2(m_MouseState.X, m_MouseState.Y);
            }

            else if(m_MouseState.LeftButton == ButtonState.Released && m_LastMouseState.LeftButton == ButtonState.Pressed)
            {
                if (m_Zoomed)
                    m_Zoomed = false;
                else
                {
                    m_Zoomed = true;
                    var isoScale = Math.Sqrt(0.5 * 0.5 * 2) / 5.10;
                    var hb = m_GraphicsDevice.Viewport.Width * isoScale;
				    var vb = m_GraphicsDevice.Viewport.Height * isoScale;

				    m_TargVOffX = (float)(-hb+m_MouseState.X * isoScale * 2);
                    m_TargVOffY = (float)(vb - m_MouseState.Y * isoScale * 2);
                }
            }



            FixedTimeUpdate();
            SetTimeOfDay(m_DayNightCycle%1);
            m_DayNightCycle += 0.001; //adjust the cycle speed here. When ingame, set m_DayNightCycle to to the percentage of time passed through the day. (0 to 1)

            m_ViewOffX = (m_TargVOffX) * m_ZoomProgress;
            m_ViewOffY = (m_TargVOffY) * m_ZoomProgress;

        }

        private void SetTimeOfDay(double time) {
            Color col1 = m_TimeColors[(int)Math.Floor(time * (m_TimeColors.Length - 1))];
            Color col2 = m_TimeColors[(int)Math.Floor(time * (m_TimeColors.Length - 1))+1];
            double Progress = (time * (m_TimeColors.Length - 1)) % 1;

            m_TintColor = Color.Lerp(col1, col2, (float)Progress);

            m_LightPosition = new Vector3(0, 0, -263);
            Matrix Transform = Matrix.Identity;

            Transform *= Matrix.CreateRotationY((float)((((time+0.25)%0.5)+0.5) * Math.PI * 2.0));
            Transform *= Matrix.CreateRotationZ((float)(Math.PI*(45.0/180.0)));
            Transform *= Matrix.CreateRotationY((float)(Math.PI * 0.3));
            Transform *= Matrix.CreateTranslation(new Vector3(256, 0, 256));

            m_LightPosition = Vector3.Transform(m_LightPosition, Transform);

            if (Math.Abs((time % 0.5) - 0.25) < 0.05)
            {
                m_ShadowMult = (float)(1-(Math.Abs((time % 0.5) - 0.25)*20))*0.35f+0.65f;
            }
            else
            {
                m_ShadowMult = 0.65f;
            }
        }

        private void FixedTimeUpdate()
        {
            m_SpotOsc = (m_SpotOsc + 0.01f) % 1;
            if (m_Zoomed)
            {
                m_ZoomProgress += (1.0f - m_ZoomProgress) / 5.0f;
                bool Triggered = false;

                if (m_MouseMove)
                {
                    m_TargVOffX += (m_MouseState.X - m_MouseStart.X) / 1000;
                    m_TargVOffY -= (m_MouseState.Y - m_MouseStart.Y) / 1000;
                    
                    /*var dir = Math.Round((Math.Atan2(m_MouseStart.X - m_MouseState.Y,
                        m_MouseState.X - m_MouseStart.X) / Math.PI) * 4) + 4;
                    ChangeCursor(dir);*/
                }
                else
                {
                    if (m_MouseState.X > m_GraphicsDevice.Viewport.Width - 32)
                    {
					    Triggered = true;
					    m_TargVOffX += m_ScrollSpeed;
					    //changeCursor("right.cur")
				    }
                    if (m_MouseState.X < 32) 
                    {
					    Triggered = true;
					    m_TargVOffX -= m_ScrollSpeed;
					    //changeCursor("left.cur");
				    }
                    if (m_MouseState.Y > m_GraphicsDevice.Viewport.Height - 32)
                    {
					    Triggered = true;
					    m_TargVOffY -= m_ScrollSpeed;
					    //changeCursor("down.cur");
				    }
                    if (m_MouseState.Y < 32)
                    {
					    Triggered = true;
                        m_TargVOffY += m_ScrollSpeed;
					    //changeCursor("up.cur");
				    } 

				    if (!Triggered)
                    {
					    m_ScrollSpeed = 0.1f;
					    //changeCursor("auto", true);
				    } 
                    else
					    m_ScrollSpeed += 0.005f;
                }

                m_TargVOffX = Math.Max(-135, Math.Min(m_TargVOffX, 138));
                m_TargVOffY = Math.Max(-100, Math.Min(m_TargVOffY, 103));
            }
            else
                m_ZoomProgress += (0 - m_ZoomProgress) / 5.0f;
        }

        private Texture2D DrawDepth(Effect VertexShader, Effect PixelShader)
        {
            DepthStencilBuffer OldDSBuffer = m_GraphicsDevice.DepthStencilBuffer;

            RenderTarget2D RTarget = new RenderTarget2D(m_GraphicsDevice, ShadowRes, ShadowRes, 0, SurfaceFormat.Single,
                RenderTargetUsage.PreserveContents);
            DepthStencilBuffer DSBuffer = new DepthStencilBuffer(m_GraphicsDevice, ShadowRes, ShadowRes,
                OldDSBuffer.Format);

            m_GraphicsDevice.DepthStencilBuffer = DSBuffer;
            m_GraphicsDevice.SetRenderTarget(0, RTarget);

            m_GraphicsDevice.Clear(Color.CornflowerBlue);

            VertexShader.Begin();
            VertexShader.CurrentTechnique.Passes[1].Begin();
            PixelShader.Begin();
            PixelShader.CurrentTechnique.Passes[1].Begin();

            m_GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, m_Verts.Length / 3);

            VertexShader.CurrentTechnique.Passes[1].End();
            VertexShader.End();
            PixelShader.CurrentTechnique.Passes[1].End();
            PixelShader.End();

            m_GraphicsDevice.SetRenderTarget(0, null);
            m_GraphicsDevice.DepthStencilBuffer = OldDSBuffer;
            Texture2D Return = RTarget.GetTexture();
            DSBuffer.Dispose();
            RTarget.Dispose();

            return Return;
        }

        public void Draw2DPoly()
        {
            if (m_2DVerts.Count == 0) return;
            m_GraphicsDevice.RenderState.DepthBufferEnable = false;

            VertexPositionColor[] Vert2D = new VertexPositionColor[m_2DVerts.Count];
            m_2DVerts.CopyTo(Vert2D);

            Matrix View = new Matrix(1.0f, 0.0f, 0.0f, 0.0f,
            0.0f, -1.0f, 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f);

            Matrix Projection = Matrix.CreateOrthographicOffCenter(0, (float)m_GraphicsDevice.Viewport.Width, -(float)m_GraphicsDevice.Viewport.Height, 0, 0, 1);

            Shader2D.CurrentTechnique = Shader2D.Techniques[0];
            Shader2D.Parameters["Projection"].SetValue(Projection);
            Shader2D.Parameters["View"].SetValue(View);
            Shader2D.CommitChanges();

            Shader2D.Begin();
            Shader2D.CurrentTechnique.Passes[0].Begin();

            VertexDeclaration decl = new VertexDeclaration(m_GraphicsDevice, VertexPositionColor.VertexElements);
            m_GraphicsDevice.VertexDeclaration = decl;

            m_GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, Vert2D, 0, Vert2D.Length/3);

            Shader2D.CurrentTechnique.Passes[0].End();
            Shader2D.End();

            m_GraphicsDevice.RenderState.DepthBufferEnable = true;
        }

        public void Draw(Effect VertexShader, Effect PixelShader, Matrix ProjectionMatrix, Matrix ViewMatrix, Matrix WorldMatrix)
        {
            m_GraphicsDevice.RenderState.CullMode = CullMode.None; 

            float FisoScale = (float)Math.Sqrt(0.5 * 0.5 * 2) / 5.10f; // is 5.10 on far zoom
		    float ZisoScale = (float)Math.Sqrt(0.5 * 0.5 * 2) / 144f;  // currently set 144 to near zoom

            float IsoScale = (1 - m_ZoomProgress) * FisoScale + (m_ZoomProgress) * ZisoScale;

            float HB = m_GraphicsDevice.Viewport.Width * IsoScale;
            float VB = m_GraphicsDevice.Viewport.Height * IsoScale;

            ProjectionMatrix = Matrix.CreateOrthographicOffCenter(-HB + m_ViewOffX, HB + m_ViewOffX, -VB + m_ViewOffY, VB + m_ViewOffY, 0.1f, 1000);


            Matrix LightView = Matrix.CreateLookAt(m_LightPosition, new Vector3(256, 0, 256), new Vector3(0, 1, 0));
            Vector2 pos = CalculateR(new Vector2(m_ViewOffX, -m_ViewOffY));
            Vector3 LightOff = Vector3.Transform(new Vector3(pos.X, 0, pos.Y), LightView);

            float size = (1 - m_ZoomProgress) * 262 + (m_ZoomProgress * 40);
            Matrix LightProject = Matrix.CreateOrthographicOffCenter(-size + LightOff.X, size + LightOff.X, -size + LightOff.Y, size + LightOff.Y, 0.1f, 524);

            ViewMatrix = Matrix.Identity;
            WorldMatrix = Matrix.Identity;

            ViewMatrix *= Matrix.CreateScale(new Vector3(1, 0.5f + (float)(1.0 - m_ZoomProgress) / 2, 1));


            ViewMatrix *= Matrix.CreateRotationY((45.0f / 180.0f) * (float)Math.PI);
            ViewMatrix *= Matrix.CreateRotationX((30.0f / 180.0f) * (float)Math.PI);
            ViewMatrix *= Matrix.CreateTranslation(new Vector3(-360f, 0f, -512f));
            

            VertexShader.CurrentTechnique = VertexShader.Techniques[0];
            VertexShader.Parameters["BaseMatrix"].SetValue((WorldMatrix*ViewMatrix)*ProjectionMatrix);
            VertexShader.Parameters["LightMatrix"].SetValue((WorldMatrix*LightView)*LightProject);
            VertexShader.CommitChanges();

            PixelShader.CurrentTechnique = PixelShader.Techniques[0];
            PixelShader.Parameters["LightCol"].SetValue(new Vector4(m_TintColor.R/255.0f, m_TintColor.G/255.0f, m_TintColor.B/255.0f, 1));
            PixelShader.Parameters["VertexColorTex"].SetValue(m_VertexColor);
            PixelShader.Parameters["TextureAtlasTex"].SetValue(Atlas);
            PixelShader.Parameters["TransAtlasTex"].SetValue(TransAtlas);
            PixelShader.Parameters["RoadAtlasTex"].SetValue(RoadAtlas);
            PixelShader.Parameters["RoadAtlasCTex"].SetValue(RoadCAtlas);
            PixelShader.Parameters["ShadowMult"].SetValue(m_ShadowMult);

            //used for shadowing
            PixelShader.Parameters["LightMatrix"].SetValue((WorldMatrix * LightView) * LightProject);

            PixelShader.CommitChanges();

            VertexDeclaration decl = new VertexDeclaration(m_GraphicsDevice, MeshVertex.VertexElements);

            m_GraphicsDevice.VertexDeclaration = decl;
            m_GraphicsDevice.Vertices[0].SetSource(vertBuf, 0, MeshVertex.SizeInBytes);

            Texture2D ShadowMap = null;

            if (ShadowsEnabled)
            {
                ShadowMap = DrawDepth(VertexShader, PixelShader);
                PixelShader.Parameters["ShadowMap"].SetValue(ShadowMap);
            }
            m_GraphicsDevice.Clear(Color.Black);

            VertexShader.Begin();
            VertexShader.CurrentTechnique.Passes[0].Begin();
            PixelShader.Begin();
            if (ShadowsEnabled) PixelShader.CurrentTechnique.Passes[0].Begin();
            else PixelShader.CurrentTechnique.Passes[2].Begin();

            m_GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, m_Verts.Length / 3);

            VertexShader.CurrentTechnique.Passes[0].End();
            VertexShader.End();
            if (ShadowsEnabled)
            {
                PixelShader.CurrentTechnique.Passes[0].End();
                ShadowMap.Dispose();
            }
            else PixelShader.CurrentTechnique.Passes[2].End();
            PixelShader.End();

            m_MovMatrix = ViewMatrix;

            if (!m_Zoomed) DrawHouses(HB);

            m_2DVerts = new ArrayList(); //refresh list for tris under houses
            DrawSprites(HB, VB);

            m_2DVerts = new ArrayList(); //refresh list for spotlights
            DrawSpotlights(HB);
            Draw2DPoly();
            
        }
    }
}