using System;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.LotView.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FSO.LotView.Utils.Camera
{
    public class CameraControllerDirect : CameraControllerFP
    {
        public AvatarComponent FirstPersonAvatar;

        public CameraControllerDirect(GraphicsDevice gd, WorldState state) : base(gd, state)
        {
        }

        public override void InvalidateCamera(WorldState state)
        {
            var baseHeight = 0;
            Camera.Position = new Vector3(state.CenterTile.X * WorldSpace.WorldUnitsPerTile, baseHeight + FPCamHeight, state.CenterTile.Y * WorldSpace.WorldUnitsPerTile);

            var mat = Matrix.CreateRotationZ((_RotationY - (float)Math.PI / 2) * 0.99f) * Matrix.CreateRotationY(_RotationX);
            Camera.Target = Camera.Position + Vector3.Transform(new Vector3(-10, 0, 0), mat);
        }

        public override void Update(UpdateState state, World world)
        {
            float nearPlane = 0.5f;

            if (Camera.NearPlane != nearPlane)
            {
                Camera.NearPlane = nearPlane;
                Camera.ProjectionDirty();
            }

            if (!CaptureMouse)
            {
                LastFP = false;
            }
            else if (!FixedCam)
            {
                var worldState = world.State;
                var terrainHeight = CorrectCameraHeight(world);
                var hz = FSOEnvironment.RefreshRate;
                if (state.WindowFocused)
                {
                    var mx = (int)worldState.WorldSpace.WorldPxWidth / 2;
                    var my = (int)worldState.WorldSpace.WorldPxHeight / 2;

                    var mpos = state.MouseState.Position;
                    var camera = Camera;
                    if (LastFP && !(mpos.X == 0 && mpos.Y == 0))
                    {
                        RotationX -= ((mpos.X - mx) / 500f) * camera.FOV;
                        RotationY += ((mpos.Y - my) / 500f) * camera.FOV;
                    }
                    Mouse.SetPosition(mx, my);

                    if (FirstPersonAvatar != null)
                    {
                        FirstPersonAvatar.Avatar.HideHead = true;
                    }

                    LastFP = true;
                }
                else
                {
                    LastFP = false;
                }
            }
        }

        public override void PreDraw(World world)
        {
            if (FirstPersonAvatar != null)
            {
                if (Camera.FOV != 0.9f) Camera.FOV = 0.9f;
                var headPos = FirstPersonAvatar.GetHeadlinePos() * FirstPersonAvatar.Scale + FirstPersonAvatar.Position;
                world.State.CenterTile = new Vector2(headPos.X, headPos.Y);
                FPCamHeight = headPos.Z * 3 + 0.25f * FirstPersonAvatar.Scale;
            }
        }
    }
}
