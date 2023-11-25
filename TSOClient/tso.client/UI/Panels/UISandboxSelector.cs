using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Screens;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Client.UI.Panels
{
    public class UISandboxSelector : UIDialog
    {
        public UIButton CloseButton { get; set; }
        public UIListBox BookmarkListBox { get; set; }
        public UIListBoxTextStyle BookmarkListBoxColors { get; set; }

        public UISlider BookmarkListSlider { get; set; }
        public UIButton BookmarkListScrollUpButton { get; set; }
        public UIButton BookmarkScrollDownButton { get; set; }
        public UIButton SimsTabButton { get; set; }
        public UIButton IgnoreTabButton { get; set; }

        public UIImage SimsTab { get; set; }
        public UIImage IgnoreTab { get; set; }

        public Binding<Avatar> Binding;

        public UISandboxSelector() : base(UIDialogStyle.Close, true)
        {
            if (GlobalSettings.Default.DebugBody == 0) {
                GameThread.NextUpdate(x => FSOFacade.Controller.ShowPersonCreation(null));
            }
            var ui = this.RenderScript("bookmarks.uis");
            Caption = "Host a lot on :37564";

            //var background = ui.Create<UIImage>("BookmarkBackground");
            //SimsTab = ui.Create<UIImage>("SimsTab");
            //AddAt(0, SimsTab);
            //IgnoreTab = ui.Create<UIImage>("IgnoreTab");
            //AddAt(0, IgnoreTab);
            //IgnoreTab.Visible = false;

            //AddAt(0, ui.Create<UIImage>("Tab1Background"));
            //AddAt(0, ui.Create<UIImage>("Tab2Background"));
            var listBg = ui.Create<UIImage>("ListBoxBackground");
            AddAt(4, listBg);
            //AddAt(0, background);


            //UIUtils.MakeDraggable(background, this, true);
            listBg.With9Slice(25, 25, 25, 25);
            listBg.Height += 180;
            BookmarkListBox.VisibleRows += 10;
            BookmarkListSlider.SetSize(10, 170 + 180);
            BookmarkScrollDownButton.Y += 180;
            BookmarkListSlider.AttachButtons(BookmarkListScrollUpButton, BookmarkScrollDownButton, 1);
            BookmarkListBox.AttachSlider(BookmarkListSlider);
            BookmarkListBox.OnDoubleClick += BookmarkListBox_OnDoubleClick;
            BookmarkListBoxColors = ui.Create<UIListBoxTextStyle>("BookmarkListBoxColors", BookmarkListBox.FontStyle);
            Remove(CloseButton);
            Remove(SimsTabButton);
            Remove(IgnoreTabButton);
            base.CloseButton.OnButtonClick += CloseButton_OnButtonClick;

            //IgnoreTabButton.OnButtonClick += (btn) => { ChangeType(BookmarkType.IGNORE_AVATAR); };
            //SimsTabButton.OnButtonClick += (btn) => { ChangeType(BookmarkType.AVATAR); };

            populateWithXMLHouses();

            var joinButton = new UIButton();
            joinButton.Caption = "Join a server";
            joinButton.OnButtonClick += (btn) =>
            {
                UIAlert alert = null;
                alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    Message = "Enter the address of the server you wish to connect to. (can optionally include port, eg localhost:6666)",
                    Width = 400,
                    TextEntry = true,
                    Buttons = new UIAlertButton[]
                    {
                        new UIAlertButton(UIAlertButtonType.Cancel, (btn2) => { UIScreen.RemoveDialog(alert); }),
                        new UIAlertButton(UIAlertButtonType.OK, (btn2) => {
                            UIScreen.RemoveDialog(alert);
                            var addr = alert.ResponseText;
                            if (!addr.Contains(':'))
                            {
                                addr += ":37564";
                            }
                            UIScreen.RemoveDialog(this);
                            LotSwitch(addr, true);
                        })
                    }
                }, true);
                alert.ResponseText = "127.0.0.1";
            };
            joinButton.Width = 190;
            joinButton.X = 25;
            joinButton.Y = 500 - 50;
            Add(joinButton);

            var casButton = new UIButton();
            casButton.Caption = "CAS";
            casButton.OnButtonClick += (btn) =>
            {
                if (UIScreen.Current is SandboxGameScreen)
                {
                    ((SandboxGameScreen)UIScreen.Current).CleanupLastWorld();
                }
                FSOFacade.Controller.ShowPersonCreation(null);
            };
            casButton.Width = 50;
            casButton.X = 300-(25+50);
            casButton.Y = 500 - 50;
            Add(casButton);

            SetSize(300, 500);
        }

        public void LotSwitch(string location, bool external)
        {
            if (UIScreen.Current is SandboxGameScreen)
            {
                var sand = (SandboxGameScreen)UIScreen.Current;
                sand.Initialize(location, external);
            } else
            {
                FSOFacade.Controller.EnterSandboxMode(location, external);
            }
        }

        public void populateWithXMLHouses()
        {
            var xmlHouses = new List<UIXMLLotEntry>();

            string[] paths = Directory.GetFiles(Path.Combine(FSOEnvironment.ContentDir, "Blueprints/"), "*.xml", SearchOption.AllDirectories);
            for (int i = 0; i < paths.Length; i++)
            {
                string entry = paths[i];
                string filename = Path.GetFileName(entry);
                xmlHouses.Add(new UIXMLLotEntry { Filename = filename, Path = entry });
            }

            paths = Directory.GetFiles(Path.Combine(GlobalSettings.Default.StartupPath, @"housedata/blueprints/"), "*.xml", SearchOption.AllDirectories);
            for (int i = 0; i < paths.Length; i++)
            {
                string entry = paths[i];
                string filename = Path.GetFileName(entry);
                xmlHouses.Add(new UIXMLLotEntry { Filename = filename, Path = entry });
            }
            
            try
            {

                paths = Directory.GetFiles(Path.Combine(GlobalSettings.Default.TS1HybridPath, @"UserData/Houses/"), "House**.iff", SearchOption.AllDirectories);
                for (int i = 0; i < paths.Length; i++)
                {
                    string entry = paths[i];
                    string filename = Path.GetFileName(entry);
                    xmlHouses.Add(new UIXMLLotEntry { Filename = filename, Path = entry });
                }
            }
            catch { }

            try
            {
                paths = Directory.GetFiles(Path.Combine(FSOEnvironment.ContentDir, "LocalHouse/"), "*", SearchOption.AllDirectories);
                for (int i = 0; i < paths.Length; i++)
                {
                    string entry = paths[i];
                    if (!entry.ToLowerInvariant().EndsWith(".fsor"))
                        entry = entry.Substring(0, entry.Length - 5) + ".xml";
                    string filename = Path.GetFileName(entry);
                    if (!xmlHouses.Any(x => x.Filename == filename))
                    {
                        xmlHouses.Add(new UIXMLLotEntry { Filename = filename, Path = entry });
                    }
                }
            }
            catch { }


            BookmarkListBox.Columns[0].Alignment = TextAlignment.Left | TextAlignment.Top;
            BookmarkListBox.Columns[0].Width = (int)BookmarkListBox.Width;
            BookmarkListBox.Items = xmlHouses.Select(x => new UIListBoxItem(x, x.Filename.Substring(0, x.Filename.Length-4))).ToList();
        }

        private void ChangeType(BookmarkType type)
        {
            var bookmark = type == BookmarkType.AVATAR;
            SimsTabButton.Selected = bookmark;
            SimsTab.Visible = bookmark;
            IgnoreTabButton.Selected = !bookmark;
            IgnoreTab.Visible = !bookmark;
        }

        private void BookmarkListBox_OnDoubleClick(UIElement button)
        {
            if (BookmarkListBox.SelectedItem == null) { return; }
            var item = (UIXMLLotEntry)BookmarkListBox.SelectedItem.Data;
            UIScreen.RemoveDialog(this);
            LotSwitch(item.Path, false);
        }

        private void CloseButton_OnButtonClick(UIElement button)
        {
            UIScreen.RemoveDialog(this);
        }
    }
}
