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
    internal class UIArchiveCreateServer : UIDialog
    {
        private const int CITY_IMAGE_WIDTH = 148;
        private const int CITY_IMAGE_HEIGHT = 112;
        private const int CITY_IMAGE_RADIUS = 8;
        private const int CITY_IMAGE_MARGIN = 4;

        private struct ServerFlag
        {
            public ArchiveConfigFlags Value;
            public string Caption;
            public bool DefaultValue;
            public int Indentation;
            public Action HelpAction;
            public UIButton FlagCheck;

            public ServerFlag(ArchiveConfigFlags value, string caption, bool defaultValue, int indentation = 0, Action helpAction = null)
            {
                Value = value;
                Caption = caption;
                DefaultValue = defaultValue;
                Indentation = indentation;
                HelpAction = helpAction;
                FlagCheck = null;
            }
        }

        private ServerFlag[] Flags = new ServerFlag[]
        {
            new ServerFlag(ArchiveConfigFlags.Offline, "Offline mode", false),
            new ServerFlag(ArchiveConfigFlags.UPnP, "Use UPnP", true, 0, UPnPHelp),
            new ServerFlag(ArchiveConfigFlags.HideNames, "Hide display names", false),
            new ServerFlag(ArchiveConfigFlags.Verification, "Require user verification", false, 0, VerificationHelp),
            default, // Gap (flag value is 0)
            new ServerFlag(ArchiveConfigFlags.AllOpenable, "All lots openable", true, 0, AllOpenableHelp),
            new ServerFlag(ArchiveConfigFlags.DebugFeatures, "All-player debug mode", false, 0, DebugModeHelp),
            //new ServerFlag(ArchiveConfigFlags.None, "Skill/money speed scale", false),
            new ServerFlag(ArchiveConfigFlags.AllowLotCreation, "Allow lot creation", true),
            new ServerFlag(ArchiveConfigFlags.AllowSimCreation, "Allow character creation", true),
            new ServerFlag(ArchiveConfigFlags.LockArchivedSims, "Lock archived characters", false, 1, ArchivedCharacterHelp),
            new ServerFlag(ArchiveConfigFlags.CityEditor, "City editor", false, 0, CityEditorHelp),
        };

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

            Caption = "Host Server";

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
                Caption = "Server name:"
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
                Tooltip = "Export Config"
            });

            actionsHbox.Add(UsersButton = new UIButton(custom.Get("archive_configusers.png").Get(gd))
            {
                Tooltip = "Users"
            });

            actionsHbox.Add(CustomPortsButton = new UIButton(custom.Get("archive_configports.png").Get(gd))
            {
                Tooltip = "Ports"
            });

            actionsHbox.Add(EventsButton = new UIButton(custom.Get("archive_configevents.png").Get(gd))
            {
                Tooltip = "Events"
            });

            actionsHbox.Add(CheatsButton = new UIButton(custom.Get("archive_configcheats.png").Get(gd))
            {
                Tooltip = "Cheats"
            });

            Add(StartButton = new UIButton()
            {
                Caption = "Start"
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

            string map = selected.Map;

            var fsoMap = int.Parse(map) >= 100;

            try
            {
                var cityThumb = (fsoMap) ?
                Path.Combine(FSOEnvironment.ContentDir, "Cities/city_" + map + "/thumbnail.png")
                : GameFacade.GameFilePath("cities/city_" + map + "/thumbnail.bmp");

                //Take a copy so we dont change the original when we alpha mask it
                Texture2D cityThumbTex = TextureUtils.Resize(GameFacade.GraphicsDevice, TextureUtils.TextureFromFile(
                   GameFacade.GraphicsDevice, cityThumb), CITY_IMAGE_WIDTH, CITY_IMAGE_HEIGHT);

                var mask = TextureGenerator.GenerateRoundedRectangle(GameFacade.GraphicsDevice, Color.White, CITY_IMAGE_WIDTH, CITY_IMAGE_HEIGHT, CITY_IMAGE_RADIUS);
                TextureUtils.CopyAlpha(ref cityThumbTex, mask);

                mask.Dispose();

                CityImage.Texture = cityThumbTex;
            }
            catch
            {
                CityImage.Texture = null;
                return;
            }
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

        private ArchiveConfigFlags GetDefaultFlags()
        {
            ArchiveConfigFlags result = ArchiveConfigFlags.None;

            foreach (var flag in Flags)
            {
                if (flag.DefaultValue)
                {
                    result |= flag.Value;
                }
            }

            return result;
        }

        private void UpdateButtons()
        {
            CustomPortsButton.Disabled = Config.Flags.HasFlag(ArchiveConfigFlags.UPnP);
            CustomPortsButton.Tooltip = CustomPortsButton.Disabled ? GameFacade.Strings.GetString("f128", "18") : "Ports";
        }

        private void ToggleFlag(ArchiveConfigFlags flag)
        {
            Config.Flags ^= flag;

            foreach (var item in Flags)
            {
                if (item.FlagCheck != null)
                {
                    item.FlagCheck.Selected = (item.Value & Config.Flags) != 0;
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
            UIAlert.Alert("UPnP", "UPnP attempts to automatically forward ports on your router to allow public access to your game server. Some routers have this disabled by default or simply don't support it, in which case you'll need to uncheck this option and manually forward the ports.", true);
        }

        public static void AllOpenableHelp()
        {
            UIAlert.Alert("All lots openable", "The original game server only allowed offline lots to be opened by their owner or roommates. When this option is set to true, any player can open any lot, as long as they are allowed to enter.", true);
        }

        public static void DebugModeHelp()
        {
            UIAlert.Alert("All-player debug mode", "The archive server gives admins the ability to spawn debug objects and use debug interactions. When this option is set to true, anyone can use in-lot debug features regardless of permissions.", true);
        }

        public static void ArchivedCharacterHelp()
        {
            UIAlert.Alert("Lock archived characters", "The archive server allows players to enter the game as any character that has been archived, or create their own. When this option is set to true, only admins and mods can enter the game as archived characters - normal users must create their own.", true);
        }

        public static void VerificationHelp()
        {
            UIAlert.Alert("User verification", "Without user verification, any user with the server IP can connect and join the city - authentication is automatic. Users can be banned by client and IP, but new users are always given the benefit of the doubt. \n\nWhen this option is enabled, new users will require verification from an admin or mod before they can interact with the game server. You can verify users from the User List ingame, which appears at the bottom left in the UCP. The button will start flashing if there are any pending verifications.", true);
        }
        public static void CityEditorHelp()
        {
            UIAlert.Alert("City editor", GameFacade.Strings.GetString("f128", "121"), true);
        }
    }
}
