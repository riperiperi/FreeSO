using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Rendering.Framework.Model;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Input;
using FSO.Files;
using FSO.Content.Model;
using FSO.Client.Rendering.City.Plugins.PainterModes;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using FSO.Client.Controllers;

namespace FSO.Client.Rendering.City.Plugins
{
    public class MapPainterPlugin : AbstractCityPlugin
    {
        private static int ClientCommandID;

        private TerrainController Controller;

        public Vector2 LastPos;

        public bool Erasing { get; private set; }
        public bool Accelerate { get; private set; }

        public Color[] TerrainTypes = new Color[] {
            new Color(0, 255, 0), //grass
            new Color(12, 0, 255), //water
            new Color(255, 0, 0), //rock
            new Color(255, 255, 255), //snow
            new Color(255, 255, 0) //sand
        };

        public TerrainType[] TerrainTypeIndices = [
            TerrainType.GRASS,
            TerrainType.WATER,
            TerrainType.ROCK, 
            TerrainType.SNOW,
            TerrainType.SAND,
        ];

        public string[] TerrainTypeNames = new string[] {
            "Grass",
            "Water",
            "Rock",
            "Snow",
            "Sand"
        };

        public byte[] ForestDensities = new byte[] {
            0,
            64,
            128,
            192,
            255
        };

        public ForestType[] ForestTypeIndices = [
            ForestType.HEAVY,
            ForestType.LIGHT,
            ForestType.CACTI,
            ForestType.PALM,
            ForestType.NULL,
            ];
            
        public Color[] ForestTypes = new Color[] {
            new Color(0, 0x6A, 0x28),
            new Color(0, 0xEB, 0x42),
            new Color(255, 0, 0),
            new Color(255, 0xFC, 0),
            new Color(0, 0, 0),
        };

        public Color[] ForestDensityColors;

        public int SelectedModifier;
        public int BrushSize;
        public PainterMode Mode;

        private IMapPainterMode Tool;

        public MapPainterPlugin(Terrain city) : base(city)
        {
            ForestDensityColors = ForestDensities.Select(x => new Color(x, x, x, (byte)255)).ToArray();
            ForceNear = true;

            Controller = City.FindController<TerrainController>();

            SwitchMode(PainterMode.ROAD);
        }

        public override void Draw(SpriteBatch sb)
        {
            sb.Begin();

            Tool?.Draw(sb);

            sb.End();
        }

        public override void TileHover(Vector2? tile)
        {
            Tool?.TileHover(tile);

            if (tile != null) LastPos = tile.Value;
        }

        public override void TileMouseDown(Vector2 tile)
        {
            Tool?.TileMouseDown(tile);
        }

        public override void TileMouseUp(Vector2? tile)
        {
            Tool?.TileMouseUp(tile);
        }

        public void Commit(bool hasChange)
        {
            if (hasChange)
            {
                var cmd = Tool?.Command;

                if (cmd != null)
                {
                    cmd.UserModId = ClientCommandID;

                    Controller.UpdateTempMapChange(cmd);
                    Controller.CommitMapChange(cmd);

                    ClientCommandID++;
                }
            }
            else
            {
                // Only clears the temp command.

                Controller.UpdateTempMapChange(null);
            }
        }

        public void UpdateTemp()
        {
            var cmd = Tool?.Command;

            if (cmd != null)
            {
                cmd.UserModId = ClientCommandID;

                Controller.UpdateTempMapChange(cmd);
            }
            else
            {
                Controller.UpdateTempMapChange(null);
            }
        }

        public void SwitchMode(PainterMode newMode)
        {
            if (Mode != newMode) TileMouseUp(null);

            Tool = newMode switch
            {
                PainterMode.ROAD => new MapPainterRoad(this),
                PainterMode.ELEVATION_CIRCLE => new MapPainterElevationCircle(this),
                PainterMode.ELEVATION_FLAT => new MapPainterElevationFlat(this),
                PainterMode.TERRAINTYPE => new MapPainterPaint<TerrainType>(this, TerrainTypes, TerrainTypeIndices, CityEditPaintType.TerrainType),
                PainterMode.FORESTTYPE => new MapPainterPaint<ForestType>(this, ForestTypes, ForestTypeIndices, CityEditPaintType.ForestType),
                PainterMode.FORESTDENSITY => new MapPainterPaint<byte>(this, ForestDensityColors, ForestDensities, CityEditPaintType.ForestDensity),
                _ => null
            };

            Mode = newMode;
        }

        public override void Update(UpdateState state)
        {
            Tool?.Update(state);
            
            var pressed = state.NewKeys;
            Erasing = state.CtrlDown;
            Accelerate = state.ShiftDown;

            ///*
            for (int i = 2; i<11; i++)
            {
                Keys key;
                if (Enum.TryParse("F"+i, out key))
                {
                    var dir = Path.Combine(Common.FSOEnvironment.UserDir, "CityPainterSave" + i + "/");
                    if (pressed.Contains(key))
                    {
                        if (Accelerate)
                        {
                            City.MapData.Save(dir);
                            UIScreen.GlobalShowAlert(new UI.Controls.UIAlertOptions { Title = "Save Success", Message = "Saved city data " + i + "." }, true);
                        }
                        else if (Directory.Exists(dir))
                        {
                            //City.MapData.Load(dir, LoadTex, "png");
                            City.GenerateCityMesh(GameFacade.GraphicsDevice, null);
                            //UIScreen.GlobalShowAlert(new UI.Controls.UIAlertOptions { Title = "Load Success", Message = "Loaded city data " + i + "." }, true);
                        }
                        else
                        {
                            UIScreen.GlobalShowAlert(new UI.Controls.UIAlertOptions { Title = "Load Failed", Message = "Could not find city data " + i + "." }, true);
                        }
                    }
                }
            }
            //*/

            var keys = state.KeyboardState;
            if (keys.IsKeyDown(Keys.R)) SwitchMode(PainterMode.ROAD);
            else if (keys.IsKeyDown(Keys.T)) SwitchMode(PainterMode.TERRAINTYPE);
            else if (keys.IsKeyDown(Keys.E)) SwitchMode(PainterMode.ELEVATION_CIRCLE);
            else if (keys.IsKeyDown(Keys.F)) SwitchMode(PainterMode.ELEVATION_FLAT);
            else if (keys.IsKeyDown(Keys.C)) SwitchMode(PainterMode.FORESTTYPE);
            else if (keys.IsKeyDown(Keys.D)) SwitchMode(PainterMode.FORESTDENSITY);

            var oldS = SelectedModifier;
            if (keys.IsKeyDown(Keys.NumPad0)) SelectedModifier = 0;
            if (keys.IsKeyDown(Keys.NumPad1)) SelectedModifier = 1;
            if (keys.IsKeyDown(Keys.NumPad2)) SelectedModifier = 2;
            if (keys.IsKeyDown(Keys.NumPad3)) SelectedModifier = 3;
            if (keys.IsKeyDown(Keys.NumPad4)) SelectedModifier = 4;

            if (pressed.Contains(Keys.Up)) BrushSize += 1;
            if (pressed.Contains(Keys.Down)) BrushSize = Math.Max(0, BrushSize - 1);    
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
    }

    public enum PainterMode
    {
        ROAD,
        TERRAINTYPE,
        ELEVATION_CIRCLE,
        ELEVATION_FLAT,
        FORESTTYPE,
        FORESTDENSITY
    }
}
