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
        private float ThirdPersonDistance;
        private float ThirdPersonTargetDistance;
        private float ThirdPersonLimitDistance = 4;

        private int LastWheel = 0;

        public CameraControllerDirect(GraphicsDevice gd, WorldState state) : base(gd, state)
        {
        }

        private Vector3 GetCameraDirection()
        {
            var mat = Matrix.CreateRotationZ((_RotationY - (float)Math.PI / 2) * 0.99f) * Matrix.CreateRotationY(_RotationX);
            return Vector3.Transform(new Vector3(-10, 0, 0), mat);
        }

        public override void InvalidateCamera(WorldState state)
        {
            var baseHeight = 0;
            Camera.Position = new Vector3(state.CenterTile.X * WorldSpace.WorldUnitsPerTile, baseHeight + FPCamHeight, state.CenterTile.Y * WorldSpace.WorldUnitsPerTile);
            Camera.Target = Camera.Position + GetCameraDirection();
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
                var power = 60f / hz;
                var interpRate = (float)(1f - (float)Math.Pow(0.8f, power));

                if (state.WindowFocused)
                {
                    var mx = (int)worldState.WorldSpace.WorldPxWidth / 2;
                    var my = (int)worldState.WorldSpace.WorldPxHeight / 2;

                    var mpos = state.MouseState.Position;
                    var camera = Camera;
                    if (LastFP && !(mpos.X == 0 && mpos.Y == 0))
                    {
                        RotationX -= ((mpos.X - mx) / 166f) * camera.FOV * power;
                        RotationY += ((mpos.Y - my) / 166f) * camera.FOV * power;
                    }
                    Mouse.SetPosition(mx, my);

                    var wheel = state.MouseState.ScrollWheelValue;

                    if (LastFP && wheel != LastWheel)
                    {
                        var diff = (wheel - LastWheel) / -300f;
                        ThirdPersonTargetDistance += diff;
                        ThirdPersonTargetDistance = Math.Clamp(ThirdPersonTargetDistance, 0, 4);
                    }

                    ThirdPersonDistance += (ThirdPersonTargetDistance - ThirdPersonDistance) * interpRate;

                    LastWheel = wheel;

                    if (FirstPersonAvatar != null)
                    {
                        FirstPersonAvatar.Avatar.HideHead = ThirdPersonDistance < 0.3;

                        var avatarIndoors = world.Architecture.Blueprint.IsIndoorsPrecise(new Vector2(FirstPersonAvatar.Position.X, FirstPersonAvatar.Position.Y), FirstPersonAvatar.Level - 1);
                        var targLimit = avatarIndoors ? 2 : 4;

                        ThirdPersonLimitDistance += (targLimit - ThirdPersonLimitDistance) * interpRate;
                    }

                    LastFP = true;
                }
                else
                {
                    LastFP = false;
                }
            }
        }

        private float GetWallLimitedDistance(World world, Vector3 basePos, Vector3 frontVec, float targetDistance)
        {
            const float wallMargin = 0.30f;

            var dir = -new Vector3(frontVec.X, frontVec.Z, frontVec.Y);
            dir.Normalize();
            var ray = new Ray(new Vector3(basePos.X, basePos.Z, basePos.Y) * 3, dir);

            var hit = WallRaycaster.RaycastMultifloor(ray, world.Architecture.Blueprint, (targetDistance + wallMargin) * 3);

            if (hit != null)
            {
                return Math.Clamp(hit.Value.Item1 / 3 - wallMargin, 0f, targetDistance);
            }

            return targetDistance;
        }

        private float GetFloorLimitedDistance(World world, Vector3 basePos, Vector3 frontVec, float targetDistance)
        {
            const float floorMargin = 0.15f;
            const float perAttempt = 0.25f;
            const float floorHeight = 2.95f;

            float attemptDistance = 0;
            var bp = world.Architecture.Blueprint;
            float lastFloorDist = basePos.Z - bp.InterpAltitudeWithSubworlds(basePos);

            float floorBottom = Math.Max(0, (float)Math.Floor(lastFloorDist / floorHeight)) * floorHeight;
            float min = floorBottom + floorMargin;
            float max = floorBottom + floorHeight - floorMargin;

            while (attemptDistance < targetDistance)
            {
                attemptDistance += perAttempt;

                var attempt = basePos - attemptDistance * frontVec;

                float floorDist = attempt.Z - bp.InterpAltitudeWithSubworlds(attempt);

                if (floorDist < min)
                {
                    // What percentage of the attempt distance passed the threshold, roughly?
                    float diff = (min - lastFloorDist) / (floorDist - lastFloorDist);

                    return attemptDistance - perAttempt * (1 - diff);
                }

                if (floorDist > max && bp.IsIndoorsPrecise(new Vector3(attempt.X, attempt.Y, basePos.Z)))
                {
                    // What percentage of the attempt distance passed the threshold, roughly?
                    float diff = (max - lastFloorDist) / (floorDist - lastFloorDist);

                    return attemptDistance - perAttempt * (1 - diff);
                }

                lastFloorDist = floorDist;
            }

            return targetDistance;
        }

        public override void PreDraw(World world)
        {
            if (FirstPersonAvatar != null)
            {
                if (Camera.FOV != 0.9f) Camera.FOV = 0.9f;
                var headPos = FirstPersonAvatar.GetHeadlinePos() * FirstPersonAvatar.Scale + FirstPersonAvatar.Position;

                headPos.Z += (0.25f * FirstPersonAvatar.Scale) / 3f;

                if (ThirdPersonDistance > 0)
                {
                    var originalHeadPos = headPos;
                    var tpPos = FirstPersonAvatar.Position + new Vector3(0, 0, FirstPersonAvatar.IsPet ? 0.65f : 1.77f) * FirstPersonAvatar.Scale;

                    var frontVec = GetCameraDirection();
                    frontVec.Normalize();
                    frontVec = new Vector3(frontVec.X, frontVec.Z, frontVec.Y);

                    var downVec = new Vector3(0, 0, 1);
                    var sideVec = Vector3.Cross(frontVec, downVec);

                    headPos = tpPos;
                    headPos -= sideVec * 0.27f + downVec * 0.17f;

                    var dist = GetWallLimitedDistance(world, headPos, frontVec, Math.Min(ThirdPersonLimitDistance, ThirdPersonDistance));
                    dist = GetFloorLimitedDistance(world, headPos, frontVec, dist);

                    ThirdPersonLimitDistance = dist;

                    headPos -= frontVec * dist;

                    if (ThirdPersonDistance < 0.5)
                    {
                        headPos = Vector3.Lerp(originalHeadPos, headPos, ThirdPersonDistance * 2);
                    }
                    // 
                }

                world.State.CenterTile = new Vector2(headPos.X, headPos.Y);
                FPCamHeight = headPos.Z * 3;
                InvalidateCamera(world.State);
            }
        }

        public void Inherit(CameraControllerDirect direct)
        {
            ThirdPersonDistance = direct.ThirdPersonDistance;
            ThirdPersonTargetDistance = direct.ThirdPersonTargetDistance;
            ThirdPersonLimitDistance = direct.ThirdPersonLimitDistance;

            LastWheel = direct.LastWheel;
        }

        public override void OnActive(ICameraController previous, World world)
        {
            base.OnActive(previous, world);

            if (previous is CameraControllerDirect direct)
            {
                Inherit(direct);
            }
        }
    }
}
