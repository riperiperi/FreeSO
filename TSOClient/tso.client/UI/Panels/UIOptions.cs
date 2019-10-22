/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Controls;
using FSO.Common;
using FSO.HIT;
using FSO.HIT.Model;
using FSO.Client.UI.Screens;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Model.TSOPlatform;
using Microsoft.Xna.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.UI.Controls;
using FSO.SimAntics.NetPlay.Model;

namespace FSO.Client.UI.Panels
{

    /// <summary>
    /// Options Panel
    /// </summary>
    public class UIOptions : UICachedContainer
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

            Background = new UIImage(GetTexture((FSOEnvironment.UIZoomFactor>1f || GlobalSettings.Default.GraphicsWidth < 1024) ? (ulong)0x000000D800000002 : (ulong)0x0000018300000002));
            this.AddAt(0, Background);
            Background.BlockInput();
            Size = Background.Size.ToVector2();

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
                        var dialog = new UIGraphicsOptionsDialog();
                        UIScreen.GlobalShowDialog(dialog, true);
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
                if (Panel == null)
                {
                    CurrentPanel = -1;
                    return;
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
            UIAlert alert = null;
            var options = new UIAlertOptions
            {
                Title = GameFacade.Strings.GetString("185", "6"),
                Message = GameFacade.Strings.GetString("185", "7"),
                Buttons = new UIAlertButton[]
                {
                    new UIAlertButton(UIAlertButtonType.Yes, (btn) => {
                        FSOFacade.Controller.Disconnect();
                        UIScreen.RemoveDialog(alert);
                    }),
                    new UIAlertButton(UIAlertButtonType.No, (btn) => { UIScreen.RemoveDialog(alert); })
                }
            };
            alert = UIScreen.GlobalShowAlert(options, true);
        }
    }

    public class UIProfanityOptions : UIContainer
    {
        public UIButton AddButton { get; set; }
        public UIButton SubtractButton { get; set; }
        public UIButton EnableFilterCheckButton { get; set; }

        public UIButton PrevColorButton { get; set; }
        public UIButton NextColorButton { get; set; }

        public UILabel ChatColorLabel { get; set; }
        public UILabel ChatColor { get; set; }
        public UILabel ProfanityFilterTitle { get; set; }
        public UILabel EnterWordLabel { get; set; }
        public UITextEdit EntryBox { get; set; }

        public UISlider PitchSlider { get; set; }
        public int PitchTimer = 0;

        public UIProfanityOptions()
        {
            //var alert = UIScreen.GlobalShowAlert(new UIAlertOptions { Title = "Not Implemented", Message = "This feature is not implemented yet!" }, true);
            var uis = this.RenderScript("profanitypanel.uis");
            //don't draw, this currently breaks the uis parser
            //var bg = uis.Create<UIImage>("Background");
            //AddAt(0, bg);
            Remove(PrevColorButton);
            Remove(NextColorButton);
            Remove(AddButton);
            Remove(SubtractButton);
            Remove(EnableFilterCheckButton);
            Remove(ProfanityFilterTitle);
            Remove(EntryBox);

            EnterWordLabel.Caption = GameFacade.Strings.GetString("f113", "5");

            var ttsToggleContainer = new UIHBoxContainer();
            ttsToggleContainer.Add(new UILabel() { Caption = GameFacade.Strings.GetString("f113", "1") });
            var ttsMode = GlobalSettings.Default.TTSMode;
            for (int i = 0; i < 3; i++)
            {
                var radio = new UIRadioButton();
                radio.RadioData = i;
                radio.RadioGroup = "ttsOpt";
                radio.OnButtonClick += TTSOptSet;
                radio.Tooltip = GameFacade.Strings.GetString("f113", (2 + i).ToString());
                radio.Selected = ttsMode == i;
                ttsToggleContainer.Add(radio);
                ttsToggleContainer.Add(new UILabel() { Caption = radio.Tooltip });
            }

            ttsToggleContainer.Position = new Vector2(10, 10);
            Add(ttsToggleContainer);
            ttsToggleContainer.AutoSize();

            var col = new Color(GlobalSettings.Default.ChatColor);
            ChatColor.CaptionStyle = ChatColor.CaptionStyle.Clone();
            ChatColor.CaptionStyle.Color = col;
            ChatColor.CaptionStyle.Shadow = true;
            ChatColor.Caption = "#" + col.R.ToString("x2") + col.G.ToString("x2") + col.B.ToString("x2");

            var changeBtn = new UIButton();
            changeBtn.Caption = GameFacade.Strings.GetString("f113", "6");
            changeBtn.Position = new Microsoft.Xna.Framework.Vector2(155 + 100, 74-7);
            Add(changeBtn);
            changeBtn.OnButtonClick += (btn1) =>
            {
                UIAlert alert = null;
                alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    Title = "",
                    Message = GameFacade.Strings.GetString("f113", "8"),
                    GenericAddition = new UIColorPicker(),
                    Buttons = new UIAlertButton[]
                    {
                        new UIAlertButton(UIAlertButtonType.OK, (btn) => {
                            //set the color
                            var col2 = int.Parse(alert.ResponseText);
                            GlobalSettings.Default.ChatColor = new Color(col2>>16, (byte)(col2>>8), (byte)col2).PackedValue;
                            SetChatParams();
                            UIScreen.RemoveDialog(alert);
                        }),
                        new UIAlertButton(UIAlertButtonType.No, (btn) => {
                            //set the color
                            var rand = new Random();
                            GlobalSettings.Default.ChatColor = VMTSOAvatarState.RandomColours[rand.Next(VMTSOAvatarState.RandomColours.Length)].PackedValue;
                            SetChatParams();
                            UIScreen.RemoveDialog(alert);
                        }, GameFacade.Strings.GetString("f113", "7")),
                        new UIAlertButton(UIAlertButtonType.Cancel)
                    }
                }, true);
            };
            var chatTimestampContainer = new UIHBoxContainer();
            chatTimestampContainer.Add(new UILabel() { Caption = GameFacade.Strings.GetString("f113", "44") });
            var chatShowTimestamp = GlobalSettings.Default.ChatShowTimestamp;
            var radioChatTSOff = new UIRadioButton();
            radioChatTSOff.RadioData = false;
            radioChatTSOff.RadioGroup = "chatTSToggle";
            radioChatTSOff.OnButtonClick += ChatTsOptSet;
            radioChatTSOff.Tooltip = GameFacade.Strings.GetString("f113", "45");
            radioChatTSOff.Selected = chatShowTimestamp == false;
            chatTimestampContainer.Add(radioChatTSOff);
            chatTimestampContainer.Add(new UILabel() { Caption = radioChatTSOff.Tooltip });

            var radioChatTSOn = new UIRadioButton();
            radioChatTSOn.RadioData = true;
            radioChatTSOn.RadioGroup = "chatTSToggle";
            radioChatTSOn.OnButtonClick += ChatTsOptSet;
            radioChatTSOn.Tooltip = GameFacade.Strings.GetString("f113", "46");
            radioChatTSOn.Selected = chatShowTimestamp == true;
            chatTimestampContainer.Add(radioChatTSOn);
            chatTimestampContainer.Add(new UILabel() { Caption = radioChatTSOn.Tooltip });

            chatTimestampContainer.Position = new Vector2(368, 10);
            Add(chatTimestampContainer);
            chatTimestampContainer.AutoSize();

            var chatOpacityContainer = new UIHBoxContainer();
            chatOpacityContainer.Position = new Vector2(368, 32);
            var chatOpacityLabel = new UILabel() { Caption = GameFacade.Strings.GetString("f113", "47") };
            chatOpacityContainer.Add(chatOpacityLabel);
            var chatOpacity = GlobalSettings.Default.ChatWindowsOpacity;
            var chatOpacitySlider = new UISlider();
            chatOpacitySlider.Orientation = 0;
            chatOpacitySlider.Texture = GetTexture(0x42500000001);
            chatOpacitySlider.MinValue = 20;
            chatOpacitySlider.MaxValue = 100;
            chatOpacitySlider.AllowDecimals = false;
            chatOpacitySlider.Position = chatOpacityLabel.Position + new Vector2(115, 24);
            chatOpacitySlider.Value = GlobalSettings.Default.ChatWindowsOpacity*100;
            chatOpacitySlider.SetSize(140f, 0f);
            chatOpacityContainer.Add(chatOpacitySlider);
            Add(chatOpacityContainer);
            chatOpacityContainer.AutoSize();

            PitchSlider = new UISlider();
            PitchSlider.Orientation = 0;
            PitchSlider.Texture = GetTexture(0x42500000001);
            PitchSlider.MinValue = -100f;
            PitchSlider.MaxValue = 100f;
            PitchSlider.AllowDecimals = false;
            PitchSlider.Position = EnterWordLabel.Position + new Vector2(115, 3);
            PitchSlider.Value = GlobalSettings.Default.ChatTTSPitch;
            PitchSlider.SetSize(150f, 0f);
            Add(PitchSlider);

            chatOpacitySlider.OnChange += chatOpacitySlider_OnChange;
            PitchSlider.OnChange += PitchSlider_OnChange;
        }

        private void chatOpacitySlider_OnChange(UIElement element)
        {
            var data = (int)((UISlider)element).Value/100f;
            GlobalSettings.Default.ChatWindowsOpacity = data;
            GlobalSettings.Default.Save();

        }

        private void ChatTsOptSet(UIElement button)
        {
            var data = (bool)((UIRadioButton)button).RadioData;
            if (GlobalSettings.Default.ChatShowTimestamp == data) return;

            GlobalSettings.Default.ChatShowTimestamp = data;
            GlobalSettings.Default.Save();

            var response = "Chat timestamps " + ((data) ? "enabled" : "disabled") + ".";

            var vm = ((IGameScreen)GameFacade.Screens.CurrentUIScreen)?.vm;
            if (vm != null) vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Generic, response));
        }

        private void TTSOptSet(UIElement button)
        {
            GlobalSettings.Default.TTSMode = (int)((UIRadioButton)button).RadioData;
            GlobalSettings.Default.Save();
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (PitchTimer > 0)
            {
                PitchTimer--;
                if (PitchTimer == 0)
                {
                    SetChatParams();
                }
            }
        }

        public override void Removed()
        {
            base.Removed();
            if (PitchTimer > 0) SetChatParams();
        }

        private void PitchSlider_OnChange(UIElement element)
        {
            GlobalSettings.Default.ChatTTSPitch = (int)PitchSlider.Value;
            PitchTimer = FSOEnvironment.RefreshRate / 2;
        }

        private void SetChatParams()
        {
            var col = new Color(GlobalSettings.Default.ChatColor);
            ChatColor.CaptionStyle.Color = col;
            ChatColor.Caption = "#" + col.R.ToString("x2") + col.G.ToString("x2") + col.B.ToString("x2");
            (UIScreen.Current as IGameScreen)?.vm?.SendCommand(new VMNetChatParamCmd()
            {
                Col = new Color(GlobalSettings.Default.ChatColor),
                Pitch = (sbyte)GlobalSettings.Default.ChatTTSPitch
            });
            GlobalSettings.Default.Save();
        }
    }

    public class UISoundOptions : UIContainer
    {
        public UISlider FXSlider { get; set; }
        public UISlider MusicSlider { get; set; }
        public UISlider VoxSlider { get; set; }
        public UISlider AmbienceSlider { get; set; }

        public UISoundOptions()
        {
            this.RenderScript("soundpanel.uis");

            FXSlider.OnChange += new ChangeDelegate(ChangeVolume);
            MusicSlider.OnChange += new ChangeDelegate(ChangeVolume);
            VoxSlider.OnChange += new ChangeDelegate(ChangeVolume);
            AmbienceSlider.OnChange += new ChangeDelegate(ChangeVolume);

            FXSlider.Value = GlobalSettings.Default.FXVolume;
            MusicSlider.Value = GlobalSettings.Default.MusicVolume;
            VoxSlider.Value = GlobalSettings.Default.VoxVolume;
            AmbienceSlider.Value = GlobalSettings.Default.AmbienceVolume;
        }

        void ChangeVolume(UIElement slider)
        {
            UISlider elm = (UISlider)slider;

            if (elm == FXSlider) GlobalSettings.Default.FXVolume = (byte)elm.Value;
            else if (elm == MusicSlider) GlobalSettings.Default.MusicVolume = (byte)elm.Value;
            else if (elm == VoxSlider) GlobalSettings.Default.VoxVolume = (byte)elm.Value;
            else if (elm == AmbienceSlider) GlobalSettings.Default.AmbienceVolume = (byte)elm.Value;

            var hit = HITVM.Get();
            hit.SetMasterVolume(HITVolumeGroup.FX, GlobalSettings.Default.FXVolume / 10f);
            hit.SetMasterVolume(HITVolumeGroup.MUSIC, GlobalSettings.Default.MusicVolume / 10f);
            hit.SetMasterVolume(HITVolumeGroup.VOX, GlobalSettings.Default.VoxVolume / 10f);
            hit.SetMasterVolume(HITVolumeGroup.AMBIENCE, GlobalSettings.Default.AmbienceVolume / 10f);

            GlobalSettings.Default.Save();
        }
    }

    public class UIGraphicOptions : UIContainer
    {

        public UIButton AntiAliasCheckButton { get; set; }
        public UIButton ShadowsCheckButton { get; set; }
        public UIButton LightingCheckButton { get; set; }
        public UIButton UIEffectsCheckButton { get; set; }
        public UIButton EdgeScrollingCheckButton { get; set; }
        public UIButton Wall3DButton { get; set; }

        // High-Medium-Low detail buttons:

        public UIButton TerrainDetailLowButton { get; set; }
        public UIButton TerrainDetailMedButton { get; set; }
        public UIButton TerrainDetailHighButton { get; set; }

        public UIButton CharacterDetailLowButton { get; set; }
        public UIButton CharacterDetailMedButton { get; set; }
        public UIButton CharacterDetailHighButton { get; set; }

        public UILabel UIEffectsLabel { get; set; }
        public UILabel CharacterDetailLabel { get; set; }
        public UILabel ShadowsLabel { get; set; }
        public UILabel LightingLabel { get; set; }

        public UILabel TerrainDetailLabel { get; set; }
        public UILabel Wall3DLabel { get; set; }

        public UISlider LightingSlider;
        private bool InternalChange;

        public UIGraphicOptions()
        {
            var script = this.RenderScript("graphicspanel.uis");
            
            UIEffectsLabel.Caption = GameFacade.Strings.GetString("f103", "2");
            UIEffectsLabel.Alignment = TextAlignment.Middle;
            CharacterDetailLabel.Caption = GameFacade.Strings.GetString("f103", "4");
            TerrainDetailLabel.Caption = GameFacade.Strings.GetString("f103", "1");
            ShadowsLabel.Caption = GameFacade.Strings.GetString("f103", "6");
            LightingLabel.Caption = GameFacade.Strings.GetString("f103", "3");

            AntiAliasCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            ShadowsCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            LightingCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            UIEffectsCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            EdgeScrollingCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);

            ShadowsCheckButton.Tooltip = ShadowsLabel.Caption;
            LightingCheckButton.Tooltip = LightingLabel.Caption;
            UIEffectsCheckButton.Tooltip = UIEffectsLabel.Caption;

            CharacterDetailLowButton.OnButtonClick += new ButtonClickDelegate(ChangeShadowDetail);
            CharacterDetailMedButton.OnButtonClick += new ButtonClickDelegate(ChangeShadowDetail);
            CharacterDetailHighButton.OnButtonClick += new ButtonClickDelegate(ChangeShadowDetail);

            TerrainDetailLowButton.OnButtonClick += new ButtonClickDelegate(ChangeSurroundingDetail);
            TerrainDetailMedButton.OnButtonClick += new ButtonClickDelegate(ChangeSurroundingDetail);
            TerrainDetailHighButton.OnButtonClick += new ButtonClickDelegate(ChangeSurroundingDetail);

            TerrainDetailLowButton.Tooltip = GameFacade.Strings.GetString("f103", "8");
            TerrainDetailMedButton.Tooltip = GameFacade.Strings.GetString("f103", "9");
            TerrainDetailHighButton.Tooltip = GameFacade.Strings.GetString("f103", "10");

            Wall3DButton = new UIButton(AntiAliasCheckButton.Texture);
            Wall3DButton.Position = AntiAliasCheckButton.Position + new Microsoft.Xna.Framework.Vector2(110, 0);
            Wall3DButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            Add(Wall3DButton);
            Wall3DLabel = new UILabel();
            Wall3DLabel.Caption = GameFacade.Strings.GetString("f103", "12");
            Wall3DLabel.CaptionStyle = UIEffectsLabel.CaptionStyle;
            Wall3DLabel.Position = AntiAliasCheckButton.Position + new Microsoft.Xna.Framework.Vector2(134, 0);
            Add(Wall3DLabel);

            //switch lighting and uieffects label. replace lighting check with a slider

            var ltLoc = LightingLabel.Position;
            LightingLabel.Position = UIEffectsLabel.Position;
            UIEffectsLabel.Position = ltLoc;
            ltLoc = LightingCheckButton.Position;
            LightingCheckButton.Position = UIEffectsCheckButton.Position;
            UIEffectsCheckButton.Position = ltLoc;
            LightingCheckButton.Visible = false;

            LightingSlider = new UISlider();
            LightingSlider.Orientation = 0;
            LightingSlider.Texture = GetTexture(0x42500000001);
            LightingSlider.MinValue = 0f;
            LightingSlider.MaxValue = 3f;
            LightingSlider.AllowDecimals = false;
            LightingSlider.Position = LightingCheckButton.Position + new Microsoft.Xna.Framework.Vector2(96, 4);
            LightingSlider.SetSize(96f, 0f);
            Add(LightingSlider);
            LightingLabel.X -= 24;

            SettingsChanged();

            LightingSlider.OnChange += (elem) =>
            {
                if (InternalChange) return;
                var settings = GlobalSettings.Default;
                settings.LightingMode = (int)LightingSlider.Value;
                GlobalSettings.Default.Save();
                SettingsChanged();
            };
        }

        private void FlipSetting(UIElement button)
        {
            var settings = GlobalSettings.Default;
            if (button == AntiAliasCheckButton) settings.AntiAlias = settings.AntiAlias ^ 1;
            else if (button == ShadowsCheckButton) settings.SmoothZoom = !(settings.SmoothZoom);
            else if (button == LightingCheckButton) settings.Lighting = !(settings.Lighting);
            else if (button == UIEffectsCheckButton) settings.CityShadows = !(settings.CityShadows);
            else if (button == EdgeScrollingCheckButton) settings.EdgeScroll = !(settings.EdgeScroll);
            else if (button == Wall3DButton)
            {
                settings.CitySkybox = !settings.CitySkybox;
            }
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

        private void ChangeSurroundingDetail(UIElement button)
        {
            var settings = GlobalSettings.Default;
            if (button == TerrainDetailLowButton) settings.SurroundingLotMode = 0;
            else if (button == TerrainDetailMedButton) settings.SurroundingLotMode = 1;
            else if (button == TerrainDetailHighButton) settings.SurroundingLotMode = 2;
            GlobalSettings.Default.Save();
            SettingsChanged();
        }

        private void SettingsChanged()
        {
            var settings = GlobalSettings.Default;
            AntiAliasCheckButton.Selected = settings.AntiAlias > 0; //antialias for render targets
            ShadowsCheckButton.Selected = settings.SmoothZoom;
            LightingCheckButton.Selected = settings.Lighting;
            UIEffectsCheckButton.Selected = settings.CityShadows; //instead of being able to disable UI transparency, you can toggle City Shadows.
            EdgeScrollingCheckButton.Selected = settings.EdgeScroll;

            // Character detail changed for city shadow detail.
            CharacterDetailLowButton.Selected = (settings.ShadowQuality <= 512);
            CharacterDetailMedButton.Selected = (settings.ShadowQuality > 512 && settings.ShadowQuality <= 1024);
            CharacterDetailHighButton.Selected = (settings.ShadowQuality > 1024);

            //not used right now! We need to determine if this should be ingame or not... It affects the density of grass blades on the simulation terrain.
            TerrainDetailLowButton.Selected = (settings.SurroundingLotMode == 0);
            TerrainDetailMedButton.Selected = (settings.SurroundingLotMode == 1);
            TerrainDetailHighButton.Selected = (settings.SurroundingLotMode == 2);

            InternalChange = true;
            LightingSlider.Value = settings.LightingMode;
            InternalChange = false;

            Wall3DButton.Selected = settings.CitySkybox;

            var oldSurrounding = LotView.WorldConfig.Current.SurroundingLots;
            LotView.WorldConfig.Current = new LotView.WorldConfig()
            {
                LightingMode = settings.LightingMode,
                SmoothZoom = settings.SmoothZoom,
                SurroundingLots = settings.SurroundingLotMode,
                AA = settings.AntiAlias,
                Weather = settings.Weather,
                Directional = settings.DirectionalLight3D,
                Complex = settings.ComplexShaders,
                EnableTransitions = settings.EnableTransitions
            };

            var vm = ((IGameScreen)GameFacade.Screens.CurrentUIScreen)?.vm;
            if (vm != null)
            {
                vm.Context.World.ChangedWorldConfig(GameFacade.GraphicsDevice);
                if (oldSurrounding != settings.SurroundingLotMode)
                {
                    SimAntics.Utils.VMLotTerrainRestoreTools.RestoreSurroundings(vm, vm.HollowAdj);
                }
            }
        }
    }
}
