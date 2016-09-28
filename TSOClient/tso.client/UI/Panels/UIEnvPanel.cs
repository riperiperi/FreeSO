using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.SimAntics.NetPlay.Model.Commands;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// Just to make things confusing, this panel has 3 subpanels. So they're sub-subpanels.
    /// </summary>
    public class UIEnvPanel : UIContainer
    {
        public UIButton LightColorsButton { get; set; }
        public UIButton TimeOfDayButton { get; set; }
        public UIButton SoundsButton { get; set; }

        public Texture2D DividerImage { get; set; }

        private Dictionary<UIButton, int> BtnToMode;
        private UIContainer Panel;
        private int CurrentPanel;
        private UILotControl LotControl;

        public UIImage Divider;

        public UIEnvPanel(UILotControl lotController)
        {
            var script = this.RenderScript("envpanel.uis");
            Divider = script.Create<UIImage>("Divider");
            Divider.Texture = DividerImage;
            Add(Divider);

            BtnToMode = new Dictionary<UIButton, int>()
            {
                { LightColorsButton, 0 },
                { TimeOfDayButton, 1 },
                { SoundsButton, 2 }
            };

            foreach (var btn in BtnToMode.Keys)
                btn.OnButtonClick += SetMode;

            LotControl = lotController;
            CurrentPanel = -1;
        }

        private void SetMode(Framework.UIElement button)
        {
            var btn = (UIButton)button;
            int newPanel = -1;
            BtnToMode.TryGetValue(btn, out newPanel);

            foreach (var ui in BtnToMode.Keys)
                ui.Selected = false;

            if (CurrentPanel != -1) this.Remove(Panel);
            if (newPanel != CurrentPanel)
            {
                btn.Selected = true;
                switch (newPanel)
                {
                    case 0:
                        Panel = new UILightingPanel(LotControl);
                        Panel.X = 130; //TODO: use uiscript positions
                        Panel.Y = 9;
                        break;
                    case 1:
                        Panel = new UITimeOfDayPanel(LotControl);
                        Panel.X = 140; //TODO: use uiscript positions
                        Panel.Y = 9;
                        break;
                    case 2:
                        Panel = new UISoundsPanel(LotControl);
                        Panel.X = 55; //TODO: use uiscript positions
                        Panel.Y = 0;
                        break;
                    default:
                        btn.Selected = false;
                        break;
                }

                this.Add(Panel);
                CurrentPanel = newPanel;
            }
            else
            {
                CurrentPanel = -1;
            }
        }
    }

    /// <summary>
    /// Same as TS1 house stats. TODO: make freeso calculate these values.
    /// </summary>
    public class UITimeOfDayPanel : UIContainer
    {
        public UITimeOfDayPanel(UILotControl lotController)
        {
            this.RenderScript("timeofdaypanel.uis");
        }
    }

    /// <summary>
    /// Same as TS1 house stats. TODO: make freeso calculate these values.
    /// </summary>
    public class UILightingPanel : UIContainer
    {
        public UILightingPanel(UILotControl lotController)
        {
            this.RenderScript("lightingpanel.uis");
        }
    }

    /// <summary>
    /// Same as TS1 house stats. TODO: make freeso calculate these values.
    /// </summary>
    public class UISoundsPanel : UIContainer
    {
        public UIImage AnimalsTab;
        public UIImage MechanicalTab;
        public UIImage WeatherTab;
        public UIImage PeopleTab;
        public UIImage LoopsTab;
        public UIImage ThemesTab;

        public UIButton AnimalsTabButton { get; set; }
        public UIButton MechanicalTabButton { get; set; }
        public UIButton WeatherTabButtton { get; set; } //sic
        public UIButton PeopleTabButton { get; set; }
        public UIButton LoopsTabButtton { get; set; }
        public UIButton ThemesTabButton { get; set; }

        public UIButton ScrollLeftButton { get; set; }
        public UIButton ScrollRightButton { get; set; }

        public UILabel Label1 { get; set; }
        public UILabel Label2 { get; set; }
        public UILabel Label3 { get; set; }
        public UILabel Label4 { get; set; }
        public UILabel label5 { get; set; }
        public UILabel Label6 { get; set; }

        public UIButton RadioButton1 { get; set; }
        public UIButton RadioButton2 { get; set; }
        public UIButton RadioButton3 { get; set; }
        public UIButton RadioButton4 { get; set; }
        public UIButton RadioButton5 { get; set; }
        public UIButton RadioButton6 { get; set; }

        public UIButton CheckButton1 { get; set; }
        public UIButton CheckButton2 { get; set; }
        public UIButton CheckButton3 { get; set; }
        public UIButton CheckButton4 { get; set; }
        public UIButton CheckButton5 { get; set; }
        public UIButton CheckButton6 { get; set; }

        public Texture2D BackgroundImage { get; set; }
        public UIImage Background;

        public string[][] Collections =
        {
            //animals
            new string[] {
                "AnimalsDog",
                "AnimalsWolf",
                "AnimalsSongBirds",
                "AnimalsSeaBirds",
                "AnimalsInsects",
                "AnimalsJungle",
                "AnimalsFarm",
                "AnimalsNightBirds"
            },

            //mechanical
            new string[]
            {
                "MechanicalConstruction",
                "MechanicalExplosions",
                "MechanicalPlanes",
                "MechanicalSirens",
                "MechanicalSciBleeps",
                "MechanicalGunshot",
                "MechanicalIndustrial",
                "MechanicalDriveBy",
                "MechanicalSmallMachines"
            },

            //weather
            new string[]
            {
                "WeatherLightingThunder",
                "WeatherRainDrops",
                "WeatherBreeze",
                "WeatherHowlingWind"
            },

            //people
            new string[]
            {
                "PeopleScreams",
                "PeopleGhost",
                "PeopleMagic",
                "PeopleOffice",
                "PeopleRestaurant",
                "PeopleGym"
            },

            //loops
            new string[]
            {
                "LoopInsects",
                "LoopHeartbeat",
                "LoopRain",
                "LoopStorm",
                "LoopWind",
                "LoopCrowd",
                "LoopOutdoor",
                "LoopIndoor",
                "LoopTraffic",
                "LoopBrook",
                "LoopOcean",
                "LoopTechno"
            },

            //themes
            new string[]
            {
                "ThemeUrban",
                "ThemeSpooky",
                "ThemeJungle",
                "ThemeSciFi",
                "ThemeBattle",
                "ThemeRural",
                "ThemeSuburbs",
                "ThemeOffice"
            }
        };

        public UIImage[] TabImages;
        public UIButton[] TabButtons;

        public UIButton[] CheckButtons;
        public UIButton[] RadioButtons;
        public UILabel[] ItemLabels;
        public int CurrentTab;
        public int CurrentPage;
        private UIScript Script;
        private UILotControl LotControl;

        public UISoundsPanel(UILotControl lotController)
        {
            var script = this.RenderScript("soundspanel.uis");

            Background = new UIImage(BackgroundImage);
            AddAt(0, Background);

            AnimalsTab = script.Create<UIImage>("AnimalsTab");
            MechanicalTab = script.Create<UIImage>("MechanicalTab");
            WeatherTab = script.Create<UIImage>("WeatherTab");
            PeopleTab = script.Create<UIImage>("PeopleTab");
            LoopsTab = script.Create<UIImage>("LoopsTab");
            ThemesTab = script.Create<UIImage>("ThemesTab");

            TabImages = new UIImage[]
            {
                AnimalsTab, MechanicalTab, WeatherTab, PeopleTab, LoopsTab, ThemesTab
            };
            TabButtons = new UIButton[]
            {
                AnimalsTabButton, MechanicalTabButton, WeatherTabButtton, PeopleTabButton, LoopsTabButtton, ThemesTabButton
            };

            for (int i = 0; i < 6; i++) {
                var tabId = i;
                TabButtons[i].OnButtonClick += (btn) => { SetTab(tabId); };
            }

            CheckButtons = new UIButton[]
            {
                CheckButton1,CheckButton2,CheckButton3,CheckButton4,CheckButton5,CheckButton6
            };
            RadioButtons = new UIButton[]
            {
                RadioButton1,RadioButton2,RadioButton3,RadioButton4,RadioButton5,RadioButton6,
            };
            ItemLabels = new UILabel[]
            {
                Label1,Label2, Label3,Label4,label5,Label6
            };

            foreach (var img in TabImages) AddAt(1, img);
            foreach (var item in ItemLabels)
            {
                item.Alignment = TextAlignment.Left;
                item.X += 3;
                item.CaptionStyle = item.CaptionStyle.Clone();
                item.CaptionStyle.Shadow = true;
            }
            var j = 0;
            foreach (var item in CheckButtons)
            {
                var index = j++; item.OnButtonClick += (btn) => { SelectItem(item, index, false); };
            }
            j = 0;
            foreach (var item in RadioButtons)
            {
                var index = j++; item.OnButtonClick += (btn) => { SelectItem(item, index, true); };
            }
            Script = script;
            LotControl = lotController;
            SetTab(0);

            ScrollLeftButton.OnButtonClick += (btn) => { SetPage(CurrentPage - 1); };
            ScrollRightButton.OnButtonClick += (btn) => { SetPage(CurrentPage + 1); };
        }

        public void SelectItem(UIButton btn, int num, bool radio)
        {
            var item = CurrentPage * 6 + num;
            var col = Collections[CurrentTab];
            if (item >= col.Length) return; //???
            var name = col[item];
            var cmd = new VMNetChangeEnvironmentCmd { GUIDsToAdd = new List<uint>(), GUIDsToClear = new List<uint>() };
            var amb = LotControl.vm.Context.Ambience;

            var wasSelected = btn.Selected;
            var targList = (wasSelected) ? cmd.GUIDsToClear : cmd.GUIDsToAdd;
            if (radio)
            {
                //loops or themes. Last selected button zeroed out.
                //special behaviour for themes
                foreach (var btn2 in RadioButtons) btn2.Selected = false;

                if (CurrentTab == 5)
                {
                    //theme, TODO
                } else
                {
                    //loop
                    var targ = amb.GetAmbienceFromName(name);
                    if (targ != null) targList.Add(targ.Value.GUID);
                }

                btn.Selected = !wasSelected;
            } else
            {
                //just enable one item
                var targ = amb.GetAmbienceFromName(name);
                if (targ != null) targList.Add(targ.Value.GUID);
                btn.Selected = !btn.Selected;
            }
            LotControl.vm.SendCommand(cmd);
        }

        public void SetTab(int tab)
        {
            foreach (var img in TabImages) img.Visible = false;
            CurrentTab = tab;
            TabImages[tab].Visible = true;

            SetPage(0);
        }

        public void SetPage(int page)
        {
            UIButton[] HideBtns;
            UIButton[] ActiveBtns;
            if (CurrentTab > 3)
            {
                HideBtns = CheckButtons;
                ActiveBtns = RadioButtons;
            } else
            {
                HideBtns = RadioButtons;
                ActiveBtns = CheckButtons;
            }
            foreach (var btn in HideBtns) btn.Visible = false;
            foreach (var btn in ActiveBtns) btn.Visible = false;
            foreach (var lbl in ItemLabels) lbl.Visible = false;

            var col = Collections[CurrentTab];
            if (page * 6 >= col.Length) page--;
            if (page < 0) page = 0;

            ScrollLeftButton.Disabled = page == 0;
            ScrollRightButton.Disabled = page == (col.Length-1) / 6;

            var amb = LotControl.vm.Context.Ambience;
            int j = 0;
            for (int i=page*6; i<col.Length && i<(page+1)*6; i++)
            {
                ActiveBtns[j].Visible = true;
                ItemLabels[j].Visible = true;
                ItemLabels[j].Caption = Script.GetString(col[i]);


                if (i < col.Length)
                {
                    var name = col[i];

                    var snd = amb.GetAmbienceFromName(name);
                    if (snd != null) ActiveBtns[j].Selected = amb.ActiveSounds.ContainsKey(amb.GetAmbienceFromGUID(snd.Value.GUID));
                }
                j++;
            }
            CurrentPage = page;
        }
    }
}
