/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
using TSOClient.Code.UI.Panels;
using TSOClient.Code.UI.Model;
using TSOClient.LUI;
using TSOClient.Code.Rendering.City;
using Microsoft.Xna.Framework;
using TSOClient.Code.Utils;
using TSO.Common.rendering.framework.model;
using TSO.Common.rendering.framework.io;
using TSO.Common.rendering.framework;
using tso.world;
using tso.world.model;
using TSO.Simantics;
using TSO.Simantics.utils;
using tso.debug;

namespace TSOClient.Code.UI.Screens
{
    public class CoreGameScreen : TSOClient.Code.UI.Framework.GameScreen
    {
        public UIUCP ucp;
        public UIGizmo gizmo;
        public UIInbox Inbox;
        public UIGameTitle Title;
        private UIButton VMDebug;
        private string[] CityMusic;

        private Terrain CityRenderer; //city view

        public UILotControl LotController; //world, lotcontrol and vm will be null if we aren't in a lot.
        private World World; 
        public TSO.Simantics.VM vm;
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
                        if (m_ZoomLevel > 3)
                        {
                            PlayBackgroundMusic(new string[]{"none"}); //disable city music
                            CityRenderer.Visible = false;
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
                    if (CityRenderer == null) ZoomLevel = 3; //set to far zoom... again, we should eventually create this.
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

        public CoreGameScreen()
        {
            /** City Scene **/
            ListenForMouse(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new UIMouseEvent(MouseHandler));

            CityRenderer = new Terrain(GameFacade.Game.GraphicsDevice); //The Terrain class implements the ThreeDAbstract interface so that it can be treated as a scene but manage its own drawing and updates.

            String city = "Queen Margaret's";
            if (PlayerAccount.CurrentlyActiveSim != null)
                city = PlayerAccount.CurrentlyActiveSim.ResidingCity.Name;

            CityRenderer.m_GraphicsDevice = GameFacade.GraphicsDevice;

            CityRenderer.Initialize(city, new CityDataRetriever());
            CityRenderer.RegenData = true;
            
            CityRenderer.LoadContent(GameFacade.GraphicsDevice);

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
            m_ZoomLevel = 5; //screen always starts at far zoom, city visible.
            PlayBackgroundMusic(CityMusic);

            VMDebug = new UIButton()
            {
                Caption = "Simantics",
                Y = 45,
                Width = 100,
                X = GlobalSettings.Default.GraphicsWidth - 110
            };
            VMDebug.OnButtonClick += new ButtonClickDelegate(VMDebug_OnButtonClick);
            this.Add(VMDebug);

            ucp = new UIUCP(this);
            ucp.Y = ScreenHeight - 210;
            ucp.SetInLot(false);
            ucp.UpdateZoomButton();
            this.Add(ucp);

            gizmo = new UIGizmo();
            gizmo.X = ScreenWidth - 500;
            gizmo.Y = ScreenHeight - 300;
            this.Add(gizmo);

            Title = new UIGameTitle();
            Title.SetTitle(city);
            this.Add(Title);

            //OpenInbox();

            //THIS IS KEPT HERE AS A DOCUMENTATION OF THE MESSAGE PASSING API FOR NOW.
            /*
            this.Add(GameFacade.MessageController);
            GameFacade.MessageController.OnSendLetter += new LetterSendDelegate(MessageController_OnSendLetter);

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

            GameFacade.Scenes.Add((_3DAbstract)CityRenderer);

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

        public override void Update(TSO.Common.rendering.framework.model.UpdateState state)
        {
            base.Update(state);
            if (ZoomLevel > 3 && CityRenderer.m_Zoomed != (ZoomLevel == 4)) ZoomLevel = (CityRenderer.m_Zoomed) ? 4 : 5;

            if (InLot) //if we're in a lot, use the VM's more accurate time!
                CityRenderer.SetTimeOfDay((vm.Context.Clock.Hours / 24.0) + (vm.Context.Clock.Minutes / 1440.0) + (vm.Context.Clock.Seconds / 86400.0));
            else
                CityRenderer.SetTimeOfDay(0.5); //Afr0, please implement time of day sync with server! Right now speed is one minute per second, but final will be per 3 seconds.

            if (vm != null) vm.Update(state.Time);
        }

        public void InitTestLot()
        {
            var lotInfo = XmlHouseData.Parse(GameFacade.GameFilePath("housedata/blueprints/restaurant08_00.xml"));

            World = new World(GameFacade.Game.GraphicsDevice);
            GameFacade.Scenes.Add(World);

            vm = new TSO.Simantics.VM(new VMContext(World));
            vm.Init();

            var activator = new VMWorldActivator(vm, World);
            var blueprint = activator.LoadFromXML(lotInfo);

            World.InitBlueprint(blueprint);
            vm.Context.Blueprint = blueprint;

            var sim = activator.CreateAvatar();
            sim.Position = new Vector3(26.5f, 41.5f, 0.0f);

            var sim2 = activator.CreateAvatar();
            sim2.Position = new Vector3(27.5f, 41.5f, 0.0f);

            LotController = new UILotControl(vm, World);
            this.AddAt(0, LotController);

            vm.Context.Clock.Hours = 6;

            ucp.SelectedAvatar = sim;
            ucp.SetInLot(true);
            if (m_ZoomLevel > 3) World.Visible = false;
        }

        void VMDebug_OnButtonClick(UIElement button)
        {
            if (vm == null) return;
            System.Windows.Forms.Form gameWindowForm =
                (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(GameFacade.Game.Window.Handle);

            var debugTools = new Simantics(vm);
            debugTools.Show();
            debugTools.Location = new System.Drawing.Point(gameWindowForm.Location.X + gameWindowForm.Width, gameWindowForm.Location.Y);
            debugTools.UpdateAQLocation();

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
            //todo: change handler to game engine when in simulation mode.

            CityRenderer.UIMouseEvent(type.ToString()); //all the city renderer needs are events telling it if the mouse is over it or not.
            //if the mouse is over it, the city renderer will handle the rest.
        }
    }
}
