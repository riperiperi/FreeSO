using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FSO.LotView.Utils;
using FSO.Common;
using FSO.Files;
using System.IO;
using FSO.LotView.Model;
using FSO.Common.Utils;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.LotView.Components
{
    public class ParticleComponent : WorldComponent
    {
        public VertexBuffer Vertices;
        public IndexBuffer Indices;
        public int Primitives;
        public BoundingBox Volume;
        public Matrix OwnerWorld;
        public int NumParticles;
        public ParticleType Mode = ParticleType.SNOW;
        public Texture2D Tex;
        public Texture2D Indoors;
        public byte[] IndoorsDat;
        public bool AutoBounds = true;
        public bool BoundsDirty = true;
        public Blueprint Bp;
        public List<ParticleComponent> Particles;
        public Color Tint = Color.White;
        public PART Resource;
        public EntityComponent Owner;

        /*
        public static Dictionary<ParticleType, Vector4[]> Params = new Dictionary<ParticleType, Vector4[]>()
        {
            {
                ParticleType.SNOW,
                new Vector4[]
                {
                    new Vector4(Volume.Min.Y, Volume.Max.Y - Volume.Min.Y, 1f, 0.2f),
                    new Vector4(30f, 10f, 10f, 100f),
                    new Vector4(Volume.Min.X, Volume.Max.X - Volume.Min.X, Volume.Min.Z, Volume.Max.Z - Volume.Min.Z)
                }
            }
        };*/

        public float WeatherIntensity;
        public float? FadeProgress = null;
        public float Duration
        {
            get
            {
                return Resource?.Duration ?? 0f;
            }
        }

        public ParticleComponent(Blueprint bp, List<ParticleComponent> particles)
        {
            Bp = bp;
            Particles = particles;
        }

        public void InitParticleVolume(GraphicsDevice gd, BoundingBox area, int particleCount)
        {
            if (Vertices != null) Vertices.Dispose();
            if (Indices != null) Indices.Dispose();
            if (Mode == ParticleType.SNOW)
            {
                using (var file = File.OpenRead(Path.Combine(FSOEnvironment.ContentDir, "Textures/snowflake.png")))
                    Tex = ImageLoader.FromStream(gd, file);
            }
            var verts = new List<ParticleVertex>();
            var indices = new List<int>();
            var rand = new Random();
            for (int i=0; i<particleCount; i++)
            {
                var pos = new Vector3(
                    (float)(area.Min.X + rand.NextDouble() * (area.Max.X - area.Min.X)),
                    (float)(area.Min.Y + rand.NextDouble() * (area.Max.Y - area.Min.Y)),
                    (float)(area.Min.Z + rand.NextDouble() * (area.Max.Z - area.Min.Z))
                    );

                var v = verts.Count;
                indices.Add(v); indices.Add(v+1); indices.Add(v+2);
                indices.Add(v+2); indices.Add(v+3); indices.Add(v);

                var r = (float)rand.NextDouble();

                verts.Add(new ParticleVertex(pos, new Vector3(-0.5f, -0.5f, 0), new Vector3(0, 0, r)));
                verts.Add(new ParticleVertex(pos, new Vector3(-0.5f, 0.5f, 0), new Vector3(0, 1, r)));
                verts.Add(new ParticleVertex(pos, new Vector3(0.5f, 0.5f, 0), new Vector3(1, 1, r)));
                verts.Add(new ParticleVertex(pos, new Vector3(0.5f, -0.5f, 0), new Vector3(1, 0, r)));
            }

            Vertices = new VertexBuffer(gd, typeof(ParticleVertex), verts.Count, BufferUsage.None);
            Vertices.SetData(verts.ToArray());
            Indices = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.None);
            Indices.SetData(indices.ToArray());
            Primitives = indices.Count / 3;
        }

        public float Time;
        public float TimeRate;
        public float StopTime = float.PositiveInfinity;
        public bool Dead;

        public override void Update(GraphicsDevice device, WorldState world)
        {
            base.Update(device, world);
            if (world != null) TimeRate = world.SimSpeed;
            else TimeRate = 1f;
            Time += TimeRate*((Mode < ParticleType.GENERIC_BOX)?(0.001f / FSOEnvironment.RefreshRate):(1f / FSOEnvironment.RefreshRate));

            if (Time > StopTime + Duration)
            {
                Particles?.Remove(this);
                Dispose();
                Dead = true;
                return;
            }

            if (FadeProgress != null) { 
                if (FadeProgress.Value < 0)
                {
                    FadeProgress += 0.01f/4;
                    if (FadeProgress.Value >= 0) FadeProgress = null;
                } else
                {
                    FadeProgress += 0.01f/4;
                    if (FadeProgress.Value >= 1)
                    {
                        Particles?.Remove(this);
                        Dispose();
                    }
                }
            }
        }

        public void Stop()
        {
            StopTime = Time;
        }

        public WorldZoom LastZoom;
        public CameraRenderMode LastMode;

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            int scale2d = 1;
            var weather = (Mode < ParticleType.GENERIC_BOX);
            var mode = world.CameraMode;
            if (Vertices == null || ((LastZoom != world.Zoom || LastMode != mode) && weather))
            {
                LastZoom = world.Zoom;
                LastMode = mode;
                if (weather)
                {
                    if (world.CameraMode == CameraRenderMode._3D)
                        Volume = new BoundingBox(new Vector3(-50, -50, -50), new Vector3(50, 50, 50));
                    else
                    {
                        scale2d = (1 << (3 - (int)world.Zoom));
                        Volume = new BoundingBox(new Vector3(-100, 0, -100) * scale2d, new Vector3(100 * scale2d, 2.95f * 3 * 5 * 2, 100 * scale2d));
                    }
                    if (Indoors == null)
                        Indoors = new Texture2D(device, Bp.Width, Bp.Height, false, SurfaceFormat.Alpha8);
                    InitParticleVolume(device, Volume, (int)(12500 * WeatherIntensity));
                } else
                {
                    var volVec = Volume.Max - Volume.Min;
                    if (volVec.X < 0.1f) volVec.X = 0.1f;
                    if (volVec.Y < 0.1f) volVec.Y = 0.1f;
                    if (volVec.Z < 0.1f) volVec.Z = 0.1f;

                    var maxDim = Math.Max(volVec.X, Math.Max(volVec.Y, volVec.Z));
                    NumParticles = (int)(maxDim * Resource.Particles);

                    if (NumParticles == 0) NumParticles = 1;// return;
                    InitParticleVolume(device, Volume, NumParticles);
                }
                
                //return;
            }
            //get our billboard

            if (weather)
            {
                var indoors = Bp.GetIndoors();
                if (IndoorsDat != indoors)
                {
                    IndoorsDat = indoors;
                    Indoors.SetData(indoors);
                }
            }

            var rot = world.View;
            rot.Translation = Vector3.Zero;
            var inv = Matrix.Invert(rot);

            var forward = Vector3.Transform(new Vector3(0, 0, 1), inv);
            var effect = WorldContent.ParticleEffect;
            Matrix trans;
            var basealt = Bp.InterpAltitude(new Vector3(world.CenterTile, 0));
            var pos = Vector3.Transform(Vector3.Zero, Matrix.Invert(world.View));
            Vector3 transp;

            float opacity = 1;
            var cam3d = world.CameraMode == CameraRenderMode._3D;
            if (weather)
            {
                if (cam3d)
                {
                    transp = (pos + forward * -20f + new Vector3(Volume.Max.X, 0, Volume.Max.Z)) * 2;
                }
                else
                {
                    transp = new Vector3(world.CenterTile.X * 3 + Volume.Max.X, basealt * 3, world.CenterTile.Y * 3 + Volume.Max.Z) * 2;
                }
                trans = Matrix.CreateTranslation(transp);
                effect.Parameters["World"].SetValue(trans);

                var velocity = (world.CameraMode == CameraRenderMode._3D) ? transp - LastPosition : new Vector3();
                effect.Parameters["CameraVelocity"].SetValue(velocity);
                if (Mode == ParticleType.RAIN)
                {
                    opacity = Math.Min(1, (3f / velocity.Length() + 0.001f));
                    opacity /= (float)Math.Sqrt(Math.Max(1, TimeRate));
                }
                LastPosition = transp;
                effect.Parameters["Level"].SetValue((float)(Math.Min((world.Level + 1), Bp.Stories) - 0.999f));
            } else
            {
                effect.Parameters["World"].SetValue(OwnerWorld);// Matrix.CreateScale(3,3,3));
                effect.Parameters["Level"].SetValue(Level-0.999f);
            }

            effect.Parameters["View"].SetValue(world.View);
            effect.Parameters["Projection"].SetValue(world.Projection);
            effect.Parameters["InvRotation"].SetValue(inv * Matrix.CreateScale(0.5f));
            
            Tint = Color.White * opacity;
            if (Mode == ParticleType.RAIN)
            {
                //rot.Up = new Vector3(0, 1, 0);
                //rot.Backward = new Vector3(0, 0, 1);
                rot.Up = new Vector3(0, 1, 0);
                var invxz = (cam3d)?Matrix.Invert(rot): Matrix.Identity;
                effect.Parameters["InvXZRotation"].SetValue(invxz * Matrix.CreateScale(0.5f));
                effect.Parameters["SubColor"].SetValue(Bp.OutsideColor.ToVector4() * 0.6f * opacity);
            } else
            {
                effect.Parameters["SubColor"].SetValue(Vector4.Zero);
            }
            effect.Parameters["ClipLevel"].SetValue(cam3d ? float.MaxValue : world.Level);
            effect.Parameters["BaseAlt"].SetValue(basealt * 3);
            effect.Parameters["BpSize"].SetValue(new Vector2(Bp.Width * 3, Bp.Height * 3));
            effect.Parameters["Stories"].SetValue((float)(Bp.Stories + 1));
            InternalDraw(device, effect, scale2d, true, cam3d);
        }

        private Vector3 LastPosition;

        public void GenericDraw(GraphicsDevice device, Common.Rendering.Framework.Camera.ICamera camera, Color lightColor, bool useDepth)
        {
            if (Vertices == null)
            {
                Volume = new BoundingBox(new Vector3(-50, -50, -50), new Vector3(50, 50, 50));
                InitParticleVolume(device, Volume, (int)(12500 * WeatherIntensity));
                //return;
            }
            //get our billboard

            var rot = camera.View;
            rot.Translation = Vector3.Zero;
            var inv = Matrix.Invert(rot);

            var forward = Vector3.Transform(new Vector3(0, 0, 1), inv);
            var effect = WorldContent.ParticleEffect;
            var basealt = -1000f;
            var transp = (camera.Position + forward * -20f + new Vector3(Volume.Max.X, 0, Volume.Max.Z)) * 2;
            var trans = Matrix.CreateTranslation(transp);
            effect.Parameters["advancedLight"].SetValue(Common.Utils.TextureGenerator.GetPxWhite(device));
            Blueprint.SetLightColor(effect, Color.White, Color.White * 0.75f);
            Tint = lightColor;

            effect.Parameters["World"].SetValue(trans);
            effect.Parameters["CameraVelocity"].SetValue(transp - LastPosition);
            effect.Parameters["View"].SetValue(camera.View);
            effect.Parameters["Projection"].SetValue(camera.Projection);
            effect.Parameters["InvRotation"].SetValue(inv * Matrix.CreateScale(0.5f));
            if (Mode == ParticleType.RAIN)
            {
                rot.Up = new Vector3(0, 1, 0);
                var invxz = Matrix.Invert(rot);
                effect.Parameters["InvXZRotation"].SetValue(invxz * Matrix.CreateScale(0.5f));
                effect.Parameters["SubColor"].SetValue(lightColor.ToVector4()*0.5f);// * new Vector4(0.25f, 0.25f, 0.5f, 0.25f));
            }
            else
            {
                effect.Parameters["SubColor"].SetValue(Vector4.Zero);
            }
            effect.Parameters["Level"].SetValue((float)((5 + 1) - 0.999f));
            effect.Parameters["ClipLevel"].SetValue(float.MaxValue);
            effect.Parameters["BaseAlt"].SetValue(basealt * 3);
            effect.Parameters["BpSize"].SetValue(new Vector2(3, 3));
            effect.Parameters["Stories"].SetValue(6f);
            InternalDraw(device, effect, 1, useDepth, true);
            LastPosition = transp;
        }

        private void InternalDraw(GraphicsDevice device, Effect effect, int scale, bool useDepth, bool cam3d)
        {
            effect.Parameters["BaseTex"].SetValue(Tex);
            effect.Parameters["IndoorsTex"].SetValue(Indoors);

            var fade = FadeProgress ?? 0f;
            effect.Parameters["Color"].SetValue(Tint.ToVector4() * (1 - Math.Abs(fade)));
            effect.Parameters["TimeRate"].SetValue(Math.Max(1,TimeRate)*0.001f/ FSOEnvironment.RefreshRate);

            //Parameters:
            //miny, yrange, fall speed, fall speed variation
            //wind x, wind z, wind variation 
            //minx, xrange, minz, zrange
            switch (Mode) {
                case ParticleType.SNOW:
                    effect.Parameters["Parameters1"].SetValue(new Vector4(Volume.Min.Y, Volume.Max.Y - Volume.Min.Y, 1f, 0.2f));
                    effect.Parameters["Parameters2"].SetValue(new Vector4(30f, 10f, 10f, 100f));
                    effect.Parameters["Parameters3"].SetValue(new Vector4(Volume.Min.X, Volume.Max.X - Volume.Min.X, Volume.Min.Z, Volume.Max.Z - Volume.Min.Z));
                    break;
                case ParticleType.RAIN:
                    effect.Parameters["Parameters1"].SetValue(new Vector4(Volume.Min.Y, Volume.Max.Y - Volume.Min.Y, 0.20f, 0.01f)); //0.1f
                    effect.Parameters["Parameters2"].SetValue(new Vector4(30f, 10f, 10f, ((cam3d || Indoors == null)? 0.3f:1f)));
                    effect.Parameters["Parameters3"].SetValue(new Vector4(Volume.Min.X, Volume.Max.X - Volume.Min.X, Volume.Min.Z, Volume.Max.Z - Volume.Min.Z));
                    break;
                //(deltax, deltay, deltaz, gravity)
                //(deltavar, rotdeltavar, size, sizevel)
                //(duration, fadein, fadeout, sizevar)
                case ParticleType.GENERIC_BOX:
                    if (Resource.Parameters == null) Resource.BakeParameters();
                    var p = Resource.Parameters;
                    effect.Parameters["Parameters1"].SetValue(p[0]);
                    effect.Parameters["Parameters2"].SetValue(p[1]);
                    effect.Parameters["Parameters3"].SetValue(p[2]);
                    effect.Parameters["Parameters4"].SetValue(p[3]);

                    effect.Parameters["Frequency"].SetValue(Resource.Frequency);
                    effect.Parameters["StopTime"].SetValue(StopTime);
                    break;
            }

            effect.CurrentTechnique = effect.Techniques[(int)Mode * 2 + (useDepth?0:1)];

            device.BlendState = Mode == ParticleType.RAIN ? BlendState.Additive : BlendState.AlphaBlend;
            device.DepthStencilState = useDepth?DepthStencilState.DepthRead:DepthStencilState.None;

            device.SetVertexBuffer(Vertices);
            device.Indices = Indices;

            for (int i = 0; i < scale; i++)
            {
                effect.Parameters["Time"].SetValue(Time + i);
                effect.CurrentTechnique.Passes[0].Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, Primitives);
            }

            device.BlendState = BlendState.NonPremultiplied;
            device.DepthStencilState = DepthStencilState.Default;
        }

        public void Dispose()
        {
            Vertices?.Dispose();
            Indices?.Dispose();
            Vertices = null;
            Indices = null;

            if (Mode < ParticleType.GENERIC_BOX) Tex?.Dispose();
            Indoors?.Dispose();
        }
    }

    public enum ParticleType : int
    {
        SNOW = 0,
        RAIN = 1,
        GENERIC_BOX = 2
    }
}
