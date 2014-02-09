/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.LUI;
using TSOClient.Code.UI.Controls;
using tso.world;

namespace TSOClient.Code.UI.Panels
{

    public delegate void UCPZoomChangeEvent(WorldZoom zoom);
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
            var mode = WorldZoom.Far;
            if (button == CloseZoomButton)
            {
                mode = WorldZoom.Near;
            }
            else if (button == MediumZoomButton)
            {
                mode = WorldZoom.Medium;
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
