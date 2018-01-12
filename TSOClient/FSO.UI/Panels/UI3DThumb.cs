using FSO.Client;
using FSO.Common.Rendering.Framework;
using FSO.Common.Rendering.Framework.Camera;
using FSO.Content;
using FSO.LotView.Components;
using FSO.LotView.Debug;
using FSO.SimAntics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.UI.Panels
{
    public class UI3DThumb
    {
        public _3DTargetScene Scene;
        public BasicCamera Camera;
        public Texture2D Tex
        {
            get { return Scene.Target; }
        }

        public float RotationSpeed = 0.20f;
        public float RotationX = -(float)Math.PI;
        public Vector3 Ctr;
        public float Size;

        public List<Debug3DDGRPComponent> Comp3D;
        public UI3DThumb(VMEntity ent)
        {
            Camera = new BasicCamera(GameFacade.GraphicsDevice, new Vector3(3, 1, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
            Camera.NearPlane = 0.001f;
            Scene = new _3DTargetScene(GameFacade.GraphicsDevice, Camera, new Point(150, 150), 0);
            Scene.Initialize(GameFacade.Scenes);

            if (Comp3D != null)
            {
                foreach (var e in Comp3D)
                {
                    e.Dispose();
                    Scene.Remove(e);
                }
            }
            Comp3D = new List<Debug3DDGRPComponent>();

            BoundingBox? total = null;
            var pos = ent.MultitileGroup.GetBasePositions();
            var i = 0;
            foreach (var obj in ent.MultitileGroup.Objects)
            {
                var c = new Debug3DDGRPComponent();
                var dgrp = ((ObjectComponent)obj.WorldUI).DGRP;
                c.Mesh = (dgrp == null) ? null : Content.Content.Get().RCMeshes.Get(dgrp, obj.Object.OBJ); //new DGRP3DMesh(((ObjectComponent)obj.WorldUI).DGRP, obj.Object.OBJ, GameFacade.GraphicsDevice, null);
                Scene.Add(c);
                if (c.Mesh == null) continue;

                var vp = pos[i++];
                c.Position = new Vector3((vp.X - 0.5f), vp.Z, (vp.Y - 0.5f));
                if (total == null) total = OffsetBox(c.Mesh.Bounds ?? new BoundingBox(), c.Position);
                else total = BoundingBox.CreateMerged(total.Value, OffsetBox(c.Mesh.Bounds ?? new BoundingBox(), c.Position));
                c.Initialize();
                Comp3D.Add(c);
            }

            if (total != null)
            {
                Ctr = new Vector3((total.Value.Max.X + total.Value.Min.X) / 2, (total.Value.Max.Y + total.Value.Min.Y) / 2, (total.Value.Max.Z + total.Value.Min.Z) / 2);
                var diag = total.Value.Max - total.Value.Min;
                Size = diag.Length();
            }
        }

        public BoundingBox OffsetBox(BoundingBox box, Vector3 off)
        {
            return new BoundingBox(box.Min + off, box.Max + off);
        }

        public void RecalcBounds()
        {
            BoundingBox? total = null;
            foreach (var c in Comp3D)
            {
                if (total == null) total = OffsetBox(c.Mesh.Bounds ?? new BoundingBox(), c.Position);
                else total = BoundingBox.CreateMerged(total.Value, OffsetBox(c.Mesh.Bounds ?? new BoundingBox(), c.Position));
            }

            if (total != null)
            {
                Ctr = new Vector3((total.Value.Max.X + total.Value.Min.X) / 2, (total.Value.Max.Y + total.Value.Min.Y) / 2, (total.Value.Max.Z + total.Value.Min.Z) / 2);
                var diag = total.Value.Max - total.Value.Min;
                Size = diag.Length();
            }
        }

        public void Draw()
        {
            RecalcBounds();
            RotationX += RotationSpeed;
            RotationSpeed += (0.01f - RotationSpeed) / 20;

            var mat = Microsoft.Xna.Framework.Matrix.CreateRotationY(RotationX);
            Camera.Position = Ctr + Vector3.Transform(new Vector3(4, 3, 0)*(0.1f + Size*0.2f), mat);
            Camera.Target = Ctr + new Vector3(0, 0, 0);
            var old = GameFacade.GraphicsDevice.BlendState;
            GameFacade.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            Scene.Draw(GameFacade.GraphicsDevice);
            GameFacade.GraphicsDevice.BlendState = old;
        }

        public void Dispose()
        {
            Scene.Dispose();
            if (Comp3D != null)
            {
                foreach (var e in Comp3D) e.Dispose();
            }
        }
    }
}
