using FSO.Client.Controllers;
using FSO.Client.Model.Archive;
using FSO.Client.UI.Archive.Management;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.Utils;
using FSO.Server.Embedded;
using FSO.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveCreateServer : UIArchiveDialog
    {
        private const int CITY_IMAGE_WIDTH = 148;
        private const int CITY_IMAGE_HEIGHT = 112;
        private const int CITY_IMAGE_RADIUS = 8;
        private const int CITY_IMAGE_MARGIN = 4;

        private struct ServerSubFlag
        {
            public ArchiveConfigFlags Value;
            public string Caption;
            public UIButton FlagCheck;
            public UILabel Label;

            public ServerSubFlag(ArchiveConfigFlags value, string caption)
            {
                Value = value;
                Caption = caption;
            }
        }

        private struct ServerFlag
        {
            public ArchiveConfigFlags Value;
            public string Caption;
            public int Indentation;
            public Action HelpAction;
            public UIButton FlagCheck;
            public ServerSubFlag[] SubFlags;

            public ServerFlag(ArchiveConfigFlags value, string caption, int indentation = 0, Action helpAction = null, ServerSubFlag[] subFlags = null)
            {
                Value = value;
                Caption = caption;
                Indentation = indentation;
                HelpAction = helpAction;
                FlagCheck = null;
                SubFlags = subFlags;
            }
        }

        private ServerFlag[] Flags =
        [
            new ServerFlag(ArchiveConfigFlags.Offline, GetString("200")),
            new ServerFlag(ArchiveConfigFlags.UPnP, GetString("201"), 0, UPnPHelp),
            new ServerFlag(ArchiveConfigFlags.Verification, GetString("202"), 0, VerificationHelp),
            new ServerFlag(ArchiveConfigFlags.CityEditor, GetString("203"), 0, CityEditorHelp, [new ServerSubFlag(ArchiveConfigFlags.CityEditorMods, GetString("220")), new ServerSubFlag(ArchiveConfigFlags.CityEditorAllUsers, GetString("221"))]),
            default, // Gap (flag value is 0)
            new ServerFlag(ArchiveConfigFlags.AllOpenable, GetString("204"), 0, AllOpenableHelp),
            new ServerFlag(ArchiveConfigFlags.DebugFeatures, GetString("205"), 0, DebugModeHelp, [new ServerSubFlag(ArchiveConfigFlags.DebugFeaturesMods, GetString("220")), new ServerSubFlag(ArchiveConfigFlags.DebugFeaturesAllUsers, GetString("221"))]),
            new ServerFlag(ArchiveConfigFlags.AllowLotCreation, GetString("206")),
            new ServerFlag(ArchiveConfigFlags.AllowSimCreation, GetString("207")),
            new ServerFlag(ArchiveConfigFlags.LockArchivedSims, GetString("208"), 1, ArchivedCharacterHelp),
            new ServerFlag(ArchiveConfigFlags.HideNames, GetString("209")),
        ];

        private UIArchiveDisplayName DisplayName;
        private UIButton ExportButton;
        private UIButton UsersButton;
        private UIButton CustomPortsButton;
        private UIButton EventsButton;
        private UIButton CheatsButton;
        private UIButton StartButton;
        private UITextBox NameInput;
        private ArchiveConfiguration Config;
        private Texture2D HelpButtonTexture = GetTexture(0x0000034200000001);
        private UIImage CityImage;

        private UICombobox SaveCombo;

        public UIArchiveCreateServer() : base(UIDialogStyle.Close, true)
        {
            var gd = GameFacade.GraphicsDevice;
            var custom = Content.Content.Get().CustomUI;

            var clientConfig = ClientArchiveConfiguration.Default;
            Config = clientConfig.ToHostConfig();

            Caption = GetString("241");

            var vbox = new UIVBoxContainer();

            var headHbox = new UIHBoxContainer() { VerticalAlignment = UIContainerVerticalAlignment.Middle, Spacing = 10 };

            var imageBg = new UIImage(custom.Get("archive_translist.png").Get(gd)).With9Slice(13, 13, 13, 13);
            imageBg.SetSize(CITY_IMAGE_WIDTH + CITY_IMAGE_MARGIN * 2, CITY_IMAGE_HEIGHT + CITY_IMAGE_MARGIN * 2);
            headHbox.Add(imageBg);

            var saveVbox = new UIVBoxContainer()
            {
                Spacing = 0
            };

            saveVbox.Add(DisplayName = new UIArchiveDisplayName());
            saveVbox.Add(new UISpacer(8));

            SaveCombo = new UICombobox()
            {
                Width = 160
            };
            SaveCombo.OnSelect += UpdateSelectedSave;

            saveVbox.Add(SaveCombo);
            saveVbox.Add(new UISpacer(5));

            PopulateSaves();
            SelectSaveByName(clientConfig.SelectedArchiveName);

            saveVbox.Add(new UILabel()
            {
                Caption = GetString("230")
            });
            saveVbox.Add(new UISpacer(2));

            saveVbox.Add(NameInput = new UITextBox()
            {
                Size = new Microsoft.Xna.Framework.Vector2(160, 25),
                CurrentText = clientConfig.GetServerNameOrDefault(),
            });

            saveVbox.AutoSize();

            headHbox.Add(saveVbox);

            headHbox.AutoSize();
            vbox.Add(headHbox);

            var flagsVbox = new UIVBoxContainer();

            for (int i = 0; i < Flags.Length; i++)
            {
                ref var flag = ref Flags[i];

                if (flag.Value != ArchiveConfigFlags.None)
                {
                    var flagHbox = new UIHBoxContainer();

                    var check = new UIButton(GetTexture(0x0000083600000001));
                    check.Selected = Config.Flags.HasFlag(flag.Value);

                    if (flag.Indentation > 0)
                    {
                        flagHbox.Add(new UISpacer(16, 1));
                    }

                    flag.FlagCheck = check;

                    flagHbox.Add(check);
                    var value = flag.Value;

                    check.OnButtonClick += (elem) =>
                    {
                        ToggleFlag(value);
                    };

                    flagHbox.Add(new UILabel()
                    {
                        Caption = flag.Caption,
                    });

                    if (flag.HelpAction != null)
                    {
                        UIButton helpBtn = new UIButton(HelpButtonTexture);
                        var helpAction = flag.HelpAction;
                        helpBtn.OnButtonClick += (elem) => helpAction();
                        flagHbox.Add(helpBtn);
                    }

                    if (flag.SubFlags != null)
                    {
                        for (int j = 0; j < flag.SubFlags.Length; j++)
                        {
                            flagHbox.Add(new UISpacer(0));

                            ref var sub = ref flag.SubFlags[j];

                            var subcheck = new UIButton(GetTexture(0x0000083600000001))
                            {
                                Visible = check.Selected,
                                Selected = Config.Flags.HasFlag(sub.Value)
                            };
                            sub.FlagCheck = subcheck;

                            flagHbox.Add(subcheck);
                            var subvalue = sub.Value;

                            subcheck.OnButtonClick += (elem) =>
                            {
                                ToggleFlag(subvalue);
                            };

                            var label = new UILabel()
                            {
                                Caption = sub.Caption,
                                Visible = check.Selected,
                            };

                            flagHbox.Add(label);
                            sub.Label = label;
                        }
                    }

                    flagHbox.AutoSize();

                    flagsVbox.Add(flagHbox);
                }
                else
                {
                    flagsVbox.Add(new UISpacer(16));
                }
            }

            vbox.Add(new UISpacer(5));

            flagsVbox.AutoSize();

            vbox.Add(flagsVbox);

            vbox.Add(new UISpacer(10));

            var actionsHbox = new UIHBoxContainer() { Spacing = 10 };

            actionsHbox.Add(ExportButton = new UIButton(custom.Get("archive_configexport.png").Get(gd))
            {
                Tooltip = GetString("231")
            });

            actionsHbox.Add(UsersButton = new UIButton(custom.Get("archive_configusers.png").Get(gd))
            {
                Tooltip = GetString("232")
            });

            actionsHbox.Add(CustomPortsButton = new UIButton(custom.Get("archive_configports.png").Get(gd))
            {
                Tooltip = GetString("233")
            });

            actionsHbox.Add(EventsButton = new UIButton(custom.Get("archive_configevents.png").Get(gd))
            {
                Tooltip = GetString("234")
            });

            actionsHbox.Add(CheatsButton = new UIButton(custom.Get("archive_configcheats.png").Get(gd))
            {
                Tooltip = GetString("235")
            });

            Add(StartButton = new UIButton()
            {
                Caption = GetString("79")
            });

            actionsHbox.AutoSize();
            vbox.Add(actionsHbox);

            vbox.AutoSize();
            vbox.Position = new Vector2(20, 45);

            // Manually position the start button at the bottom right of the box.

            StartButton.Position = vbox.Position + vbox.Size - StartButton.Size + new Vector2(0, 5);

            // (hack) Move to end so it draws on top.
            saveVbox.Remove(SaveCombo);
            saveVbox.Add(SaveCombo);

            vbox.Remove(headHbox);
            vbox.Add(headHbox);

            // Added after auto sizing, since it floats on top.
            headHbox.Add(CityImage = new UIImage()
            {
                Position = imageBg.Position + new Vector2(CITY_IMAGE_MARGIN),
                Size = new Vector2(CITY_IMAGE_WIDTH, CITY_IMAGE_HEIGHT),
            });

            UpdateSelectedSave(SaveCombo);

            SetSize((int)vbox.Size.X + 40, (int)vbox.Size.Y + 70);
            DynamicOverlay.Add(vbox);

            NameInput.OnChange += ValidateInputs;
            CustomPortsButton.OnButtonClick += ChangePorts;
            EventsButton.OnButtonClick += EditEvents;
            CheatsButton.OnButtonClick += Cheats;
            StartButton.OnButtonClick += Start;
            CloseButton.OnButtonClick += Close;
            ExportButton.OnButtonClick += Export;
            UsersButton.OnButtonClick += Users;
            DisplayName.OnChange += DisplayNameChanged;

            ValidateInputs(NameInput);

            UpdateButtons();
        }

        private void DisplayNameChanged(string newName)
        {
            var clientConfig = ClientArchiveConfiguration.Default;
            var defaultName = clientConfig.GetDefaultServerName();

            if (NameInput.CurrentText == defaultName)
            {
                clientConfig.PlayerName = newName;
                NameInput.CurrentText = clientConfig.GetDefaultServerName();
            }
        }

        private void Cheats(UIElement button)
        {
            var selected = SaveCombo.SelectedItem as ArchiveManifest;

            var factory = new ArchiveServerFactory(Config, null);
            factory.Prepare(selected, (success) =>
            {
                if (success)
                {
                    UIScreen.GlobalShowDialog(new UIArchiveGameplayScale(Config), true);
                }
            });
        }

        private void NewFromTemplate(ArchiveManifest template)
        {
            var cityPicker = new UIArchiveCitySelector(template);
            cityPicker.OnResult += (ArchiveManifest manifest) =>
            {
                SaveCombo.SelectedIndex = -1;
                PopulateSaves();

                int index = -1;
                if (manifest != null)
                {
                    index = SaveCombo.Items.FindIndex(x => ((ArchiveManifest)x.Value).ActivePath == manifest.ActivePath);
                }

                if (index == -1)
                {
                    var clientConfig = ClientArchiveConfiguration.Default;
                    SelectSaveByName(clientConfig.SelectedArchiveName);
                }
                else
                {
                    SaveCombo.SelectedIndex = index;
                }
            };

            UIScreen.ShowDialog(cityPicker, true);
        }

        private Texture2D LoadCityThumbnail(string path)
        {
            try
            {
                //Take a copy so we dont change the original when we alpha mask it
                Texture2D cityThumbTex = TextureUtils.Resize(GameFacade.GraphicsDevice, TextureUtils.TextureFromFile(
                   GameFacade.GraphicsDevice, path), CITY_IMAGE_WIDTH, CITY_IMAGE_HEIGHT);

                var mask = TextureGenerator.GenerateRoundedRectangle(GameFacade.GraphicsDevice, Color.White, CITY_IMAGE_WIDTH, CITY_IMAGE_HEIGHT, CITY_IMAGE_RADIUS);
                TextureUtils.CopyAlpha(ref cityThumbTex, mask);

                mask.Dispose();

                return cityThumbTex;
            }
            catch
            {
                return null;
            }
        }

        private void UpdateSelectedSave(object obj)
        {
            if (CityImage == null)
            {
                return;
            }

            if (CityImage.Texture != null)
            {
                CityImage.Texture.Dispose();
            }

            if (SaveCombo.SelectedIndex == -1)
            {
                CityImage.Texture = null;
                return;
            }

            var selected = SaveCombo.SelectedItem as ArchiveManifest;

            if (selected.Template)
            {
                NewFromTemplate(selected);
            }

            // Load the city image.

            try
            {
                if (selected.LocalDir != null)
                {
                    // TODO: get archive shard? currently just assumes it's 1
                    var customThumbPath = Path.Combine(Path.GetDirectoryName(selected.ActivePath), selected.LocalDir, "City1/thumbnail.png");

                    if (File.Exists(customThumbPath))
                    {
                        CityImage.Texture = LoadCityThumbnail(customThumbPath);
                        return;
                    }
                }
            }
            catch
            {
                // Try load the default map image
            }

            string map = selected.Map;
            var fsoMap = int.Parse(map) >= 100;


            var cityThumb = (fsoMap) ?
                Path.Combine(FSOEnvironment.ContentDir, "Cities/city_" + map + "/thumbnail.png")
                : GameFacade.GameFilePath("cities/city_" + map + "/thumbnail.bmp");

            CityImage.Texture = LoadCityThumbnail(cityThumb);
        }

        private void EditEvents(UIElement button)
        {
            var selected = SaveCombo.SelectedItem as ArchiveManifest;

            var factory = new ArchiveServerFactory(Config, null);
            factory.Prepare(selected, (success) =>
            {
                if (success)
                {
                    UIScreen.GlobalShowDialog(new UIArchiveEventsDialog(factory.GetConfig()), true);
                }
            });
        }

        private void Users(UIElement button)
        {
            var selected = SaveCombo.SelectedItem as ArchiveManifest;

            var factory = new ArchiveServerFactory(Config, null);
            factory.Prepare(selected, (success) =>
            {
                if (success)
                {
                    UIScreen.GlobalShowDialog(new UIArchiveUserManageDialog(new ArchiveManagement(factory.GetConfig())), true);
                }
            });
        }

        private void SelectSaveByName(string name)
        {
            SaveCombo.SelectedIndex = Math.Max(0, SaveCombo.Items.FindIndex((item) => item.Name == name));
        }

        private void ChangePorts(UIElement button)
        {
            UIArchiveServerPorts portDialog = null;
            portDialog = new UIArchiveServerPorts(Config, () =>
            {
                if (portDialog.GetLotPort(out ushort lotPort))
                {
                    Config.LotPort = lotPort;
                }

                if (portDialog.GetCityPort(out ushort cityPort))
                {
                    Config.CityPort = cityPort;
                }
            });

            UIScreen.GlobalShowDialog(portDialog, true);
        }

        private void Export(UIElement button)
        {
            var selected = SaveCombo.SelectedItem as ArchiveManifest;
            UIScreen.GlobalShowDialog(new UIArchiveConfigExportDialog(Config, selected), true);
        }

        private void PopulateSaves()
        {
            var manifests = ArchiveSaves.ListManifests();
            var templates = ArchiveSaves.ListManifests(true);

            SaveCombo.Items = [.. manifests.Select(x => new UIComboboxItem() { Name = x.Name, Value = x }), .. templates.Select(x => new UIComboboxItem() { Name = x.Name, Value = x }),];

            SaveCombo.SelectedIndex = manifests.Count > 0 ? 0 : -1;
        }

        private void UpdateButtons()
        {
            CustomPortsButton.Disabled = Config.Flags.HasFlag(ArchiveConfigFlags.UPnP);
            CustomPortsButton.Tooltip = CustomPortsButton.Disabled ? GetString("18") : GetString("233");
        }

        private void ToggleFlag(ArchiveConfigFlags flag)
        {
            Config.Flags ^= flag;

            foreach (var item in Flags)
            {
                bool selected = (item.Value & Config.Flags) != 0;
                if (item.FlagCheck != null)
                {
                    item.FlagCheck.Selected = selected;
                }

                if (item.SubFlags != null)
                {
                    foreach (var sub in item.SubFlags)
                    {
                        if (sub.FlagCheck != null)
                        {
                            sub.FlagCheck.Visible = selected;
                            sub.FlagCheck.Selected = (sub.Value & Config.Flags) != 0;
                        }

                        if (sub.Label != null)
                        {
                            sub.Label.Visible = selected;
                        }
                    }
                }
            }

            UpdateButtons();
        }

        private void Close(Framework.UIElement button)
        {
            SaveConfig();
            FindController<ConnectArchiveController>().SwitchMode(ConnectArchiveMode.Landing);
        }

        private void ValidateInputs(Framework.UIElement element)
        {
            StartButton.Disabled = NameInput.CurrentText.Length == 0;
        }

        private void Start(Framework.UIElement button)
        {
            SaveConfig();

            Visible = false;
            var selected = SaveCombo.SelectedItem as ArchiveManifest;

            var factory = new ArchiveServerFactory(Config, FindController<ConnectArchiveController>());

            factory.Start(selected, (bool success) =>
            {
                if (!success)
                {
                    Visible = true;
                }
            });
        }

        private void SaveConfig()
        {
            var clientConfig = ClientArchiveConfiguration.Default;
            var selected = SaveCombo.SelectedItem as ArchiveManifest;

            var defaultName = clientConfig.GetDefaultServerName();
            clientConfig.ServerName = defaultName == NameInput.CurrentText ? "" : NameInput.CurrentText;
            Config.Name = clientConfig.GetServerNameOrDefault();

            clientConfig.ApplyHostConfig(Config);
            clientConfig.SelectedArchiveName = selected?.Name ?? "";
            clientConfig.Save();
        }

        public static void UPnPHelp()
        {
            UIAlert.Alert(GetString("201"), GetString("211"), true);
        }

        public static void AllOpenableHelp()
        {
            UIAlert.Alert(GetString("204"), GetString("214"), true);
        }

        public static void DebugModeHelp()
        {
            UIAlert.Alert(GetString("205"), GetString("215"), true);
        }

        public static void ArchivedCharacterHelp()
        {
            UIAlert.Alert(GetString("208"), GetString("218"), true);
        }

        public static void VerificationHelp()
        {
            UIAlert.Alert(GetString("202"), GetString("212"), true);
        }

        public static void CityEditorHelp()
        {
            UIAlert.Alert(GetString("203"), GetString("121"), true);
        }
    }
}
