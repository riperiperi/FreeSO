using FSO.Client;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework;
using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework.Model;
using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Drivers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Shapes;
using FSO.LotView.Debug;
using FSO.LotView.Utils;
using FSO.Files.RC;

namespace FSO.IDE.Common.Debug
{
    public class UI3DDGRP : UIInteractiveDGRP
    {
        public List<Debug3DDGRPComponent> Comp3D;

        public _3DScene Scene;
        public BasicCamera Camera;

        public UI3DDGRP(uint guid) : base(guid)
        {
            Camera = new BasicCamera(GameFacade.GraphicsDevice, new Vector3(3, 1, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
            Scene = new _3DScene(GameFacade.GraphicsDevice, Camera);
            var cube = new _3DCube(Color.Red, new Vector3(1, 0.1f, 1));
            cube.Position = new Vector3(-0.5f, -0.1f, -0.5f);
            Scene.Add(cube);
            Scene.Initialize(GameFacade.Scenes);
        }

        public override void Removed()
        {
            base.Removed();
            if (Comp3D != null)
            {
                foreach (var e in Comp3D) e.Dispose();
            }
        }

        private float RotationX;
        private float RotationY;
        private bool MouseDown;
        private Point LastMouse;
        public override void Update(UpdateState state)
        {
            Scene.Update(state);
            //ChangeGraphic(128+5);
            Invalidate();

            if (Comp3D != null)
            {
                foreach (var e in Comp3D) e.Wireframe = state.MouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }

            if (!MouseDown) LastMouse = state.MouseState.Position;
            MouseDown = state.MouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            if (MouseDown)
            {
                var mpos = state.MouseState.Position;
                RotationX += (mpos.X - LastMouse.X) / 100f;
                RotationY += (mpos.Y - LastMouse.Y) / 100f;
                LastMouse = mpos;
            }

            var mat = Microsoft.Xna.Framework.Matrix.CreateRotationZ(RotationY) * Microsoft.Xna.Framework.Matrix.CreateRotationY(RotationX);
            Camera.Position = Vector3.Transform(new Vector3(4, 4, 0), mat);
            Camera.Target = new Vector3(0, 1f, 0);
            RotationX += 0.01f;

            base.Update(state);
            if ((bool)state.SharedData["ExternalDraw"] && TargetTile?.WorldUI is ObjectComponent)
            {
                if (Comp3D != null)
                {
                    foreach (var e in Comp3D)
                    {
                        e.Dispose();
                        Scene.Remove(e);
                    }
                }
                Comp3D = new List<Debug3DDGRPComponent>();

                foreach (var obj in TargetTile.MultitileGroup.Objects)
                {
                    if (obj.GetValue(VMStackObjectVariable.Room) == 2) continue;
                    var c = new Debug3DDGRPComponent();
                    var dgrp = ((ObjectComponent)obj.WorldUI).DGRP;
                    c.Mesh = (dgrp == null)?null:Content.Content.Get().RCMeshes.Get(dgrp, obj.Object.OBJ); //new DGRP3DMesh(((ObjectComponent)obj.WorldUI).DGRP, obj.Object.OBJ, GameFacade.GraphicsDevice, null);
                    Scene.Add(c);
                    var vp = obj.VisualPosition;
                    c.Position = new Vector3(-(vp.X-0.5f), vp.Z, -(vp.Y-0.5f));
                    c.Initialize();
                    Comp3D.Add(c);
                }
                //try get our dgrp;
            }
        }

        public override void PreDraw(UISpriteBatch batch)
        {
            Scene.PreDraw(batch.GraphicsDevice);
        }

        public override void Draw(UISpriteBatch batch)
        {
            batch.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            batch.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            Camera.ProjectionOrigin = new Vector2(batch.GraphicsDevice.Viewport.Width, batch.GraphicsDevice.Viewport.Height) / 2;
            Scene.Draw(batch.GraphicsDevice);

            batch.DrawString(GameFacade.MainFont.GetNearest(8).Font, (Comp3D?.Sum(x => x.Mesh?.Geoms?.FirstOrDefault()?.Sum(y => y.Value.PrimCount) ??0)??0)+" tris", new Vector2(10, 10), Color.Red);
        }
    }
}
