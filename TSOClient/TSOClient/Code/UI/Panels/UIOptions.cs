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

namespace TSOClient.Code.UI.Panels
{

    /// <summary>
    /// Options Panel
    /// </summary>
    public class UIOptions : UIContainer
    {
        public UIImage Background;
        public UIImage Divider;
        
        public UIButton ExitButton { get; set; }
        public UIButton GraphicsButton { get; set; }
        public UIButton ProfanityButton { get; set; }
        public UIButton SelectSimButton { get; set; }
        public UIButton SoundButton { get; set; }

        public Texture2D BackgroundGameImage { get; set; }
        public Texture2D DividerImage { get; set; }

        private UIContainer Panel;
        private int CurrentPanel;

        public UIOptions()
        {
            var script = this.RenderScript("optionspanel.uis");

            /*var bgimage = new TSOClient.Code.UI.Framework.Parser.UINode();
            var imageAtts = new Dictionary<string,string>();
            imageAtts.Add("assetID", (GlobalSettings.Default.GraphicsWidth < 1024)?"0x000000D800000002":"0x0000018300000002");
            bgimage.ID = "BackgroundGameImage";

            bgimage.AddAtts(imageAtts);
            script.DefineImage(bgimage);*/
            
            //we really need to figure out how graphics reset works to see what and how we need to reload things

            Background = new UIImage(GetTexture((GlobalSettings.Default.GraphicsWidth < 1024) ? (ulong)0x000000D800000002 : (ulong)0x0000018300000002));
            this.AddAt(0, Background);
            Background.BlockInput();

            Divider = new UIImage(DividerImage);
            Divider.X = 227;
            Divider.Y = 17;
            this.Add(Divider);

            ExitButton.OnButtonClick += new ButtonClickDelegate(ExitButton_OnButtonClick);
            SelectSimButton.OnButtonClick += new ButtonClickDelegate(SelectSimButton_OnButtonClick);

            GraphicsButton.OnButtonClick += new ButtonClickDelegate(GraphicsButton_OnButtonClick);
            ProfanityButton.OnButtonClick += new ButtonClickDelegate(ProfanityButton_OnButtonClick);
            SoundButton.OnButtonClick += new ButtonClickDelegate(SoundButton_OnButtonClick);

            CurrentPanel = -1;
        }

        public void SetPanel(int newPanel)
        {
            GraphicsButton.Selected = false;
            ProfanityButton.Selected = false;
            SoundButton.Selected = false;

            if (CurrentPanel != -1) this.Remove(Panel);
            if (newPanel != CurrentPanel)
            {
                switch (newPanel)
                {
                    case 0:
                        Panel = new UIGraphicOptions();
                        GraphicsButton.Selected = true;
                        break;
                    case 1:
                        Panel = new UIProfanityOptions();
                        ProfanityButton.Selected = true;
                        break;
                    case 2:
                        Panel = new UISoundOptions();
                        SoundButton.Selected = true;
                        break;
                    default:
                        break;
                }
                Panel.X = 240;
                Panel.Y = 0;
                this.Add(Panel);
                CurrentPanel = newPanel;
            }
            else
            {
                CurrentPanel = -1;
            }

        }

        private void ExitButton_OnButtonClick(UIElement button)
        {
            UIScreen.ShowDialog(new UIExitDialog(), true);
        }

        private void GraphicsButton_OnButtonClick(UIElement button)
        {
            SetPanel(0);
        }

        private void ProfanityButton_OnButtonClick(UIElement button)
        {
            SetPanel(1);
        }

        private void SoundButton_OnButtonClick(UIElement button)
        {
            SetPanel(2);
        }

        private void SelectSimButton_OnButtonClick(UIElement button)
        {
            var alert = UIScreen.ShowAlert(new UIAlertOptions { Title = "Not Implemented", Message = "This feature is not implemented yet!" }, true);
        }
    }

    public class UIProfanityOptions : UIContainer
    {
        public UIProfanityOptions()
        {
            var alert = UIScreen.ShowAlert(new UIAlertOptions { Title = "Not Implemented", Message = "This feature is not implemented yet!" }, true);
            //this.RenderScript("profanitypanel.uis");
            //don't draw, this currently breaks the uis parser
        }
    }

    public class UISoundOptions : UIContainer
    {
        public UISoundOptions()
        {
            this.RenderScript("soundpanel.uis");
            //todo: horizontal sliders
        }
    }

    public class UIGraphicOptions : UIContainer
    {

        public UIButton AntiAliasCheckButton { get; set; }
        public UIButton ShadowsCheckButton { get; set; }
        public UIButton LightingCheckButton { get; set; }
        public UIButton UIEffectsCheckButton { get; set; }
        public UIButton EdgeScrollingCheckButton { get; set; }

        // High-Medium-Low detail buttons:

        public UIButton TerrainDetailLowButton { get; set; }
        public UIButton TerrainDetailMedButton { get; set; }
        public UIButton TerrainDetailHighButton { get; set; }

        public UIButton CharacterDetailLowButton { get; set; }
        public UIButton CharacterDetailMedButton { get; set; }
        public UIButton CharacterDetailHighButton { get; set; }

        public UILabel UIEffectsLabel { get; set; }
        public UILabel CharacterDetailLabel { get; set; }

        public UIGraphicOptions()
        {
            var script = this.RenderScript("graphicspanel.uis");
            UIEffectsLabel.Caption = "City Shadows";
            UIEffectsLabel.Alignment = TextAlignment.Middle;
            CharacterDetailLabel.Caption = "Shadow Detail";

            AntiAliasCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            ShadowsCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            LightingCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            UIEffectsCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            EdgeScrollingCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);

            CharacterDetailLowButton.OnButtonClick += new ButtonClickDelegate(ChangeShadowDetail);
            CharacterDetailMedButton.OnButtonClick += new ButtonClickDelegate(ChangeShadowDetail);
            CharacterDetailHighButton.OnButtonClick += new ButtonClickDelegate(ChangeShadowDetail);

            SettingsChanged();
        }

        private void FlipSetting(UIElement button)
        {
            var settings = GlobalSettings.Default;
            if (button == AntiAliasCheckButton) settings.AntiAlias = !(settings.AntiAlias);
            else if (button == ShadowsCheckButton) settings.SimulationShadows = !(settings.SimulationShadows);
            else if (button == LightingCheckButton) settings.Lighting = !(settings.Lighting);
            else if (button == UIEffectsCheckButton) settings.CityShadows = !(settings.CityShadows);
            else if (button == EdgeScrollingCheckButton) settings.EdgeScroll = !(settings.EdgeScroll);
            GlobalSettings.Default.Save();
            SettingsChanged();
        }

        private void ChangeShadowDetail(UIElement button)
        {
            var settings = GlobalSettings.Default;
            if (button == CharacterDetailLowButton) settings.ShadowQuality = 512;
            else if (button == CharacterDetailMedButton) settings.ShadowQuality = 1024;
            else if (button == CharacterDetailHighButton) settings.ShadowQuality = 2048;
            GlobalSettings.Default.Save();
            SettingsChanged();
        }

        private void SettingsChanged()
        {
            var settings = GlobalSettings.Default;
            AntiAliasCheckButton.Selected = settings.AntiAlias;
            ShadowsCheckButton.Selected = settings.SimulationShadows;
            LightingCheckButton.Selected = settings.Lighting;
            UIEffectsCheckButton.Selected = settings.CityShadows; //instead of being able to disable UI transparency, you can toggle City Shadows.
            EdgeScrollingCheckButton.Selected = settings.EdgeScroll;

            // Character detail changed for city shadow detail.
            CharacterDetailLowButton.Selected = (settings.ShadowQuality <= 512);
            CharacterDetailMedButton.Selected = (settings.ShadowQuality > 512 && settings.ShadowQuality <= 1024);
            CharacterDetailHighButton.Selected = (settings.ShadowQuality > 1024);

            //not used right now! We need to determine if this should be ingame or not... It affects the density of grass blades on the simulation terrain.
            TerrainDetailLowButton.Disabled = true;
            TerrainDetailMedButton.Disabled = true;
            TerrainDetailHighButton.Disabled = true;
        }
    }
}
