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

namespace FSO.LotView.Components
{
    public class ParticleComponent : WorldComponent
    {
        public VertexBuffer Vertices;
        public IndexBuffer Indices;
        public int Primitives;
        public BoundingBox Volume;
        public ParticleType Mode = ParticleType.SNOW;
        public Texture2D Tex;
        public Texture2D Indoors;
        public byte[] IndoorsDat;
        public Blueprint Bp;
        public List<ParticleComponent> Particles;
        public Color Tint = Color.White;

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

        public ParticleComponent(Blueprint bp, List<ParticleComponent> particles)
        {
            Bp = bp;
            Particles = particles;
        }

        public void InitParticleVolume(GraphicsDevice gd, BoundingBox area, int particleCount)
        {
            Console.WriteLine("initVolume " + ((Vertices == null) ? "null" : ""));
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

                verts.Add(new ParticleVertex(pos, new Vector3(-0.5f, -0.5f, 0), new Vector2(0, 0)));
                verts.Add(new ParticleVertex(pos, new Vector3(-0.5f, 0.5f, 0), new Vector2(0, 1)));
                verts.Add(new ParticleVertex(pos, new Vector3(0.5f, 0.5f, 0), new Vector2(1, 1)));
                verts.Add(new ParticleVertex(pos, new Vector3(0.5f, -0.5f, 0), new Vector2(1, 0)));
            }

            Vertices = new VertexBuffer(gd, typeof(ParticleVertex), verts.Count, BufferUsage.None);
            Vertices.SetData(verts.ToArray());
            Indices = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.None);
            Indices.SetData(indices.ToArray());
            Primitives = indices.Count / 3;
        }

        public override float PreferredDrawOrder
        {
            get
            {
                return float.MaxValue;
            }
        }

        public float Time;

        public override void Update(GraphicsDevice device, WorldState world)
        {
            base.Update(device, world);
            Time += 0.001f / FSOEnvironment.RefreshRate;

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
                        Particles.Remove(this);
                        Dispose();
                    }
                }
            }
        }

        public WorldZoom LastZoom;

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            var scale2d = (1 << (3 - (int)world.Zoom));
            if (Vertices == null || LastZoom != world.Zoom)
            {
                LastZoom = world.Zoom;
                if (FSOEnvironment.Enable3D)
                    Volume = new BoundingBox(new Vector3(-50, -50, -50), new Vector3(50, 50, 50));
                else
                {
                    Volume = new BoundingBox(new Vector3(-100, 0, -100)*scale2d, new Vector3(100 * scale2d, 2.95f*3*5 * 2, 100 * scale2d));
                }
                if (Indoors == null)
                    Indoors = new Texture2D(device, Bp.Width, Bp.Height, false, SurfaceFormat.Alpha8);
                InitParticleVolume(device, Volume, (int)(12500*WeatherIntensity));
                //return;
            }
            //get our billboard

            var indoors = Bp.GetIndoors();
            if (IndoorsDat != indoors)
            {
                IndoorsDat = indoors;
                Indoors.SetData(indoors);
            }

            var rot = world.Camera.View;
            rot.Translation = Vector3.Zero;
            var inv = Matrix.Invert(rot);

            var forward = Vector3.Transform(new Vector3(0, 0, 1), inv);
            var effect = WorldContent.ParticleEffect;
            Matrix trans;
            var basealt = Bp.InterpAltitude(new Vector3(world.CenterTile, 0));
            var pos = Vector3.Transform(Vector3.Zero, Matrix.Invert(world.Camera.View));
            Vector3 transp;

            if (FSOEnvironment.Enable3D) {
                transp = (pos + forward * -20f + new Vector3(Volume.Max.X, 0, Volume.Max.Z)) * 2;
            }
            else {
                transp = new Vector3(world.CenterTile.X * 3 + Volume.Max.X, basealt * 3, world.CenterTile.Y * 3 + Volume.Max.Z) *2;
            }
            trans = Matrix.CreateTranslation(transp);
            effect.Parameters["World"].SetValue(trans);
            var velocity = (FSOEnvironment.Enable3D)?transp - LastPosition:new Vector3();
            effect.Parameters["CameraVelocity"].SetValue(velocity);
            effect.Parameters["View"].SetValue(world.Camera.View);
            effect.Parameters["Projection"].SetValue(world.Camera.Projection);
            effect.Parameters["InvRotation"].SetValue(inv * Matrix.CreateScale(0.5f));
            float opacity = Math.Min(1, (3f/velocity.Length() + 0.001f));
            Tint = Color.White * opacity;
            if (Mode == ParticleType.RAIN)
            {
                //rot.Up = new Vector3(0, 1, 0);
                //rot.Backward = new Vector3(0, 0, 1);
                rot.Up = new Vector3(0, 1, 0);
                var invxz = (FSOEnvironment.Enable3D)?Matrix.Invert(rot): Matrix.Identity;
                effect.Parameters["InvXZRotation"].SetValue(invxz * Matrix.CreateScale(0.5f));
                effect.Parameters["SubColor"].SetValue(Bp.OutsideColor.ToVector4() * 0.6f * opacity);
            } else
            {
                effect.Parameters["SubColor"].SetValue(Vector4.Zero);
            }
            effect.Parameters["Level"].SetValue((float)(Math.Min((world.Level + 1), Bp.Stories) - 0.999f));
            effect.Parameters["ClipLevel"].SetValue(FSOEnvironment.Enable3D ? float.MaxValue : world.Level);
            effect.Parameters["BaseAlt"].SetValue(basealt * 3);
            effect.Parameters["BpSize"].SetValue(new Vector2(Bp.Width * 3, Bp.Height * 3));
            effect.Parameters["Stories"].SetValue((float)(Bp.Stories + 1));
            InternalDraw(device, effect, scale2d, true);
            LastPosition = transp;
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
            InternalDraw(device, effect, 1, useDepth);
            LastPosition = transp;
        }

        private void InternalDraw(GraphicsDevice device, Effect effect, int scale, bool useDepth)
        {
            effect.Parameters["BaseTex"].SetValue(Tex);
            effect.Parameters["IndoorsTex"].SetValue(Indoors);

            var fade = FadeProgress ?? 0f;
            effect.Parameters["Color"].SetValue(Tint.ToVector4() * (1 - Math.Abs(fade)));
            effect.Parameters["TimeRate"].SetValue(0.001f/ FSOEnvironment.RefreshRate);

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
                    effect.Parameters["Parameters2"].SetValue(new Vector4(30f, 10f, 10f, ((FSOEnvironment.Enable3D || Indoors == null)? 0.3f:1f)));
                    effect.Parameters["Parameters3"].SetValue(new Vector4(Volume.Min.X, Volume.Max.X - Volume.Min.X, Volume.Min.Z, Volume.Max.Z - Volume.Min.Z));
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
        }

        public void Dispose()
        {
            Vertices?.Dispose();
            Indices?.Dispose();
            Tex?.Dispose();
            Indoors?.Dispose();
        }
    }

    public enum ParticleType : int
    {
        SNOW = 0,
        RAIN = 1
    }
}
