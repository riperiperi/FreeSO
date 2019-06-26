using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Model;
using FSO.Common.Utils;
using FSO.Files;
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
    /// A class which loads all textures, models and generates texture atlases.
    /// </summary>
    public class CityContent
    {
        public Texture2D[] TerrainTextures = new Texture2D[5]; //grass, sand, rock, snow, water
        public Texture2D VertexColor;
        public CityMapData MapData;

        public Texture2D[] TransA; //moved into an atlas
        public Texture2D[] TransB; //moved into an atlas

        public Texture2D[] Roads;  //moved into an atlas
        public Texture2D[] RoadCorners;  //moved into an atlas

        public Texture2D[] TransAtlas = new Texture2D[4];
        public Texture2D RoadAtlas;
        public Texture2D Forest;
        public Texture2D WhiteLine;
        public Texture2D stpWhiteLine;

        public Texture2D LotOnline;
        public Texture2D LotOffline;
        public Texture2D DefaultHouse;

        public Texture2D BigWNormal; //big water normal, for large scale normal map changes
        public Texture2D SmallWNormal; //small water normal, for small scale normal map changes
        public Texture2D TreeTex;

        public string[] NeighTexNames = new string[] { "circles.png", "triangles.png", "squares.png" };
        public Texture2D[] NeighTextures = new Texture2D[3];

        /// <summary>
        /// Each blend flag's index in the atlas.
        /// The atlas is arranged in such a way to minimize visual errors from mipmapping and linear blending.
        /// 
        /// 7x3
        /// </summary>
        public static int[] FlagLayout = new int[]
        {
            11, 7, 15, 2, 9, 6, 0, 4, 1, 16, 20, 12, 14, 18, 10, 8
        };

        public static int RoadWidth = 8;
        public static int RoadHeight = 4;
        public static int[] RoadLayout = new int[] { -1, 5, 12, 13, 7, 6, 15, 14, 28, 29, 20, 21, 31, 30, 23, 22 };
        public static int[] RoadCLayout = new int[] { -1, 8, 2, 26, 3, 17, 16, 10, 25, 24, 9, 18, 1, 27, 11, 19 };

        public void LoadContent(GraphicsDevice gd, int cityNumber)
        {
            String gamepath = GameFacade.GameFilePath("");

            string CityStr = "city_" + cityNumber.ToString("0000");
            string ext = "bmp";
            if (cityNumber >= 100)
            {
                //start FSO cities
                //the first few will be client included
                //probably after 200 will be inherited from content packs, when they are implemented
                ext = "png";
                CityStr = Path.Combine(FSOEnvironment.ContentDir, "Cities/", CityStr);
            }
            else
            {
                CityStr = gamepath + "cities/" + CityStr;
            }
            VertexColor = LoadTex(CityStr + "/vertexcolor." + ext);

            MapData = new CityMapData();
            MapData.Load(CityStr, LoadTex, ext);
            
            //special tuning from server
            var terrainTuning = DynamicTuning.Global?.GetTable("city", 0);
            float forceSnow;
            if (terrainTuning != null && terrainTuning.TryGetValue(0, out forceSnow)) ForceSnow(forceSnow > 0);

            //grass, sand, rock, snow, water
            TerrainTextures[0] = RTToMip(LoadTex(gamepath + "gamedata/terrain/newformat/gr.tga"), gd);
            TerrainTextures[1] = RTToMip(LoadTex(gamepath + "gamedata/terrain/newformat/sd.tga"), gd);
            TerrainTextures[2] = RTToMip(LoadTex(gamepath + "gamedata/terrain/newformat/rk.tga"), gd);
            TerrainTextures[3] = RTToMip(LoadTex(gamepath + "gamedata/terrain/newformat/sn.tga"), gd);
            TerrainTextures[4] = RTToMip(LoadTex(gamepath + "gamedata/terrain/newformat/wt.tga"), gd);
            Forest = LoadTex(gamepath + "gamedata/farzoom/forest00a.tga");
            DefaultHouse = LoadTex(gamepath + "userdata/houses/defaulthouse.bmp");//, new TextureCreationParameters(128, 64, 24, 0, SurfaceFormat.Rgba32, TextureUsage.Linear, Color.Black, FilterOptions.None, FilterOptions.None));
            //Can crash on some setups on dx11?
            TextureUtils.ManualTextureMaskSingleThreaded(ref DefaultHouse, new uint[] { new Color(0x00, 0x00, 0x00, 0xFF).PackedValue });

            LotOnline = UIElement.GetTexture(0x0000032F00000001);
            LotOffline = UIElement.GetTexture(0x0000033100000001);

            //fills used for line drawing
            
            WhiteLine = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, Color.White);
            stpWhiteLine = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, new Color(255, 255, 255, 128));

            string Num;

            TransA = new Texture2D[30];
            for (int x = 0; x < 30; x = x + 2)
            {
                Num = (x / 2).ToString().PadLeft(2, '0');
                TransA[x] = LoadTex(gamepath + "gamedata/terrain/newformat/transa" + Num + "a.tga");
                TransA[x + 1] = LoadTex(gamepath + "gamedata/terrain/newformat/transa" + Num + "b.tga");
            }

            TransB = new Texture2D[30];
            for (int x = 0; x < 30; x = x + 2)
            {
                Num = (x / 2).ToString().PadLeft(2, '0');
                TransB[x] = LoadTex(gamepath + "gamedata/terrain/newformat/transb" + Num + "a.tga");
                TransB[x + 1] = LoadTex(gamepath + "gamedata/terrain/newformat/transb" + Num + "b.tga");
            }

            var terrainpath = "Content/Textures/terrain/"; //gamepath + "gamedata/terrain/";
            //TODO: optionally load non-freeso textures

            Roads = new Texture2D[16];
            for (int x = 0; x < 16; x++)
            {
                Num = (x).ToString().PadLeft(2, '0');
                Roads[x] = LoadTex(terrainpath + "road" + Num + ".png");
            }

            RoadCorners = new Texture2D[16];
            for (int x = 0; x < 16; x++)
            {
                Num = (x).ToString().PadLeft(2, '0');
                RoadCorners[x] = LoadTex(terrainpath + "roadcorner" + Num + ".png");
            }

            BigWNormal = RTToMip(LoadTex(terrainpath + "bigwnormal.jpg"), gd);
            SmallWNormal = RTToMip(LoadTex(terrainpath + "smallwnormal.jpg"), gd);
            using (var strm = new FileStream(terrainpath + "trees.png", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                TreeTex = ImageLoader.FromStream(gd, strm);
                if (FSOEnvironment.EnableNPOTMip)
                    TreeTex = RTToMip(TreeTex, gd);
            }
                

            for (int i=0; i<3; i++)
            {
                NeighTextures[i] = RTToMip(LoadTex("Content/Textures/" + NeighTexNames[i]), gd);
            }

            var batch = new SpriteBatch(GameFacade.GraphicsDevice);
            for (int i=0; i<4; i++) CreateTransparencyAtlas(gd, batch, i);
            CreateRoadAtlas(gd, batch);

            for (int x = 0; x < 30; x++) TransA[x].Dispose();
            for (int x = 0; x < 30; x++) TransB[x].Dispose();
            for (int x = 0; x < 16; x++) Roads[x].Dispose();
            for (int x = 0; x < 16; x++) RoadCorners[x].Dispose();
        }

        public void ForceSnow(bool toGrass)
        {
            var dat = new Color[VertexColor.Width * VertexColor.Height];
            VertexColor.GetData(dat);
            var typeC = MapData.TerrainTypeColorData;
            var type = MapData.TerrainType;

            for (int i = 0; i < dat.Length; i++)
            {
                var old = dat[i];
                var greater = Math.Max(old.R, old.G);
                if (!toGrass)
                {
                    if (old.B < greater)
                    {
                        //make this pixel grayscale
                        dat[i] = new Color(greater, greater, greater);
                    }
                }
                var oldType = typeC[i];
                if (toGrass) //change snow to grass
                {
                    if (oldType == Color.White)
                    {
                        typeC[i] = new Color(0, 255, 0);
                        type[i] = 0;
                    }
                }
                else
                {
                    if (oldType == new Color(0, 255, 0) || oldType == Color.Yellow)
                    {
                        typeC[i] = Color.White;
                        type[i] = 3;
                    }
                }
            }

            VertexColor.SetData(dat);
        }

        private Texture2D RTToMip(Texture2D texture, GraphicsDevice device)
        {
            var data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            
            Texture2D newTex = null;
            try
            {
                newTex = new Texture2D(device, texture.Width, texture.Height, true, SurfaceFormat.Color);
                TextureUtils.UploadWithAvgMips(newTex, device, data);
                texture.Dispose();
                texture = newTex;
            } catch
            {
                try
                {
                    newTex?.Dispose();
                } catch
                {

                }
            }
            return texture;
        }


        private Texture2D LoadTex(string Path)
        {
            using (var strm = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadTex(strm);
        }

        private Texture2D LoadTex(Stream stream)
        {
            Texture2D result = null;
            try
            {
                result = ImageLoader.FromStream(GameFacade.GraphicsDevice, stream);
            }
            catch (Exception)
            {
                result = new Texture2D(GameFacade.GraphicsDevice, 1, 1);
            }
            stream.Close();
            return result;
        }

        public void CreateTransparencyAtlas(GraphicsDevice gd, SpriteBatch spriteBatch, int type)
        {
            var source = (type > 1)?TransB:TransA;
            var index = type % 2;

            var sizeX = source[index].Width;
            var sizeY = source[index].Height;

            RenderTarget2D RTarget = new RenderTarget2D(gd, sizeX*7, sizeY*3, false, SurfaceFormat.Color, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
            gd.SetRenderTarget(RTarget);

            gd.Clear(Color.Black);

            spriteBatch.Begin();

            for (int i=0; i<15; i++)
            {
                var x = FlagLayout[i] % 7;
                var y = FlagLayout[i] / 7;
                spriteBatch.Draw(source[index+i*2], new Rectangle(x * sizeX, y*sizeY, sizeX, sizeY), Color.White);
            }

            Texture2D black = new Texture2D(gd, 1, 1);
            black.SetData<Color>(new Color[] { Color.Black });
            spriteBatch.Draw(black, new Rectangle(1024 - 64, 0, 64, 256), Color.Black);
            //fill far end with black to cause no blend if adjacency bitmask is "0000"

            spriteBatch.End();
            gd.SetRenderTarget(null);

            if (FSOEnvironment.EnableNPOTMip)
                TransAtlas[type] = RTToMip(RTarget, gd);
            else
                TransAtlas[type] = RTarget;
        }

        public void CreateRoadAtlas(GraphicsDevice gd, SpriteBatch spriteBatch)
        {
            var sizeX = Roads[0].Width;
            var sizeY = Roads[0].Height;

            RenderTarget2D RTarget = new RenderTarget2D(gd, sizeX * RoadWidth, sizeY * RoadHeight, false, SurfaceFormat.Color, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
            gd.SetRenderTarget(RTarget);

            gd.Clear(Color.TransparentBlack);

            spriteBatch.Begin();

            for (int i = 1; i < 16; i++)
            {
                var x = RoadLayout[i] % RoadWidth;
                var y = RoadLayout[i] / RoadWidth;
                spriteBatch.Draw(Roads[i], new Rectangle(x * sizeX, y * sizeY, sizeX, sizeY), Color.White);
            }


            for (int i = 1; i < 16; i++)
            {
                var x = RoadCLayout[i] % RoadWidth;
                var y = RoadCLayout[i] / RoadWidth;
                spriteBatch.Draw(RoadCorners[i], new Rectangle(x * sizeX, y * sizeY, sizeX, sizeY), Color.White);
            }

            spriteBatch.End();
            gd.SetRenderTarget(null);

            if (FSOEnvironment.EnableNPOTMip)
                RoadAtlas = RTToMip(RTarget, gd);
            else
                RoadAtlas = RTarget;
        }

        public void Dispose()
        {
            foreach (var tex in TerrainTextures) tex.Dispose();
            VertexColor.Dispose();

            foreach (var tex in TransAtlas) tex.Dispose();
            RoadAtlas.Dispose();
            Forest.Dispose();

            LotOnline.Dispose();
            LotOffline.Dispose();
            DefaultHouse.Dispose();

            BigWNormal.Dispose();
            SmallWNormal.Dispose();
            TreeTex.Dispose();

            foreach (var tex in NeighTextures) tex.Dispose();
        }
    }
}
