using FSO.Common.Rendering.Framework.Camera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.Controllers;
using FSO.Common.Rendering.Framework.Model;
using FSO.LotView;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common;
using FSO.Common.Rendering.Framework;
using Microsoft.Xna.Framework.Input;
using FSO.Client.UI.Framework;
using FSO.LotView.RC;
using FSO.Client.UI.Panels;
using FSO.Common.Rendering.Framework.IO;

namespace FSO.Client.Rendering.City
{
    public class CityCamera3D : BasicCamera, ICityCamera, I3DRotate, ITouchable
    {
        public Vector2 CenterTile = new Vector2(184, 328);
        private Point LastMouse;
        private bool MouseWasDown;
        private UILotControlTouchHelper Touch;
        public CityCameraCenter CenterCam { get; set; }

        public float FarUIFade
        {
            get { return Math.Max(0, Math.Min(1, 6.5f - Zoom3D)); }
        }

        public float LotZoomProgress
        {
            get; set;
        }

        public float LotSquish
        {
            get
            {
                return 1f / (0.33f + (float)(1.0 - LotZoomProgress) / 1.5f);
            }
        }

        public float DepthBiasScale
        {
            get
            {
                return 1f;
            }
        }

        public float FogMultiplier
        {
            get
            {
                float m;
                if (HideUI) m = Math.Min(1, Math.Max(1, FPCamHeight) / 50f);
                else m = (Zoom3D + 1f) / 21f;
                return 1 - (float)Math.Pow(1 - m, 1 / 3f);
            }
        }

        public bool HideUI
        {
            get
            {
                return CameraMode;
            }
        }

        private TerrainZoomMode _Zoomed;
        public TerrainZoomMode Zoomed
        {
            get
            {
                return _Zoomed;
            }

            set
            {
                if (_Zoomed == value) return;
                if (value == TerrainZoomMode.Far) TargetZoom = 0.25f;
                else if (_Zoomed == TerrainZoomMode.Lot)
                {
                }
                else TargetZoom = 1.65f;
                _Zoomed = value;
            }
        }

        public float ZoomProgress
        {
            get
            {
                return 0f;
            }
            set
            {

            }
        }

        public CityCamera3D() : base(GameFacade.GraphicsDevice, new Vector3(256, 0, 256), new Vector3(256, 0, 256), Vector3.Up)
        {
            NearPlane = 0.25f;
            Touch = new UILotControlTouchHelper(this);
            Touch.MinZoom = 0.25f;
            Touch.MaxZoom = 2.5f;
        }

        public Vector2 CalculateR()
        {
            return new Vector2(Target.X, Target.Z);
        }

        public Vector2 CalculateRShadow()
        {
            return new Vector2(256, 256);
        }

        public float GetIsoScale()
        {
            return 1f;
        }

        private float TargRX;
        private float TargRY;
        public void InheritPosition(Terrain parent, World lotWorld, CoreGameScreenController controller)
        {
            if (controller != null)
            {
                var id = controller.GetCurrentLotID();
                if (id != 0)
                {
                    //center on this lot, with the given camera offset
                    var x = id >> 16;
                    var y = id & 0xFFFF;

                    if (x >= 512 || y >= 512)
                    {
                        x = 255;
                        y = 255;
                    }

                    float elev = parent.GetElevationAt((int)x, (int)y);

                    var tile = (lotWorld.State.CenterTile - new Vector2(2, 2)) / 72; //72 is the base lot size

                    parent.LotPosition = new Vector3((float)(x + 1), elev / 12.0f, (float)(y + 0));

                    CenterTile += (new Vector2((float)(x + 1) - tile.Y, (float)(y + 0) + tile.X) - CenterTile) * (1f - (float)Math.Pow(0.975f, 60f / FSOEnvironment.RefreshRate));
                    TargRX = (((LotView.RC.WorldStateRC)lotWorld.State).RotationX - (float)Math.PI / 2);
                    TargRY = (((LotView.RC.WorldStateRC)lotWorld.State).RotationY);

                    if (LotZoomProgress == 0)
                    {
                        ((LotView.RC.WorldStateRC)lotWorld.State).RotationX = RotationX + (float)Math.PI / 2;
                        (((LotView.RC.WorldStateRC)lotWorld.State).RotationY) = (RotationY - 1.10f) / (1.10f / (float)(Math.PI / 2));
                    }
                    else if (LotZoomProgress != 1)
                    {
                        RotationX += (TargRX - RotationX) / 10;
                        RotationY += (TargRY - RotationY) / 10;
                    }
                    else
                    {
                        RotationX = TargRX;
                        RotationY = TargRY * (1.10f / (float)(Math.PI / 2)) + 1.10f;
                    }

                    TargetZoom += (1.8f - TargetZoom) / 3;
                }
            }
        }

        public void MouseEvent(UIMouseEventType type, UpdateState state)
        {
            if (type == UIMouseEventType.MouseDown)
            {
                Touch.MiceDown.Add(state.CurrentMouseID);
            }
            else if (type == UIMouseEventType.MouseUp)
            {
                Touch.MiceDown.Remove(state.CurrentMouseID);
            }
            else if (type == UIMouseEventType.MouseOut)
            {
                LastWheelPos = null;
            }
        }

        public Vector2[] GetScrollBasis(bool multiplied)
        {
            var mat = Matrix.CreateRotationZ(-RotationX);
            var z = multiplied ? ((0.25f + Zoom3D)) : 1;
            return new Vector2[] {
                Vector2.Transform(new Vector2(0, -1), mat) * z,
                Vector2.Transform(new Vector2(1, 0), mat) * z
            };
        }

        public float TargetZoom { get; set; } = 0.25f;
        public bool UserModZoom { get; set; }
        public int? LastWheelPos;

        private bool RMBScroll;
        private int RMBScrollX;
        private int RMBScrollY;
        private bool LastFP;
        private Vector3 FPCamVelocity;
        private Terrain Parent;

        public void Update(UpdateState state, Terrain city)
        {
            Focused = state.WindowFocused;
            Parent = city;
            Touch.Update(state);
            var inCity = Zoomed != TerrainZoomMode.Lot;
            if (TargetZoom > 2.3f)
            {
                //to lot view
                var screen = (GameFacade.Screens.CurrentUIScreen as UI.Screens.CoreGameScreen);
                if (screen.vm != null && screen.vm.Ready && screen.WorldLoaded)
                {
                    var controller = screen.FindController<CoreGameScreenController>();
                    var id = controller.GetCurrentLotID();

                    var x = id >> 16;
                    var y = id & 0xFFFF;

                    if ((new Vector2(x, y) - new Vector2(Target.X, Target.Z)).Length() < 4f)
                    {
                        screen.ZoomLevel = 3;
                        city.InheritPosition(screen.vm.Context.World, controller);
                    }
                }
            }
            if (TargetZoom > 2f && inCity) TargetZoom -= (TargetZoom - 2f) * (1f - (float)Math.Pow(0.975f, 60f / FSOEnvironment.RefreshRate));
            Zoom3D += ((12f - (TargetZoom - 0.25f) * 6.8571428571428571428571428571429f) - Zoom3D) / 10;

            /*
             * replaced by touch helper
             * 
            if (LastWheelPos != null && state.WindowFocused && state.MouseState.ScrollWheelValue != 0 && Zoomed != TerrainZoomMode.Lot) {
                var diff = state.MouseState.ScrollWheelValue - LastWheelPos.Value;
                UserModZoom = diff != 0;
                TargetZoom = TargetZoom + diff / 1600f;
                TargetZoom = Math.Max(0.25f, Math.Min(TargetZoom, 2.5f));
            }
            if (state.WindowFocused)
            {
                LastWheelPos = state.MouseState.ScrollWheelValue;
            } else
            {
                LastWheelPos = null;
            }
            */

            //rmb scroll
            if (state.MouseState.RightButton == ButtonState.Pressed)
            {
                if (!RMBScroll)
                {
                    RMBScroll = true;
                    state.InputManager.SetFocus(null);
                    RMBScrollX = state.MouseState.X;
                    RMBScrollY = state.MouseState.Y;
                }
            }
            else
            {
                RMBScroll = false;
                if (inCity) GameFacade.Cursor.SetCursor(CursorType.Normal);
            }

            if (RMBScroll && inCity)
            {
                ClearCenter();
                Vector2 scrollBy = new Vector2();
                if (state.TouchMode)
                {
                    scrollBy = new Vector2(RMBScrollX - state.MouseState.X, RMBScrollY - state.MouseState.Y);
                    RMBScrollX = state.MouseState.X;
                    RMBScrollY = state.MouseState.Y;
                    scrollBy /= 128f;
                    scrollBy /= FSOEnvironment.DPIScaleFactor;
                }
                else
                {
                    scrollBy = new Vector2(state.MouseState.X - RMBScrollX, state.MouseState.Y - RMBScrollY);
                    scrollBy *= 0.0005f;

                    var angle = (Math.Atan2(state.MouseState.X - RMBScrollX, (RMBScrollY - state.MouseState.Y) * 2) / Math.PI) * 4;
                    angle += 8;
                    angle %= 8;

                    CursorType type = CursorType.ArrowUp;
                    switch ((int)Math.Round(angle))
                    {
                        case 0: type = CursorType.ArrowUp; break;
                        case 1: type = CursorType.ArrowUpRight; break;
                        case 2: type = CursorType.ArrowRight; break;
                        case 3: type = CursorType.ArrowDownRight; break;
                        case 4: type = CursorType.ArrowDown; break;
                        case 5: type = CursorType.ArrowDownLeft; break;
                        case 6: type = CursorType.ArrowLeft; break;
                        case 7: type = CursorType.ArrowUpLeft; break;
                    }
                    GameFacade.Cursor.SetCursor(type);
                }
                Scroll(scrollBy * (60f / FSOEnvironment.RefreshRate), true);
            }

            //get the camera height.

            float terrainHeight = 0;

            var relative = ComputeCenterRelative();
            terrainHeight = (city.InterpElevationAt(CenterTile));
            var targHeight = terrainHeight;
            var heightAtCam = city.InterpElevationAt(new Vector2(Position.X, Position.Z));
            if (relative.Y + targHeight < heightAtCam + 0.5f) targHeight = (heightAtCam + 0.5f) - relative.Y;
            //targHeight = Math.Max(heightAtCam, terrainHeight);
            CamHeight += (targHeight - CamHeight) * (1f - (float)Math.Pow(0.8f, 60f / FSOEnvironment.RefreshRate));

            if (inCity && state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Tab) && !state.AltDown)
            {
                CameraMode = !CameraMode;
            }
            if (!inCity && CameraMode)
            {
                CameraMode = false;
            }

            if (CameraMode)
            {
                if (state.WindowFocused)
                {
                    var mx = (int)UIScreen.Current.ScreenWidth / 2;
                    var my = (int)UIScreen.Current.ScreenHeight / 2;

                    var mpos = state.MouseState.Position;
                    if (LastFP)
                    {
                        RotationX -= (mpos.X - mx) / 500f;
                        RotationY += (mpos.Y - my) / 500f;
                    }
                    Mouse.SetPosition(mx, my);

                    var speed = (state.KeyboardState.IsKeyDown(Keys.LeftShift)) ? 1.5f : 0.5f;
                    speed *= 1 + FPCamHeight / 10f;

                    if (state.KeyboardState.IsKeyDown(Keys.W))
                        FPCamVelocity.Z -= speed;
                    if (state.KeyboardState.IsKeyDown(Keys.S))
                        FPCamVelocity.Z += speed;
                    if (state.KeyboardState.IsKeyDown(Keys.A))
                        FPCamVelocity.X -= speed;
                    if (state.KeyboardState.IsKeyDown(Keys.D))
                        FPCamVelocity.X += speed;
                    if (state.KeyboardState.IsKeyDown(Keys.Q))
                        FPCamVelocity.Y -= speed / 2;
                    if (state.KeyboardState.IsKeyDown(Keys.E))
                        FPCamVelocity.Y += speed / 2;
                    LastFP = true;
                }
                else
                {
                    LastFP = false;
                }

                Scroll(new Vector2(FPCamVelocity.X / FSOEnvironment.RefreshRate, FPCamVelocity.Z / FSOEnvironment.RefreshRate), false);
                FPCamHeight = Math.Min(600, Math.Max((terrainHeight - CamHeight) - 0.25f, FPCamHeight + (FPCamVelocity.Y * 3) / FSOEnvironment.RefreshRate));
                for (int i = 0; i < FSOEnvironment.RefreshRate / 60; i++)
                    FPCamVelocity *= 0.9f;


                if (_Zoomed != TerrainZoomMode.Lot)
                {
                    var zoom = (FPCamHeight > 45f) ? TerrainZoomMode.Far : TerrainZoomMode.Near;
                    if (_Zoomed != zoom)
                    {
                        _Zoomed = zoom;
                    }
                }
            }
            else
            {
                LastFP = false;
                var md = state.MouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;

                if (_Zoomed != TerrainZoomMode.Lot && UserModZoom)
                {
                    var zoom = (Zoom3D > 4.5f) ? TerrainZoomMode.Far : TerrainZoomMode.Near;
                    if (_Zoomed != zoom)
                    {
                        _Zoomed = zoom;
                    }
                }

                if (MouseWasDown)
                {
                    var mpos = state.MouseState.Position;
                    RotationX += (mpos.X - LastMouse.X) / 250f;
                    RotationY += (mpos.Y - LastMouse.Y) / 150f;
                    ClearCenter();
                }

                UserModZoom = false;
                if (md)
                {
                    LastMouse = state.MouseState.Position;
                }
                MouseWasDown = md;
            }

            var rScale = 60f / FSOEnvironment.RefreshRate;
            //lot zoom.
            if (Zoomed == TerrainZoomMode.Lot)
            {
                if (LotZoomProgress <= 0) LotZoomProgress = 0.0001f;
                LotZoomProgress -= (0 - LotZoomProgress) * (float)(1 - Math.Pow(8f / 10.0f, rScale));
                LotZoomProgress = Math.Min(1, LotZoomProgress);
            }
            else LotZoomProgress += (0 - LotZoomProgress) * (float)(1 - Math.Pow(8f / 10.0f, rScale));

            if (CenterCam != null)
            {
                CenterTile += (CenterCam.Center - CenterTile) * (float)(1 - Math.Pow(9f / 10.0f, rScale));
                TargetZoom += (CenterCam.Dist - TargetZoom) * (float)(1 - Math.Pow(9f / 10.0f, rScale));
                RotationY += (CenterCam.YAngle - RotationY) * (float)(1 - Math.Pow(9f / 10.0f, rScale));
                RotationX += 0.2f / FSOEnvironment.RefreshRate;
                if (TargetZoom > 0.75f && (CenterCam.Center - CenterTile).Length() < 5f) _Zoomed = TerrainZoomMode.Near;
                else _Zoomed = TerrainZoomMode.Far;
                InvalidateCamera();
            }
        }

        public void Scroll(Vector2 dir, bool multiply)
        {
            var basis = GetScrollBasis(multiply);
            CenterTile += dir.X * basis[0] + dir.Y * basis[1];
            LimitCenter();
            InvalidateCamera();
        }

        public void LimitCenter()
        {
            var trans = new Vector2((CenterTile.X + CenterTile.Y) / 2, (CenterTile.Y - CenterTile.X) / 2);

            trans.X = Math.Max(153.5f, Math.Min(358.5f, trans.X));
            trans.Y = Math.Max(-152, Math.Min(152, trans.Y));

            CenterTile = new Vector2(trans.X - trans.Y, trans.X + trans.Y);
        }

        private float _RotationX = -(float)(Math.PI * 3 / 4);
        private float _RotationY = 0f;
        private float _Zoom3D = 3.7f;

        public float RotationX
        {
            get { return _RotationX; }
            set { _RotationX = value; InvalidateCamera(); }
        }

        public float RotationY
        {
            get { return _RotationY; }
            set { _RotationY = (float)Math.Min(Math.PI, Math.Max(0, value)); InvalidateCamera(); }
        }

        public float Zoom3D
        {
            get { return _Zoom3D; }
            set { value = Math.Min(100, Math.Max(0, value)); _Zoom3D = value; InvalidateCamera(); }
        }

        private float _CamHeight;
        public float CamHeight
        {
            get
            {
                return _CamHeight;
            }
            set
            {
                _CamHeight = value; InvalidateCamera();
            }
        }

        public float FPCamHeight;
        private float SavedYRot;

        private bool _CameraMode;
        private bool _SwitchingMode;
        public bool CameraMode
        {
            get
            {
                return _CameraMode;
            }
            set
            {
                _SwitchingMode = true;
                if (value != CameraMode)
                {

                    if (value)
                    {
                        //switch into first person
                        var relative = ComputeCenterRelative();
                        SavedYRot = _RotationY;
                        var relNorm = relative;
                        relNorm.Normalize();
                        var rotY = (float)Math.Acos(Vector3.Dot(new Vector3(0, 1, 0), -relNorm));
                        //var rotY = (float)((1 - Math.Cos(_RotationY)) * Math.PI * 0.245f);
                        _RotationY = rotY;// - (float)Math.PI/2;
                        CenterTile += new Vector2(relative.X, relative.Z);
                        FPCamHeight = relative.Y;
                    }
                    else
                    {
                        _RotationY = SavedYRot;
                        var relative = ComputeCenterRelative();
                        CenterTile -= new Vector2(relative.X, relative.Z);

                        _CamHeight -= relative.Y - FPCamHeight;

                    }
                }
                _SwitchingMode = false;
                _CameraMode = value;
                InvalidateCamera();
            }
        }

        public float BBScale
        {
            get
            {
                return 1;
            }
        }

        public I3DRotate Rotate
        {
            get
            {
                return this;
            }
        }

        public bool TVisible
        {
            get
            {
                return Focused;
            }
        }

        public bool Focused;

        public void InvalidateCamera()
        {
            if (_SwitchingMode) return;
            var baseHeight = CamHeight;
            if (CameraMode)
            {
                //if (FixedCam) return;
                Position = new Vector3(CenterTile.X, baseHeight + 0.5f + FPCamHeight, CenterTile.Y);

                var mat = Matrix.CreateRotationZ((_RotationY - (float)Math.PI / 2) * 0.99f) * Matrix.CreateRotationY(_RotationX);
                Target = Position + Vector3.Transform(new Vector3(-10, 0, 0), mat);
            }
            else
            {
                Target = new Vector3(CenterTile.X, baseHeight + 0.5f, CenterTile.Y);
                Position = Target + ComputeCenterRelative();
            }
        }

        public Vector3 ComputeCenterRelative()
        {
            var mat = Matrix.CreateRotationY(_RotationX);

            var rotY = (float)((1 - Math.Cos(_RotationY)) * Math.PI * 0.245f);

            var panMat = Matrix.CreateRotationZ(rotY);
            var panMatFar = Matrix.CreateRotationZ(rotY / 2);
            var z = Zoom3D * Zoom3D;
            var baseDist = 3.5f;
            if (TargetZoom > 2f) baseDist -= (TargetZoom - 2) * 2;

            return Vector3.Transform(
                Vector3.Transform(new Vector3(baseDist, 0, 0), panMat) +
                Vector3.Transform(new Vector3(z * 1.30f, z * 1f, 0), panMatFar)
                , mat);

        }

        public void Click(Point pt, UpdateState state)
        {
            Parent?.Click(pt, state);
        }

        public void Scroll(Vector2 vec)
        {
            Scroll(vec, true);
            ClearCenter();
        }

        public void CenterCamera(CityCameraCenter center)
        {
            center.RotAngle = RotationX;
            CenterCam = center;
        }

        public void ClearCenter()
        {
            CenterCam = null;
        }
    }
}
