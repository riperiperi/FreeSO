/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Model;
using FSO.Client.Rendering.City;
using Microsoft.Xna.Framework;
using FSO.Client.Utils;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework;
using FSO.Client.Network;
using FSO.LotView;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Utils;
using FSO.Debug;
using FSO.SimAntics.Primitives;
using FSO.HIT;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model.Commands;
using System.IO;
using FSO.SimAntics.NetPlay;
using FSO.Client.UI.Controls;
using FSO.Client.Debug;
using FSO.Client.UI.Panels.WorldUI;

namespace FSO.Client.UI.Screens
{
    public class CoreGameScreen : FSO.Client.UI.Framework.GameScreen
    {
        public UIUCP ucp;
        public UIGizmo gizmo;
        public UIInbox Inbox;
        public UIGameTitle Title;
        private UIButton SaveHouseButton;
        private string[] CityMusic;
        private String city;

        private bool Connecting;
        private UILoginProgress ConnectingDialog;

        private Terrain CityRenderer; //city view

        public UILotControl LotController; //world, lotcontrol and vm will be null if we aren't in a lot.
        private LotView.World World;
        public FSO.SimAntics.VM vm;
        public bool InLot
        {
            get
            {
                return (vm != null);
            }
        }

        private int m_ZoomLevel;
        public int ZoomLevel
        {
            get
            {
                return m_ZoomLevel;
            }
            set
            {
                value = Math.Max(1, Math.Min(5, value));
                if (value < 4)
                {
                    if (vm == null) ZoomLevel = 4; //call this again but set minimum cityrenderer view
                    else
                    {
                        Title.SetTitle(LotController.GetLotTitle());
                        if (m_ZoomLevel > 3)
                        {
                            PlayBackgroundMusic(new string[] { "none" }); //disable city music
                            if (CityRenderer != null) CityRenderer.Visible = false;
                            gizmo.Visible = false;
                            LotController.Visible = true;
                            World.Visible = true;
                            ucp.SetMode(UIUCP.UCPMode.LotMode);
                        }
                        m_ZoomLevel = value;
                        vm.Context.World.State.Zoom = (WorldZoom)(4 - ZoomLevel); //near is 3 for some reason... will probably revise
                    }
                }
                else //cityrenderer! we'll need to recreate this if it doesn't exist...
                {
                    Title.SetTitle(city);
                    if (CityRenderer == null) m_ZoomLevel = value; //set to far zoom... again, we should eventually create this.
                    else
                    {


                        if (m_ZoomLevel < 4)
                        { //coming from lot view... snap zoom % to 0 or 1
                            CityRenderer.m_ZoomProgress = (value == 4) ? 1 : 0;
                            PlayBackgroundMusic(CityMusic); //play the city music as well
                            CityRenderer.Visible = true;
                            gizmo.Visible = true;
                            if (World != null)
                            {
                                World.Visible = false;
                                LotController.Visible = false;
                            }
                            ucp.SetMode(UIUCP.UCPMode.CityMode);
                        }
                        m_ZoomLevel = value;
                        CityRenderer.m_Zoomed = (value == 4);
                    }
                }
                ucp.UpdateZoomButton();
            }
        } //in future, merge LotDebugScreen and CoreGameScreen so that we can store the City+Lot combo information and controls in there.

        private int _Rotation = 0;
        public int Rotation
        {
            get
            {
                return _Rotation;
            }
            set
            {
                _Rotation = value;
                if (World != null)
                {
                    switch (_Rotation)
                    {
                        case 0:
                            World.State.Rotation = WorldRotation.TopLeft; break;
                        case 1:
                            World.State.Rotation = WorldRotation.TopRight; break;
                        case 2:
                            World.State.Rotation = WorldRotation.BottomRight; break;
                        case 3:
                            World.State.Rotation = WorldRotation.BottomLeft; break;
                    }
                }
            }
        }

        public sbyte Level
        {
            get
            {
                if (World == null) return 1;
                else return World.State.Level;
            }
            set
            {
                if (World != null)
                {
                    World.State.Level = value;
                }
            }
        }

        public sbyte Stories
        {
            get
            {
                if (World == null) return 2;
                return World.Stories;
            }
        }

        public CoreGameScreen()
        {
            /** City Scene **/
            ListenForMouse(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new UIMouseEvent(MouseHandler));

            CityRenderer = new Terrain(GameFacade.Game.GraphicsDevice); //The Terrain class implements the ThreeDAbstract interface so that it can be treated as a scene but manage its own drawing and updates.

            city = "Queen Margaret's";
            if (PlayerAccount.CurrentlyActiveSim != null)
                city = PlayerAccount.CurrentlyActiveSim.ResidingCity.Name;

            CityRenderer.m_GraphicsDevice = GameFacade.GraphicsDevice;

            CityRenderer.Initialize(city, GameFacade.CDataRetriever);
            CityRenderer.LoadContent(GameFacade.GraphicsDevice);
            CityRenderer.RegenData = true;

            CityRenderer.SetTimeOfDay(0.5);

            /**
            * Music
            */
            CityMusic = new string[]{
                GlobalSettings.Default.StartupPath + "\\music\\modes\\map\\tsobuild1.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\map\\tsobuild3.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\map\\tsomap2_v2.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\map\\tsomap3.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\map\\tsomap4_v1.mp3"
            };
            PlayBackgroundMusic(CityMusic);

            /*VMDebug = new UIButton()
            {
                Caption = "Simantics",
                Y = 45,
                Width = 100,
                X = GlobalSettings.Default.GraphicsWidth - 110
            };
            VMDebug.OnButtonClick += new ButtonClickDelegate(VMDebug_OnButtonClick);
            this.Add(VMDebug);*/

            SaveHouseButton = new UIButton()
            {
                Caption = "Save House",
                Y = 10,
                Width = 100,
                X = GlobalSettings.Default.GraphicsWidth - 110
            };
            SaveHouseButton.OnButtonClick += new ButtonClickDelegate(SaveHouseButton_OnButtonClick);
            this.Add(SaveHouseButton);

            ucp = new UIUCP(this);
            ucp.Y = ScreenHeight - 210;
            ucp.SetInLot(false);
            ucp.UpdateZoomButton();
            ucp.MoneyText.Caption = PlayerAccount.Money.ToString();
            this.Add(ucp);

            gizmo = new UIGizmo();
            gizmo.X = ScreenWidth - 500;
            gizmo.Y = ScreenHeight - 300;
            this.Add(gizmo);

            Title = new UIGameTitle();
            Title.SetTitle(city);
            this.Add(Title);

            //OpenInbox();

            this.Add(GameFacade.MessageController);
            GameFacade.MessageController.OnSendLetter += new LetterSendDelegate(MessageController_OnSendLetter);
            GameFacade.MessageController.OnSendMessage += new MessageSendDelegate(MessageController_OnSendMessage);

            NetworkFacade.Controller.OnNewTimeOfDay += new OnNewTimeOfDayDelegate(Controller_OnNewTimeOfDay);
            NetworkFacade.Controller.OnPlayerJoined += new OnPlayerJoinedDelegate(Controller_OnPlayerJoined);

            //THIS IS KEPT HERE AS A DOCUMENTATION OF THE MESSAGE PASSING API FOR NOW.
            /*
            MessageAuthor Author = new MessageAuthor();
            Author.Author = "Whats His Face";
            Author.GUID = Guid.NewGuid().ToString();

            GameFacade.MessageController.PassMessage(Author, "you suck");
            GameFacade.MessageController.PassMessage(Author, "no rly");
            GameFacade.MessageController.PassMessage(Author, "jk im just testing message recieving please love me");

            Author.Author = "yer maw";
            Author.GUID = Guid.NewGuid().ToString();

            GameFacade.MessageController.PassMessage(Author, "dont let whats his face get to you");
            GameFacade.MessageController.PassMessage(Author, "i will always love you");

            Author.Author = "M.O.M.I";
            Author.GUID = Guid.NewGuid().ToString();

            GameFacade.MessageController.PassEmail(Author, "Ban Notice", "You have been banned for playing too well. \r\n\r\nWe don't know why you still have access to the game, but it's probably related to you playing the game pretty well. \r\n\r\nPlease stop immediately.\r\n\r\n - M.O.M.I. (this is just a test message btw, you're not actually banned)");
            */

            GameFacade.Scenes.Add(CityRenderer);

            ZoomLevel = 5; //screen always starts at far zoom, city visible.
        }

        #region Network handlers

        private void Controller_OnNewTimeOfDay(DateTime TimeOfDay)
        {
            if (TimeOfDay.Hour <= 12)
                ucp.TimeText.Caption = TimeOfDay.Hour + ":" + TimeOfDay.Minute + "am";
            else ucp.TimeText.Caption = TimeOfDay.Hour + ":" + TimeOfDay.Minute + "pm";

            double time = TimeOfDay.Hour / 24.0 + TimeOfDay.Minute / (1440.0) + TimeOfDay.Second / (86400.0);
        }

        private void Controller_OnPlayerJoined(LotTileEntry TileEntry)
        {
            LotTileEntry[] TileEntries = new LotTileEntry[GameFacade.CDataRetriever.LotTileData.Length + 1];
            TileEntries[0] = TileEntry;
            GameFacade.CDataRetriever.LotTileData.CopyTo(TileEntries, 1);
            CityRenderer.populateCityLookup(TileEntries);
        }

        #endregion

        private void MessageController_OnSendMessage(string message, string GUID)
        {
            //TODO: Implement special packet for message (as opposed to letter)?
            //Don't send empty strings!!
            Network.UIPacketSenders.SendLetter(Network.NetworkFacade.Client, message, "Empty", GUID);
        }

        /// <summary>
        /// Message was sent by player to another player.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="subject">Subject of message.</param>
        /// <param name="destinationUser">GUID of destination user.</param>
        private void MessageController_OnSendLetter(string message, string subject, string destinationUser)
        {
            Network.UIPacketSenders.SendLetter(Network.NetworkFacade.Client, message, subject, destinationUser);
        }

        public override void Update(FSO.Common.Rendering.Framework.Model.UpdateState state)
        {
            GameFacade.Game.IsFixedTimeStep = (vm == null || vm.Ready);

            base.Update(state);

            if (CityRenderer != null)
            {
                if (ZoomLevel > 3 && CityRenderer.m_Zoomed != (ZoomLevel == 4)) ZoomLevel = (CityRenderer.m_Zoomed) ? 4 : 5;

                if (InLot) //if we're in a lot, use the VM's more accurate time!
                    CityRenderer.SetTimeOfDay((vm.Context.Clock.Hours / 24.0) + (vm.Context.Clock.Minutes / 1440.0) + (vm.Context.Clock.Seconds / 86400.0));
            }

            if (vm != null) vm.Update();
        }

        public void CleanupLastWorld()
        {
            if (ZoomLevel < 4) ZoomLevel = 5;
            vm.Context.Ambience.Kill();
            vm.CloseNet();
            GameFacade.Scenes.Remove(World);
            this.Remove(LotController);
            ucp.SetPanel(-1);
            ucp.SetInLot(false);
        }

        public void ClientStateChange(int state, float progress)
        {
            //TODO: queue these up and try and sift through them in an update loop to avoid UI issues. (on main thread)
            if (state == 4) //disconnected
            {
                var alert = UIScreen.ShowAlert(new UIAlertOptions
                {
                    Title = GameFacade.Strings.GetString("222", "3"),
                    Message = GameFacade.Strings.GetString("222", "2", new string[] { "0" }),
                }, true);

                if (Connecting)
                {
                    UIScreen.RemoveDialog(ConnectingDialog);
                    ConnectingDialog = null;
                    Connecting = false;
                }

                alert.ButtonMap[UIAlertButtonType.OK].OnButtonClick += DisconnectedOKClick;
            }

            if (ConnectingDialog == null) return;
            switch (state)
            {
                case 1:
                    ConnectingDialog.ProgressCaption = GameFacade.Strings.GetString("211", "26");
                    ConnectingDialog.Progress = 25f;
                    break;
                case 2:
                    ConnectingDialog.ProgressCaption = GameFacade.Strings.GetString("211", "27");
                    ConnectingDialog.Progress = 100f*(0.5f+progress*0.5f);
                    break;
                case 3:
                    UIScreen.RemoveDialog(ConnectingDialog);
                    ConnectingDialog = null;
                    Connecting = false;
                    ZoomLevel = 1;
                    ucp.SetInLot(true);
                    break;
            }
        }

        private void DisconnectedOKClick(UIElement button)
        {
            if (vm != null) CleanupLastWorld();
            Connecting = false;
        }

        public void InitTestLot(string path, bool host)
        {
            if (Connecting) return;

            if (vm != null) CleanupLastWorld();

            World = new LotView.World(GameFacade.Game.GraphicsDevice);
            GameFacade.Scenes.Add(World);

            VMNetDriver driver;
            if (host)
            {
                driver = new VMServerDriver(37564);
            }
            else
            {
                Connecting = true;
                ConnectingDialog = new UILoginProgress();

                ConnectingDialog.Caption = GameFacade.Strings.GetString("211", "1");
                ConnectingDialog.ProgressCaption = GameFacade.Strings.GetString("211", "24");
                //this.Add(ConnectingDialog);

                UIScreen.ShowDialog(ConnectingDialog, true);

                driver = new VMClientDriver(path, 37564, ClientStateChange);
            }

            vm = new VM(new VMContext(World), driver, new UIHeadlineRendererProvider());
            vm.Init();
            vm.LotName = (path == null) ? "localhost" : path.Split('/').LastOrDefault(); //quick hack just so we can remember where we are

            if (host)
            {
                short jobLevel = -1;

                //quick hack to find the job level from the chosen blueprint
                //the final server will know this from the fact that it wants to create a job lot in the first place...
                string filename = Path.GetFileName(path);
                try
                {
                    if (filename.StartsWith("nightclub") || filename.StartsWith("restaurant") || filename.StartsWith("robotfactory"))
                        jobLevel = Convert.ToInt16(filename.Substring(filename.Length - 9, 2));
                }
                catch (Exception) { }

                vm.SendCommand(new VMBlueprintRestoreCmd
                {
                    JobLevel = jobLevel,
                    XMLData = File.ReadAllBytes(path)
                });
            }

            uint simID = (uint)(new Random()).Next();
            vm.MyUID = simID;

            vm.SendCommand(new VMNetSimJoinCmd
            {
                ActorUID = simID,
                HeadID = GlobalSettings.Default.DebugHead,
                BodyID = GlobalSettings.Default.DebugBody,
                SkinTone = (byte)GlobalSettings.Default.DebugSkin,
                Gender = !GlobalSettings.Default.DebugGender,
                Name = GlobalSettings.Default.LastUser
            });

            LotController = new UILotControl(vm, World);
            LotController.SelectedSimID = simID;
            this.AddAt(0, LotController);

            vm.Context.Clock.Hours = 10;
            if (m_ZoomLevel > 3)
            {
                World.Visible = false;
                LotController.Visible = false;
            }

            if (host)
            {
                ZoomLevel = 1;
                ucp.SetInLot(true);
            } else
            {
                ZoomLevel = Math.Max(ZoomLevel, 4);
            }

            if (IDEHook.IDE != null) IDEHook.IDE.StartIDE(vm);

            vm.OnFullRefresh += VMRefreshed;
            vm.OnChatEvent += Vm_OnChatEvent;

        }

        private void Vm_OnChatEvent(SimAntics.NetPlay.Model.VMChatEvent evt)
        {
            if (ZoomLevel < 4)
            {
                Title.SetTitle(LotController.GetLotTitle());
            }
        }

        private void VMRefreshed()
        {
            if (vm == null) return;
            LotController.ActiveEntity = null;
            LotController.RefreshCut();
        }

        private void VMDebug_OnButtonClick(UIElement button)
        {
            /*
            if (vm == null) return;

            var debugTools = new Simantics(vm);

            var window = GameFacade.Game.Window;
            debugTools.Show();
            debugTools.Location = new System.Drawing.Point(window.ClientBounds.X + window.ClientBounds.Width, window.ClientBounds.Y);
            debugTools.UpdateAQLocation();
            */

        }

        private void SaveHouseButton_OnButtonClick(UIElement button)
        {
            if (vm == null) return;


            var exporter = new VMWorldExporter();
            exporter.SaveHouse(vm, GameFacade.GameFilePath("housedata/blueprints/house_00.xml"));

        }

        public void CloseInbox()
        {
            this.Remove(Inbox);
            Inbox = null;
        }

        public void OpenInbox()
        {
            if (Inbox == null)
            {
                Inbox = new UIInbox();
                this.Add(Inbox);
                Inbox.X = GlobalSettings.Default.GraphicsWidth / 2 - 332;
                Inbox.Y = GlobalSettings.Default.GraphicsHeight / 2 - 184;
            }
            //todo, on already visible move to front
        }

        private void MouseHandler(UIMouseEventType type, UpdateState state)
        {

            if (CityRenderer != null) CityRenderer.UIMouseEvent(type.ToString()); //all the city renderer needs are events telling it if the mouse is over it or not.
            //if the mouse is over it, the city renderer will handle the rest.
        }
    }
}
