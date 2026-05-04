using FSO.Client.Controllers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.UI.Controls;
using FSO.UI.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Net;
using System.Net.NetworkInformation;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveHostInformation : UIDialog
    {
        private enum ArchiveServerType : int
        {
            ThisClient,
            OtherClient,
            Dedicated,
            Offline
        }

        public UIVBoxContainer Container;
        public UILabel ServerTypeLabel;
        public UILabel ServerWarningLabel;
        public UIButton ShowIPButton;
        public UIButton DiscordButton;

        public UIHBoxContainer PublicIPContainer;
        public UILabel PublicIPLabel;

        private ArchiveServerType ServerType;
        private Texture2D CopyButtonTexture;
        private TextStyle TitleStyle;
        private TextStyle InterfaceStyle;
        private TextStyle CopyStyle;

        private string Port;

        public UIArchiveHostInformation(CoreGameScreenController controller) : base(UIDialogStyle.Close, false)
        {
            Caption = GameFacade.Strings.GetString("f128", "19");
            var ui = Content.Content.Get().CustomUI;
            CopyButtonTexture = ui.Get("chat_cat.png").Get(GameFacade.GraphicsDevice);

            TitleStyle = TextStyle.DefaultLabel.Clone();
            TitleStyle.Color = Color.White;
            TitleStyle.Shadow = true;
            TitleStyle.Size = 15;

            InterfaceStyle = TextStyle.DefaultLabel.Clone();
            InterfaceStyle.Color = new Color(new Vector3(0.7f));

            CopyStyle = TextStyle.DefaultLabel.Clone();
            CopyStyle.Size = 8;
            CopyStyle.Shadow = true;

            DiscordButton = new UIButton(ui.Get("archive_discord.png").Get(GameFacade.GraphicsDevice));
            DiscordButton.OnButtonClick += EnableDiscord;

            Add(DiscordButton);

            UpdateDiscordButtonState();

            var addr = controller.ArchiveHost.CityAddress;
            int colonInd = addr.LastIndexOf(':');
            Port = colonInd == -1 ? ":33101" : addr.Substring(colonInd);

            var warningStyle = TextStyle.DefaultLabel.Clone();
            warningStyle.Size--;
            warningStyle.Color = new Color(255, 122, 77);

            var serverType = GetServerType(controller);
            ServerType = serverType;

            var vbox = new UIVBoxContainer()
            {
                HorizontalAlignment = UIContainerHorizontalAlignment.Center
            };
            vbox.Position = new Vector2(20, 45);

            vbox.Add(ServerTypeLabel = new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f128", (20 + (int)serverType).ToString()),
                Wrapped = true,
            });

            if (serverType < ArchiveServerType.Dedicated)
            {
                vbox.Add(ServerWarningLabel = new UILabel()
                {
                    Caption = GameFacade.Strings.GetString("f128", "24"),
                    CaptionStyle = warningStyle
                });
            }

            if (serverType < ArchiveServerType.Offline)
            {
                vbox.Add(new UISpacer(1, 8));

                vbox.Add(ShowIPButton = new UIButton()
                {
                    Caption = GameFacade.Strings.GetString("f128", "25")
                });

                ShowIPButton.OnButtonClick += ShowIPs;
            }

            Add(vbox);

            Container = vbox;

            RecalculateSize();

            Background.BlockInput();
        }

        private void UpdateDiscordButtonState()
        {
            var enabled = DiscordRpcEngine.PublicArchive;

            DiscordButton.Tooltip = GameFacade.Strings.GetString("f128", enabled ? "112" : "110");
            DiscordButton.Disabled = enabled;
        }

        private void EnableDiscord(UIElement button)
        {
            DiscordButton.Disabled = true;

            if (ServerType == ArchiveServerType.ThisClient)
            {
                DetermineMyPublicIp().ContinueWith((task) =>
                {
                    GameThread.InUpdate(() =>
                    {
                        if (task.IsCanceled || task.IsFaulted)
                        {
                            UIAlert.Alert("", GameFacade.Strings.GetString("f128", "119"), true);
                        }
                        else
                        {
                            DiscordRpcEngine.SetArchiveAddress(AddPort(task.Result));
                            UIAlert.Alert(GameFacade.Strings.GetString("f128", "110"), GameFacade.Strings.GetString("f128", "111"), true);
                        }

                        UpdateDiscordButtonState();
                    });
                });
            }
            else
            {
                var ip = FindController<CoreGameScreenController>().ArchiveHost.CityAddress;

                DiscordRpcEngine.SetArchiveAddress(ip);
                UIAlert.Alert(GameFacade.Strings.GetString("f128", "110"), GameFacade.Strings.GetString("f128", "111"), true);

                UpdateDiscordButtonState();
            }
        }

        public override void Update(UpdateState state)
        {
            PositionDialog();

            base.Update(state);
        }

        private ArchiveServerType GetServerType(CoreGameScreenController controller)
        {
            if (controller == null || controller.Mode != Regulators.CityConnectionMode.ARCHIVE || controller.ArchiveConfig.HasFlag(Common.ArchiveConfigFlags.Offline))
            {
                return ArchiveServerType.Offline;
            }

            if (controller.ArchiveHost.SelfHost)
            {
                return ArchiveServerType.ThisClient;
            }

            if (controller.ArchiveConfig.HasFlag(Common.ArchiveConfigFlags.DedicatedServer))
            {
                return ArchiveServerType.Dedicated;
            }

            return ArchiveServerType.OtherClient;
        }

        private void PositionDialog()
        {
            var screenWidth = GameFacade.Screens.CurrentUIScreen.ScreenWidth;

            var pos = new Vector2(MathF.Round((screenWidth - Size.X) / 2), 24);

            if (Position != pos)
            {
                Position = pos;
            }
        }

        public void ShowIPs(UIElement element)
        {
            var vbox = Container;
            vbox.Remove(ShowIPButton);

            vbox.Add(new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f128", "26"), // Public IP:
                CaptionStyle = TitleStyle,
            });

            var hbox = new UIHBoxContainer();
            hbox.Add(PublicIPLabel = new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f128", "27") // fetching
            });
            hbox.AutoSize();

            PublicIPContainer = hbox;

            vbox.Add(hbox);

            if (ServerType == ArchiveServerType.ThisClient)
            {
                DetermineMyPublicIp();

                vbox.Add(new UISpacer(1, 8));

                // Display private IPs too
                vbox.Add(new UILabel()
                {
                    Caption = GameFacade.Strings.GetString("f128", "28"), // Private IPs:
                    CaptionStyle = TitleStyle,
                });

                NetworkInterface[] network = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface intf in network)
                {
                    var props = intf.GetIPProperties();
                    foreach (var unicast in props.UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            var ip = unicast.Address.ToString();

                            if (ip == "127.0.0.1")
                            {
                                // Not really useful to know the loopback.
                                continue;
                            }

                            hbox = new UIHBoxContainer();
                            hbox.Add(new UILabel()
                            {
                                Caption = $"{intf.Name}:",
                                CaptionStyle = InterfaceStyle
                            });
                            hbox.Add(new UILabel()
                            {
                                Caption = AddPort(ip)
                            });

                            AddCopyButton(hbox, AddPort(ip));

                            vbox.Add(hbox);
                        }
                    }
                }
            }
            else
            {
                var ip = FindController<CoreGameScreenController>().ArchiveHost.CityAddress;

                PublicIPLabel.Caption = ip;
                PublicIPLabel.Size = default;
                PublicIPLabel.AutoSize();
                AddCopyButton(PublicIPContainer, ip);
                RecalculateSize();
            }

            RecalculateSize();
        }

        private void AddCopyButton(UIHBoxContainer container, string copyString)
        {
            var btn = new UIButton()
            {
                Texture = CopyButtonTexture,
                Caption = GameFacade.Strings.GetString("f128", "33"), // Copy
                CaptionStyle = CopyStyle,
            };

            btn.OnButtonClick += (elem) =>
            {
                ClipboardHandler.Default.Set(copyString);
                UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    Message = GameFacade.Strings.GetString("f128", "34"), // Copied to clipboard
                }, true);
            };

            container.Add(btn);

            container.AutoSize();
        }

        private void RecalculateSize()
        {
            var vbox = Container;

            vbox.AutoSize();

            var showIPBase = vbox.Position + (ShowIPButton?.Position ?? new Vector2(0, vbox.Size.Y));
            DiscordButton.Position = new Vector2(vbox.Size.X - 22, showIPBase.Y + 4);

            SetSize((int)vbox.Size.X + 40, (int)vbox.Size.Y + 70);
            PositionDialog();
        }

        private string AddPort(string ip)
        {
            // When the port is different from the default,
            // append it to the IP.
            return Port == ":33101" ? ip : (ip + Port);
        }

        private Task<string> DetermineMyPublicIp()
        {
            return Task.Run(() =>
            {
                WebClient webClient = new WebClient();

                string result;
                try
                {
                    result = webClient.DownloadString("https://api.ipify.org");
                }
                catch
                {
                    result = null;
                }

                if (result != null && !IPAddress.TryParse(result, out IPAddress addr))
                {
                    result = null;
                }

                GameThread.InUpdate(() =>
                {
                    if (PublicIPLabel != null)
                    {
                        if (result == null)
                        {
                            PublicIPLabel.Caption = GameFacade.Strings.GetString("f128", "35");
                        }
                        else
                        {
                            PublicIPLabel.Caption = AddPort(result);
                            PublicIPLabel.Size = default;
                            PublicIPLabel.AutoSize();
                            AddCopyButton(PublicIPContainer, AddPort(result));
                            RecalculateSize();
                        }
                    }
                });

                return result;
            });
        }
    }
}
