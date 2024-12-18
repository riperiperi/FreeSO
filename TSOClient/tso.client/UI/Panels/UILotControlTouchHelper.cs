using FSO.Client.UI.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using FSO.Common;
using FSO.HIT;
using FSO.Client.UI.Model;
using FSO.Common.Utils;
using FSO.Client.UI.Screens;
using FSO.LotView.Model;

namespace FSO.Client.UI.Panels
{
    public interface ITouchable
    {
        void Click(Point pt, UpdateState state);
        void Scroll(Vector2 vec);
        float TargetZoom { get; set; }
        float BBScale { get; }
        I3DRotate Rotate { get; }
        bool TVisible { get; }
        bool UserModZoom { get; set; }
        void ClearCenter();
    }

    public class UILotControlTouchHelper : UIElement
    {
        private ITouchable Master;
        public HashSet<int> MiceDown = new HashSet<int>();
        private int UpdatesSinceDraw;
        private Vector2 ScrollVelocity;
        private Vector2 LastValidScrollVelocity;
        private int MiceDownTimer;
        private Point TapPoint;
        private const int TAP_TIMER = 8; //current time for a tap to register is a third of a second. if
        private const int TAP_POINT_DIST = 30; //current px distance for a tap to move to become a scroll. TODO: dpi scale?
        private List<Vector2> ScrollVelocityHistory = new List<Vector2>();

        private int Mode = -1; //-1: none, 0: touch, 1: scroll, 2: zoomscroll, 3: touched
        //you can't revert back to touch after activating it
        private Vector2 BaseVector;
        private float StartScale;
        private float RotateAngle;
        //for 3D rotate
        private float? LastAngleX;

        private float[] SnapZooms =
        {
            0.25f,
            0.5f,
            1f
        };

        public UILotControlTouchHelper(ITouchable master)
        {
            Master = master;
        }

        private Point GetScaledPoint(Point TapPoint)
        {
            var screenMiddle = new Point(
                (int)(GameFacade.Screens.CurrentUIScreen.ScreenWidth / (2 / FSOEnvironment.DPIScaleFactor)),
                (int)(GameFacade.Screens.CurrentUIScreen.ScreenHeight / (2 / FSOEnvironment.DPIScaleFactor))
                );
            return ((TapPoint - screenMiddle).ToVector2() / Master.BBScale).ToPoint() + screenMiddle;
        }

        private int LastMouseWheel;
        private bool ScrollWheelInvalid = true;
        private int ZoomFreezeTime;

        public float MinZoom = -0.75f;
        public float MaxZoom = 2f;
        public bool _3D;

        public override void Update(UpdateState state)
        {
            var _3d = _3D;
            MinZoom = _3d ? -0.75f : 0.25f;
            base.Update(state);
            bool rotated = false;

            var invalidNow = !Master.TVisible || !UIScreen.Current.Visible || !state.ProcessMouseEvents || state.MouseState.ScrollWheelValue == 0;
            if (invalidNow) ScrollWheelInvalid = invalidNow;

            if (!FSOEnvironment.SoftwareKeyboard)
            {
                //if (!state.WindowFocused) ScrollWheelInvalid = true;
                if (ScrollWheelInvalid)
                {
                    LastMouseWheel = state.MouseState.ScrollWheelValue;
                    //ScrollWheelInvalid = false;
                }
                else if (state.WindowFocused && state.MouseState.ScrollWheelValue != LastMouseWheel)
                {
                    var diff = state.MouseState.ScrollWheelValue - LastMouseWheel;
                    Master.TargetZoom = Master.TargetZoom + diff / 1600f;
                    LastMouseWheel = state.MouseState.ScrollWheelValue;
                    Master.TargetZoom = Math.Max(MinZoom, Math.Min(Master.TargetZoom, MaxZoom));
                    Master.UserModZoom = true;
                    ZoomFreezeTime = (10 * FSOEnvironment.RefreshRate) / 60;
                    Master.ClearCenter();
                }
                ScrollWheelInvalid = invalidNow;
            }

            MiceDown = new HashSet<int>(MiceDown.Intersect(state.MouseStates.Select(x => x.ID)));

            int transitionTo = -2;
            if (MiceDown.Count == 0)
            {
                if (Mode != -1) transitionTo = -1;
            }
            else if (MiceDown.Count == 1)
            {
                if (Mode == -1) transitionTo = 0;
                if (Mode == 2) transitionTo = 1;
            }
            else if (MiceDown.Count >= 2)
            {
                //cannot possibly be a touch
                if (Mode < 2) transitionTo = 2;
                if (Mode == -1) Mode = 0;
            }

            bool scrollOnHold = FSOEnvironment.SoftwareKeyboard;

            switch (Mode)
            {
                case -1:
                    if (transitionTo == 0)
                    {
                        var mouse = state.MouseStates.FirstOrDefault(x => x.ID == MiceDown.First());
                        if (mouse != null)
                        {
                            TapPoint = new Point(mouse.MouseState.X, mouse.MouseState.Y);
                            MiceDownTimer = 0;
                            Mode = 0;
                        }
                    }
                    break;
                case 0:
                case 1:
                    if (transitionTo == 2)
                    {
                        //register the first distance between the two taps
                        var m1 = state.MouseStates.FirstOrDefault(x => x.ID == MiceDown.ElementAt(0));
                        var m2 = state.MouseStates.FirstOrDefault(x => x.ID == MiceDown.ElementAt(1));
                        BaseVector = (new Point(m2.MouseState.X, m2.MouseState.Y) - new Point(m1.MouseState.X, m1.MouseState.Y)).ToVector2();
                        StartScale = Master.TargetZoom;
                        if (_3d) LastAngleX = null;

                        //scroll anchor should change to center of two touches without drastically changing scroll
                        TapPoint = (new Point(m2.MouseState.X / 2, m2.MouseState.Y / 2) + new Point(m1.MouseState.X / 2, m1.MouseState.Y / 2));

                        Mode = 2;
                    }
                    else
                    {
                        if (Mode == 0)
                        {
                            ScrollVelocity = new Vector2();
                            if (transitionTo == -1)
                            {
                                Mode = -1;
                                Master.Click(GetScaledPoint(TapPoint), state);
                            }
                            else
                            {
                                var mouse = state.MouseStates.FirstOrDefault(x => x.ID == MiceDown.First());
                                var time = FSOEnvironment.SoftwareKeyboard ? ((TAP_TIMER * FSOEnvironment.RefreshRate) / 60) : 0;
                                if ((TapPoint - new Point(mouse.MouseState.X, mouse.MouseState.Y)).ToVector2().Length() > TAP_POINT_DIST)
                                {
                                    Mode = 1; //become a scroll
                                }
                                else if (scrollOnHold && ++MiceDownTimer > time)
                                {
                                    Mode = 3;
                                    Master.Click(GetScaledPoint(TapPoint), state);
                                }
                            }
                        }
                        if (Mode == 1)
                        {
                            if (transitionTo == -1)
                            {
                                //release our scroll velocity
                                Mode = -1;
                                if (!scrollOnHold)
                                {
                                    Master.Click(GetScaledPoint(TapPoint), state);
                                }
                            }
                            else
                            {

                                var mouse = state.MouseStates.FirstOrDefault(x => x.ID == MiceDown.First());
                                var newTap = new Point(mouse.MouseState.X, mouse.MouseState.Y);

                                if (scrollOnHold)
                                {
                                    ScrollVelocity = (newTap - TapPoint).ToVector2();
                                }

                                TapPoint = newTap;
                            }
                        }
                    }
                    break;
                case 2:
                    if (transitionTo != -2)
                    {
                        //release rotation gesture. todo.
                    }
                    if (transitionTo == 1)
                    {
                        //go back to being a normal scroll
                        //again, anchor should change to single point without drastic scroll change
                        var mouse = state.MouseStates.FirstOrDefault(x => x.ID == MiceDown.First());
                        TapPoint = new Point(mouse.MouseState.X, mouse.MouseState.Y);
                        Mode = 1;
                    }
                    else if (transitionTo == -1)
                    {
                        Mode = -1;
                    }
                    else if (transitionTo == -2)
                    {
                        var m1 = state.MouseStates.FirstOrDefault(x => x.ID == MiceDown.ElementAt(0));
                        var m2 = state.MouseStates.FirstOrDefault(x => x.ID == MiceDown.ElementAt(1));
                        var vector = (new Point(m2.MouseState.X, m2.MouseState.Y) - new Point(m1.MouseState.X, m1.MouseState.Y)).ToVector2();
                        var newTap = (new Point(m2.MouseState.X / 2, m2.MouseState.Y / 2) + new Point(m1.MouseState.X / 2, m1.MouseState.Y / 2));
                        ScrollVelocity = (newTap - TapPoint).ToVector2();
                        TapPoint = newTap;

                        Master.TargetZoom = (vector.Length() / BaseVector.Length()) * StartScale;
                        Master.UserModZoom = true;

                        //clockwise if dot product b against a rotated 90 degrees clockwise is positive
                        var a = BaseVector;
                        var b = vector;
                        a.Normalize(); b.Normalize();
                        var clockwise = ((-a.Y) * b.X + a.X * b.Y) > 0;
                        var angle = (float)Math.Acos(Vector2.Dot(a, b));
                        RotateAngle = (clockwise) ? angle : -angle;

                        if (_3d)
                        {
                            var rcState = Master.Rotate;
                            if (LastAngleX != null)
                            {
                                float rot = rcState.RotationX - (float)DirectionUtils.Difference(RotateAngle, LastAngleX.Value);
                                if (!float.IsNaN(rot))
                                    rcState.RotationX = rot;
                            }
                            LastAngleX = RotateAngle;
                            rcState.RotationY += ScrollVelocity.Y / 200;
                            ScrollVelocity = Vector2.Zero;
                        }
                        else
                        {
                            if (Math.Abs(RotateAngle) > Math.PI / 8) Master.TargetZoom = StartScale;
                        }
                    }
                    break;
                case 3:
                    if (transitionTo == -1) Mode = -1;
                    break;
            }
            if (Mode != 2 && RotateAngle != 0 && !_3d)
            {
                if (Math.Abs(RotateAngle) > Math.PI / 4)
                {
                    //confirmed
                    var screen = ((IGameScreen)GameFacade.Screens.CurrentUIScreen);
                    if (RotateAngle > 0)
                    {
                        screen.Rotation = (screen.Rotation + 1) % 4;
                    }
                    else
                    {
                        screen.Rotation = (screen.Rotation + 3) % 4;
                    }

                    HITVM.Get().PlaySoundEvent(UISounds.ObjectRotate);
                    rotated = true;
                }
                RotateAngle = 0;
            }
            ScrollVelocityHistory.Insert(0, ScrollVelocity);
            if (ScrollVelocityHistory.Count > 5) ScrollVelocityHistory.RemoveAt(ScrollVelocityHistory.Count - 1);
            if (transitionTo == -1)
            {
                //ScrollVelocity = LastValidScrollVelocity / UpdatesSinceDraw;
                if (ScrollVelocityHistory.Count > 1)
                {
                    int total = 0;
                    ScrollVelocity = new Vector2();
                    for (int i = 1; i < ScrollVelocityHistory.Count; i++)
                    {
                        total++;
                        ScrollVelocity += ScrollVelocityHistory[i];
                    }
                    ScrollVelocity /= total;
                }
                ScrollVelocityHistory.Clear();
            }

            if (Mode == -1)
            {
                ScrollVelocity *= 0.95f * Math.Min(ScrollVelocity.Length(), 1);
                if (Master.TargetZoom < 1.25f && ZoomFreezeTime == 0 && !_3d)
                {
                    Master.UserModZoom = true;
                    float snapZoom = 1f;
                    float dist = 200f;
                    foreach (var snappable in SnapZooms)
                    {
                        var newDist = Math.Abs(Master.TargetZoom - snappable);
                        if (newDist < dist)
                        {
                            dist = newDist;
                            snapZoom = snappable;
                        }
                    }
                    if (dist > 0)
                    {
                        var move = snapZoom - Master.TargetZoom;
                        if (move != 0)
                        {
                            if (move > 0) Master.TargetZoom += 0.01f;
                            else Master.TargetZoom -= 0.01f;
                        }
                        if (move * (snapZoom - Master.TargetZoom) < 0) Master.TargetZoom = snapZoom;
                    }
                }
                if (ZoomFreezeTime > 0) ZoomFreezeTime--;
            }
            if (ScrollVelocity.Length() > 0.001f) Master.Scroll(-ScrollVelocity / (Master.TargetZoom * 128));

            UpdatesSinceDraw++;
        }

        public override void Draw(UISpriteBatch batch)
        {
            //todo: rotation graphic
            UpdatesSinceDraw = 1;
        }
    }
}
