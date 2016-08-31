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
using FSO.Client.Controllers;
using FSO.Client.Controllers.Panels;
using FSO.Client.Debug;
using FSO.Client.UI.Panels.WorldUI;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.Common;

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

        public UIContainer WindowContainer;
        public UIPersonPage PersonPage;
        public UILotPage LotPage;

        private Queue<SimConnectStateChange> StateChanges;

        private Terrain CityRenderer; //city view
        public UICustomTooltip CityTooltip;
        public UICustomTooltipContainer CityTooltipHitArea;
        public UIMessageTray MessageTray;
        public UIJoinLotProgress JoinLotProgress;

        public UILotControl LotControl; //world, lotcontrol and vm will be null if we aren't in a lot.
        private LotView.World World;
        public FSO.SimAntics.VM vm;
        public VMClientDriver Driver;

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

                if(value == 5)
                {
                    var controller = FindController<CoreGameScreenController>();

                    if (controller != null)
                    {
                        controller.Terrain.ZoomOut();
                    }
                }

                if (value < 4)
                {
                    if (vm == null) ZoomLevel = 4; //call this again but set minimum cityrenderer view
                    else
                    {
                        Title.SetTitle(LotControl.GetLotTitle());
                        if (m_ZoomLevel > 3)
                        {
                            HITVM.Get().PlaySoundEvent(UIMusic.None);
                            if (CityRenderer != null) CityRenderer.Visible = false;
                            gizmo.Visible = false;
                            LotControl.Visible = true;
                            World.Visible = true;
                            ucp.SetMode(UIUCP.UCPMode.LotMode);
                        }
                        m_ZoomLevel = value;
                        vm.Context.World.State.Zoom = (WorldZoom)(4 - ZoomLevel); //near is 3 for some reason... will probably revise
                    }
                }
                else //cityrenderer! we'll need to recreate this if it doesn't exist...
                {
                    if (CityRenderer == null) m_ZoomLevel = value; //set to far zoom... again, we should eventually create this.
                    else
                    {
                        if (m_ZoomLevel < 4)
                        { //coming from lot view... snap zoom % to 0 or 1
                            CityRenderer.m_ZoomProgress = (value == 4) ? 1 : 0;
                            HITVM.Get().PlaySoundEvent(UIMusic.Map); //play the city music as well
                            CityRenderer.Visible = true;
                            gizmo.Visible = true;
                            if (World != null)
                            {
                                World.Visible = false;
                                LotControl.Visible = false;
                            }
                            ucp.SetMode(UIUCP.UCPMode.CityMode);
                        }
                        m_ZoomLevel = value;
                        CityRenderer.m_Zoomed = (value == 4);
                    }
                }
                ucp.UpdateZoomButton();
            }
        }

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

        public CoreGameScreen() : base()
        {
            StateChanges = new Queue<SimConnectStateChange>();
            /**
            * Music
            */
            HITVM.Get().PlaySoundEvent(UIMusic.Map);

            /*VMDebug = new UIButton()
            {
                Caption = "Simantics",
                Y = 45,
                Width = 100,
                X = GlobalSettings.Default.GraphicsWidth - 110
            };
            VMDebug.OnButtonClick += new ButtonClickDelegate(VMDebug_OnButtonClick);
            this.Add(VMDebug);*/

            /*SaveHouseButton = new UIButton()
            {
                Caption = "Save House",
                Y = 10,
                Width = 100,
                X = GlobalSettings.Default.GraphicsWidth - 110
            };
            SaveHouseButton.OnButtonClick += new ButtonClickDelegate(SaveHouseButton_OnButtonClick);
            this.Add(SaveHouseButton);*/

            ucp = new UIUCP(this);
            ucp.Y = ScreenHeight - 210;
            ucp.SetInLot(false);
            ucp.UpdateZoomButton();
            ucp.MoneyText.Caption = "0";// PlayerAccount.Money.ToString();
            this.Add(ucp);

            gizmo = new UIGizmo();
            gizmo.BindController<GizmoController>();
            gizmo.X = ScreenWidth - 430;
            gizmo.Y = ScreenHeight - 230;
            this.Add(gizmo);

            Title = new UIGameTitle();
            Title.SetTitle("");
            this.Add(Title);
            
            this.Add(GameFacade.MessageController);

            MessageTray = new UIMessageTray();
            MessageTray.X = ScreenWidth - 70;
            MessageTray.Y = 12;
            this.Add(MessageTray);

            WindowContainer = new UIContainer();
            Add(WindowContainer);

            PersonPage = new UIPersonPage();
            PersonPage.Visible = false;
            PersonPage.BindController<PersonPageController>();
            WindowContainer.Add(PersonPage);

            LotPage = new UILotPage();
            LotPage.Visible = false;
            LotPage.BindController<LotPageController>();
            WindowContainer.Add(LotPage);
        }


        public void Initialize(string cityName, int cityMap, TerrainController terrainController)
        {
            Title.SetTitle(cityName);
            InitializeMap(cityMap);
            InitializeMouse();
            ZoomLevel = 5; //screen always starts at far zoom, city visible.

            JoinLotProgress = new UIJoinLotProgress();
            JoinLotProgress.BindController<JoinLotProgressController>();

            terrainController.Init(CityRenderer);
            CityRenderer.SetController(terrainController);
        }

        private void InitializeMap(int cityMap)
        {
            CityRenderer = new Terrain(GameFacade.Game.GraphicsDevice); //The Terrain class implements the ThreeDAbstract interface so that it can be treated as a scene but manage its own drawing and updates.
            CityRenderer.m_GraphicsDevice = GameFacade.GraphicsDevice;
            CityRenderer.Initialize(cityMap);
            CityRenderer.LoadContent(GameFacade.GraphicsDevice);
            CityRenderer.RegenData = true;
            CityRenderer.SetTimeOfDay(0.5);
            GameFacade.Scenes.Add(CityRenderer);

            CityTooltip = new UICustomTooltip();
            Add(CityTooltip);
            CityTooltipHitArea = new UICustomTooltipContainer(CityTooltip);
            CityTooltipHitArea.SetSize(ScreenWidth, ScreenHeight);
            AddAt(0, CityTooltipHitArea);
        }

        private void InitializeMouse(){
            /** City Scene **/
            UIContainer mouseHitArea = new UIContainer();
            mouseHitArea.ListenForMouse(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new UIMouseEvent(MouseHandler));
            AddAt(0, mouseHitArea);
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

            lock (StateChanges)
            {
                while (StateChanges.Count > 0)
                {
                    var e = StateChanges.Dequeue();
                    ClientStateChangeProcess(e.State, e.Progress);
                }
            }

            if (vm != null) vm.Update();
        }

        public void CleanupLastWorld()
        {
            if (vm == null) return;
            if (ZoomLevel < 4) ZoomLevel = 5;
            vm.Context.Ambience.Kill();
            foreach (var ent in vm.Entities) { //stop object sounds
                var threads = ent.SoundThreads;
                for (int i = 0; i < threads.Count; i++)
                {
                    threads[i].Sound.RemoveOwner(ent.ObjectID);
                }
                threads.Clear();
            }
            vm.CloseNet(VMCloseNetReason.LeaveLot);
            Driver.OnClientCommand -= VMSendCommand;
            GameFacade.Scenes.Remove(World);
            this.Remove(LotControl);
            ucp.SetPanel(-1);
            ucp.SetInLot(false);
            vm = null;
            World = null;
            Driver = null;
        }

        private void VMSendCommand(byte[] data)
        {
            var controller = FindController<CoreGameScreenController>();

            if (controller != null)
            {
                controller.SendVMMessage(data);
            }
            //TODO: alternate controller for sandbox/standalone mode?
        }

        private void VMShutdown(VMCloseNetReason reason)
        {
            var controller = FindController<CoreGameScreenController>();

            if (controller != null)
            {
                controller.HandleVMShutdown(reason);
            }
        }

        public void ClientStateChange(int state, float progress)
        {
            lock (StateChanges) StateChanges.Enqueue(new SimConnectStateChange(state, progress));
        }

        public void ClientStateChangeProcess(int state, float progress)
        {     
            switch (state)
            {
                case 2:
                    JoinLotProgress.ProgressCaption = GameFacade.Strings.GetString("211", "27");
                    JoinLotProgress.Progress = 100f*(0.5f+progress*0.5f);
                    break;
                case 3:
                    UIScreen.RemoveDialog(JoinLotProgress);
                    ZoomLevel = 1;
                    ucp.SetInLot(true);
                    break;
            }
        }

        public void InitializeLot()
        {
            CleanupLastWorld();

            World = new LotView.World(GameFacade.Game.GraphicsDevice);
            GameFacade.Scenes.Add(World);
            Driver = new VMClientDriver(ClientStateChange);
            Driver.OnClientCommand += VMSendCommand;
            Driver.OnShutdown += VMShutdown;

            vm = new VM(new VMContext(World), Driver, new UIHeadlineRendererProvider());
            vm.Init();

            LotControl = new UILotControl(vm, World);
            this.AddAt(1, LotControl);

            vm.Context.Clock.Hours = 10;
            if (m_ZoomLevel > 3)
            {
                World.Visible = false;
                LotControl.Visible = false;
            }

            ZoomLevel = Math.Max(ZoomLevel, 4);

            if (IDEHook.IDE != null) IDEHook.IDE.StartIDE(vm);

            vm.OnFullRefresh += VMRefreshed;
            vm.OnChatEvent += Vm_OnChatEvent;
            vm.OnEODMessage += LotControl.EODs.OnEODMessage;
        }

        public void InitTestLot(string path, bool host)
        {
            /*
            if (Connecting) return;

            if (vm != null) CleanupLastWorld();

            World = new LotView.World(GameFacade.Game.GraphicsDevice);
            GameFacade.Scenes.Add(World);

            VMNetDriver driver;
            if (host)
            {
                driver = new VMServerDriver(new VMTSOGlobalLinkStub());
            }
            else
            {
                Connecting = true;
                ConnectingDialog = new UILoginProgress();

                ConnectingDialog.Caption = GameFacade.Strings.GetString("211", "1");
                ConnectingDialog.ProgressCaption = GameFacade.Strings.GetString("211", "24");
                //this.Add(ConnectingDialog);

                UIScreen.ShowDialog(ConnectingDialog, true);

                driver = new VMClientDriver(ClientStateChange);
            }

            vm = new VM(new VMContext(World), driver, new UIHeadlineRendererProvider());
            vm.Init();
            vm.LotName = (path == null) ? "localhost" : path.Split('/').LastOrDefault(); //quick hack just so we can remember where we are

            if (host)
            {
                //check: do we have an fsov to try loading from?

                string filename = Path.GetFileName(path);
                try
                {
                    using (var file = new BinaryReader(File.OpenRead(Path.Combine(FSOEnvironment.UserDir, "LocalHouse/")+filename.Substring(0, filename.Length-4)+".fsov")))
                    {
                        var marshal = new SimAntics.Marshals.VMMarshal();
                        marshal.Deserialize(file);
                        vm.Load(marshal);
                        vm.Reset();
                    }
                }
                catch (Exception) {
                    short jobLevel = -1;

                    //quick hack to find the job level from the chosen blueprint
                    //the final server will know this from the fact that it wants to create a job lot in the first place...

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

            LotControl = new UILotControl(vm, World);
            this.AddAt(0, LotControl);

            vm.Context.Clock.Hours = 10;
            if (m_ZoomLevel > 3)
            {
                World.Visible = false;
                LotControl.Visible = false;
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
            vm.OnEODMessage += LotControl.EODs.OnEODMessage;
            */
        }

        private void Vm_OnChatEvent(SimAntics.NetPlay.Model.VMChatEvent evt)
        {
            if (ZoomLevel < 4)
            {
                Title.SetTitle(LotControl.GetLotTitle());
            }
        }

        private void VMRefreshed()
        {
            if (vm == null) return;
            LotControl.ActiveEntity = null;
            LotControl.RefreshCut();
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
            var marshal = vm.Save();
            Directory.CreateDirectory(Path.Combine(FSOEnvironment.UserDir, "LocalHouse/"));
            using (var output = new FileStream(Path.Combine(FSOEnvironment.UserDir, "LocalHouse/house_00.fsov"), FileMode.Create))
            {
                marshal.SerializeInto(new BinaryWriter(output));
            }
            if (vm.GlobalLink != null) ((VMTSOGlobalLinkStub)vm.GlobalLink).Database.Save();
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

    public class SimConnectStateChange
    {
        public int State;
        public float Progress;
        public SimConnectStateChange(int state, float progress)
        {
            State = state; Progress = progress;
        }
    }
}
