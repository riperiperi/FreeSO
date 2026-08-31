using FSO.Client.UI.Archive.Management;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Screens;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.Utils;
using FSO.Files;
using FSO.UI.Controls;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace FSO.Client.UI.Panels
{
    public class UISandboxSelector : UIDialog
    {
        private enum UISandboxCategory
        {
            Saved,
            Job
        }

        private UIHBoxContainer RootBox;
        private UIVBoxContainer ListingBox;
        private UIVBoxContainer ActionsBox;
        private UIHBoxContainer CategoriesHbox;
        private UIButton SavesButton;
        private UIButton JobButton;

        private UIButton CASButton;
        private UIButton JoinButton;
        private UIButton CreateButton;
        private UIButton FolderButton;

        private UIGenericTable SaveTable;
        private UILotThumbButton LotThumb;

        private static string GetString(string index)
        {
            return GameFacade.Strings.GetString("f133", index);
        }

        public UISandboxSelector() : base(UIDialogStyle.Close, true)
        {
            var cui = Content.Content.Get().CustomUI;
            var btnTex = cui.Get("chat_cat.png").Get(GameFacade.GraphicsDevice);
            var btnFont = TextStyle.DefaultButton.Clone();
            btnFont.Size = 8;
            btnFont.Shadow = true;

            Caption = GetString("1");

            RootBox = new UIHBoxContainer() { VerticalAlignment = UIContainerVerticalAlignment.Bottom };

            RootBox.Add(ListingBox = new UIVBoxContainer()
            {
                HorizontalAlignment = UIContainerHorizontalAlignment.Center
            });

            // Left section

            ListingBox.Add(CategoriesHbox = new UIHBoxContainer());

            CategoriesHbox.Add(SavesButton = new UIButton(btnTex)
            {
                Caption = GetString("6"),
                CaptionStyle = btnFont
            });
            CategoriesHbox.Add(JobButton = new UIButton(btnTex)
            {
                Caption = GetString("7"),
                CaptionStyle = btnFont
            });
            CategoriesHbox.Add(new UISpacer(15));

            CategoriesHbox.AutoSize();

            ListingBox.Add(SaveTable = new UIGenericTable([new("Name", 200)], 300, false)
            {
                Loading = false
            });

            // Right section

            RootBox.Add(ActionsBox = new UIVBoxContainer());

            ActionsBox.Add(FolderButton = new UIButton()
            {
                Caption = GetString("13"),
                Size = new Microsoft.Xna.Framework.Vector2(150, 35)
            });

            ActionsBox.Add(CASButton = new UIButton()
            {
                Caption = GetString("5"),
                Size = new Microsoft.Xna.Framework.Vector2(150, 35),
                Tooltip = GetString("9")
            });

            bool needCas = GlobalSettings.Default.DebugBody == 0;
            string casTooltip = needCas ? GetString("11") : null;

            ActionsBox.Add(JoinButton = new UIButton()
            {
                Caption = GetString("4"),
                Disabled = needCas,
                Tooltip = casTooltip ?? GetString("12"),
                Size = new Microsoft.Xna.Framework.Vector2(150, 35)
            });

            ActionsBox.Add(CreateButton = new UIButton()
            {
                Caption = GetString("3"),
                Disabled = needCas,
                Tooltip = casTooltip ?? GetString("8"),
                Size = new Microsoft.Xna.Framework.Vector2(150, 35)
            });

            RootBox.AutoSize();
            RootBox.Position = new Microsoft.Xna.Framework.Vector2(20, 35);
            DynamicOverlay.Add(RootBox);

            SetSize((int)RootBox.Size.X + 40, (int)RootBox.Size.Y + 55);

            // Extra (lot thumbnail)

            LotThumb = new UILotThumbButton();
            LotThumb.Init(GetTexture(0x0000079300000001), GetTexture(0x0000079300000001));
            LotThumb.Position = RootBox.Position + new Microsoft.Xna.Framework.Vector2(SaveTable.Size.X + 5 + (150 - LotThumb.Size.X) / 2, SaveTable.Position.Y);
            Add(LotThumb);

            // Handlers

            CloseButton.OnButtonClick += CloseButton_OnButtonClick;

            JoinButton.OnButtonClick += JoinLot;

            CASButton.OnButtonClick += OpenCAS;

            CreateButton.OnButtonClick += BookmarkListBox_OnDoubleClick;

            FolderButton.OnButtonClick += OpenFolder;

            SavesButton.OnButtonClick += (btn) =>
            {
                PopulateHouses(UISandboxCategory.Saved);
            };

            JobButton.OnButtonClick += (btn) =>
            {
                PopulateHouses(UISandboxCategory.Job);
            };

            SaveTable.OnChange += SaveChanged;

            PopulateHouses(UISandboxCategory.Saved);

            GameThread.NextUpdate((state) =>
            {
                GameFacade.Screens.inputManager.SetFocus(CreateButton);
            });
        }

        private void OpenFolder(UIElement button)
        {
            var target = Path.GetFullPath(Path.Combine(FSOEnvironment.ContentDir, "LocalHouse/"));

            try
            {
                if (!Directory.Exists(target))
                {
                    Directory.CreateDirectory(target);
                }

                Process.Start(new ProcessStartInfo()
                {
                    FileName = target,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch
            {
                // Just fail silently for now.
            }
        }

        private Texture2D TryLoadThumbnail(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return ImageLoader.FromStream(GameFacade.GraphicsDevice, File.OpenRead(path));
                }
            }
            catch
            {
                // Doesn't do much.
            }

            return null;
        }

        private void SaveChanged(UIElement element)
        {
            var item = SaveTable.SelectedItem;

            if (LotThumb.CurrentLotThumb != 0)
            {
                LotThumb.DisposeThumbnail();
            }

            if (item?.Data is UIXMLLotEntry entry)
            {
                // Try load a thumbnail for the entry. (they should be in the same folder)

                var thumbPath = Path.Combine(Path.GetDirectoryName(entry.Path), Path.GetFileNameWithoutExtension(entry.Path) + ".png");

                var thumb = TryLoadThumbnail(thumbPath);

                if (thumb != null)
                {
                    LotThumb.SetThumbnail(thumb, 1);
                    return;
                }

                // Job lot fallback - try to load prebaked thumbnails from FreeSO content.
                thumbPath = Path.Combine(FSOEnvironment.ContentDir, "uigraphics/jobthumb", Path.GetFileNameWithoutExtension(entry.Path) + ".png");

                thumb = TryLoadThumbnail(thumbPath);

                if (thumb != null)
                {
                    LotThumb.SetThumbnail(thumb, 1);
                    return;
                }
            }

            var defaultThumb = TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, GameFacade.GameFilePath("userdata/houses/defaulthouse.bmp"));
            TextureUtils.ManualTextureMask(ref defaultThumb, new uint[] { 0xFF000000 });
            LotThumb.SetThumbnail(defaultThumb, 1);
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

        private void PopulateHouses(UISandboxCategory category)
        {
            var xmlHouses = new List<UIXMLLotEntry>();

            switch (category)
            {
                case UISandboxCategory.Saved:
                    {
                        xmlHouses.Add(new UIXMLLotEntry()
                        {
                            Filename = GetString("2"),
                            Path = Path.Combine(FSOEnvironment.ContentDir, "Blueprints/empty_lot_fso.xml")
                        });

                        try
                        {
                            string[] paths = Directory.GetFiles(Path.Combine(FSOEnvironment.ContentDir, "LocalHouse/"), "*.fsov", SearchOption.AllDirectories);
                            for (int i = 0; i < paths.Length; i++)
                            {
                                string entry = paths[i];
                                if (!entry.ToLowerInvariant().EndsWith(".fsor"))
                                    entry = entry.Substring(0, entry.Length - 5) + ".xml";
                                string filename = Path.GetFileNameWithoutExtension(entry);
                                if (!xmlHouses.Any(x => x.Filename == filename))
                                {
                                    xmlHouses.Add(new UIXMLLotEntry { Filename = filename, Path = entry });
                                }
                            }
                        }
                        catch { }
                        break;
                    }
                case UISandboxCategory.Job:
                    {
                        string[] included = Directory.GetFiles(Path.Combine(FSOEnvironment.ContentDir, "Blueprints/"), "*.xml", SearchOption.AllDirectories);
                        string[] includedNames = [.. included.Select(x => Path.GetFileNameWithoutExtension(x))];
                        string[] paths = Directory.GetFiles(Path.Combine(GlobalSettings.Default.StartupPath, @"housedata/blueprints/"), "*.xml", SearchOption.AllDirectories);
                        for (int i = 0; i < paths.Length; i++)
                        {
                            string entry = paths[i];
                            string filename = Path.GetFileNameWithoutExtension(entry);

                            int replacementInd = Array.IndexOf(includedNames, filename);
                            if (replacementInd != -1)
                            {
                                entry = included[replacementInd];
                            }

                            xmlHouses.Add(new UIXMLLotEntry { Filename = filename, Path = entry });
                        }
                        break;
                    }
            }

            SaveTable.Items = [.. xmlHouses.Select(x => new UIListBoxItem(x, x.Filename))];
            SaveTable.SelectedIndex = 0;

            SavesButton.Selected = category == UISandboxCategory.Saved;
            JobButton.Selected = category == UISandboxCategory.Job;
        }

        public override void Removed()
        {
            base.Removed();

            LotThumb.DisposeThumbnail();
        }

        private void OpenCAS(UIElement button)
        {
            if (UIScreen.Current is SandboxGameScreen screen)
            {
                screen.CleanupLastWorld();
            }
            FSOFacade.Controller.ShowPersonCreation(null);
        }

        private void JoinLot(UIElement button)
        {
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Message = GetString("10"),
                Width = 400,
                TextEntry = true,
                Buttons =
                [
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
                ]
            }, true);
            alert.ResponseText = "127.0.0.1";
        }

        private void BookmarkListBox_OnDoubleClick(UIElement button)
        {
            if (GlobalSettings.Default.DebugBody == 0)
            {
                OpenCAS(button);
                return;
            }

            if (SaveTable.SelectedItem == null) { return; }
            var item = (UIXMLLotEntry)SaveTable.SelectedItem.Data;
            UIScreen.RemoveDialog(this);
            LotSwitch(item.Path, false);
        }

        private void CloseButton_OnButtonClick(UIElement button)
        {
            UIScreen.RemoveDialog(this);
        }
    }
}
