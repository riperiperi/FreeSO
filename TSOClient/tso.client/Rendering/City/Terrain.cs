/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Timers;
using System.Text;
using ProtocolAbstractionLibraryD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using FSO.Client.UI.Screens;
using FSO.Common.Rendering.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.Files;
using FSO.Client.Utils;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Utils;

namespace FSO.Client.Rendering.City
{
    public class Terrain : _3DAbstract
    {
        public override List<_3DComponent> GetElements()
        {
            return new List<_3DComponent>();
        }
        public override void Add(_3DComponent item) 
        {
            //needs this to be a ThreeDScene, however the city renderer cannot have elements added to it!
        }

        public GraphicsDevice m_GraphicsDevice;

        public bool ShadowsEnabled = true;
        public int ShadowRes = 2048;
        public bool RegenData = false;

        private bool m_HandleMouse = false;
        private CityDataRetriever m_CityData;
        private Dictionary<Vector2, LotTileEntry> m_CityLookup;
        private Dictionary<int, Texture2D> m_HouseGraphics;
        private Texture2D m_Elevation, m_VertexColor, m_TerrainType, m_ForestType, m_ForestDensity, m_RoadMap;
        private Color m_TintColor;

        public Texture2D Atlas, TransAtlas, RoadAtlas, RoadCAtlas;
        public Texture2D[] m_Roads = new Texture2D[16], m_RoadCorners = new Texture2D[16];
        public Effect Shader2D, PixelShader, VertexShader;
        private Color[] m_TerrainTypeColorData;
        private Texture2D[] m_TransA = new Texture2D[30], TransB = new Texture2D[30];
        private Texture2D m_Ground, m_Rock, m_Snow, m_Water, m_Sand, m_Forest, m_DefaultHouse, m_LotOnline, m_LotOffline;
        private Vector3 m_LightPosition;

        private MeshVertex[] m_Verts;
        private int m_MeshTris, m_CityNumber;
        private ArrayList m_2DVerts;

        private Dictionary<Color, int> m_ToBlendPrio = new Dictionary<Color, int>();
        private Dictionary<Color, double[]> m_AtlasOff = new Dictionary<Color, double[]>();
        private Dictionary<Color, int> m_ForestTypes = new Dictionary<Color, int>();
        private Dictionary<int, double> m_Prio2Map = new Dictionary<int, double>();
        private Dictionary<string, double[]> m_EdgeBLookup = new Dictionary<string, double[]>();
        private Dictionary<string, int> m_CityNames = new Dictionary<string, int>();
        private double[][] m_AtlasOffPrio = new double[5][];

        private byte[] m_ElevationData, m_ForestDensityData;
            
        private Color[] m_ForestTypeData;

        public static uint[] MASK_COLORS = new uint[]{
            new Color(0xFF, 0x00, 0xFF, 0xFF).PackedValue,
            new Color(0xFE, 0x02, 0xFE, 0xFF).PackedValue,
            new Color(0xFF, 0x01, 0xFF, 0xFF).PackedValue
        };

        private MouseState m_MouseState, m_LastMouseState;
        private bool m_MouseMove = false;
        public bool m_Zoomed = false;
        private Vector2 m_MouseStart;
        private int m_ScrHeight, m_ScrWidth;
        private float m_ScrollSpeed;
        private float m_ViewOffX, m_ViewOffY, m_TargVOffX, m_TargVOffY;
        public float m_ZoomProgress = 0; //settable to avoid discontinuities
        private float m_SpotOsc = 0;
        private float m_ShadowMult = 1;
        //private double m_DayNightCycle = 0.0;
        private int[] m_SelTile = new int[] { -1, -1 };
        private int[] m_SelTileTmp = new int[] { -1, -1 }; //For storing value of SelTile to use when clicking dialogs.
        private Matrix m_MovMatrix;
        private Texture2D m_WhiteLine;
        private Texture2D m_stpWhiteLine;
        private VertexBuffer vertBuf;
        private int[][] m_SurTileOffs = new int[][] 
        {
            new int[] {0, -1},
            new int[] {1, -1},
            new int[] {1, 0},
            new int[] {1, 1},
            new int[] {0, 1},
            new int[] {-1, 1},
            new int[] {-1, 0},
            new int[] {-1, -1},
        };

        private Color[] m_TimeColors = new Color[] 
        {
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

        //Network related stuff.
        private int m_LotCost = 0;
        private static LotTileEntry m_CurrentLot; //Current lot received by server.
        private UIAlert m_BuyPropertyAlert;
        private UIAlert m_LotUnbuildableAlert;
        private Timer m_PacketTimer = new Timer(1000); //Timer for regulating packet interval.
        private static bool m_CanSend = false;

        private Texture2D LoadTex(string Path)
        {
            return LoadTex(new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        private Texture2D LoadTex(Stream stream)
        {
            Texture2D result = null;
            try
            {
                result = ImageLoader.FromStream(m_GraphicsDevice, stream);
            }
            catch (Exception)
            {
                result = new Texture2D(m_GraphicsDevice, 1, 1);
            }
            stream.Close();
            return result;
        }

        public void LoadContent(GraphicsDevice GfxDevice)
        {
            m_GraphicsDevice = GfxDevice;
            VertexShader = GameFacade.Game.Content.Load<Effect>("Effects\\VerShader");
            PixelShader = GameFacade.Game.Content.Load<Effect>("Effects\\PixShader");
            Shader2D = GameFacade.Game.Content.Load<Effect>("Effects\\colorpoly2d");

            String gamepath = GameFacade.GameFilePath("");

            string CityStr = gamepath + "cities\\" + ((m_CityNumber >= 10) ? "city_00" + m_CityNumber.ToString() : "city_000" + m_CityNumber.ToString());
            m_Elevation = LoadTex(CityStr + "\\elevation.bmp");
            m_VertexColor = LoadTex(CityStr + "\\vertexcolor.bmp");
            m_TerrainType = LoadTex(CityStr + "\\terraintype.bmp");
            m_ForestType = LoadTex(CityStr + "\\foresttype.bmp");
            m_ForestDensity = LoadTex(CityStr + "\\forestdensity.bmp");
            m_RoadMap = LoadTex(CityStr + "\\roadmap.bmp");

            m_Ground = LoadTex(gamepath + "gamedata\\terrain\\newformat\\gr.tga");
            m_Rock = LoadTex(gamepath + "gamedata\\terrain\\newformat\\rk.tga");
            m_Water = LoadTex(gamepath + "gamedata\\terrain\\newformat\\wt.tga");
            m_Sand = LoadTex(gamepath + "gamedata\\terrain\\newformat\\sd.tga");
            m_Snow = LoadTex(gamepath + "gamedata\\terrain\\newformat\\sn.tga");
            m_Forest = LoadTex(gamepath + "gamedata\\farzoom\\forest00a.tga");
            m_DefaultHouse = LoadTex(gamepath + "userdata\\houses\\defaulthouse.bmp");//, new TextureCreationParameters(128, 64, 24, 0, SurfaceFormat.Rgba32, TextureUsage.Linear, Color.Black, FilterOptions.None, FilterOptions.None));
            TextureUtils.ManualTextureMaskSingleThreaded(ref m_DefaultHouse, new uint[] { new Color(0x00, 0x00, 0x00, 0xFF).PackedValue });

            m_LotOnline = UIElement.GetTexture(0x0000032F00000001);
            m_LotOffline = UIElement.GetTexture(0x0000033100000001);

            //fills used for line drawing

            m_WhiteLine = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, Color.White);
            m_stpWhiteLine = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, new Color(255, 255, 255, 128));

            string Num;

            for (int x = 0; x < 30; x = x + 2)
            {
                Num = ZeroPad((x / 2).ToString(), 2);
                m_TransA[x] = LoadTex(gamepath + "gamedata\\terrain\\newformat\\transa" + Num + "a.tga");
                m_TransA[x + 1] = LoadTex(gamepath + "gamedata\\terrain\\newformat\\transa" + Num + "b.tga");
            }

            for (int x = 0; x < 30; x = x + 2)
            {
                Num = ZeroPad((x / 2).ToString(), 2);
                TransB[x] = LoadTex(gamepath + "gamedata\\terrain\\newformat\\transb" + Num + "a.tga");
                TransB[x + 1] = LoadTex(gamepath + "gamedata\\terrain\\newformat\\transb" + Num + "b.tga");
            }

            for (int x = 0; x < 16; x++)
            {
                Num = ZeroPad((x).ToString(), 2);
                m_Roads[x] = LoadTex(gamepath + "gamedata\\terrain\\road" + Num + ".tga");
            }

            for (int x = 0; x < 16; x++)
            {
                Num = ZeroPad((x).ToString(), 2);
                m_RoadCorners[x] = LoadTex(gamepath + "gamedata\\terrain\\roadcorner" + Num + ".tga");
            }

            m_Width = m_Elevation.Width;
            m_Height = m_Elevation.Height;
        }

        public Terrain(GraphicsDevice Device) : base(Device)
        {
            //LoadContent(GfxDevice, Content);
        }

        private string ZeroPad(string Str, int NumZeroes) //pads any string with zeroes until its length equals NumZeroes.
        {
            while (Str.Length < NumZeroes)
                Str = "0" + Str;

            return Str;
        }

        public override void DeviceReset(GraphicsDevice Device)
        {
            UnloadEverything();
            LoadContent(m_GraphicsDevice);
            RegenData = true;
        }

        public void Initialize(int mapId, CityDataRetriever cityData)
        {
            m_CityData = cityData;

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

            m_CityNumber = mapId;

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
            m_ForestTypes.Add(new Color(0, 0, 0), -1);  //nothing; no forest

            m_HouseGraphics = new Dictionary<int,Texture2D>();
            populateCityLookup();

            m_PacketTimer.Elapsed += new ElapsedEventHandler(m_PacketTimer_Elapsed);
            m_PacketTimer.AutoReset = true;
            m_PacketTimer.Start();
        }


        #region Network handlers

        /*private void Controller_OnLotPurchaseSuccessful(int Money)
        {
            CoreGameScreen CurrentUIScr = (CoreGameScreen)GameFacade.Screens.CurrentUIScreen;

            PlayerAccount.Money = Money;
            CurrentUIScr.ucp.MoneyText.Caption = Money.ToString();

            //TODO: Add popup dialog.
        }

        private void Controller_OnLotUnbuildable()
        {
            UIAlertOptions AlertOptions = new UIAlertOptions();
            AlertOptions.Title = GameFacade.Strings.GetString("246", "1");
            //This isn't exported as a string. WTF Maxis??
            AlertOptions.Message = "This property cannot be purchased!\r\n";
            m_LotUnbuildableAlert = UIScreen.ShowAlert(AlertOptions, true);
        }*/

        /*private void Controller_OnLotPurchaseFailed(Network.Events.TransactionEvent e)
        {
            UIAlertOptions AlertOptions = new UIAlertOptions();
            AlertOptions.Title = GameFacade.Strings.GetString("246", "1");

            if (EventSink.EventQueue[0].ECode == EventCodes.TRANSACTION_PLAYER_OUT_OF_MONEY)
            {
                //For now this says "Error! Transaction refused by server", because I couldn't find the right string.
                AlertOptions.Message = GameFacade.Strings.GetString("224", "16");

                //Doing this instead of EventQueue.Clear() ensures we won't accidentally remove any 
                //events that may have been added to the end.
                EventSink.EventQueue.Remove(EventSink.EventQueue[0]);
            }
            else
            {
                AlertOptions.Message = "General Error";
                //Doing this instead of EventQueue.Clear() ensures we won't accidentally remove any 
                //events that may have been added to the end.
                EventSink.EventQueue.Remove(EventSink.EventQueue[0]);
            }
            m_LotUnbuildableAlert = UIScreen.ShowAlert(AlertOptions, true);
        }*/

        private void Controller_OnLotCost(LotTileEntry Entry)
        {
            m_CurrentLot = Entry;
            m_LotCost = Entry.cost;
        }

        private void m_PacketTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            m_CanSend = true;
        }

        #endregion

        private void populateCityLookup()
        {
            LotTileEntry[] data = m_CityData.LotTileData; //ideally this should change and we should poll the server for new info every 10 seconds when in city view
            m_CityLookup = new Dictionary<Vector2, LotTileEntry>();
            for (int i = 0; i < data.Length; i++)
            {
                m_CityLookup[new Vector2 ( data[i].x, data[i].y )] = data[i];
            }
        }

        public void populateCityLookup(LotTileEntry[] TileData)
        {
            LotTileEntry[] data = TileData;
            m_CityLookup = new Dictionary<Vector2, LotTileEntry>();
            for (int i = 0; i < data.Length; i++)
            {
                m_CityLookup[new Vector2(data[i].x, data[i].y)] = data[i];
            }
        }

        public void ClearOldData()
        {
            if (Atlas != null) Atlas.Dispose();
            if (RoadAtlas != null) RoadAtlas.Dispose();
            if (RoadCAtlas != null) RoadCAtlas.Dispose();
            if (TransAtlas != null) TransAtlas.Dispose();
            if (vertBuf != null) vertBuf.Dispose();
        }

        public void GenerateAssets()
        {
            SpriteBatch spriteBatch = new SpriteBatch(m_GraphicsDevice);
            ClearOldData();
            GenerateCityMesh(m_GraphicsDevice); //generates the city mesh
            CreateTextureAtlas(spriteBatch); //generates the many atlases used when rendering the city.
            CreateTransparencyAtlas(spriteBatch);
            RoadAtlas = CreateRoadAtlas(m_Roads, spriteBatch);
            RoadCAtlas = CreateRoadAtlas(m_RoadCorners, spriteBatch);
            spriteBatch.Dispose();
            RegenData = false; //don't do this again next frame...
        }

        public void UnloadEverything()
        {
            ClearOldData();
            m_Elevation.Dispose(); 
            m_VertexColor.Dispose(); 
            m_TerrainType.Dispose(); 
            m_ForestType.Dispose(); 
            m_ForestDensity.Dispose(); 
            m_RoadMap.Dispose();
            m_Ground.Dispose(); 
            m_Rock.Dispose();
            m_Snow.Dispose(); 
            m_Water.Dispose(); 
            m_Sand.Dispose(); 
            m_Forest.Dispose();
            m_DefaultHouse.Dispose(); 
            m_LotOnline.Dispose(); 
            m_LotOffline.Dispose();
            m_WhiteLine.Dispose();
            m_stpWhiteLine.Dispose();

            for (int x = 0; x < 30; x++) m_TransA[x].Dispose();
            for (int x = 0; x < 30; x++) TransB[x].Dispose();
            for (int x = 0; x < 16; x++) m_Roads[x].Dispose();
            for (int x = 0; x < 16; x++) m_RoadCorners[x].Dispose();

            foreach (var entry in m_HouseGraphics)
            {
                m_HouseGraphics[entry.Key].Dispose();
            }
            m_HouseGraphics.Clear();
        }

        private void DrawLine(Texture2D Fill, Vector2 Start, Vector2 End, SpriteBatch spriteBatch, int lineWidth, float opacity) //draws a line from Start to End.
        {
            double length = Math.Sqrt(Math.Pow(End.X - Start.X, 2) + Math.Pow(End.Y - Start.Y, 2));
            float direction = (float)Math.Atan2(End.Y - Start.Y, End.X - Start.X);
            Color tint = new Color(1f, 1f, 1f, 1f) * opacity;
            spriteBatch.Draw(Fill, new Rectangle((int)Start.X, (int)Start.Y-(int)(lineWidth/2), (int)length, lineWidth), null, tint, direction, new Vector2(0, 0.5f), SpriteEffects.None, 0); //
        }

        private Blend GetBlend(Color[] TerrainTypeData, int i, int j)
        {
            int[] edges;
            int sample;
            int t;

            edges = new int[] { -1, -1, -1, -1 };
            sample = m_ToBlendPrio[TerrainTypeData[i * 512 + j]];
            t = m_ToBlendPrio[TerrainTypeData[Math.Abs((i - 1) * 512 + j)] ];

            if ((i - 1 >= 0) && (t > sample)) edges[0] = t;
            t = m_ToBlendPrio[TerrainTypeData[i * 512 + j + 1] ];
            if ((j + 1 < 512) && (t > sample)) edges[1] = t;
            t = m_ToBlendPrio[TerrainTypeData[Math.Min((i + 1), 511) * 512 + j] ];
            if ((i + 1 < 512) && (t > sample)) edges[2] = t;
            t = m_ToBlendPrio[TerrainTypeData[i * 512 + j - 1] ];
            if ((j - 1 >= 0) && (t > sample)) edges[3] = t;

            int[] binary = new int[] {
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
            RenderTarget2D RTarget = new RenderTarget2D(m_GraphicsDevice, 512, 1024, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            //For some reason, we have to create a new depth stencil buffer with the same size
            //as the rendertarget in order to set the rendertarget on the GraphicsDevice.
            m_GraphicsDevice.SetRenderTarget(RTarget);

            //m_GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            spriteBatch.Draw(m_Ground, new Rectangle(0, 0, m_Ground.Width, m_Ground.Height), Color.White);
            spriteBatch.Draw(m_Water, new Rectangle(256, 0, m_Water.Width, m_Water.Height), Color.White);
            spriteBatch.Draw(m_Rock, new Rectangle(0, 256, m_Rock.Width, m_Rock.Height), Color.White);
            spriteBatch.Draw(m_Snow, new Rectangle(256, 256, m_Snow.Width, m_Snow.Height), Color.White);
            spriteBatch.Draw(m_Sand, new Rectangle(0, 512, m_Sand.Width, m_Sand.Height), Color.White);
            spriteBatch.End();

            m_GraphicsDevice.SetRenderTarget(null);

            Atlas = RTarget;
            //RTarget.Dispose(); //free up memory used by render target, as we have moved the data to a texture.
        }

        /// <summary>
        /// Creates a transparency atlas with which to texture the terrain.
        /// </summary>
        /// <param name="spriteBatch">A spritebatch to draw with.</param>
        public void CreateTransparencyAtlas(SpriteBatch spriteBatch)
        {
            RenderTarget2D RTarget = new RenderTarget2D(m_GraphicsDevice, 1024, 256, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            m_GraphicsDevice.SetRenderTarget(RTarget);

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

            m_GraphicsDevice.SetRenderTarget(null);

            TransAtlas = RTarget;
            //RTarget.Dispose(); //free up memory used by render target, as we have moved the data to a texture.
        }

        public Texture2D CreateRoadAtlas(Texture2D[] input, SpriteBatch spriteBatch)
        {

            RenderTarget2D RTarget = new RenderTarget2D(m_GraphicsDevice, 512, 512, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            m_GraphicsDevice.SetRenderTarget(RTarget);

            m_GraphicsDevice.Clear(Color.Transparent);

            spriteBatch.Begin();

            for (int i = 0; i < 16; i++)
            {
                spriteBatch.Draw(input[i], new Rectangle((i%4) * 128, (int)(i/4.0)*128, 128, 128), Color.White);
            }

            spriteBatch.End();

            m_GraphicsDevice.SetRenderTarget(null);

            Texture2D Ret = RTarget;
            //RTarget.Dispose(); //free up memory used by render target, as we have moved the data to a texture.
            return Ret;
        }

        private string ToBinaryString(int[] Array)
        {
            StringBuilder StrBuilder = new StringBuilder();

            for (int i = 0; i < Array.Length; i++)
                StrBuilder.Append(Array[i].ToString());

            return StrBuilder.ToString();
        }

        public void GenerateCityMesh(GraphicsDevice GfxDevice)
        {
            m_Verts = new MeshVertex[m_Width * m_Height * 3]; //6 verts per pixel, but only half the pixels in the image are used, so multiplier is 3!
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

            byte[] RoadData = ConvertToBinaryArray(RoadMapData); //we need binary arrays for these as the values are accessed directly instead of being compared.
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
                for (int j = xStart; j < xEnd; j++)
                { //where the magic happens
                    var blendData = GetBlend(m_TerrainTypeColorData, i, j); //gets information on what this tile blends into and what blend image to use for the alpha.

                    var bOff = blendData.AtlasPosition; //texture used for blend alpha
                    double[] temp = m_AtlasOff[m_TerrainTypeColorData[((i * 512) + j)]];
                    double[] off = new double[] { temp[0], temp[1] }; //texture for this tile (grass, rock etc)
                    off[0] += 0.125 * (j % 4);
                    off[1] += (0.125 / 2.0) * (i % 4); //vertically 2 times as large
                    double[] temp2 = m_AtlasOffPrio[blendData.MaxEdge];
                    double[] off2 = new double[] { temp2[0], temp2[1] }; //texture this tile is blending into (grass, rock etc)
                    off2[0] += 0.125 * (j % 4);
                    off2[1] += (0.125 / 2.0) * (i % 4);

                    float toX = 0; //vertex colour offset, adjust to try and fix vertexcolor offset.
				    float toY = 0;

                    byte roadByte = RoadData[(i * 512 + j) * 4];
                    double[] off3 = new double[] { ((roadByte & 15) % 4) * 0.25, ((int)((roadByte & 15) / 4)) * 0.25 }; //normal road uv selection
                    double[] off4 = new double[] { ((roadByte >> 4) % 4) * 0.25, ((int)((roadByte >> 4) / 4)) * 0.25 }; //road corners uv selection

                    //huge segment of code for generating triangles incoming

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
            vertBuf = new VertexBuffer(m_GraphicsDevice, typeof(MeshVertex), m_Verts.Length, BufferUsage.WriteOnly);
            vertBuf.SetData(m_Verts); //use vertex buffer to draw mesh as the data is always the same. we only have to set data once.
            m_MeshTris = m_Verts.Length / 3;
            m_Verts = null; //clear m_Verts now that it's copied to save some RAM.
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

        private int[] GetHoverSquare()
        {
            double ResScale = 768.0/m_ScrHeight;
            double fisoScale = (Math.Sqrt(0.5*0.5*2)/5.10)*ResScale; // is 5.10 on far zoom
            double zisoScale = Math.Sqrt(0.5*0.5*2)/144.0; // currently set 144 to near zoom
            double isoScale = (1-m_ZoomProgress)*fisoScale + (m_ZoomProgress)*zisoScale;
            double width = m_ScrWidth;
            float iScale = (float)(width/(width*isoScale*2));
            
            Vector2 mid = CalculateR(new Vector2(m_ViewOffX, -m_ViewOffY));
            mid.X -= 6;
            mid.Y += 6;
            double[] bounds = new double[] {Math.Round(mid.X-19), Math.Round(mid.Y-19), Math.Round(mid.X+19), Math.Round(mid.Y+19)};
            double[] pos = new double[] { m_MouseState.X, m_MouseState.Y };

            for (int y=(int)bounds[1]; y<bounds[3]; y++) 
            {
                if (y < 0 || y > 511) continue;
                for (int x=(int)bounds[0]; x<bounds[2]; x++) 
                {
                    if (x < 0 || x > 511) continue;
                    //get the 4 points of this tile, and check if the mouse cursor is inside them.
                    var xy = transformSpr(iScale, new Vector3(x+0, m_ElevationData[(y*512+x)*4]/12.0f, y+0));
                    var xy2 = transformSpr(iScale, new Vector3(x + 1, m_ElevationData[(y * 512 + Math.Min(x + 1, 511)) * 4] / 12.0f, y + 0));
                    var xy3 = transformSpr(iScale, new Vector3(x + 1, m_ElevationData[(Math.Min(y + 1, 511) * 512 + Math.Min(x + 1, 511)) * 4] / 12.0f, y + 1));
                    var xy4 = transformSpr(iScale, new Vector3(x + 0, m_ElevationData[(Math.Min(y + 1, 511) * 512 + x) * 4] / 12.0f, y + 1));
                    if (IsInsidePoly(new double[] {xy.X, xy.Y, xy2.X, xy2.Y, xy3.X, xy3.Y, xy4.X, xy4.Y}, pos)) return new int[] {x, y}; //we have a match
                }
            }
            return new int[] {-1, -1}; //no match, return invalid mouse selection (-1, -1)
        }

        private bool IsInsidePoly(double[] Poly, double[] Pos)
        {
            if (Poly.Length % 2 != 0) return false; //invalid polygon
		    int n = Poly.Length / 2;
		    bool result = false;
		    
            for (int i=0; i<n; i++)
            {
			    double x1 = Poly[i*2];
                double y1 = Poly[i * 2 + 1];
                double x2 = Poly[((i + 1) * 2) % Poly.Length];
                double y2 = Poly[((i + 1) * 2 + 1) % Poly.Length];
                double slope = (y2 - y1) / (x2 - x1);
                double c = y1 - (slope * x1);
                if ((Pos[1] < (slope * Pos[0]) + c) && (Pos[0] >= Math.Min(x1, x2)) && (Pos[0] < Math.Max(x1, x2))) 
                    result = !(result);
		    }

		    return result;
        }

        private Vector2 CalculateR(Vector2 m) //get approx 3d position of 2d screen position in model/tile space.
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

        private void DrawTileBorders(float iScale, SpriteBatch spriteBatch)
        {
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
                        for (int i=0; i<m_SurTileOffs.Length; i++) { //check 8 adjacent tiles to determine what combination of border lines to use. (road border draws between two buildable tiles)
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

        private bool isLandBuildable(int x, int y) 
        {
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
            float iScale = (float)m_ScrWidth/(HB*2.0f);
		
            float spotlightScale = (float)(iScale*(2.0*Math.Sqrt(0.5*0.5*2)/5.10));
            LotTileEntry[] lots = m_CityData.LotTileData;

            for (int i = 0; i < lots.Length; i++)
            {
                if ((lots[i].flags & 2) > 0)
                {
                    Vector2 pos = new Vector2(lots[i].x, lots[i].y);
                    Vector2 xy = transformSpr(iScale, new Vector3(pos.X + 0.5f, m_ElevationData[((int)pos.Y * 512 + (int)pos.X) * 4] / 12.0f, pos.Y + 0.5f)); //get position to place spotlight
                    Vector3 xyz = new Vector3(xy.X, xy.Y, 1);

                    Matrix trans = Matrix.Identity;
                    trans = Matrix.CreateRotationZ((float)(0.33 * Math.Sin(2.0 * Math.PI * ((m_SpotOsc + i * 0.43) % 1)))); //makes spotlight sway back and forth!

                    m_2DVerts.Add(new VertexPositionColor(xyz, new Color(1, 1, 1, 0.5f))); //bottom point of spotlight, set to 0.5 opacity
                    m_2DVerts.Add(new VertexPositionColor((xyz + (Vector3.Transform(new Vector3(-12, -100, 0), trans) * spotlightScale)), new Color(1, 1, 1, 0.0f))); //top two vertices set to 0 opacity, creates gradient for spotlight effect.
                    m_2DVerts.Add(new VertexPositionColor((xyz + (Vector3.Transform(new Vector3(12, -100, 0), trans) * spotlightScale)), new Color(1, 1, 1, 0.0f)));
                }
            }
        }

        private void DrawHouses(float HB) //draws house icons in far view
        {
            SpriteBatch spriteBatch = new SpriteBatch(m_GraphicsDevice);
            spriteBatch.Begin();
            float iScale = (float)m_ScrWidth / (HB * 2);
            LotTileEntry[] lots = m_CityData.LotTileData;
            for (int i=0; i<lots.Length; i++) {
				short x = lots[i].x;
				short y = lots[i].y;
				Vector2 xy = transformSpr(iScale, new Vector3(x+0.5f, m_ElevationData[(y*512+x)*4]/12.0f, y+0.5f));
                bool online = ((lots[i].flags & 1) == 1);
                Texture2D img = (online) ? m_LotOnline : m_LotOffline; //if house is online, use red house instead of gray one
				double alpha = online?(0.5+Math.Sin(4*Math.PI*(m_SpotOsc%1))/2.0):1; //if house is online, flash the opacity using the oscillator variable.
				spriteBatch.Draw(img, new Rectangle((int)Math.Round(xy.X-1), (int)Math.Round(xy.Y-2), 4, 3), Color.White*(float)alpha);
			}
            spriteBatch.End();
        }

        private void PathTile(int x, int y, float iScale, float opacity) { //quick and dirty function to fill a tile with white using the 2DVerts system. Used in near view for online houses.
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

        /// <summary>
        /// Draws a tooltip at the specified coordinates.
        /// </summary>
        /// <param name="batch">A SpriteBatch instance.</param>
        /// <param name="tooltip">String to be drawn.</param>
        /// <param name="position">Position of tooltip.</param>
        /// <param name="opacity">Tooltip's opacity.</param>
        public void DrawTooltip(SpriteBatch batch, string tooltip, Vector2 position, float opacity)
        {
            TextStyle style = TextStyle.DefaultLabel.Clone();
            style.Color = Color.Black;
            style.Size = 8;

            var scale = new Vector2(1, 1);
            if (style.Scale != 1.0f)
            {
                scale = new Vector2(scale.X * style.Scale, scale.Y * style.Scale);
            }

            var wrapped = UIUtils.WordWrap(tooltip, 290, style, scale); //tooltip max width should be 300. There is a 5px margin on each side.

            int width = wrapped.MaxWidth + 10;
            int height = 13 * wrapped.Lines.Count + 4; //13 per line + 4.

            position.X = Math.Min(position.X, GlobalSettings.Default.GraphicsWidth - width);
            position.Y = Math.Max(position.Y, height);

            var whiteRectangle = new Texture2D(batch.GraphicsDevice, 1, 1);
            whiteRectangle.SetData(new[] { Color.White });

            batch.Draw(whiteRectangle, new Rectangle((int)position.X, (int)position.Y - height, width, height), Color.White * opacity); //note: in XNA4 colours need to be premultiplied

            //border
            batch.Draw(whiteRectangle, new Rectangle((int)position.X, (int)position.Y - height, 1, height), new Color(0, 0, 0, opacity));
            batch.Draw(whiteRectangle, new Rectangle((int)position.X, (int)position.Y - height, width, 1), new Color(0, 0, 0, opacity));
            batch.Draw(whiteRectangle, new Rectangle((int)position.X + width, (int)position.Y - height, 1, height), new Color(0, 0, 0, opacity));
            batch.Draw(whiteRectangle, new Rectangle((int)position.X, (int)position.Y, width, 1), new Color(0, 0, 0, opacity));

            position.Y -= height;

            for (int i = 0; i < wrapped.Lines.Count; i++)
            {
                int thisWidth = (int)(style.SpriteFont.MeasureString(wrapped.Lines[i]).X * scale.X);
                batch.DrawString(style.SpriteFont, wrapped.Lines[i], position + new Vector2((width - thisWidth) / 2, 0), new Color(0, 0, 0, opacity), 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                position.Y += 13;
            }
        }

        private void DrawSprites(float HB, float VB)
        {
            SpriteBatch spriteBatch = new SpriteBatch(m_GraphicsDevice);
            spriteBatch.Begin();

            if (!m_Zoomed && m_HandleMouse)
            {
                //draw rectangle to indicate zoom position
                DrawLine(m_WhiteLine, new Vector2(m_MouseState.X - 15, m_MouseState.Y - 11), new Vector2(m_MouseState.X - 15, m_MouseState.Y + 11), spriteBatch, 2, 1);
                DrawLine(m_WhiteLine, new Vector2(m_MouseState.X - 16, m_MouseState.Y + 10), new Vector2(m_MouseState.X + 16, m_MouseState.Y + 10), spriteBatch, 2, 1);
                DrawLine(m_WhiteLine, new Vector2(m_MouseState.X + 15, m_MouseState.Y + 11), new Vector2(m_MouseState.X + 15, m_MouseState.Y - 11), spriteBatch, 2, 1);
                DrawLine(m_WhiteLine, new Vector2(m_MouseState.X + 16, m_MouseState.Y - 10), new Vector2(m_MouseState.X - 16, m_MouseState.Y - 10), spriteBatch, 2, 1);
            }
            else if (m_Zoomed && m_HandleMouse)
            {
                if (m_LotCost != 0)
                {
                    float X = GetHoverSquare()[0];
                    float Y = GetHoverSquare()[1];
                    //TODO: Should this have opacity? Might have to change this to render only when hovering over a lot.
                    DrawTooltip(spriteBatch, m_LotCost.ToString() + "§", new Vector2(X, Y), 0.5f);
                }
                else
                {
                    if (m_CurrentLot != null)
                    {
                        float X = GetHoverSquare()[0];
                        float Y = GetHoverSquare()[1];
                        bool Online = ProtoHelpers.GetBit(m_CurrentLot.flags, 0);
                        string OnlineStr = (Online == true) ? "Online" : "Offline";
                        //TODO: Should this have opacity? Might have to change this to render only when hovering over a lot.
                        DrawTooltip(spriteBatch, GameFacade.Strings.GetString("215", "3", new string[]{m_CurrentLot.name}) + "\n" 
                            + OnlineStr, new Vector2(X, Y), 0.5f);
                    }
                }
            }
            
            if (m_ZoomProgress < 0.5)
            {
                spriteBatch.End();
                spriteBatch.Dispose();
                return;
            }

            float iScale = (float)m_ScrWidth / (HB * 2);

		    float treeWidth = (float)(Math.Sqrt(2)*(128.0/144.0));
		    float treeHeight = treeWidth*(80/128);

		    Vector2 mid = CalculateR(new Vector2(m_ViewOffX, -m_ViewOffY)); //determine approximate tile position at center of screen
		    mid.X -= 6;
		    mid.Y += 6;
            float[] bounds = new float[] { (float)Math.Round(mid.X - 19), (float)Math.Round(mid.Y - 19), (float)Math.Round(mid.X + 19), (float)Math.Round(mid.Y + 19) };
    		
		    Texture2D img = m_Forest;
		    float fade = Math.Max(0, Math.Min(1, (m_ZoomProgress - 0.4f) * 2));

            DrawTileBorders(iScale, spriteBatch);

            for (short y = (short)bounds[1]; y < bounds[3]; y++) //iterate over tiles close to the approximate tile position at the center of the screen and draw any trees/houses on them
            {
                if (y < 0 || y > 511) continue;
                for(short x = (short)bounds[0]; x < bounds[2]; x++)
                {
                    if (x < 0 || x > 511) continue;

                    float elev = (m_ElevationData[(y * 512 + x) * 4] + m_ElevationData[(y * 512 + Math.Min(x + 1, 511)) * 4] + 
                        m_ElevationData[(Math.Min(y + 1, 511) * 512 + Math.Min(x + 1, 511)) * 4] + 
                        m_ElevationData[(Math.Min(y + 1, 511) * 512 + x) * 4]) / 4; //elevation of sprite is the average elevation of the 4 vertices of the tile

                    var xy = transformSpr(iScale, new Vector3((float)(x + 0.5), elev / 12.0f, (float)(y + 0.5)));

                    if (xy.X > -64 && xy.X < m_ScrWidth + 64 && xy.Y > -40 && xy.Y < m_ScrHeight + 40) //is inside screen
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
                        if (house != null) //if there is a house here, draw it
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
                        else //if there is no house, draw the forest that's meant to be here.
                        {
                            double fType = m_ForestTypes[m_ForestTypeData[(y * 512 + x)]];
                            double fDens = Math.Round((double)(m_ForestDensityData[(y * 512 + x) * 4] * 4 / 255));
                            if (!(fType == -1 || fDens == 0))
                            {
                                double scale = treeWidth * iScale / 128.0;
                                spriteBatch.Draw(m_Forest, new Rectangle((int)(xy.X - 64.0 * scale), (int)(xy.Y - 56.0 * scale), (int)(scale * 128), (int)(scale * 80)), new Rectangle((int)(128 * (fDens - 1)), (int)(80 * fType), 128, 80), m_TintColor);
                                //draw correct forest from forest atlas
                            }
                        }
                    }
                }
            }

            Draw2DPoly(); //fill the tiles below online houses BEFORE actually drawing the houses and trees!
            spriteBatch.End();
            spriteBatch.Dispose();
        }

        public Vector2 transformSpr(float iScale, Vector3 pos) 
        { //transform 3d position to view.
            Vector3 temp = Vector3.Transform(pos, m_MovMatrix);
            int width = m_ScrWidth;
            int height = m_ScrHeight;
            return new Vector2((temp.X-m_ViewOffX)*iScale+width/2, (-(temp.Y-m_ViewOffY)*iScale)+height/2);
        }

        public void UIMouseEvent(String type)
        {
            System.Diagnostics.Debug.WriteLine(type);
            if (type.Equals("MouseOver", StringComparison.InvariantCultureIgnoreCase)) m_HandleMouse = true;
            if (type.Equals("MouseOut", StringComparison.InvariantCultureIgnoreCase)) m_HandleMouse = false;
        }

        public override void Update(UpdateState state)
        {
            CoreGameScreen CurrentUIScr = (CoreGameScreen)GameFacade.Screens.CurrentUIScreen;

            if (Visible)
            { //if we're not visible, do not update CityRenderer state...
                m_LastMouseState = m_MouseState;
                m_MouseState = Mouse.GetState();

                m_MouseMove = (m_MouseState.RightButton == ButtonState.Pressed);

                if (m_HandleMouse)
                {
                    if (m_Zoomed)
                    {
                        m_SelTile = GetHoverSquare();

                        if (m_CanSend)
                        {
                            //Network.UIPacketSenders.SendLotCostRequest(Network.NetworkFacade.Client, (short)m_SelTile[0], (short)m_SelTile[1]);
                            m_CanSend = false;
                        }
                    }

                    if (m_MouseState.RightButton == ButtonState.Pressed && m_LastMouseState.RightButton == ButtonState.Released)
                    {
                        m_MouseStart = new Vector2(m_MouseState.X, m_MouseState.Y); //if middle mouse button activated, record where we started pressing it (to use for panning)
                    }

                    else if (m_MouseState.LeftButton == ButtonState.Released && m_LastMouseState.LeftButton == ButtonState.Pressed) //if clicked...
                    {
                        if (!m_Zoomed)
                        {
                            m_Zoomed = true;
                            double ResScale = 768.0 / m_ScrHeight;
                            double isoScale = (Math.Sqrt(0.5 * 0.5 * 2) / 5.10) * ResScale;
                            double hb = m_ScrWidth * isoScale;
                            double vb = m_ScrHeight * isoScale;

                            m_TargVOffX = (float)(-hb + m_MouseState.X * isoScale * 2);
                            m_TargVOffY = (float)(vb - m_MouseState.Y * isoScale * 2); //zoom into approximate location of mouse cursor if not zoomed already
                        }
                        else
                        {
                            if (m_SelTile[0] != -1 && m_SelTile[1] != -1)
                            {
                                m_SelTileTmp[0] = m_SelTile[0];
                                m_SelTileTmp[1] = m_SelTile[1];

                                UIAlertOptions AlertOptions = new UIAlertOptions();
                                AlertOptions.Title = GameFacade.Strings.GetString("246", "1");
                                AlertOptions.Message = GameFacade.Strings.GetString("215", "23", new string[] 
                                { m_LotCost.ToString(), CurrentUIScr.ucp.MoneyText.Caption });
                                AlertOptions.Buttons = new UIAlertButton[] {
                                    new UIAlertButton(UIAlertButtonType.Yes, new ButtonClickDelegate(BuyPropertyAlert_OnButtonClick)),
                                    new UIAlertButton(UIAlertButtonType.No) };

                                m_BuyPropertyAlert = UIScreen.GlobalShowAlert(AlertOptions, true);
                            }
                        }

                        CurrentUIScr.ucp.UpdateZoomButton();
                    }
                }
                else
                {
                    m_SelTile = new int[] { -1, -1 };
                }

                //m_SecondsBehind += time.ElapsedGameTime.TotalSeconds;
                //m_SecondsBehind -= 1 / 60;
                FixedTimeUpdate();
                //SetTimeOfDay(m_DayNightCycle % 1); //calculates sun/moon light colour and position
                //m_DayNightCycle += 0.001; //adjust the cycle speed here. When ingame, set m_DayNightCycle to to the percentage of time passed through the day. (0 to 1)

                m_ViewOffX = (m_TargVOffX) * m_ZoomProgress;
                m_ViewOffY = (m_TargVOffY) * m_ZoomProgress;
            }
        }

        /// <summary>
        /// Player selected "yes" to buy a new lot.
        /// </summary>
        private void BuyPropertyAlert_OnButtonClick(UIElement Button)
        {
            UIScreen.RemoveDialog(m_BuyPropertyAlert);
        }

        public void SetTimeOfDay(double time) 
        {
            Color col1 = m_TimeColors[(int)Math.Floor(time * (m_TimeColors.Length - 1))]; //first colour
            Color col2 = m_TimeColors[(int)Math.Floor(time * (m_TimeColors.Length - 1))+1]; //second colour
            double Progress = (time * (m_TimeColors.Length - 1)) % 1; //interpolation progress (mod 1)

            m_TintColor = Color.Lerp(col1, col2, (float)Progress); //linearly interpolate between the two colours for this specific time.

            m_LightPosition = new Vector3(0, 0, -263);
            Matrix Transform = Matrix.Identity;

            Transform *= Matrix.CreateRotationY((float)((((time+0.25)%0.5)+0.5) * Math.PI * 2.0)); //Controls the rotation of the sun/moon around the city. 
            Transform *= Matrix.CreateRotationZ((float)(Math.PI*(45.0/180.0))); //Sun is at an angle of 45 degrees to horizon at it's peak. idk why, it's winter maybe? looks nice either way
            Transform *= Matrix.CreateRotationY((float)(Math.PI * 0.3)); //Offset from front-back a little. This might need some adjusting for the nicest sunset/sunrise locations.
            Transform *= Matrix.CreateTranslation(new Vector3(256, 0, 256)); //Move pivot center to center of mesh.

            m_LightPosition = Vector3.Transform(m_LightPosition, Transform);

            if (Math.Abs((time % 0.5) - 0.25) < 0.05) //Near the horizon, shadows should gracefully fade out into the opposite shadows (moonlight/sunlight)
            {
                m_ShadowMult = (float)(1-(Math.Abs((time % 0.5) - 0.25)*20))*0.35f+0.65f;
            }
            else
            {
                m_ShadowMult = 0.65f; //Shadow strength. Remember to change the above if you alter this.
            }
        }

        private void FixedTimeUpdate()
        {
            m_SpotOsc = (m_SpotOsc + 0.01f) % 1; //spotlight oscillation. Cycles fully every 100 frames.
            if (m_Zoomed)
            {
                m_ZoomProgress += (1.0f - m_ZoomProgress) / 5.0f;
                bool Triggered = false;

                if (m_MouseMove)
                {
                    m_TargVOffX += (m_MouseState.X - m_MouseStart.X) / 1000; //move by fraction of distance between the mouse and where it started in both axis
                    m_TargVOffY -= (m_MouseState.Y - m_MouseStart.Y) / 1000;
                    
                    //it's your duty to deal with the mouse cursor stuff when moving into PD!

                    /*var dir = Math.Round((Math.Atan2(m_MouseStart.X - m_MouseState.Y,
                        m_MouseState.X - m_MouseStart.X) / Math.PI) * 4) + 4;
                    ChangeCursor(dir);*/
                }
                else //edge scroll check - do this even if mouse events are blocked
                {
                    if (m_MouseState.X > m_ScrWidth - 32)
                    {
					    Triggered = true;
					    m_TargVOffX += m_ScrollSpeed;
                        CursorManager.INSTANCE.SetCursor(CursorType.ArrowRight);
					    //changeCursor("right.cur")
				    }
                    if (m_MouseState.X < 32) 
                    {
					    Triggered = true;
					    m_TargVOffX -= m_ScrollSpeed;
                        CursorManager.INSTANCE.SetCursor(CursorType.ArrowLeft);
					    //changeCursor("left.cur");
				    }
                    if (m_MouseState.Y > m_ScrHeight - 32)
                    {
					    Triggered = true;
					    m_TargVOffY -= m_ScrollSpeed;
                        CursorManager.INSTANCE.SetCursor(CursorType.ArrowDown);
					    //changeCursor("down.cur");
				    }
                    if (m_MouseState.Y < 32)
                    {
					    Triggered = true;
                        m_TargVOffY += m_ScrollSpeed;
                        CursorManager.INSTANCE.SetCursor(CursorType.ArrowUp);
					    //changeCursor("up.cur");
				    } 

				    if (!Triggered)
                    {
					    m_ScrollSpeed = 0.1f; //not scrolling. Reset speed, set default cursor.
                        CursorManager.INSTANCE.SetCursor(CursorType.Normal);
					    //changeCursor("auto", true); AKA the default cursor.
				    } 
                    else
					    m_ScrollSpeed += 0.005f; //if edge scrolling make the speed increase the longer the mouse is at the edge.
                }

                m_TargVOffX = Math.Max(-135, Math.Min(m_TargVOffX, 138)); //maximum offsets for zoomed camera. Need adjusting for other screen sizes...
                m_TargVOffY = Math.Max(-100, Math.Min(m_TargVOffY, 103));
            }
            else
                m_ZoomProgress += (0 - m_ZoomProgress) / 5.0f; //zoom progress interpolation. Isn't very fixed but it's a nice gradiation.
        }

        private Texture2D DrawDepth(Effect VertexShader, Effect PixelShader)
        {
            RenderTarget2D RTarget = new RenderTarget2D(m_GraphicsDevice, ShadowRes, ShadowRes, false, SurfaceFormat.Single, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);

            m_GraphicsDevice.SetRenderTarget(RTarget);

            m_GraphicsDevice.Clear(Color.CornflowerBlue);

            VertexShader.CurrentTechnique.Passes[1].Apply();
            PixelShader.CurrentTechnique.Passes[1].Apply();

            m_GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, m_MeshTris); //draw depth texture of city mesh to render target to use for shadowing.

            m_GraphicsDevice.SetRenderTarget(null);
            Texture2D Return = RTarget; //todo: how to dispose of this safely

            return Return;
        }

        public void Draw2DPoly()
        {
            if (m_2DVerts.Count == 0) return;
            m_GraphicsDevice.DepthStencilState = DepthStencilState.None;

            VertexPositionColor[] Vert2D = new VertexPositionColor[m_2DVerts.Count];
            m_2DVerts.CopyTo(Vert2D);

            Matrix View = new Matrix(1.0f, 0.0f, 0.0f, 0.0f,
            0.0f, -1.0f, 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f);

            Matrix Projection = Matrix.CreateOrthographicOffCenter(0, (float)m_ScrWidth, -(float)m_ScrHeight, 0, 0, 1);

            Shader2D.CurrentTechnique = Shader2D.Techniques[0];
            Shader2D.Parameters["Projection"].SetValue(Projection);
            Shader2D.Parameters["View"].SetValue(View);

            Shader2D.CurrentTechnique.Passes[0].Apply();

            m_GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, Vert2D, 0, Vert2D.Length/3); //draw 2d coloured triangle array (for spotlights etc)

            m_GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public override void Draw(GraphicsDevice gfx)
        {
            m_GraphicsDevice = gfx;

            ShadowRes = GlobalSettings.Default.ShadowQuality;
            ShadowsEnabled = GlobalSettings.Default.CityShadows;

            //if (RegenData) GenerateAssets();

            m_GraphicsDevice.RasterizerState = RasterizerState.CullNone; //don't cull
            m_GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            m_ScrHeight = GlobalSettings.Default.GraphicsHeight;
            m_ScrWidth = GlobalSettings.Default.GraphicsWidth;

            if (RegenData) GenerateAssets(); //if assets are flagged as requiring regeneration, regenerate them!

            float ResScale = 768.0f/m_ScrHeight; //scales up the vertical height to match that of the target resolution (for the far view)

            float FisoScale = (float)(Math.Sqrt(0.5 * 0.5 * 2) / 5.10f) * ResScale; // is 5.10 on far zoom
		    float ZisoScale = (float)Math.Sqrt(0.5 * 0.5 * 2) / 144f;  // currently set 144 to near zoom

            float IsoScale = (1 - m_ZoomProgress) * FisoScale + (m_ZoomProgress) * ZisoScale;

            float HB = m_ScrWidth * IsoScale;
            float VB = m_ScrHeight * IsoScale;

            Matrix ProjectionMatrix = Matrix.CreateOrthographicOffCenter(-HB + m_ViewOffX, HB + m_ViewOffX, -VB + m_ViewOffY, VB + m_ViewOffY, 0.1f, 524);
            Matrix ViewMatrix = Matrix.Identity;
            Matrix WorldMatrix = Matrix.Identity;

            Matrix LightView = Matrix.CreateLookAt(m_LightPosition, new Vector3(256, 0, 256), new Vector3(0, 1, 0)); //Create light view - looks from light position to center of mesh.
            Vector2 pos = CalculateR(new Vector2(m_ViewOffX, -m_ViewOffY));
            Vector3 LightOff = Vector3.Transform(new Vector3(pos.X, 0, pos.Y), LightView); //finds position in light space of approximate center of camera (to be used for only shadowing near the camera in near view)

            float size = (1 - m_ZoomProgress) * 262 + (m_ZoomProgress * 40); //size of draw window to use for shadowing. 40 is good for near view, it could be less but that wouldn't work correctly on higher ground.
            Matrix LightProject = Matrix.CreateOrthographicOffCenter(-size + LightOff.X, size + LightOff.X, -size + LightOff.Y, size + LightOff.Y, 0.1f, 524); //create light projection using offsets + size.

            ViewMatrix *= Matrix.CreateScale(new Vector3(1, 0.5f + (float)(1.0 - m_ZoomProgress) / 2, 1)); //makes world flatter in near view. This effect is present in the original, 
            //you just can't notice it as there is no zoom in animation. It also renders in true isometric... but that's an awful idea and makes lots look unusual when placed on flat tiles.

            ViewMatrix *= Matrix.CreateRotationY((45.0f / 180.0f) * (float)Math.PI);
            ViewMatrix *= Matrix.CreateRotationX((30.0f / 180.0f) * (float)Math.PI); //render in pseudo-isometric: http://en.wikipedia.org/wiki/Isometric_graphics_in_video_games_and_pixel_art
            ViewMatrix *= Matrix.CreateTranslation(new Vector3(-360f, 0f, -262f)); //move model to center of screen.

            VertexShader.CurrentTechnique = VertexShader.Techniques[0];
            VertexShader.Parameters["BaseMatrix"].SetValue((WorldMatrix*ViewMatrix)*ProjectionMatrix);
            VertexShader.Parameters["LightMatrix"].SetValue((WorldMatrix*LightView)*LightProject);

            PixelShader.CurrentTechnique = PixelShader.Techniques[0];
            PixelShader.Parameters["LightCol"].SetValue(new Vector4(m_TintColor.R/255.0f, m_TintColor.G/255.0f, m_TintColor.B/255.0f, 1));
            PixelShader.Parameters["VertexColorTex"].SetValue(m_VertexColor);
            PixelShader.Parameters["TextureAtlasTex"].SetValue(Atlas);
            PixelShader.Parameters["TransAtlasTex"].SetValue(TransAtlas);
            PixelShader.Parameters["RoadAtlasTex"].SetValue(RoadAtlas);
            PixelShader.Parameters["RoadAtlasCTex"].SetValue(RoadCAtlas);
            PixelShader.Parameters["ShadowMult"].SetValue(m_ShadowMult);

            m_GraphicsDevice.SetVertexBuffer(vertBuf);

            Texture2D ShadowMap = null;

            if (ShadowsEnabled)
            {
                ShadowMap = DrawDepth(VertexShader, PixelShader);
                PixelShader.Parameters["ShadowMap"].SetValue(ShadowMap);
                PixelShader.Parameters["ShadSize"].SetValue(new Vector2(ShadowMap.Width, ShadowMap.Height));
            }
            m_GraphicsDevice.Clear(Color.Black);


            if (ShadowsEnabled)
            {
                PixelShader.CurrentTechnique.Passes[0].Apply();
                VertexShader.CurrentTechnique.Passes[0].Apply();
            }
            else
            {
                PixelShader.CurrentTechnique.Passes[2].Apply();
                VertexShader.CurrentTechnique.Passes[2].Apply();
            }

            m_GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, m_MeshTris);

            if (ShadowsEnabled)
            {
                ShadowMap.Dispose(); //free up space used by this frame's shadow map.
            }
            else
            {
            }

            m_MovMatrix = ViewMatrix;

            if (!m_Zoomed) DrawHouses(HB); //draw far view house icons

            m_2DVerts = new ArrayList(); //refresh list for tris under houses
            DrawSprites(HB, VB); //draw near view trees and houses

            m_2DVerts = new ArrayList(); //refresh list for spotlights
            DrawSpotlights(HB); //draw far view spotlights
            Draw2DPoly(); //draw spotlights using 2DVert shader
        }
    }
}