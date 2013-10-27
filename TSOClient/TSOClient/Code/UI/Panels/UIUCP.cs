using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.LUI;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.Rendering.Lot.Model;

namespace TSOClient.Code.UI.Panels
{

    public delegate void UCPZoomChangeEvent(HouseZoom zoom);
    public delegate void UCPRotateChangeEvent(UCPRotateDirection direction);

    public enum UCPRotateDirection
    {
        Clockwise,
        CounterClockwise
    }


    /// <summary>
    /// UCP
    /// </summary>
    public class UIUCP : UIContainer
    {
        public event UCPZoomChangeEvent OnZoomChanged;
        public event UCPRotateChangeEvent OnRotateChanged;


        /// <summary>
        /// Variables which get wired up by the UIScript
        /// </summary>
        public Texture2D BackgroundGameImage { get; set; }
        public Texture2D BackgroundMatchmakerImage { get; set; }

        /// <summary>
        /// Mode buttons
        /// </summary>
        public UIButton LiveModeButton { get; set; }
        public UIButton BuyModeButton { get; set; }
        public UIButton BuildModeButton { get; set; }
        public UIButton HouseModeButton { get; set; }
        public UIButton OptionsModeButton { get; set; }

        /// <summary>
        /// House view buttons
        /// </summary>
        public UIButton FirstFloorButton { get; set; }
        public UIButton SecondFloorButton { get; set; }
        public UIButton WallsDownButton { get; set; }
        public UIButton WallsCutawayButton { get; set; }
        public UIButton WallsUpButton { get; set; }
        public UIButton RoofButton { get; set; }
        public UIButton RotateClockwiseButton { get; set; }
        public UIButton RotateCounterClockwiseButton { get; set; }


        public UIButton CloseZoomButton {get;set;}
        public UIButton MediumZoomButton {get; set;}
        public UIButton FarZoomButton { get; set; }

        /// <summary>
        /// Backgrounds
        /// </summary>
        private UIImage BackgroundMatchmaker;
        private UIImage Background;


        public UIUCP()
        {
            this.RenderScript("ucp.uis");

            Background = new UIImage(BackgroundGameImage);
            this.AddAt(0, Background);


            BackgroundMatchmaker = new UIImage(BackgroundMatchmakerImage);
            BackgroundMatchmaker.Y = 81;
            this.AddAt(0, BackgroundMatchmaker);

            CloseZoomButton.OnButtonClick += new ButtonClickDelegate(ZoomButton_OnButtonClick);
            MediumZoomButton.OnButtonClick += new ButtonClickDelegate(ZoomButton_OnButtonClick);
            FarZoomButton.OnButtonClick += new ButtonClickDelegate(ZoomButton_OnButtonClick);

            RotateClockwiseButton.OnButtonClick += new ButtonClickDelegate(RotateButton_OnButtonClick);
            RotateCounterClockwiseButton.OnButtonClick += new ButtonClickDelegate(RotateButton_OnButtonClick);

            SetMode(UCPMode.LotMode);
        }

        void RotateButton_OnButtonClick(UIElement button)
        {
            if (OnRotateChanged != null)
            {
                if (button == RotateClockwiseButton)
                {
                    OnRotateChanged(UCPRotateDirection.Clockwise);
                }
                else
                {
                    OnRotateChanged(UCPRotateDirection.CounterClockwise);
                }
            }
        }

        void ZoomButton_OnButtonClick(UIElement button)
        {
            var mode = HouseZoom.FarZoom;
            if (button == CloseZoomButton)
            {
                mode = HouseZoom.CloseZoom;
            }
            else if (button == MediumZoomButton)
            {
                mode = HouseZoom.MediumZoom;
            }

            if (OnZoomChanged != null)
            {
                OnZoomChanged(mode);
            }
        }

        public void SetMode(UCPMode mode)
        {
            var isLotMode = mode == UCPMode.LotMode;
            var isCityMode = mode == UCPMode.CityMode;

            FirstFloorButton.Visible = isLotMode;
            SecondFloorButton.Visible = isLotMode;
            WallsDownButton.Visible = isLotMode;
            WallsCutawayButton.Visible = isLotMode;
            WallsUpButton.Visible = isLotMode;
            RoofButton.Visible = isLotMode;

            LiveModeButton.Visible = isLotMode;
            BuyModeButton.Visible = isLotMode;
            BuildModeButton.Visible = isLotMode;
            HouseModeButton.Visible = isLotMode;

            BackgroundMatchmaker.Visible = isCityMode;
            Background.Visible = isLotMode;
        }


        public enum UCPMode
        {
            LotMode,
            CityMode
        }

    }
}
