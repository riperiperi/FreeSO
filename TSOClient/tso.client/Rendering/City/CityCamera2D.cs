using FSO.Common.Rendering.Framework.Camera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework.Input;
using FSO.Common.Rendering.Framework;
using FSO.LotView;
using FSO.Client.Controllers;
using FSO.Common.Rendering.Framework.IO;

namespace FSO.Client.Rendering.City
{
    public class CityCamera2D : ICityCamera
    {
        public static float NEAR_ZOOM_SIZE = 288;
        public float m_WheelZoom;
        public float LotZoomProgress { get; set; } = 0;
        public float ZoomProgress { get; set; } //settable to avoid discontinuities
        public float m_LotZoomSize = 72 * 128; //near zoom, set by world
        public CityCameraCenter CenterCam { get; set; }

        public float FarUIFade
        {
            get { return ZoomProgress; }
        }

        public float LotSquish
        {
            get
            {
                return 1.5f / (0.5f + (float)(1.0 - ZoomProgress) / 2);
            }
        }

        public float FogMultiplier
        {
            get
            {
                return 1f;
            }
        }

        public float DepthBiasScale
        {
            get
            {
                return 0.01f;
            }
        }

        public bool HideUI
        {
            get
            {
                return false;
            }
        }

        public TerrainZoomMode Zoomed { get; set; } = TerrainZoomMode.Far;
        public float m_WheelZoomTarg = 0.5f;
        private int? m_LastWheelPos; //null if invalid, increments in 120 it seems.

        private Vector2 LastTargOff;
        public float m_ViewOffX, m_ViewOffY, m_TargVOffX, m_TargVOffY;
        private float m_ScrollSpeed;
        private Vector2 m_MouseStart;
        private bool WasRMBDown;

        public float GetIsoScale()
        {
            float ResScale = 768.0f / UIScreen.Current.ScreenHeight; //scales up the vertical height to match that of the target resolution (for the far view)
            float FisoScale = (float)(Math.Sqrt(0.5 * 0.5 * 2) / 5.10f) * ResScale; // is 5.10 on far zoom
            float ZisoScale = (float)Math.Sqrt(0.5 * 0.5 * 2) / (NEAR_ZOOM_SIZE * m_WheelZoom);  // currently set 144 to near zoom
            float LisoScale = (float)Math.Sqrt(0.5 * 0.5 * 2) / m_LotZoomSize;  // currently set 144 to near zoom

            float IsoScale = (1 - ZoomProgress) * FisoScale + (ZoomProgress) * ZisoScale;
            if (FSOEnvironment.Enable3D) return IsoScale;
            return (1 - LotZoomProgress) * IsoScale + LotZoomProgress * LisoScale;
        }

        public void MouseEvent(UIMouseEventType type, UpdateState state)
        {
            if (type == UIMouseEventType.MouseOut)
                m_LastWheelPos = null;
        }

        public float AspectRatioMultiplier
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public float FarPlane
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public float NearPlane
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Vector3 Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        private Matrix _Projection;
        public Matrix Projection
        {
            get
            {
                if (PDirty)
                {
                    _Projection = CalculateProjection();
                    PDirty = false;
                }
                return _Projection;
            }
        }

        public Vector2 ProjectionOrigin
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Vector3 Target
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Vector3 Translation
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Vector3 Up
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        private Matrix _View;
        public Matrix View
        {
            get
            {
                return CalculateView();
            }
        }

        public float Zoom
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public Matrix CalculateView()
        {
            var screen = UIScreen.Current;

            Matrix ViewMatrix = Matrix.Identity;

            ViewMatrix *= Matrix.CreateScale(new Vector3(1, 0.5f + (float)(1.0 - ZoomProgress) / 2, 1)); //makes world flatter in near view. This effect is present in the original, 
            //you just can't notice it as there is no zoom in animation. It also renders in true isometric... but that's an awful idea and makes lots look unusual when placed on flat tiles.

            ViewMatrix *= Matrix.CreateRotationY((45.0f / 180.0f) * (float)Math.PI);
            ViewMatrix *= Matrix.CreateRotationX((30.0f / 180.0f) * (float)Math.PI); //render in pseudo-isometric: http://en.wikipedia.org/wiki/Isometric_graphics_in_video_games_and_pixel_art
            ViewMatrix *= Matrix.CreateTranslation(new Vector3(-360f, 0f, -262f)); //move model to center of screen.
            return ViewMatrix;
        }

        public Matrix CalculateProjection()
        {
            var isoScale = GetIsoScale();
            var screen = UIScreen.Current;
            float HB = screen.ScreenWidth * isoScale;
            float VB = screen.ScreenHeight * isoScale;

            return Matrix.CreateOrthographicOffCenter(-HB + m_ViewOffX, HB + m_ViewOffX, -VB + m_ViewOffY, VB + m_ViewOffY, 0.1f, 524);
        }
        
        private bool PDirty = true;
        public void ProjectionDirty()
        {
            PDirty = true;
        }

        public Vector2 CalculateR()
        {
            Vector2 ReturnM = new Vector2(m_ViewOffX, -m_ViewOffY);
            ReturnM.Y = -2.0f * m_ViewOffY;
            float temp = ReturnM.X;
            double cos = Math.Cos((-45.0 / 180.0) * Math.PI);
            double sin = Math.Sin((-45.0 / 180.0) * Math.PI);
            ReturnM.X = (float)(cos * ReturnM.X + sin * ReturnM.Y);
            ReturnM.Y = (float)(cos * ReturnM.Y - sin * temp);
            ReturnM.X += 254.55844122715712f;
            ReturnM.Y += 254.55844122715712f;
            return ReturnM;
        }

        public Vector2 CalculateRShadow()
        {
            return CalculateR();
        }

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

                    switch (lotWorld.State.Zoom)
                    {
                        case WorldZoom.Near:
                            m_LotZoomSize = 72 * 128;
                            break;
                        case WorldZoom.Medium:
                            m_LotZoomSize = 72 * 64;
                            break;
                        case WorldZoom.Far:
                            m_LotZoomSize = 72 * 32;
                            break;
                    }

                    parent.LotPosition = new Vector3((float)(x + 1), elev / 12.0f, (float)(y + 0));

                    Vector3 scrollPos = Vector3.Transform(new Vector3((float)(x + 1) - tile.Y, elev / 12.0f, (float)(y + 0) + tile.X), View);
                    m_TargVOffX += (scrollPos.X - m_TargVOffX) / 3;
                    m_TargVOffY += (scrollPos.Y - m_TargVOffY) / 3;
                }
            }
        }

        public void Update(UpdateState state, Terrain city)
        {
            var screen = UIScreen.Current;

            if (Zoomed == TerrainZoomMode.Near)
            {
                if (m_LastWheelPos != null && Math.Abs(m_LastWheelPos.Value - state.MouseState.ScrollWheelValue) < 1000)
                    m_WheelZoomTarg = Math.Max(0.33f, Math.Min(1f, m_WheelZoomTarg - (m_LastWheelPos.Value - state.MouseState.ScrollWheelValue) / 1000f));
            }

            if (CenterCam != null)
            {
                Zoomed = TerrainZoomMode.Near;
                var c2d = CenterCam.Center;
                var c3d = new Vector3(c2d.X, city.InterpElevationAt(c2d), c2d.Y);
                var pos = Vector3.Transform(c3d, CalculateView());
                m_TargVOffX = pos.X;
                m_TargVOffY = pos.Y;
                m_WheelZoomTarg = 0.10f / (2 - CenterCam.Dist);
            }

            var m_MouseState = Mouse.GetState();
            if (m_MouseState.RightButton == ButtonState.Pressed && !WasRMBDown)
            {
                m_MouseStart = new Vector2(m_MouseState.X, m_MouseState.Y); //if middle mouse button activated, record where we started pressing it (to use for panning)
            }

            LastTargOff = new Vector2(m_TargVOffX, m_TargVOffY);


            var rScale = 60f / FSOEnvironment.RefreshRate;
            if (Zoomed != TerrainZoomMode.Far) ZoomProgress += (1.0f - ZoomProgress) * (float)(1 - Math.Pow(4 / 5.0f, rScale));
            if (Zoomed == TerrainZoomMode.Near)
            {
                bool Triggered = false;

                var m_MouseMove = (m_MouseState.RightButton == ButtonState.Pressed);
                if (m_MouseMove)
                {
                    ClearCenter();
                    m_TargVOffX += (m_MouseState.X - m_MouseStart.X) * (float)(1 - Math.Pow(999 / 1000.0f, rScale)); //move by fraction of distance between the mouse and where it started in both axis
                    m_TargVOffY -= (m_MouseState.Y - m_MouseStart.Y) * (float)(1 - Math.Pow(999 / 1000.0f, rScale));

                    /*var dir = Math.Round((Math.Atan2(m_MouseStart.X - m_MouseState.Y,
                        m_MouseState.X - m_MouseStart.X) / Math.PI) * 4) + 4;
                    ChangeCursor(dir);*/
                }
                else if (GlobalSettings.Default.EdgeScroll && state.ProcessMouseEvents) //edge scroll check - do this even if mouse events are blocked
                {
                    if (m_MouseState.X > screen.ScreenWidth - 32)
                    {
                        Triggered = true;
                        m_TargVOffX += m_ScrollSpeed * rScale;
                        CursorManager.INSTANCE.SetCursor(CursorType.ArrowRight);
                    }
                    if (m_MouseState.X < 32)
                    {
                        Triggered = true;
                        m_TargVOffX -= m_ScrollSpeed * rScale;
                        CursorManager.INSTANCE.SetCursor(CursorType.ArrowLeft);
                    }
                    if (m_MouseState.Y > screen.ScreenHeight - 32)
                    {
                        Triggered = true;
                        m_TargVOffY -= m_ScrollSpeed * rScale;
                        CursorManager.INSTANCE.SetCursor(CursorType.ArrowDown);
                    }
                    if (m_MouseState.Y < 32)
                    {
                        Triggered = true;
                        m_TargVOffY += m_ScrollSpeed * rScale;
                        CursorManager.INSTANCE.SetCursor(CursorType.ArrowUp);
                    }

                    if (!Triggered)
                    {
                        m_ScrollSpeed = 0.1f; //not scrolling. Reset speed, set default cursor.
                        CursorManager.INSTANCE.SetCursor(CursorType.Normal);
                    }
                    else
                    {
                        ClearCenter();
                        m_ScrollSpeed += 0.005f; //if edge scrolling make the speed increase the longer the mouse is at the edge.
                    }
                }

                m_TargVOffX = Math.Max(-135, Math.Min(m_TargVOffX, 138)); //maximum offsets for zoomed camera. Need adjusting for other screen sizes...
                m_TargVOffY = Math.Max(-100, Math.Min(m_TargVOffY, 103));
            }
            else if (Zoomed == TerrainZoomMode.Far && LotZoomProgress < 0.3)
                ZoomProgress += (0 - ZoomProgress) * (float)(1 - Math.Pow(4 / 5.0f, rScale)); //zoom progress interpolation. Isn't very fixed but it's a nice gradiation.

            //lot zoom.
            if (Zoomed == TerrainZoomMode.Lot && ZoomProgress > 0.995)
            {
                LotZoomProgress += (1.0f - LotZoomProgress) * (float)(1 - Math.Pow(9 / 10.0f, rScale));
            }
            else LotZoomProgress += (0 - LotZoomProgress) * (float)(1 - Math.Pow(9 / 10.0f, rScale));

            m_WheelZoom += (m_WheelZoomTarg - m_WheelZoom) * (float)(1 - Math.Pow(9 / 10.0f, rScale));
            m_LastWheelPos = state.MouseState.ScrollWheelValue;
            m_ViewOffX = (m_TargVOffX) * ZoomProgress;
            m_ViewOffY = (m_TargVOffY) * ZoomProgress;
            WasRMBDown = m_MouseState.RightButton == ButtonState.Pressed;

            PDirty = true;
        }

        public void CenterCamera(CityCameraCenter center)
        {
            if (Zoomed == TerrainZoomMode.Far)
            {
                m_WheelZoom = 0.10f / (2 - center.Dist);
            }
            CenterCam = center;
        }

        public void ClearCenter()
        {
            CenterCam = null;
        }
    }
}
