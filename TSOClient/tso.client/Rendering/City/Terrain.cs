using FSO.Client.Controllers;
using FSO.Client.Rendering.City.Plugins;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Screens;
using FSO.Common;
using FSO.Common.Domain.RealestateDomain;
using FSO.Common.Rendering;
using FSO.Common.Rendering.Framework;
using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Content.Model;
using FSO.Files.RC;
using FSO.LotView;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.LotView.RC;
using FSO.LotView.Utils.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections;

namespace FSO.Client.Rendering.City
{
    public class Terrain : _3DAbstract, IDisposable, IRCSurroundings
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

        public readonly CityLotTiles LotTiles = new();
        public IEnumerable<LotTileEntry> LotTileData => LotTiles.List;
        public bool LotTileDataDirty = true;

        public Dictionary<Vector2, LotTileEntry> LotTileLookup => LotTiles.TileByVector;
        public HashSet<int> OccupiedTiles => LotTiles.OccupiedTiles;

        public VertexBuffer LotOfflineVerts;
        public IndexBuffer LotOfflineInds;
        public VertexBuffer LotOnlineVerts;
        public IndexBuffer LotOnlineInds;

        public bool HandleMouse = false;
        public CityMap MapData { get
            {
                return Content.MapData;
            }
        }
        private Color m_TintColor;
        private Color m_TintColorSprite;

        public Effect Shader2D, PixelShader, VertexShader;
        private Vector3 m_LightPosition;

        private IShardRealestateDomain Realestate;
        private ArrayList m_2DVerts;

        public static uint[] MASK_COLORS = [
            new Color(0xFF, 0x00, 0xFF, 0xFF).PackedValue,
            new Color(0xFE, 0x02, 0xFE, 0xFF).PackedValue,
            new Color(0xFF, 0x01, 0xFF, 0xFF).PackedValue
        ];

        public new ICityCamera Camera = (GraphicsModeControl.Mode == GlobalGraphicsMode.Full3D)?new CityCamera3D():new CityCamera2D();

        public static float NEAR_ZOOM_SIZE = 288;
        public TerrainZoomMode m_Zoomed
        {
            get
            {
                return Camera.Zoomed;
            }
            set
            {
                Camera.Zoomed = value;
            }
        }
        public float m_LotZoomProgress
        {
            get
            {
                return Camera.LotZoomProgress;
            }
            set
            {
                Camera.LotZoomProgress = value;
            }
        }

        private DateTime LastCityUpdate = DateTime.Now;

        private MouseState m_MouseState, m_LastMouseState;
        private int m_ScrHeight, m_ScrWidth;
        public float m_ZoomProgress
        {
            get
            {
                return Camera.ZoomProgress;
            }
            set
            {
                Camera.ZoomProgress = value;
            }
        }

        private float m_SpotOsc = 0;
        private float m_ShadowMult = 1;
        //private double m_DayNightCycle = 0.0;
        private int[] m_SelTile = new int[] { -1, -1 };
        private Vector2? m_VecSelTile;
        private Matrix m_MovMatrix;
        private int[][] m_SurTileOffs =
        [
            [0, -1],
            [1, -1],
            [1, 0],
            [1, 1],
            [0, 1],
            [-1, 1],
            [-1, 0],
            [-1, -1],
        ];

        private float DayOffset = 0.25f;
        private float DayDuration = 0.60f;

        private SpriteBatch m_Batch;
        private RenderTarget2D ShadowTarget;
        private int OldShadowRes;
        private int ShadowRegenTimer = 1;
        private float m_LastIsoScale;

        public AbstractSkyDome SkyDome;
        public AbstractCityPlugin Plugin;
        public List<ParticleComponent> Particles;
        public BasicCamera ParticleCamera;
        public WeatherController Weather;

        public CityContent Content;
        public CityGeometry Geometry;
        public CityGeometry SubdivGeometry;
        public CityFoliage Foliage;
        public CityNeighGeom NeighGeom;
        public CityFacadeLock NearFacades;

        private CityVertexColorGenerator VertexColorGenerator;
        private bool VertexColorDirty;

        private List<CityModification> Modifications = [];

        public void LoadContent(GraphicsDevice GfxDevice)
        {
            Content = new CityContent();
            Content.LoadContent(GfxDevice, Realestate.GetMap());
            Geometry = new CityGeometry();
            SubdivGeometry = new CityGeometry();
            Foliage = new CityFoliage();
            NeighGeom = new CityNeighGeom(this);
            NeighGeom.Generate(GfxDevice);
            VertexColorGenerator = new CityVertexColorGenerator(this);

            m_GraphicsDevice = GfxDevice;
            VertexShader = GameFacade.Game.Content.Load<Effect>("Effects/VerShader");
            PixelShader = GameFacade.Game.Content.Load<Effect>("Effects/PixShader");
            Shader2D = GameFacade.Game.Content.Load<Effect>("Effects/colorpoly2D");
            m_Batch = new SpriteBatch(GameFacade.GraphicsDevice);
        }

        public Terrain(GraphicsDevice Device) : base(Device)
        {
            Particles = new List<ParticleComponent>();
            Weather = new WeatherController(Particles);
            SkyDome = new AbstractSkyDome(Device, 0f);
            ParticleCamera = new BasicCamera(Device, Vector3.Zero, new Vector3(0, 0.5f, 0.86602540f), Vector3.Up);
        }

        public void Initialize(IShardRealestateDomain realestate)
        {
            Realestate = realestate;
            GraphicsModeControl.ModeChanged += SwitchToMode;
        }

        public void SignalCityDirty()
        {
            LotTileDataDirty = true;
        }

        public void GenerateAssets()
        {
            GenerateCityMesh(m_GraphicsDevice, null); //generates the city mesh
            RegenData = false; //don't do this again next frame...
        }

        public override void Dispose()
        {
            Content.Dispose();
            SubdivGeometry.Dispose();
            Geometry.Dispose();
            Foliage.Dispose();
            NearFacades?.Dispose();
            NeighGeom?.Dispose();
            VertexColorGenerator?.Dispose();

            foreach (var particle in Particles) particle.Dispose();
            Particles.Clear();
            GraphicsModeControl.ModeChanged -= SwitchToMode;
        }

        public void DisposeOnLot()
        {
            LastWorld = null;
            NearFacades?.Dispose();
            NearFacades = null;
        }

        internal void DrawLine(Texture2D fill, Vector2 start, Vector2 end, SpriteBatch spriteBatch, int lineWidth, Color tint) //draws a line from Start to End.
        {
            double length = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
            float direction = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
            spriteBatch.Draw(fill, new Rectangle((int)start.X, (int)start.Y - (int)(lineWidth / 2), (int)length, lineWidth), null, tint, direction, new Vector2(0, 0.5f), SpriteEffects.None, 0); //
        }

        internal void DrawLine(Texture2D fill, Vector2 start, Vector2 end, SpriteBatch spriteBatch, int lineWidth, float opacity) //draws a line from Start to End.
        {
            DrawLine(fill, start, end, spriteBatch, lineWidth, new Color(1f, 1f, 1f, 1f) * opacity);
        }

        internal void DrawSpike(Vector2 start, float height, SpriteBatch batch, int width, Color color)
        {
            if (start.X < 0 || start.Y < 0 || start.X >= 512 || start.Y >= 512)
            {
                return;
            }

            var alt = InterpElevationAt(start);

            var from = transformSpr4(new Vector3(start.X, alt, start.Y));
            var to = transformSpr4(new Vector3(start.X, alt + height, start.Y));

            if (from.Z <= 0 || to.Z <= 0)
            {
                return;
            }

            float width3d = width * GetSpriteScale();

            var fromPos = new Vector2(from.X, from.Y);
            var vec = new Vector2(to.X, to.Y) - fromPos;

            float direction = (float)Math.Atan2(vec.X, -vec.Y);
            float dist = vec.Length();

            if (dist == 0)
            {
                return;
            }

            var spike = Content.PainterSpike;

            Vector2 origin = new(spike.Width / 2, spike.Height - 7);
            Vector2 scale = new((width3d / spike.Width) / from.W, dist / origin.Y);

            Rectangle spikeTop = new Rectangle(0, 0, spike.Width, spike.Height - 7);
            Rectangle spikeBottom = new Rectangle(0, spikeTop.Height, spike.Width, 7);

            batch.Draw(spike, fromPos, spikeTop, color, direction, origin, scale, SpriteEffects.None, 0);

            float coneScale = 1.5f;
            Vector2 bottomScale = new(scale.X, scale.X * coneScale);
            batch.Draw(spike, fromPos, spikeBottom, color, direction, new Vector2(origin.X, 0), bottomScale, SpriteEffects.None, 0);

            Get2DFromTile(start.X, start.Y);
        }

        public void DrawLocal3D(SpriteBatch batch, Texture2D tex, Vector2 tile, Vector2 offset, float scale3D, Vector2 scale, Color color)
        {
            if (tile.X < 0 || tile.Y < 0 || tile.X >= 512 || tile.Y >= 512)
            {
                return;
            }

            scale3D *= GetSpriteScale();
            var alt = InterpElevationAt(tile);

            var basePos = transformSpr4(new Vector3(tile.X, alt, tile.Y));

            if (basePos.Z <= 0)
            {
                return;
            }

            var size = scale3D / basePos.W;
            var base2D = new Vector2(basePos.X, basePos.Y);

            batch.Draw(tex, base2D + offset * size, null, color, 0, Vector2.Zero, scale * size, SpriteEffects.None, 0);
        }

        public void SwitchToMode(GlobalGraphicsMode mode)
        {
            var old = Camera;
            Camera = (mode == GlobalGraphicsMode.Full3D) ? new CityCamera3D() : (ICityCamera)new CityCamera2D();
            Camera.Zoomed = old.Zoomed;
            Camera.LotZoomProgress = old.LotZoomProgress;
            Camera.ZoomProgress = old.ZoomProgress;
            Camera.CenterCam = old.CenterCam;
            Camera.Target = old.Target;
            if (Camera is CityCamera3D cam3D) cam3D.CenterTile = new Vector2(old.Target.X, old.Target.Z);

            if (Camera.Zoomed == TerrainZoomMode.Lot && LastWorld != null)
            {
                InheritPosition(LastWorld, FindController<TerrainController>()?.Parent, true);
            }
        }

        public void RegenerateVertexColor()
        {
            VertexColorDirty = true;
        }

        public void GenerateCityMesh(GraphicsDevice gd, Rectangle? range)
        {
            Geometry.MapData = Content.MapData;
            SubdivGeometry.MapData = Content.MapData;
            Foliage.MapData = Content.MapData;

            if (range == null)
            {
                Geometry.RegenMeshVerts(gd, false);
            }
            else
            {
                var pos = Camera.CalculateR();
                var slicex = Math.Max(0, Math.Min(30, (int)Math.Round(pos.X / 16f) - 1));
                var slicey = Math.Max(0, Math.Min(30, (int)Math.Round(pos.Y / 16f) - 1));
                var slice = slicex + slicey * 32;

                Geometry.RegenMeshVerts(gd, true);
                SubdivGeometry.SubRegenMeshVerts(m_GraphicsDevice, new Rectangle(slicex * 16, slicey * 16, 32, 32), 4, slice);
                Foliage.InvalidateChunks(range.Value);
            }
        }

        public Vector2? GetHoverSquare(double[] bounds)
        {
            return EstTileAtPosWithScroll(m_MouseState.Position.ToVector2() / FSOEnvironment.DPIScaleFactor, null);
        }


        #region Helper Utilities
        public Vector2 EstTileAtPosWithScroll(Vector2 pos, List<Point> hits)
        {
            pos *= new Vector2(FSOEnvironment.DPIScaleFactor);
            var sPos = new Vector3(pos, 0);

            var p1 = GameFacade.GraphicsDevice.Viewport.Unproject(sPos, Camera.Projection, Camera.View, Matrix.Identity);
            sPos.Z = 1;
            var p2 = GameFacade.GraphicsDevice.Viewport.Unproject(sPos, Camera.Projection, Camera.View, Matrix.Identity);
            var dir = p2 - p1;
            dir.Normalize();
            var ray = new Ray(p1, p2 - p1);
            ray.Direction.Normalize();

            var width = 512;
            var height = 512;
            var tileSize = 1;

            var baseBox = new BoundingBox(new Vector3(0, -5000, 0), new Vector3(width, 5000, height));
            if (baseBox.Contains(ray.Position) != ContainmentType.Contains)
            {
                //move ray start inside box
                var i = baseBox.Intersects(ray);
                if (i != null)
                {
                    ray.Position += ray.Direction * (i.Value + 0.01f);
                }
            }

            var mx = (int)ray.Position.X / tileSize;
            var my = (int)ray.Position.Z / tileSize;

            var px = (ray.Direction.X > 0);
            var py = (ray.Direction.Z > 0);

            int iteration = 0;
            while (mx >= 0 && mx < width && my >= 0 && my < height)
            {
                if (hits != null) hits.Add(new Point(mx, my));
                var plane = new Plane(
                    new Vector3(mx * tileSize, GetElevationVert(mx, my), my * tileSize),
                    new Vector3(mx * tileSize + tileSize, GetElevationVert(mx + 1, my), my * tileSize),
                    new Vector3(mx * tileSize + tileSize, GetElevationVert(mx + 1, my + 1), my * tileSize + tileSize)
                    );
                var tBounds = new BoundingBox(new Vector3(mx * tileSize, -5000, my * tileSize), new Vector3(mx * tileSize + tileSize, 5000, my * tileSize + tileSize));

                var t1 = ray.Intersects(plane);
                var t2 = BoxRC2(ray, tileSize);
                //var t2 = BoxRC(ray, tBounds);
                if (plane.DotCoordinate(ray.Position) > 0) t1 = 0;
                if (t1 != null && t2 != null && t1.Value < t2.Value)
                {
                    //hit the ground...
                    var tentative = ray.Position + ray.Direction * (t1.Value + 0.00001f);

                    //did it hit the correct side of the triangle?
                    var mySide = ((tentative.X / tileSize) % 1) - ((tentative.Z / tileSize) % 1);
                    if (mySide >= 0)
                    {
                        return new Vector2(tentative.X / tileSize, tentative.Z / tileSize);
                    }
                    else
                    {
                        plane = new Plane(
                        new Vector3(mx * tileSize, GetElevationVert(mx, my), my * tileSize),
                        new Vector3(mx * tileSize, GetElevationVert(mx, my + 1), my * tileSize + tileSize),
                        new Vector3(mx * tileSize + tileSize, GetElevationVert(mx + 1, my + 1), my * tileSize + tileSize)
                        );
                        t1 = ray.Intersects(plane);
                        if (t1 != null && t2 != null && t1.Value < t2.Value)
                        {
                            //hit the other side
                            tentative = ray.Position + ray.Direction * (t1.Value + 0.00001f);
                            return new Vector2(tentative.X / tileSize, tentative.Z / tileSize);
                        }
                    }
                }
                if (t2 == null) break;
                ray.Position += ray.Direction * (t2.Value + 0.00001f);

                mx = (!px) ? ((int)Math.Ceiling(ray.Position.X / tileSize) - 1) :
                               (int)(ray.Position.X / tileSize);
                my = (!py) ? ((int)Math.Ceiling(ray.Position.Z / tileSize) - 1) :
                               (int)(ray.Position.Z / tileSize);

                if (iteration++ > 1000) break;
            }

            //fall back to base positioning
            var bplane = new Plane(new Vector3(0, 0, 0), new Vector3(width * tileSize, 0, 0), new Vector3(0, 0, height * tileSize));
            var cast = ray.Intersects(bplane);
            if (cast != null)
            {
                ray.Position += ray.Direction * (cast.Value + 0.01f);
                return new Vector2(ray.Position.X / tileSize, ray.Position.Z / tileSize);
            }

            return new Vector2(-1, -1);
        }


        public float? BoxRC2(Ray ray, float tileSize)
        {
            var px = (ray.Direction.X > 0);
            var py = (ray.Direction.Z > 0);
            //find current tile
            int x = (!px) ? (int)Math.Ceiling(ray.Position.X / tileSize) :
                           (int)(ray.Position.X / tileSize);
            int y = (!py) ? (int)Math.Ceiling(ray.Position.Z / tileSize) :
                           (int)(ray.Position.Z / tileSize);

            //find next tile boundary
            float nx = ((px) ? (x + 1) : (x - 1)) * tileSize;
            float ny = ((py) ? (y + 1) : (y - 1)) * tileSize;

            const float Epsilon = 1e-6f;
            float? min = null;
            if (Math.Abs(ray.Direction.X) > Epsilon)
            {
                min = (nx - ray.Position.X) / ray.Direction.X;
            }

            if (Math.Abs(ray.Direction.Z) > Epsilon)
            {
                var min2 = (ny - ray.Position.Z) / ray.Direction.Z;
                if (min == null || min.Value > min2) min = min2;
            }
            return min;
        }
        #endregion

        private void drawBorderSide(Vector2 xy, Vector2 xy2, Vector2 xy3, Vector2 xy4, SpriteBatch spriteBatch, float opacity)
        {
            double o = (17.0/144.0); //used for border segments
            double p = (1-o);

            double[] int1 = [xy.X * p + xy2.X * o, xy.Y * p + xy2.Y * o];
            double[] int2 = [xy4.X * p + xy3.X * o, xy4.Y * p + xy3.Y * o];
            double[] int3 = [xy.X * o + xy2.X * p, xy.Y * o + xy2.Y * p];
            double[] int4 = [xy4.X * o + xy3.X * p, xy4.Y * o + xy3.Y * p];

            DrawLine(Content.stpWhiteLine, new Vector2((float)(int1[0]), (float)(int1[1])), new Vector2((float)(int1[0] * p + int2[0] * o), (float)(int1[1] * p + int2[1] * o)), spriteBatch, 2, opacity);
            DrawLine(Content.stpWhiteLine, new Vector2((float)(int1[0] * p + int2[0] * o), (float)(int1[1] * p + int2[1] * o)), new Vector2((float)(int3[0] * p + int4[0] * o), (float)(int3[1] * p + int4[1] * o)), spriteBatch, 2,opacity);
            DrawLine(Content.stpWhiteLine, new Vector2((float)(int3[0] * p + int4[0] * o), (float)(int3[1] * p + int4[1] * o)), new Vector2((float)(int3[0]), (float)(int3[1])), spriteBatch, 2, opacity);
        }

        private void drawPartLine(Vector2 xy, Vector2 xy2, SpriteBatch spriteBatch, float opacity)
        {
            double o = (17.0/144.0); //used for border segments
            double p = (1-o);

            DrawLine(Content.stpWhiteLine, new Vector2((float)(xy.X * p + xy2.X * o), (float)(xy.Y * p + xy2.Y * o)), new Vector2((float)(xy2.X * p + xy.X * o), (float)(xy2.Y * p + xy.Y * o)), spriteBatch, 2, opacity);
        }

        private void drawTileCorner(Vector2 xy, Vector2 xy2, Vector2 xy3, SpriteBatch spriteBatch, float opacity)
        {
		    double o = (17.0/144.0); //used for border segments
		    double p = (1-o);
            DrawLine(Content.stpWhiteLine, new Vector2((float)(xy2.X * p + xy.X * o), (float)(xy2.Y * p + xy.Y * o)), new Vector2((float)(xy2.X), (float)(xy2.Y)), spriteBatch, 2, opacity);
            DrawLine(Content.stpWhiteLine, new Vector2((float)(xy2.X), (float)(xy2.Y)), new Vector2((float)(xy2.X * p + xy3.X * o), (float)(xy2.Y * p + xy3.Y * o)), spriteBatch, 2, opacity);
	    }

        private void DrawTileBorders(float iScale, SpriteBatch spriteBatch)
        {
            var tint = m_TintColorSprite.ToVector3();
            float baseOpacity = (tint.X + tint.Y + tint.Z) / 3f;

            if (m_SelTile[0] != -1)
            {
                for (int x = m_SelTile[0] - 3; x < m_SelTile[0] + 4; x++)
                {
                    if (x < 0 || x > 511) continue;
                    for (int y = m_SelTile[1] - 3; y < m_SelTile[1] + 4; y++)
                    {
                        if (y < 0 || y > 511) continue;

                        Vector2 mousedist = m_VecSelTile.Value - new Vector2(x+0.5f, y+0.5f);

                        var vxy = transformSpr3(new Vector3(x+0, MapData.ElevationData[(y * 512 + x)] / 12.0f, y + 0));
                        var vxy2 = transformSpr3(new Vector3(x + 1, MapData.ElevationData[(y * 512 + Math.Min(x + 1, 511))] / 12.0f, y + 0));
                        var vxy3 = transformSpr3(new Vector3(x + 1, MapData.ElevationData[(Math.Min(y + 1, 511) * 512 + Math.Min(x + 1, 511))] / 12.0f, y + 1));
                        var vxy4 = transformSpr3(new Vector3(x + 0, MapData.ElevationData[(Math.Min(y + 1, 511) * 512 + x)] / 12.0f, y + 1));

                        var minZ = Math.Min(vxy.Z, Math.Min(vxy2.Z, Math.Min(vxy3.Z, vxy4.Z)));

                        if (minZ < 0) continue;
                        //Vector2 mousedist = ((xy + xy2 + xy3 + xy4) / 4.0f - new Vector2(m_MouseState.X, m_MouseState.Y));
                        var xy = new Vector2(vxy.X, vxy.Y);
                        var xy2 = new Vector2(vxy2.X, vxy2.Y);
                        var xy3 = new Vector2(vxy3.X, vxy3.Y);
                        var xy4 = new Vector2(vxy4.X, vxy4.Y);

                        bool[] surTile = new bool[8];
                        for (int i=0; i<m_SurTileOffs.Length; i++) { //check 8 adjacent tiles to determine what combination of border lines to use. (road border draws between two buildable tiles)
                            surTile[i] = (IsLandBuildable(x + m_SurTileOffs[i][0], y + m_SurTileOffs[i][1]));
                        }

                        float opacity = baseOpacity * (float)(1.0 - (mousedist.Length() / 3.0));

                        if (IsLandBuildable(x, y))
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
                            DrawLine(Content.stpWhiteLine, xy, xy2, spriteBatch, 2, opacity);
                            DrawLine(Content.stpWhiteLine, xy2, xy3, spriteBatch, 2, opacity);
                        }

                        double o = (17.0/144.0); //used for border segments
                        double p = (1-o);

                        if (x == m_SelTile[0] && y == m_SelTile[1] && Plugin == null)
                        {
                            DrawLine(Content.WhiteLine, xy, xy2, spriteBatch, 2, baseOpacity);
                            DrawLine(Content.WhiteLine, xy2, xy3, spriteBatch, 2, baseOpacity);
                            DrawLine(Content.WhiteLine, xy3, xy4, spriteBatch, 2, baseOpacity);
                            DrawLine(Content.WhiteLine, xy4, xy, spriteBatch, 2, baseOpacity);
                        }
                    }
                }
            }
        }

        private bool IsLandBuildable(int x, int y) 
        {
            return FindController<TerrainController>().IsPurchasable(x, y);
        }

        private void DrawSpotlights(float HB)
        {
            float iScale = (float)m_ScrWidth/(HB*2.0f);
		
            float spotlightScale = (float)(iScale*(2.0*Math.Sqrt(0.5*0.5*2)/5.10));

            int i = 0;
            foreach (var entry in LotTileData)
            {
                if ((entry.flags & LotTileFlags.Spotlight) > 0)
                {
                    Vector2 pos = new Vector2(entry.x, entry.y);
                    Vector4 xy = transformSpr4(new Vector3(pos.X + 0.5f, MapData.ElevationData[((int)pos.Y * 512 + (int)pos.X)] / 12.0f, pos.Y + 0.5f)); //get position to place spotlight
                    Vector3 xyz = new Vector3(xy.X, xy.Y, 1);

                    if (xy.Z < 0) continue;

                    if (Camera is CityCamera3D) spotlightScale = 240 / xy.W;

                    Matrix trans = Matrix.Identity;
                    trans = Matrix.CreateRotationZ((float)(0.33 * Math.Sin(2.0 * Math.PI * ((m_SpotOsc + i * 0.43) % 1)))); //makes spotlight sway back and forth!

                    m_2DVerts.Add(new VertexPositionColor(xyz, new Color(1, 1, 1, 0.5f))); //bottom point of spotlight, set to 0.5 opacity
                    m_2DVerts.Add(new VertexPositionColor((xyz + (Vector3.Transform(new Vector3(-12, -100, 0), trans) * spotlightScale)), new Color(1, 1, 1, 0.0f))); //top two vertices set to 0 opacity, creates gradient for spotlight effect.
                    m_2DVerts.Add(new VertexPositionColor((xyz + (Vector3.Transform(new Vector3(12, -100, 0), trans) * spotlightScale)), new Color(1, 1, 1, 0.0f)));
                }
                i++;
            }
        }

        public Vector2 Get2DFromTile(float x, float y)
        {
            float iScale = (float)(1 / (m_LastIsoScale * 2));
            if (x < 0 || y < 0 || x >= 512 || y >= 512) return new Vector2();

            var transform = transformSpr3(new Vector3(x, InterpElevationAt(new Vector2(x, y)), y));
            return (transform.Z > 0) ? new Vector2(transform.X, transform.Y) : new Vector2(float.MaxValue, 0);
        }

        public Vector2 Get2DFromTile(int x, int y)
        {
            float iScale = (float)(1/(m_LastIsoScale * 2));
            if (x < 0 || y < 0 || x >= 512 || y >= 512) return new Vector2();

            var transform = transformSpr3(new Vector3(x, MapData.ElevationData[(y * 512 + x)] / 12.0f, y));
            return (transform.Z > 0)?new Vector2(transform.X, transform.Y):new Vector2(float.MaxValue, 0);
        }

        public Vector2 GetFar2DFromTile(int x, int y)
        {
            return Get2DFromTile(x, y);
        }

        private void Draw3DHouses(int passIndex)
        {
            if (LotTileDataDirty)
            {
                var onindices = new List<int>();
                var onverts = new List<DGRP3DVert>();
                var offindices = new List<int>();
                var offverts = new List<DGRP3DVert>();
                var vCount = 0;

                foreach (var lot in LotTileData)
                {
                    bool online = ((lot.flags & LotTileFlags.Online) > 0);
                    var indices = (online) ? onindices : offindices;
                    var verts = (online) ? onverts : offverts;
                    vCount = verts.Count;
                    indices.Add(vCount);
                    indices.Add(vCount + 1);
                    indices.Add(vCount + 2);
                    indices.Add(vCount);
                    indices.Add(vCount + 2);
                    indices.Add(vCount + 3);

                    short x = lot.x;
                    short y = lot.y;

                    if (!MapData.IsInBounds(x, y)) continue;

                    var pos = new Vector3(x + 0.5f, MapData.ElevationData[(y * 512 + x)] / 12.0f, y + 0.5f);
                    verts.Add(new DGRP3DVert(pos, Vector3.Up, new Vector2()));
                    verts.Add(new DGRP3DVert(pos, Vector3.Up, new Vector2(1, 0)));
                    verts.Add(new DGRP3DVert(pos, Vector3.Up, new Vector2(1, 1)));
                    verts.Add(new DGRP3DVert(pos, Vector3.Up, new Vector2(0, 1)));
                }

                LotOfflineVerts?.Dispose();
                LotOfflineInds?.Dispose();
                LotOnlineVerts?.Dispose();
                LotOnlineInds?.Dispose();

                if (offverts.Count > 0)
                {
                    LotOfflineVerts = new VertexBuffer(m_GraphicsDevice, typeof(DGRP3DVert), offverts.Count, BufferUsage.None);
                    LotOfflineInds = new IndexBuffer(m_GraphicsDevice, IndexElementSize.ThirtyTwoBits, offindices.Count, BufferUsage.None);
                    LotOfflineVerts.SetData(offverts.ToArray());
                    LotOfflineInds.SetData(offindices.ToArray());
                } else
                {
                    LotOfflineVerts = null;
                    LotOfflineInds = null;
                }

                if (onverts.Count > 0)
                {
                    LotOnlineVerts = new VertexBuffer(m_GraphicsDevice, typeof(DGRP3DVert), onverts.Count, BufferUsage.None);
                    LotOnlineInds = new IndexBuffer(m_GraphicsDevice, IndexElementSize.ThirtyTwoBits, onindices.Count, BufferUsage.None);
                    LotOnlineVerts.SetData(onverts.ToArray());
                    LotOnlineInds.SetData(onindices.ToArray());
                }
                else
                {
                    LotOnlineVerts = null;
                    LotOnlineInds = null;
                }

                LotTileDataDirty = false;
            }

            double onlineAlpha = (0.5 + Math.Sin(4 * Math.PI * (m_SpotOsc % 1)) / 2.0); //if house is online, flash the opacity using the oscillator variable.

            VertexShader.Parameters["SpriteSize"].SetValue(new Vector2(8f / m_ScrWidth, -8f / m_ScrHeight));
            VertexShader.CurrentTechnique = VertexShader.Techniques[3];
            PixelShader.CurrentTechnique = PixelShader.Techniques[1];

            VertexShader.Parameters["DepthBias"].SetValue(-0.4f*Camera.DepthBiasScale);
            VertexShader.Parameters["ObjModel"].SetValue(Matrix.Identity);
            VertexShader.CurrentTechnique.Passes[passIndex].Apply();

            m_GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            if (LotOfflineInds != null)
            {
                PixelShader.Parameters["ObjTex"].SetValue(Content.LotOffline);
                PixelShader.Parameters["LightCol"].SetValue(Vector4.One);
                PixelShader.CurrentTechnique.Passes[passIndex].Apply();
                m_GraphicsDevice.Indices = LotOfflineInds;
                m_GraphicsDevice.SetVertexBuffer(LotOfflineVerts);
                m_GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, LotOfflineInds.IndexCount / 3);
            }

            if (LotOnlineInds != null)
            {
                PixelShader.Parameters["ObjTex"].SetValue(Content.LotOnline);
                PixelShader.Parameters["LightCol"].SetValue(Vector4.One * (float)onlineAlpha);
                PixelShader.CurrentTechnique.Passes[passIndex].Apply();
                m_GraphicsDevice.Indices = LotOnlineInds;
                m_GraphicsDevice.SetVertexBuffer(LotOnlineVerts);
                m_GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, LotOnlineInds.IndexCount / 3);
            }
            PixelShader.Parameters["LightCol"].SetValue(new Vector4(m_TintColor.R / 255.0f, m_TintColor.G / 255.0f, m_TintColor.B / 255.0f, 1) * 1.25f);
        }

        internal void PathTile(int x, int y, float iScale, Color color) { //quick and dirty function to fill a tile with white using the 2DVerts system. Used in near view for online houses.
            if (x < 0 || y < 0 || x >= 512 || y >= 512)
            {
                return;
            }

            Vector4 vxy = transformSpr4(new Vector3(x + 0, MapData.ElevationData[(y * 512 + x)] / 12.0f, y + 0));
            Vector4 vxy2 = transformSpr4(new Vector3(x + 1, MapData.ElevationData[(y * 512 + Math.Min(x + 1, 511))] / 12.0f, y + 0));
            Vector4 vxy3 = transformSpr4(new Vector3(x + 1, MapData.ElevationData[(Math.Min(y + 1, 511) * 512 + Math.Min(x + 1, 511))] / 12.0f, y + 1));
            Vector4 vxy4 = transformSpr4(new Vector3(x + 0, MapData.ElevationData[(Math.Min(y + 1, 511) * 512 + x)] / 12.0f, y + 1));

            if (Camera is CityCamera2D)
            {
                vxy.Z = 1;
                vxy2.Z = 1;
                vxy3.Z = 1;
                vxy4.Z = 1;
            }

            var zOff = -0.12f;
            var xy = new Vector3(vxy.X, vxy.Y, (vxy.Z+zOff/vxy.Z)/ vxy.W);
            var xy2 = new Vector3(vxy2.X, vxy2.Y, (vxy2.Z + zOff / vxy2.Z) / vxy2.W);
            var xy3 = new Vector3(vxy3.X, vxy3.Y, (vxy3.Z + zOff / vxy3.Z) / vxy3.W);
            var xy4 = new Vector3(vxy4.X, vxy4.Y, (vxy4.Z + zOff / vxy4.Z) / vxy4.W);

            var minZ = Math.Min(xy.Z, Math.Min(xy2.Z, Math.Min(xy3.Z, xy4.Z)));
            var maxZ = Math.Max(xy.Z, Math.Max(xy2.Z, Math.Max(xy3.Z, xy4.Z)));
            if (minZ > 0 && maxZ <= 1)
            {
                m_2DVerts.Add(new VertexPositionColor(xy, color));
                m_2DVerts.Add(new VertexPositionColor(xy2, color));
                m_2DVerts.Add(new VertexPositionColor(xy3, color));

                m_2DVerts.Add(new VertexPositionColor(xy, color));
                m_2DVerts.Add(new VertexPositionColor(xy3, color));
                m_2DVerts.Add(new VertexPositionColor(xy4, color));
            }
	    }

        private void DrawSprites(float HB, float VB)
        {
            var spriteBatch = m_Batch;
            spriteBatch.Begin(sortMode: SpriteSortMode.Texture);

            if (Camera.Zoomed == TerrainZoomMode.Far && HandleMouse)
            {
                //draw rectangle to indicate zoom position
                DrawLine(Content.WhiteLine, new Vector2(m_MouseState.X - 15, m_MouseState.Y - 11), new Vector2(m_MouseState.X - 15, m_MouseState.Y + 11), spriteBatch, 2, 1);
                DrawLine(Content.WhiteLine, new Vector2(m_MouseState.X - 16, m_MouseState.Y + 10), new Vector2(m_MouseState.X + 16, m_MouseState.Y + 10), spriteBatch, 2, 1);
                DrawLine(Content.WhiteLine, new Vector2(m_MouseState.X + 15, m_MouseState.Y + 11), new Vector2(m_MouseState.X + 15, m_MouseState.Y - 11), spriteBatch, 2, 1);
                DrawLine(Content.WhiteLine, new Vector2(m_MouseState.X + 16, m_MouseState.Y - 10), new Vector2(m_MouseState.X - 16, m_MouseState.Y - 10), spriteBatch, 2, 1);
            }
            
            if (m_ZoomProgress < 0.5)
            {
                spriteBatch.End();
                return;
            }

            float iScale = (float)m_ScrWidth / (HB * 2);

		    float treeWidth = (float)(Math.Sqrt(2)*(128.0/144.0));
		    float treeHeight = treeWidth*(80/128);

		    Vector2 mid = Camera.CalculateR(); //determine approximate tile position at center of screen
            var isoScale = GetIsoScale();
            var range = Math.Min(50, 10 + (int)(isoScale * 2500));

            mid.X -= 6;
		    mid.Y += 6;
            float[] bounds = new float[] { (float)Math.Round(mid.X - range), (float)Math.Round(mid.Y - range), (float)Math.Round(mid.X + range), (float)Math.Round(mid.Y + range) };


    		
		    Texture2D img = Content.Forest;
		    float fade = Math.Max(0, Math.Min(1, (m_ZoomProgress - 0.4f) * 2));

            var scrollVel = 0;// (new Vector2(m_TargVOffX, m_TargVOffY) - LastTargOff).Length();

            DrawTileBorders(iScale, spriteBatch);

            for (short y = (short)bounds[1]; y < bounds[3]; y++) //iterate over tiles close to the approximate tile position at the center of the screen and draw any trees/houses on them
            {
                if (y < 0 || y > 511) continue;
                for(short x = (short)bounds[0]; x < bounds[2]; x++)
                {
                    if (x < 0 || x > 511) continue;

                    float elev = GetElevationAt(x, y);

                    var xy = transformSpr(iScale, new Vector3((float)(x + 0.5), elev / 12.0f, (float)(y + 0.5)));

                    if (xy.X > -64 && xy.X < m_ScrWidth + 64 && xy.Y > -40 && xy.Y < m_ScrHeight + 40) //is inside screen
                    {

                        Vector2 loc = new Vector2( x, y );
                        LotTileEntry house;

                        if (LotTileLookup.ContainsKey(loc))
                        {
                            house = LotTileLookup[loc];
                        }
                        else
                        {
                            house = null;
                        }
                        if (house != null) //if there is a house here, draw it
                        {
                            if ((house.flags & LotTileFlags.Online) > 0) {
							    PathTile(x, y, iScale, new Color(1.0f, 1.0f, 1.0f, (float)(0.3+Math.Sin(4*Math.PI*(m_SpotOsc%1))*0.15)));
						    }

                            Texture2D lotImg = null;
                            if (scrollVel > 0.2f) lotImg = Content.DefaultHouse;
                            else
                            {
                                var controller = FindController<TerrainController>();
                                lotImg = controller.RequestLotThumb((uint)house.packed_pos);
                            }

                            var resMultiplier = (lotImg.Width > 144) ? 2 : 1;
                            var lotImgWidth = lotImg.Width / resMultiplier;
                            var lotImgHeight = lotImg.Height / resMultiplier;

                            double scale = Math.Round((treeWidth * iScale / 128.0)*1000)/1000;

                            spriteBatch.Draw(lotImg, new Rectangle((int)(xy.X - (lotImgWidth/2) * scale), (int)(xy.Y - (lotImgHeight/2) * scale), (int)(scale * lotImgWidth), (int)(scale * lotImgHeight)), m_TintColorSprite);
                        }
                        else //if there is no house, draw the forest that's meant to be here.
                        {
                            double fType = (int)MapData.ForestTypeData[(y * 512 + x)];
                            double fDens = Math.Round((double)(MapData.ForestDensityData[(y * 512 + x)] * 4 / 255));
                            if (!(fType == -1 || fDens == 0))
                            {
                                double scale = treeWidth * iScale / 128.0;
                                spriteBatch.Draw(
                                    Content.Forest,
                                    new Rectangle((int)(xy.X - 64.0 * scale), (int)(xy.Y - 56.0 * scale), (int)(scale * 128), (int)(scale * 80)),
                                    new Rectangle((int)(128 * (fDens - 1)),
                                    (int)(80 * fType), 128, 80),
                                    m_TintColorSprite);
                                //draw correct forest from forest atlas
                            }
                        }
                    }
                }
            }

            Draw2DPoly(false); //fill the tiles below online houses BEFORE actually drawing the houses and trees!
            spriteBatch.End();
        }

        public Vector2 transformSpr(float iScale, Vector3 pos) 
        { //transform 3d position to view.
            Vector4 temp = Vector4.Transform(pos, m_MovMatrix);
            temp.X /= temp.W;
            temp.Y /= -temp.W;
            int width = m_ScrWidth;
            int height = m_ScrHeight;
            return new Vector2(width * 0.5f * (temp.X + 1f), height * 0.5f * (temp.Y + 1f));
        }

        public Vector3 transformSpr3(Vector3 pos)
        { //transform 3d position to view.
            Vector4 temp = Vector4.Transform(pos, m_MovMatrix);
            temp.X /= temp.W;
            temp.Y /= -temp.W;
            if (temp.Z < 0) return new Vector3(0, 0, -1);
            temp.Z /= temp.W;
            int width = m_ScrWidth;
            int height = m_ScrHeight;
            return new Vector3(width * 0.5f * (temp.X + 1f), height * 0.5f * (temp.Y + 1f), temp.Z);
        }

        public Vector4 transformSpr4(Vector3 pos)
        { //transform 3d position to view.
            Vector4 temp = Vector4.Transform(pos, m_MovMatrix);
            temp.X /= temp.W;
            temp.Y /= -temp.W;
            int width = m_ScrWidth;
            int height = m_ScrHeight;
            return new Vector4(width * 0.5f * (temp.X + 1f), height * 0.5f * (temp.Y + 1f), temp.Z, temp.W);
        }

        public Vector2 transformSprFar(float iScale, Vector3 pos)
        { //transform 3d position to view.
            Vector3 temp = Vector3.Transform(pos, m_MovMatrix);
            int width = m_ScrWidth;
            int height = m_ScrHeight;
            return new Vector2((temp.X) * iScale + width / 2, (-(temp.Y) * iScale) + height / 2);
        }

        public void UIMouseEvent(UIMouseEventType type, UpdateState state)
        {
            Camera.MouseEvent(type, state);
            if (type == UIMouseEventType.MouseOver) HandleMouse = true;
            if (type == UIMouseEventType.MouseOut)
            {
                HandleMouse = false;
            }
        }

        public void Click(Point pt, UpdateState state)
        {
            var currentTile = GetHoverSquare(null);
            var curTileInt = (currentTile == null) ? new int[] { -1, -1 } : new int[] { (int)currentTile.Value.X, (int)currentTile.Value.Y };
            m_SelTile = curTileInt;
            m_VecSelTile = currentTile;

            var farClickMode = NeighGeom.HoverNHood > -1 || Camera.Zoomed == TerrainZoomMode.Far;

            if (farClickMode && Plugin == null)
            {
                FindController<TerrainController>().ZoomIn();
                if (NeighGeom.HoverNHood > -1 && Camera.CenterCam == null)
                {
                    UIScreen.Current.FindController<CoreGameScreenController>()?.ShowNeighPage((uint)NeighGeom.ToDBID(NeighGeom.HoverNHood));
                    NeighGeom.CenterNHood(NeighGeom.ToDBID(NeighGeom.HoverNHood));
                }
                else
                {
                    Camera.Zoomed = TerrainZoomMode.Near;
                    Camera.ClearCenter();
                    double ResScale = 768.0 / m_ScrHeight;
                    double isoScale = (Math.Sqrt(0.5 * 0.5 * 2) / 5.10) * ResScale;
                    double hb = m_ScrWidth * isoScale;
                    double vb = m_ScrHeight * isoScale;

                    if (Camera is CityCamera2D)
                    {
                        if (currentTile != null)
                        {
                            var c2d = currentTile.Value;
                            var c3d = new Vector3(c2d.X, InterpElevationAt(c2d), c2d.Y);
                            var pos = Vector3.Transform(c3d, Camera.View);
                            ((CityCamera2D)Camera).m_TargVOffX = pos.X;
                            ((CityCamera2D)Camera).m_TargVOffY = pos.Y;

                        }

                        //((CityCamera2D)Camera).m_TargVOffX = (float)(-hb + pt.X * isoScale * 2);
                        //((CityCamera2D)Camera).m_TargVOffY = (float)(vb - pt.Y * isoScale * 2); //zoom into approximate location of mouse cursor if not zoomed already
                    }
                    else
                    {
                        var camera = (CityCamera3D)Camera;
                        if (currentTile != null)
                        {
                            camera.CenterDelta = currentTile.Value - camera.CenterTile;
                            GameFacade.Screens.Tween.To(camera, 0.3f, new Dictionary<string, float>() {
                                { "TweenDeltaX", 0f },
                                { "TweenDeltaY", 0f },
                            }, TweenQuad.EaseOut);
                        }
                        ((CityCamera3D)Camera).TargetZoom = 1.7f;
                    }
                }
            }
            else
            {
                Plugin?.TileMouseUp(m_VecSelTile);
                if (Plugin == null)
                {
                    if (m_SelTile[0] != -1 && m_SelTile[1] != -1)
                    {
                        FindController<TerrainController>().ClickLot(m_SelTile[0], m_SelTile[1], state.ShiftDown);
                    }
                }
            }

            ((CoreGameScreen)GameFacade.Screens.CurrentUIScreen).ucp.UpdateZoomButton();
        }

        private int ITime;
        public override void Update(UpdateState state)
        {
            ITime++;

            float updateRate = 1f / FSOEnvironment.RefreshRate;
            for (int i = 0; i < Modifications.Count; i++)
            {
                var mod = Modifications[i];
                mod.Timer += updateRate;
                if (mod.Timer > CityModification.EdgeDuration)
                {
                    Modifications.RemoveAt(i--);
                }
            }

            if (!(GameFacade.Screens.CurrentUIScreen is CoreGameScreen)) return;
            CoreGameScreen CurrentUIScr = (CoreGameScreen)GameFacade.Screens.CurrentUIScreen;

            if (Visible)
            { //if we're not visible, do not update CityRenderer state...
                if (VertexColorDirty || Content.VertexColor == null)
                {
                    VertexColorGenerator.Update(GameFacade.GraphicsDevice);
                    Content.VertexColor = VertexColorGenerator.GetVertexColor();
                    VertexColorDirty = false;
                }

                Weather.TintColor = m_TintColor.ToVector4();
                Weather.Update();

                //move the weather camera
                var scale = Camera.GetIsoScale();
                if (Camera is CityCamera2D)
                {
                    var c2d = (CityCamera2D)Camera;
                    if (ParticleCamera is CityCamera3D) ParticleCamera = new BasicCamera(m_GraphicsDevice, Vector3.Zero, new Vector3(0, 0.5f, 0.86602540f), Vector3.Up);
                    ParticleCamera.Position = new Vector3(0, 0.5f, 0.86602540f) * scale * 10000 + new Vector3(c2d.m_ViewOffX * 4 + 2000, 0, (c2d.m_ViewOffY * -5 + 2000));
                    ParticleCamera.Target = ParticleCamera.Position - new Vector3(0, 0.5f, 0.86602540f);
                    ParticleCamera.ProjectionDirty();
                } else
                {
                    ParticleCamera = (BasicCamera)Camera;
                }

                var parti = new List<ParticleComponent>(Particles);
                foreach (var particle in parti)
                {
                    particle.Update(m_GraphicsDevice, null);
                }
                if (DateTime.Now.Subtract(LastCityUpdate).TotalSeconds > 15)
                {
                    FindController<TerrainController>()?.RequestNewCity();
                    LastCityUpdate = DateTime.Now;
                }

                m_LastMouseState = m_MouseState;
                m_MouseState = Mouse.GetState();

                if (HandleMouse && state.ProcessMouseEvents)
                {
                    if (Camera.Zoomed == TerrainZoomMode.Near)
                    {
                        var currentTile = GetHoverSquare(null);
                        var curTileInt = (currentTile == null) ? new int[] { -1, -1 } : new int[] { (int)currentTile.Value.X, (int)currentTile.Value.Y};

                        if (Plugin == null)
                        {
                            if (m_SelTile == null || m_SelTile[0] != curTileInt[0] || m_SelTile[1] != curTileInt[1])
                            {
                                FindController<TerrainController>().HoverTile(curTileInt[0], curTileInt[1]);
                            }
                        }

                        m_SelTile = curTileInt;
                        m_VecSelTile = currentTile;
                        Plugin?.TileHover(currentTile);
                    }

                    if (m_MouseState.RightButton == ButtonState.Pressed && m_LastMouseState.RightButton == ButtonState.Released)
                    {
                    }
                    else if (m_MouseState.LeftButton == ButtonState.Released && m_LastMouseState.LeftButton == ButtonState.Pressed && !(Camera is ITouchable)) //if clicked...
                    {
                        Click(m_MouseState.Position, state);
                    }

                    if (m_VecSelTile != null && m_MouseState.LeftButton == ButtonState.Pressed && m_LastMouseState.LeftButton == ButtonState.Released) //if mousedown...
                        Plugin?.TileMouseDown(m_VecSelTile.Value);
                }
                else
                {
                    m_SelTile = new int[] { -1, -1 };
                    m_VecSelTile = null;
                }

                FixedTimeUpdate(state);

                Camera.Update(state, this);

                Plugin?.Update(state);

                NeighGeom.Update(state);
            }
        }

        private Color SRGBSpriteMul(Color linearMul)
        {
            var linearVec = linearMul.ToVector4();

            linearVec.X = (float)Math.Pow(linearVec.X, 1 / 2.2f);
            linearVec.Y = (float)Math.Pow(linearVec.Y, 1 / 2.2f);
            linearVec.Z = (float)Math.Pow(linearVec.Z, 1 / 2.2f);

            return new Color(linearVec);
        }

        private float Time;
        public void SetTimeOfDay(double time) 
        {
            time = Math.Min(0.999999999, time);
            m_TintColor = TimeOfDayConfig.ColorFromTime(time);

            Time = (float)time;
            time = FinaleUtils.BiasSunTime(time);

            if (Weather.Darken > 0)
            {
                //tint the outside colour, usually with some darkening effect.
                m_TintColor = new Color(
                        m_TintColor.ToVector4() *
                        Weather.OutsideWeatherTint.ToVector4()
                        );
            }

            m_TintColorSprite = SRGBSpriteMul(m_TintColor);

            m_LightPosition = new Vector3(0, 0, -263);
            Matrix Transform = Matrix.Identity;

            double modTime;
            var offStart = 1 - (DayOffset + DayDuration);
            if (time < DayOffset)
            {
                modTime = (offStart + time) * 0.5 / (1 - DayDuration);
            } else if (time > DayOffset+DayDuration)
            {
                modTime = (time - (1-offStart)) * 0.5 / (1 - DayDuration);
            } else
            {
                modTime = (time - DayOffset) * 0.5 / DayDuration;
            }

            modTime = FinaleUtils.BiasSunTime(modTime);

            Transform *= Matrix.CreateRotationY((float)((modTime+0.5) * Math.PI * 2.0)); //Controls the rotation of the sun/moon around the city. 
            Transform *= Matrix.CreateRotationZ((float)(Math.PI*(45.0/180.0))); //Sun is at an angle of 45 degrees to horizon at it's peak. idk why, it's winter maybe? looks nice either way
            Transform *= Matrix.CreateRotationY((float)(Math.PI * 0.3)); //Offset from front-back a little. This might need some adjusting for the nicest sunset/sunrise locations.
            Transform *= Matrix.CreateTranslation(new Vector3(256, 0, 256)); //Move pivot center to center of mesh.

            m_LightPosition = Vector3.Transform(m_LightPosition, Transform);

            if (modTime > 0.25) modTime = 0.5 - modTime;

            if (Math.Abs(modTime) < 0.05) //Near the horizon, shadows should gracefully fade out into the opposite shadows (moonlight/sunlight)
            {
                m_ShadowMult = (float)(1-(Math.Abs(modTime)*20))*0.75f+0.25f;
            }
            else
            {
                m_ShadowMult = 0.25f; //Shadow strength. Remember to change the above if you alter this.
            }
        }

        public Vector3 LotPosition;
        private World LastWorld;

        public void InheritPosition(World lotWorld, CoreGameScreenController controller, bool instant)
        {
            LastWorld = lotWorld;
            Camera.InheritPosition(this, lotWorld, controller, instant);
        }

        public float GetElevationVert(int x, int y)
        {
            x = ((x % 512) + 512)%512;
            y = ((y % 512) + 512)%512;
            return MapData.ElevationData[(y * 512 + x)] / 12.0f;
        }

        public float GetElevationAt(int x, int y)
        {
            return(MapData.ElevationData[(y * 512 + x)] + MapData.ElevationData[(y * 512 + Math.Min(x + 1, 511))] +
                        MapData.ElevationData[(Math.Min(y + 1, 511) * 512 + Math.Min(x + 1, 511))] +
                        MapData.ElevationData[(Math.Min(y + 1, 511) * 512 + x)]) / 4f; //elevation of sprite is the average elevation of the 4 vertices of the tile
        }

        public float InterpElevationAt(Vector2 Position)
        {
            var baseX = (int)Math.Max(0, Math.Min(MapData.Width - 1, Position.X));
            var baseY = (int)Math.Max(0, Math.Min(MapData.Height - 1, Position.Y));
            if (baseX < 0 || baseY < 0) return 0;
            var nextX = (int)Math.Max(0, Math.Min(MapData.Width - 1, Math.Ceiling(Position.X)));
            var nextY = (int)Math.Max(0, Math.Min(MapData.Height - 1, Math.Ceiling(Position.Y)));
            var xLerp = Position.X % 1f;
            var yLerp = Position.Y % 1f;
            float h00 = MapData.ElevationData[((baseY % MapData.Height) * MapData.Width + (baseX % MapData.Width))] / 12f;
            float h01 = MapData.ElevationData[((nextY % MapData.Height) * MapData.Width + (baseX % MapData.Width))] / 12f;
            float h10 = MapData.ElevationData[((baseY % MapData.Height) * MapData.Width + (nextX % MapData.Width))] / 12f;
            float h11 = MapData.ElevationData[((nextY % MapData.Height) * MapData.Width + (nextX % MapData.Width))] / 12f;

            float xl1 = xLerp * h10 + (1 - xLerp) * h00;
            float xl2 = xLerp * h11 + (1 - xLerp) * h01;

            return yLerp * xl2 + (1 - yLerp) * xl1;
        }

        private float GetMinElevationAt(int x, int y)
        {
            if (x == -1 || y == -1) return 0;
            return Math.Min(Math.Min(Math.Min(MapData.ElevationData[(y * 512 + x)], MapData.ElevationData[(y * 512 + Math.Min(x + 1, 511))]),
                        MapData.ElevationData[(Math.Min(y + 1, 511) * 512 + Math.Min(x + 1, 511))]),
                        MapData.ElevationData[(Math.Min(y + 1, 511) * 512 + x)]); //elevation of sprite is the average elevation of the 4 vertices of the tile
        }

        private void FixedTimeUpdate(UpdateState state)
        {
            var rScale = 60f / FSOEnvironment.RefreshRate;
            m_SpotOsc = (m_SpotOsc + 0.01f*rScale) % 1; //spotlight oscillation. Cycles fully every 100 frames.
        }

        private Texture2D DrawDepth()
        {
            if (ShadowTarget == null || OldShadowRes != ShadowRes) {
                if (ShadowTarget != null) ShadowTarget.Dispose();
                ShadowTarget = new RenderTarget2D(m_GraphicsDevice, ShadowRes, ShadowRes, false, SurfaceFormat.Single, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
                OldShadowRes = ShadowRes;
            }
            RenderTarget2D RTarget = ShadowTarget;
            m_GraphicsDevice.SetRenderTarget(RTarget);

            m_GraphicsDevice.Clear(Color.CornflowerBlue);

            Geometry.DrawAll(m_GraphicsDevice, Content, VertexShader, PixelShader, 1, 1);

            m_GraphicsDevice.SetRenderTarget(null);

            return RTarget;
        }

        public void Draw2DPoly(bool depth)
        {
            if (m_2DVerts.Count == 0) return;
            m_GraphicsDevice.DepthStencilState = depth?DepthStencilState.Default:DepthStencilState.None;

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

            if (m_GraphicsDevice.Indices != null) m_GraphicsDevice.Indices = null;
            m_GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, Vert2D, 0, Vert2D.Length/3); //draw 2d coloured triangle array (for spotlights etc)

            m_GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            m_2DVerts.Clear();
        }

        public float GetSpriteScale()
        {
            if (Camera is CityCamera3D)
            {
                var height = m_GraphicsDevice.Viewport.Height;
                return height / 800f; 
            }
            else
            {
                // Scale based off of iso scale. 
                return (1/1600f) / GetIsoScale();
            }
        }

        public float GetIsoScale()
        {
            return Camera.GetIsoScale();
        }

        public float GetFarzoomIsoScale()
        {
            float ResScale = 768.0f / m_ScrHeight; //scales up the vertical height to match that of the target resolution (for the far view)
            float FisoScale = (float)(Math.Sqrt(0.5 * 0.5 * 2) / 5.10f) * ResScale; // is 5.10 on far zoom
            return FisoScale;
        }

        private BoundingFrustum PrepareTerrainShader(Matrix view, Matrix projection, float darken)
        {
            VertexShader.CurrentTechnique = VertexShader.Techniques[2];
            var mv = view;
            var mvp = (mv) * projection;
            VertexShader.Parameters["BaseMatrix"].SetValue(mvp);
            VertexShader.Parameters["MV"].SetValue(mv);

            PixelShader.CurrentTechnique = PixelShader.Techniques[2];
            PixelShader.Parameters["LightCol"].SetValue(new Vector4(m_TintColor.R / 255.0f, m_TintColor.G / 255.0f, m_TintColor.B / 255.0f, 1) * 1.25f);
            var lightVec = Vector3.Normalize(m_LightPosition - new Vector3(256, 0, 256));
            PixelShader.Parameters["LightVec"].SetValue(lightVec);
            PixelShader.Parameters["Time"].SetValue(ITime / (float)FSOEnvironment.RefreshRate);

            var invView = Matrix.Invert(mv);
            if (Camera is CityCamera3D) invView.Translation = Vector3.Zero;
            else invView = new Matrix(new Vector4(0.7071068f, 0f, 0.7071068f, 0),
                new Vector4(0.3535534f, 0.8660254f, -0.3535534f, 0),
                new Vector4(-0.6123725f, 0.5f, 0.6123725f, 0),
                new Vector4(0, 0, 0, 1));
            PixelShader.Parameters["InvView"].SetValue(invView);
            var dist = 0.3f + lightVec.Y;
            dist *= dist;
            PixelShader.Parameters["SunStrength"].SetValue(((1 - 0.6f * darken) / dist) * (1.0f - m_ShadowMult) * 2);

            PixelShader.Parameters["BigWTex"].SetValue(Content.BigWNormal);
            PixelShader.Parameters["SmallWTex"].SetValue(Content.SmallWNormal);

            PixelShader.Parameters["WavePow"].SetValue(5 / 2f);
            PixelShader.Parameters["RealNormalPct"].SetValue(2f);
            PixelShader.Parameters["ShadowMult"].SetValue(m_ShadowMult);

            return new BoundingFrustum(mvp);
        }

        private Matrix m_LightMatrix;
        public override void Draw(GraphicsDevice gfx)
        {
            bool is2D = Camera is CityCamera2D;
            if (m_LotZoomProgress > 0 && !is2D)
            {
                return;
            }

            m_GraphicsDevice = gfx;

            ShadowRes = GlobalSettings.Default.ShadowQuality;
            ShadowsEnabled = GlobalSettings.Default.CityShadows;

            //if (RegenData) GenerateAssets();

            m_GraphicsDevice.RasterizerState = RasterizerState.CullNone; //don't cull
            m_GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            m_ScrHeight = m_GraphicsDevice.Viewport.Height;
            m_ScrWidth = m_GraphicsDevice.Viewport.Width;

            if (RegenData) GenerateAssets(); //if assets are flagged as requiring regeneration, regenerate them!

            float IsoScale = GetIsoScale();
            m_LastIsoScale = IsoScale;

            float HB = m_ScrWidth * IsoScale;
            float VB = m_ScrHeight * IsoScale;
            
            if ((Camera is CityCamera3D && m_Zoomed == TerrainZoomMode.Lot) || (Camera is CityCamera2D && m_LotZoomProgress == 1f)) return;

            Matrix ProjectionMatrix = Camera.Projection;
            Matrix ViewMatrix = Camera.View;

            var frustum = PrepareTerrainShader(ViewMatrix, ProjectionMatrix, Weather.Darken);
            var fog = true; //(Camera is CityCamera3D) || Weather.WeatherIntensity > 0.01f;
            if (fog)
            {
                var fogColor = Weather.FogColor;
                var weatherMult = (Camera is CityCamera3D) ? (1 + Weather.Darken) : 1;
                PixelShader.Parameters["FogMaxDist"].SetValue(fogColor.W*Camera.FogMultiplier * weatherMult);
                fogColor.W = 1f;
                PixelShader.Parameters["FogColor"].SetValue(fogColor);
            }

            if (ShadowsEnabled)
            {
                if (--ShadowRegenTimer < 0 || (m_ZoomProgress > 0.1f && m_ZoomProgress < 0.9f))
                {
                    RecalculateShadows();

                    ShadowRegenTimer = 60;
                }

                Texture2D shadowMap = ShadowTarget;
                if (shadowMap != null)
                {
                    PixelShader.Parameters["ShadowMap"].SetValue(shadowMap);
                    PixelShader.Parameters["ShadSize"].SetValue(new Vector2(shadowMap.Width, shadowMap.Height));
                }
            }

            m_GraphicsDevice.Clear(m_TintColor);
            VertexShader.Parameters["LightMatrix"].SetValue(m_LightMatrix);

            var dir = (m_LightPosition - new Vector3(256, 0, 256)) * new Vector3(1, 1.5f, 1);
            var tempx = dir.X;
            dir.X = -dir.Z;
            dir.Z = tempx;
            SkyDome.Draw(m_GraphicsDevice, m_TintColor, ViewMatrix, ProjectionMatrix, Time, Weather, Vector3.Normalize(dir), 1f);

            //handle slices
            if (Camera.Zoomed == TerrainZoomMode.Far)
            {
                if (SubdivGeometry.CurrentSlice != -1)
                {
                    SubdivGeometry.Ready = -1;
                    SubdivGeometry.CurrentSlice = -1;
                }
            } else
            {
                var pos = Camera.CalculateR();
                var slicex = Math.Max(0, Math.Min(30, (int)Math.Round(pos.X / 16f) - 1));
                var slicey = Math.Max(0, Math.Min(30, (int)Math.Round(pos.Y / 16f) - 1));
                var slice = slicex + slicey * 32;
                if (SubdivGeometry.CurrentSlice != slice)
                {
                    SubdivGeometry.SubRegenMeshVerts(m_GraphicsDevice, new Rectangle(slicex * 16, slicey * 16, 32, 32), 4, slice);
                }
            }

            if (ShadowsEnabled)
            {
                if (SubdivGeometry.Ready != -1) SubdivGeometry.DrawAll(m_GraphicsDevice, Content, VertexShader, PixelShader, (fog) ? 4 : 0, (fog) ? 4 : 0);
                Geometry.DrawSlice(m_GraphicsDevice, Content, VertexShader, PixelShader, (fog) ? 4 : 0, (fog) ? 4 : 0, SubdivGeometry.Ready, 16);
            }
            else
            {
                if (SubdivGeometry.Ready != -1) SubdivGeometry.DrawAll(m_GraphicsDevice, Content, VertexShader, PixelShader, (fog) ? 3 : 2, (fog) ? 3 : 2);
                Geometry.DrawSlice(m_GraphicsDevice, Content, VertexShader, PixelShader, (fog) ? 3 : 2, (fog) ? 3 : 2, SubdivGeometry.Ready, 16);
            }

            var pass = (ShadowsEnabled ? ((fog) ? 4 : 0) : ((fog) ? 3 : 2));
            m_MovMatrix = ViewMatrix * ProjectionMatrix;

            if (m_Zoomed == TerrainZoomMode.Far) Draw3DHouses(pass); //DrawHouses(HB); //draw far view house icons

            if (is2D)
            {
                m_2DVerts = new ArrayList(); //refresh list for tris under houses
                DrawSprites(HB, VB); //draw near view trees and houses
            }
            else
            {
                if (((CityCamera3D)Camera).Zoom3D < 7f || Camera.HideUI)
                    Foliage.Draw(this, m_GraphicsDevice, Content, VertexShader, PixelShader, pass, 2, frustum);
                if (Camera.Zoomed == TerrainZoomMode.Near)
                {
                    m_2DVerts = new ArrayList(); //refresh list for tris under houses
                    m_Batch.Begin(SpriteSortMode.Texture);
                    DrawTileBorders(0, m_Batch);
                    m_Batch.End();
                    m_GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                    Draw2DPoly(true);
                    DrawFacades(Camera.CalculateR(), pass, false, frustum);
                }
            }
            
            if (Camera.Zoomed != TerrainZoomMode.Lot) NeighGeom.DrawHover(m_GraphicsDevice, m_Batch, VertexShader, PixelShader, Content);
            if (Plugin is NeighbourhoodEditPlugin) NeighGeom.Draw(m_GraphicsDevice, VertexShader, PixelShader, Content);

            m_2DVerts = new ArrayList(); //refresh list for spotlights
            DrawSpotlights(HB); //draw far view spotlights
            Draw2DPoly(false); //draw spotlights using 2DVert shader

            foreach (var particle in Particles)
            {
                var tint = m_TintColor;
                particle.GenericDraw(gfx, ParticleCamera, tint, false);
            }

            DrawModifications(m_Batch);
            Plugin?.Draw(m_Batch);
        }

        private void DrawModifications(SpriteBatch batch)
        {
            if (Modifications.Count == 0) return;

            batch.Begin();

            float iScale = (float)(1 / (GetIsoScale() * 2));

            foreach (var mod in Modifications)
            {
                var (edgeColor, fillColor) = mod.GetColors();
                var px = Content.stpWhiteLine;

                var map = mod.Bitmap;

                for (int y = 0; y < map.Height; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        if (map.IsSet(x, y))
                        {
                            int tx = x + map.X;
                            int ty = y + map.Y;

                            PathTile(tx, ty, iScale, fillColor);

                            var vxy = transformSpr3(new Vector3(tx + 0, MapData.ElevationData[(ty * 512 + tx)] / 12.0f, ty + 0));
                            var vxy2 = transformSpr3(new Vector3(tx + 1, MapData.ElevationData[(ty * 512 + Math.Min(tx + 1, 511))] / 12.0f, ty + 0));
                            var vxy3 = transformSpr3(new Vector3(tx + 1, MapData.ElevationData[(Math.Min(ty + 1, 511) * 512 + Math.Min(tx + 1, 511))] / 12.0f, ty + 1));
                            var vxy4 = transformSpr3(new Vector3(tx + 0, MapData.ElevationData[(Math.Min(ty + 1, 511) * 512 + tx)] / 12.0f, ty + 1));

                            var minZ = Math.Min(vxy.Z, Math.Min(vxy2.Z, Math.Min(vxy3.Z, vxy4.Z)));

                            if (minZ < 0) continue;
                            //Vector2 mousedist = ((xy + xy2 + xy3 + xy4) / 4.0f - new Vector2(m_MouseState.X, m_MouseState.Y));
                            var xy = new Vector2(vxy.X, vxy.Y);
                            var xy2 = new Vector2(vxy2.X, vxy2.Y);
                            var xy3 = new Vector2(vxy3.X, vxy3.Y);
                            var xy4 = new Vector2(vxy4.X, vxy4.Y);

                            // Draw edges
                            if (x <= 0 || !map.IsSet(x - 1, y))
                            {
                                DrawLine(px, xy, xy4, batch, 2, edgeColor);
                            }

                            if (x >= map.Width - 1 || !map.IsSet(x + 1, y))
                            {
                                DrawLine(px, xy2, xy3, batch, 2, edgeColor);
                            }

                            if (y <= 0 || !map.IsSet(x, y - 1))
                            {
                                DrawLine(px, xy, xy2, batch, 2, edgeColor);
                            }

                            if (y >= map.Height - 1 || !map.IsSet(x, y + 1))
                            {
                                DrawLine(px, xy3, xy4, batch, 2, edgeColor);
                            }
                        }
                    }
                }
            }

            batch.End();

            Draw2DPoly(true);
        }

        private void RecalculateShadows()
        {
            Matrix LightView = Matrix.CreateLookAt(m_LightPosition, new Vector3(256, 0, 256), new Vector3(0, 1, 0)); //Create light view - looks from light position to center of mesh.
            Vector2 pos = Camera.CalculateRShadow();
            Vector3 LightOff = Vector3.Transform(new Vector3(pos.X, 0, pos.Y), LightView); //finds position in light space of approximate center of camera (to be used for only shadowing near the camera in near view)

            var shadZoom = Camera is CityCamera3D ? 0f : m_ZoomProgress;
            float size = (1 - shadZoom) * 262 + (shadZoom * 40); //size of draw window to use for shadowing. 40 is good for near view, it could be less but that wouldn't work correctly on higher ground.
            Matrix LightProject = Matrix.CreateOrthographicOffCenter(-size + LightOff.X, size + LightOff.X, -size + LightOff.Y, size + LightOff.Y, 0.1f, 524); //create light projection using offsets + size.

            m_LightMatrix = LightView * LightProject; // World matrix for terrain is Identity
            VertexShader.Parameters["LightMatrix"].SetValue(m_LightMatrix);

            DrawDepth();
        }

        public static DepthStencilState StencilWrite = new DepthStencilState()
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.Always,
            StencilFail = StencilOperation.Keep,
            StencilPass = StencilOperation.Replace,
            CounterClockwiseStencilPass = StencilOperation.Replace,
            StencilDepthBufferFail = StencilOperation.Keep,
            DepthBufferEnable = false,
            DepthBufferWriteEnable = false,
            ReferenceStencil = 1,
            TwoSidedStencilMode = true
        };

        public static DepthStencilState StencilOnly = new DepthStencilState()
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.NotEqual,
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true,
            ReferenceStencil = 1,
            TwoSidedStencilMode = true
        };
        public static BlendState NoColor = new BlendState() { ColorWriteChannels = ColorWriteChannels.None };

        public uint StencilLotID;
        public VertexBuffer StencilVertices;

        public void DrawThumbnail(GraphicsDevice gfx, RenderTarget2D target)
        {
            // Generate a temporary default 2D far zoom camera to use for the thumbnail.
            // We need to temporarily override the camera for some things to work, so we remember the current camera to restore it later.

            var oldCamera = Camera;
            var camera = new CityCamera2D();
            Camera = camera;

            m_GraphicsDevice = gfx;

            ShadowRes = 2048;
            ShadowsEnabled = true;

            m_GraphicsDevice.RasterizerState = RasterizerState.CullNone; //don't cull
            m_GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            m_ScrHeight = target.Height;
            m_ScrWidth = target.Width;

            if (RegenData) GenerateAssets(); //if assets are flagged as requiring regeneration, regenerate them!

            // Update lighting to a fixed time of day

            SetTimeOfDay(0.4f);

            PrepareTerrainShader(Camera.View, camera.CalculateProjection(target.Width, target.Height), 0);

            RecalculateShadows();
            ShadowRegenTimer = -1;
            var shadowMap = ShadowTarget;

            PixelShader.Parameters["ShadowMap"].SetValue(shadowMap);
            PixelShader.Parameters["ShadSize"].SetValue(new Vector2(shadowMap.Width, shadowMap.Height));
            VertexShader.Parameters["LightMatrix"].SetValue(m_LightMatrix);

            gfx.SetRenderTarget(target);
            m_GraphicsDevice.Clear(m_TintColor);

            PixelShader.Parameters["FogMaxDist"].SetValue(float.MaxValue);
            PixelShader.Parameters["FogColor"].SetValue(Color.White.ToVector4());

            Geometry.DrawSlice(m_GraphicsDevice, Content, VertexShader, PixelShader, 4, 4, -1, 16);

            gfx.SetRenderTarget(null);

            SetTimeOfDay(Time);
            Camera = oldCamera;

            m_ScrHeight = m_GraphicsDevice.Viewport.Height;
            m_ScrWidth = m_GraphicsDevice.Viewport.Width;
        }

        public void DrawSurrounding(GraphicsDevice gfx, ICamera camera, Vector4 fogColor, int surroundNumber)
        {
            if (!GlobalSettings.Default.CitySkybox)
            {
                if (camera is CameraControllers controllers)
                {
                    controllers.ClearExternalTransition();
                }
                return;
            }
            m_GraphicsDevice = gfx;

            var world = Matrix.CreateTranslation(-LotPosition + new Vector3(-1 / 75f, -0.011f, 1 / 75f)) * Matrix.CreateRotationY((float)Math.PI / 2) * Matrix.CreateScale(75f * 3, 12 * 100 * Blueprint.TerrainFactorConst * 3, 75f * 3);

            float IsoScale = GetIsoScale();
            m_LastIsoScale = IsoScale;

            float HB = m_ScrWidth * IsoScale;
            float VB = m_ScrHeight * IsoScale;
            Matrix? ViewMatrixN = null;
            if (camera is CameraControllers)
            {
                var controllers = (CameraControllers)camera;
                if (m_LotZoomProgress == 1)
                {
                    controllers.ClearExternalTransition();
                }
                else
                {
                    var trans = controllers.GetExternalTransition();
                    trans.IsLinear = true;
                    var dummy = trans.Camera as DummyCamera;
                    trans.Percent = 1 - m_LotZoomProgress;

                    Matrix ProjectionMatrix = Camera.Projection;
                    Matrix ViewMatrix = Camera.View;

                    ViewMatrix = Matrix.Invert(world) * ViewMatrix;
                    
                    dummy.Projection = ProjectionMatrix;
                    dummy.View = ViewMatrix;
                    ViewMatrixN = Matrix.CreateScale(new Vector3(1, 1/3f, 1)) * ViewMatrix;
                    
                }
            }

            var v = camera.View;
            var p = camera.Projection;

            Camera.CalculateLotSquish(v);

            if (ViewMatrixN != null)
            {
                var dummy = ((camera as CameraControllers)?.GetExternalTransition()?.Camera as DummyCamera);
                if (dummy != null) dummy.View = ViewMatrixN.Value;
            }

            ShadowRes = GlobalSettings.Default.ShadowQuality;
            ShadowsEnabled = GlobalSettings.Default.CityShadows;

            m_GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            m_GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            m_ScrHeight = m_GraphicsDevice.Viewport.Height;
            m_ScrWidth = m_GraphicsDevice.Viewport.Width;

            if (RegenData) GenerateAssets(); //if assets are flagged as requiring regeneration, regenerate them!

            VertexShader.CurrentTechnique = VertexShader.Techniques[2];
            var mv = world * v;
            var mvp = mv * p * Matrix.CreateScale(1f, 1f, 0.3f);
            m_MovMatrix = mvp;
            VertexShader.Parameters["BaseMatrix"].SetValue(mvp);
            VertexShader.Parameters["MV"].SetValue(mv);
            var frustum = new BoundingFrustum(mvp);
                
            PixelShader.CurrentTechnique = PixelShader.Techniques[2];
            PixelShader.Parameters["LightCol"].SetValue(new Vector4(m_TintColor.R / 255.0f, m_TintColor.G / 255.0f, m_TintColor.B / 255.0f, 1) * 1.25f);
            var lightVec = Vector3.Normalize(m_LightPosition - new Vector3(256, 0, 256));
            PixelShader.Parameters["LightVec"].SetValue(lightVec);
            
            var invView = Matrix.Invert(mv);
            invView.Translation = Vector3.Zero;
            PixelShader.Parameters["InvView"].SetValue(invView);
            var dist = 0.3f + lightVec.Y;
            dist *= dist;
            PixelShader.Parameters["SunStrength"].SetValue(((1 - 0.6f * Weather.Darken) / dist) * (1.0f - m_ShadowMult) * 2);

            PixelShader.Parameters["BigWTex"].SetValue(Content.BigWNormal);
            PixelShader.Parameters["SmallWTex"].SetValue(Content.SmallWNormal);

            PixelShader.Parameters["WavePow"].SetValue(5 / 2f);
            PixelShader.Parameters["RealNormalPct"].SetValue(2f);
            PixelShader.Parameters["ShadowMult"].SetValue(m_ShadowMult);
            PixelShader.Parameters["Time"].SetValue(ITime / (float)FSOEnvironment.RefreshRate);

            var weatherMult = (Camera is CityCamera3D) ? (1 + Weather.Darken) : 1;
            PixelShader.Parameters["FogMaxDist"].SetValue(m_LotZoomProgress*fogColor.W + (1- m_LotZoomProgress)* fogColor.W*weatherMult*Camera.FogMultiplier);
            fogColor.W = 1f;
            PixelShader.Parameters["FogColor"].SetValue(fogColor);

            VertexShader.Parameters["LightMatrix"].SetValue(m_LightMatrix);

            //first stencil out the area under this lot. the pixel shader for the city is a bit expensive 
            //so doing this actually saves some time, assuming stencil fill rate is not a problem.
            gfx.DepthStencilState = StencilWrite;
            gfx.BlendState = NoColor;

            PixelShader.CurrentTechnique.Passes[1].Apply();
            VertexShader.CurrentTechnique.Passes[3].Apply();

            var controller = UIScreen.Current.FindController<CoreGameScreenController>();
            var id = controller.GetVisualLotID();

            if (m_LotZoomProgress == 1)
            {
                if (id != StencilLotID)
                {
                    var x = id >> 16;
                    var y = id & 0xFFFF;

                    if (x >= 512 || y >= 512)
                    {
                        x = 255;
                        y = 255;
                    }

                    float minElev = float.MaxValue;

                    for (int x2 = -surroundNumber; x2 <= surroundNumber; x2++)
                    {
                        for (int y2 = -surroundNumber; y2 <= surroundNumber; y2++)
                        {
                            float elev = GetMinElevationAt((int)(x + x2), (int)(y + y2));
                            if (minElev > elev) minElev = elev;
                        }
                    }

                    var verts = new MeshVertex[]
                    {
                    new MeshVertex() { Coord = new Vector3((float)(x-surroundNumber) + 0.1f, minElev / 12.0f, (float)(y-surroundNumber) + 0.1f) },
                    new MeshVertex() { Coord = new Vector3((float)(x + 1+ surroundNumber) - 0.1f, minElev / 12.0f, (float)(y-surroundNumber) + 0.1f) },
                    new MeshVertex() { Coord = new Vector3((float)(x-surroundNumber) + 0.1f, minElev / 12.0f, (float)(y + 1+ surroundNumber) - 0.1f) },
                    new MeshVertex() { Coord = new Vector3((float)(x + 1+ surroundNumber) - 0.1f, minElev / 12.0f, (float)(y + 1+ surroundNumber) - 0.1f) },
                    };
                    if (StencilVertices != null) StencilVertices.Dispose();
                    StencilVertices = new VertexBuffer(gfx, typeof(MeshVertex), 4, BufferUsage.None);
                    StencilVertices.SetData(verts);
                    StencilLotID = id;
                }

                gfx.SetVertexBuffer(StencilVertices);
                //gfx.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                gfx.DepthStencilState = StencilOnly;
            } else
            {
                gfx.DepthStencilState = DepthStencilState.Default;
            }


            gfx.BlendState = BlendState.NonPremultiplied;

            //Geometry.DrawAll(m_GraphicsDevice, Content, VertexShader, PixelShader, 3, 3);

            if (SubdivGeometry.Ready != -1) SubdivGeometry.DrawAll(m_GraphicsDevice, Content, VertexShader, PixelShader, 3,3);
            Geometry.DrawSlice(m_GraphicsDevice, Content, VertexShader, PixelShader, 3,3, SubdivGeometry.Ready, 16);

            Foliage.Draw(this, m_GraphicsDevice, Content, VertexShader, PixelShader, 3, 1, frustum);
            DrawFacades(new Vector2(id >> 16, id & 0xFFFF), 3, true, frustum);
            gfx.DepthStencilState = DepthStencilState.Default;

            
            if (m_LotZoomProgress != 1)
            {
                foreach (var particle in Particles)
                {
                    var tint = m_TintColor * (1- m_LotZoomProgress);
                    particle.GenericDraw(gfx, ParticleCamera, tint, false);
                }
            }
        }

        private void DrawFacade(FSOF fsof, ref Matrix baseMat, Vector3 position, int passIndex, bool drawNight)
        {
            if (fsof == null) return;
            var b = (1 / 75f);
            var mat = baseMat * Matrix.CreateTranslation(position + new Vector3(1 + b, b * 0.75f, -b));
            var gfx = m_GraphicsDevice;
            VertexShader.Parameters["ObjModel"].SetValue(mat);
            VertexShader.Parameters["DepthBias"].SetValue(-0.18f * Camera.DepthBiasScale);
            VertexShader.CurrentTechnique.Passes[passIndex].Apply();

            if (drawNight)
            {
                var col = (new Vector4(m_TintColor.R / 255.0f, m_TintColor.G / 255.0f, m_TintColor.B / 255.0f, 1)) / (new Color(50, 70, 122).ToVector4() * 1.25f);// / fsof.NightLightColor.ToVector4();
                PixelShader.Parameters["LightCol"].SetValue(col);
            }

            if (fsof.WallVGPU != null)
            {
                PixelShader.Parameters["ObjTex"].SetValue((drawNight) ? fsof.NightWallTexture : fsof.WallTexture);
                PixelShader.CurrentTechnique.Passes[passIndex].Apply();
                gfx.SetVertexBuffer(fsof.WallVGPU);
                gfx.Indices = fsof.WallIGPU;

                gfx.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, fsof.WallPrims);
            }

            if (fsof.FloorVGPU != null)
            {
                PixelShader.Parameters["ObjTex"].SetValue((drawNight) ? fsof.NightFloorTexture : fsof.FloorTexture);
                PixelShader.CurrentTechnique.Passes[passIndex].Apply();
                gfx.SetVertexBuffer(fsof.FloorVGPU);
                gfx.Indices = fsof.FloorIGPU;

                gfx.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, fsof.FloorPrims);
            }

            if (drawNight)
            {
                var col = (new Vector4(m_TintColor.R / 255.0f, m_TintColor.G / 255.0f, m_TintColor.B / 255.0f, 1) * 1.25f);// / fsof.NightLightColor.ToVector4();
                PixelShader.Parameters["LightCol"].SetValue(col);
            }
        }

        private void DrawFacades(Vector2 mid, int passIndex, bool useLocked, BoundingFrustum frustum)
        {
            Span<float> bounds = [ (float)Math.Round(mid.X - 19), (float)Math.Round(mid.Y - 19), (float)Math.Round(mid.X + 19), (float)Math.Round(mid.Y + 19) ];

            var b = 1 / 75f;
            var baseMat = Matrix.CreateScale(b, b * Camera.LotSquish, b) * Matrix.CreateRotationY((float)Math.PI / -2f);

            float fade = Math.Max(0, Math.Min(1, (m_ZoomProgress - 0.4f) * 2));

            m_GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            m_GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            PixelShader.CurrentTechnique = PixelShader.Techniques[1];
            PixelShader.CurrentTechnique.Passes[passIndex].Apply();
            
            VertexShader.CurrentTechnique = VertexShader.Techniques[1];
            VertexShader.CurrentTechnique.Passes[passIndex].Apply();

            double biasTime = FinaleUtils.BiasSunTime(Time);
            var night = biasTime < 0.25f || biasTime > 0.75f;

            if (!useLocked || NearFacades == null)
            {
                if (useLocked) NearFacades = new CityFacadeLock();
                for (short y = (short)bounds[1]; y < bounds[3]; y++) //iterate over tiles close to the approximate tile position at the center of the screen and draw any trees/houses on them
                {
                    if (y < 0 || y > 511) continue;
                    for (short x = (short)bounds[0]; x < bounds[2]; x++)
                    {
                        if (x < 0 || x > 511) continue;

                        float elev = GetElevationAt(x, y);

                        //if (LotPosition.X == x + 1 && LotPosition.Z == y && Camera.Zoomed == TerrainZoomMode.Lot) continue;

                        Vector2 loc = new Vector2(x, y);
                        LotTileEntry house;

                        if (LotTileLookup.ContainsKey(loc))
                        {
                            house = LotTileLookup[loc];
                        }
                        else
                        {
                            house = null;
                        }
                        if (house != null) //if there is a house here, draw it
                        {
                            var online = (house.flags & LotTileFlags.Online) > 0;
                            if (online && !useLocked)
                            {
                                PathTile(x, y, 0, new Color(1.0f, 1.0f, 1.0f, (float)(0.3 + Math.Sin(4 * Math.PI * (m_SpotOsc % 1)) * 0.15)));
                                Draw2DPoly(true);
                            }

                            FSOF lotImg = null;
                            var controller = FindController<TerrainController>();
                            if (useLocked)
                            {
                                var pos = new Vector3(x, elev / 12.0f, y);
                                var facade = new CityFacadeEntry
                                {
                                    LotImg = controller.LockLotFacade((uint)house.packed_pos),
                                    Position = pos,
                                    Bounds = new BoundingBox(pos - new Vector3(0, 0.2f, 0), pos + new Vector3(1, 1, 1)),
                                    Location = loc
                                };
                                NearFacades.Entries.Add(facade);
                            } else {
                                lotImg = controller.RequestLotFacade((uint)house.packed_pos);

                                if (lotImg != null)
                                {
                                    DrawFacade(lotImg, ref baseMat, new Vector3(x, elev / 12.0f, y), passIndex, night && online);
                                }
                            }
                        }
                    }
                }
            }

            int drawCount = 0;
            int houseRange = (GlobalSettings.Default.SurroundingLotMode == 0) ? 0 : 1;
            if (useLocked)
            {
                foreach (var house in NearFacades.Entries)
                {
                    var x = (int)house.Location.X;
                    var y = (int)house.Location.Y;
                    if (LotPosition.X >= (x - houseRange) + 1 && LotPosition.X <= x + houseRange + 1 && LotPosition.Z >= y - houseRange && LotPosition.Z <= y + houseRange) continue;

                    if (house.Bounds.Intersects(frustum))
                    {
                        drawCount++;
                        LotTileEntry lhouse = null;

                        if (LotTileLookup.ContainsKey(house.Location)) lhouse = LotTileLookup[house.Location];
                        var online = ((lhouse?.flags ?? 0) & LotTileFlags.Online) > 0;

                        DrawFacade(house.LotImg.LotFacade, ref baseMat, house.Position, passIndex, night && online);
                    }
                }
            }

            PixelShader.CurrentTechnique = PixelShader.Techniques[2];
            VertexShader.CurrentTechnique = VertexShader.Techniques[2];
        }

        internal void AddModification(CityModification modification)
        {
            Modifications.Add(modification);
        }

        public override void DeviceReset(GraphicsDevice Device)
        {

        }
    }

    public enum TerrainZoomMode
    {
        Far,
        Near,
        Lot
    }
}
