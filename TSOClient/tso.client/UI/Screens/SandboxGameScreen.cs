using FSO.Client.Debug;
using FSO.Client.Network.Sandbox;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Panels.WorldUI;
using FSO.Common;
using FSO.Common.Rendering.Framework;
using FSO.Common.Utils;
using FSO.Files.Formats.IFF.Chunks;
using FSO.HIT;
using FSO.LotView;
using FSO.SimAntics;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Screens
{
    public class SandboxGameScreen : FSO.Client.UI.Framework.GameScreen, IGameScreen
    {
        public UIUCP ucp;
        public UIGameTitle Title;

        public UIContainer WindowContainer;
        public FSOSandboxServer SandServer;
        public FSOSandboxClient SandCli;
        public UISandboxSelector SandSelect;

        private Queue<SimConnectStateChange> StateChanges;

        public UIJoinLotProgress JoinLotProgress;
        public bool Downtown;

        public UILotControl LotControl { get; set; } //world, lotcontrol and vm will be null if we aren't in a lot.
        private LotView.World World;
        public FSO.SimAntics.VM vm { get; set; }
        public VMNetDriver Driver;
        public uint VisualBudget { get; set; }

        //for TS1 hybrid mode
        public UINeighborhoodSelectionPanel TS1NeighPanel;
        public FAMI ActiveFamily;

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
                if (m_ZoomLevel < 4 && InLot)
                {
                    return 4 - (int)World.State.Zoom;
                }
                return m_ZoomLevel;
            }
            set
            {
                value = Math.Max(1, Math.Min(5, value));

                if (value < 4)
                {
                    if (vm == null)
                    {

                    }
                    else
                    {
                        Title.SetTitle(LotControl.GetLotTitle());
                        var targ = (WorldZoom)(4 - value); //near is 3 for some reason... will probably revise
                        HITVM.Get().PlaySoundEvent(UIMusic.None);
                        LotControl.Visible = true;
                        World.Visible = true;
                        ucp.SetMode(UIUCP.UCPMode.LotMode);
                        LotControl.SetTargetZoom(targ);
                        if (!FSOEnvironment.Enable3D)
                        {
                            if (m_ZoomLevel != value) vm.Context.World.InitiateSmoothZoom(targ);
                        }
                        m_ZoomLevel = value;
                    }
                }
                else //open the sandbox mode lot browser
                {
                    SandSelect = new UISandboxSelector();
                    GlobalShowDialog(SandSelect, true);
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

        public SandboxGameScreen() : base()
        {
            StateChanges = new Queue<SimConnectStateChange>();

            ucp = new UIUCP(this);
            ucp.Y = ScreenHeight - 210;
            ucp.SetInLot(false);
            ucp.UpdateZoomButton();
            ucp.MoneyText.Caption = "0";// PlayerAccount.Money.ToString();
            this.Add(ucp);

            Title = new UIGameTitle();
            Title.SetTitle("");
            this.Add(Title);

            WindowContainer = new UIContainer();
            Add(WindowContainer);

            if (Content.Content.Get().TS1)
            {
                TS1NeighPanel = new UINeighborhoodSelectionPanel(4);
                TS1NeighPanel.OnHouseSelect += (house) =>
                {
                    ActiveFamily = Content.Content.Get().Neighborhood.GetFamilyForHouse((short)house);
                    InitializeLot(Path.Combine(Content.Content.Get().TS1BasePath, "UserData/Houses/House" + house.ToString().PadLeft(2, '0') + ".iff"), false);// "UserData/Houses/House21.iff"
                Remove(TS1NeighPanel);
                };
                Add(TS1NeighPanel);
            }
        }

        public override void GameResized()
        {
            base.GameResized();
            Title.SetTitle(Title.Label.Caption);
            ucp.Y = ScreenHeight - 210;
            World?.GameResized();
            var oldPanel = ucp.CurrentPanel;
            ucp.SetPanel(-1);
            ucp.SetPanel(oldPanel);
        }

        public void Initialize(string propertyName, bool external)
        {
            Title.SetTitle(propertyName);
            GameFacade.CurrentCityName = propertyName;
            ZoomLevel = 1; //screen always starts at near zoom

            JoinLotProgress = new UIJoinLotProgress();
            InitializeLot(propertyName, external);
        }

        private int SwitchLot = -1;

        public void ChangeSpeedTo(int speed)
        {
            //0 speed is 0x
            //1 speed is 1x
            //2 speed is 3x
            //3 speed is 10x

            if (vm == null) return;

            switch (vm.SpeedMultiplier)
            {
                case 0:
                    switch (speed)
                    {
                        case 1:
                            HITVM.Get().PlaySoundEvent(UISounds.SpeedPTo1); break;
                        case 2:
                            HITVM.Get().PlaySoundEvent(UISounds.SpeedPTo2); break;
                        case 3:
                            HITVM.Get().PlaySoundEvent(UISounds.SpeedPTo3); break;
                    }
                    break;
                case 1:
                    switch (speed)
                    {
                        case 0:
                            HITVM.Get().PlaySoundEvent(UISounds.Speed1ToP); break;
                        case 2:
                            HITVM.Get().PlaySoundEvent(UISounds.Speed1To2); break;
                        case 3:
                            HITVM.Get().PlaySoundEvent(UISounds.Speed1To3); break;
                    }
                    break;
                case 3:
                    switch (speed)
                    {
                        case 0:
                            HITVM.Get().PlaySoundEvent(UISounds.Speed2ToP); break;
                        case 1:
                            HITVM.Get().PlaySoundEvent(UISounds.Speed2To1); break;
                        case 3:
                            HITVM.Get().PlaySoundEvent(UISounds.Speed2To3); break;
                    }
                    break;
                case 10:
                    switch (speed)
                    {
                        case 0:
                            HITVM.Get().PlaySoundEvent(UISounds.Speed3ToP); break;
                        case 1:
                            HITVM.Get().PlaySoundEvent(UISounds.Speed3To1); break;
                        case 2:
                            HITVM.Get().PlaySoundEvent(UISounds.Speed3To2); break;
                    }
                    break;
            }

            switch (speed)
            {
                case 0: vm.SpeedMultiplier = 0; break;
                case 1: vm.SpeedMultiplier = 1; break;
                case 2: vm.SpeedMultiplier = 3; break;
                case 3: vm.SpeedMultiplier = 10; break;
            }
        }

        public override void Update(FSO.Common.Rendering.Framework.Model.UpdateState state)
        {
            GameFacade.Game.IsFixedTimeStep = (vm == null || vm.Ready);

            Visible = World?.Visible == true && (World?.State as FSO.LotView.RC.WorldStateRC)?.CameraMode != true;
            GameFacade.Game.IsMouseVisible = Visible;

            base.Update(state);
            if (state.NewKeys.Contains(Keys.NumPad1)) ChangeSpeedTo(1);
            if (state.NewKeys.Contains(Keys.NumPad2)) ChangeSpeedTo(2);
            if (state.NewKeys.Contains(Keys.NumPad3)) ChangeSpeedTo(3);
            if (state.NewKeys.Contains(Keys.P)) ChangeSpeedTo(0);

            if (World != null)
            { 
                //stub smooth zoom?
            }

            lock (StateChanges)
            {
                while (StateChanges.Count > 0)
                {
                    var e = StateChanges.Dequeue();
                    ClientStateChangeProcess(e.State, e.Progress);
                }
            }

            if (SwitchLot > 0)
            {

                InitializeLot(Path.Combine(Content.Content.Get().TS1BasePath, "UserData/Houses/House" + SwitchLot.ToString().PadLeft(2, '0') + ".iff"), false);
                SwitchLot = -1;
            }
            if (vm != null) vm.Update();
        }

        public override void PreDraw(UISpriteBatch batch)
        {
            base.PreDraw(batch);
            vm?.PreDraw();
        }

        public void CleanupLastWorld()
        {
            if (vm == null) return;

            //clear our cache too, if the setting lets us do that
            TimedReferenceController.Clear();
            TimedReferenceController.Clear();
            VM.ClearAssembled();

            vm.Context.Ambience.Kill();
            foreach (var ent in vm.Entities)
            { //stop object sounds
                var threads = ent.SoundThreads;
                for (int i = 0; i < threads.Count; i++)
                {
                    threads[i].Sound.RemoveOwner(ent.ObjectID);
                }
                threads.Clear();
            }
            vm.CloseNet(VMCloseNetReason.LeaveLot);
            //Driver.OnClientCommand -= VMSendCommand;
            GameFacade.Scenes.Remove(World);
            World.Dispose();
            LotControl.Dispose();
            this.Remove(LotControl);
            ucp.SetPanel(-1);
            ucp.SetInLot(false);
            vm.SuppressBHAVChanges();
            vm = null;
            World = null;
            Driver = null;
            LotControl = null;

            SandServer?.Shutdown();
            SandCli?.Disconnect();
            SandServer = null;
            SandCli = null;
        }

        /*
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
        }*/

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
                    JoinLotProgress.Progress = 100f * (0.5f + progress * 0.5f);
                    break;
                case 3:
                    GameFacade.Cursor.SetCursor(CursorType.Normal);
                    UIScreen.RemoveDialog(JoinLotProgress);
                    ZoomLevel = 1;
                    ucp.SetInLot(true);
                    break;
            }
        }

        public void InitializeLot(string lotName, bool external)
        {
            if (lotName == "") return;
            var recording = lotName.ToLowerInvariant().EndsWith(".fsor");
            CleanupLastWorld();

            if (FSOEnvironment.Enable3D)
            {
                var rc = new LotView.RC.WorldRC(GameFacade.GraphicsDevice);
                World = rc;
            }
            else World = new World(GameFacade.GraphicsDevice);
            World.Opacity = 1;
            GameFacade.Scenes.Add(World);

            var settings = GlobalSettings.Default;
            var myState = new VMNetAvatarPersistState()
            {
                Name = settings.LastUser,
                DefaultSuits = new VMAvatarDefaultSuits(settings.DebugGender),
                BodyOutfit = settings.DebugBody,
                HeadOutfit = settings.DebugHead,
                PersistID = (uint)(new Random()).Next(),
                SkinTone = (byte)settings.DebugSkin,
                Gender = (short)(settings.DebugGender ? 0 : 1),
                Permissions = SimAntics.Model.TSOPlatform.VMTSOAvatarPermissions.Admin,
                Budget = 1000000,
            };

            if (recording)
            {
                var stream = new FileStream(lotName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var rd = new VMFSORDriver(stream);
                Driver = rd;
            }
            else if (external)
            {
                var cd = new VMClientDriver(ClientStateChange);
                SandCli = new FSOSandboxClient();
                cd.OnClientCommand += (msg) => { SandCli.Write(new VMNetMessage(VMNetMessageType.Command, msg)); };
                cd.OnShutdown += (reason) => SandCli.Disconnect();
                SandCli.OnMessage += cd.ServerMessage;
                SandCli.Connect(lotName);
                Driver = cd;

                var dat = new MemoryStream();
                var str = new BinaryWriter(dat);
                myState.SerializeInto(str);
                var ava = new VMNetMessage(VMNetMessageType.AvatarData, dat.ToArray());
                dat.Close();
                SandCli.OnConnectComplete += () =>
                {
                    SandCli.Write(ava);
                };
            } else
            {
                var globalLink = new VMTSOGlobalLinkStub();
                var sd = new VMServerDriver(globalLink);
                SandServer = new FSOSandboxServer();

                Driver = sd;
                sd.OnDropClient += SandServer.ForceDisconnect;
                sd.OnTickBroadcast += SandServer.Broadcast;
                sd.OnDirectMessage += SandServer.SendMessage;
                SandServer.OnConnect += sd.ConnectClient;
                SandServer.OnDisconnect += sd.DisconnectClient;
                SandServer.OnMessage += sd.HandleMessage;

                SandServer.Start((ushort)37564);
            }

            //Driver.OnClientCommand += VMSendCommand;
            //Driver.OnShutdown += VMShutdown;

            vm = new VM(new VMContext(World), Driver, new UIHeadlineRendererProvider());
            vm.ListenBHAVChanges();
            vm.Init();

            LotControl = new UILotControl(vm, World);
            this.AddAt(0, LotControl);

            var time = DateTime.UtcNow;
            var tsoTime = TSOTime.FromUTC(time);

            vm.Context.Clock.Hours = tsoTime.Item1;
            vm.Context.Clock.Minutes = tsoTime.Item2;
            if (m_ZoomLevel > 3)
            {
                World.Visible = false;
                LotControl.Visible = false;
            }

            if (IDEHook.IDE != null) IDEHook.IDE.StartIDE(vm);

            vm.OnFullRefresh += VMRefreshed;
            vm.OnChatEvent += Vm_OnChatEvent;
            vm.OnEODMessage += LotControl.EODs.OnEODMessage;
            vm.OnRequestLotSwitch += VMLotSwitch;
            vm.OnGenericVMEvent += Vm_OnGenericVMEvent;

            if (!external && !recording)
            {
                if (!Downtown && ActiveFamily != null)
                {
                    ActiveFamily.SelectWholeFamily();
                    vm.ActivateFamily(ActiveFamily);
                }
                BlueprintReset(lotName);

                vm.TSOState.PropertyCategory = 255;
                vm.Context.Clock.Hours = 0;
                vm.TSOState.Size = (10) | (3 << 8);
                vm.Context.UpdateTSOBuildableArea();
                var myClient = new VMNetClient
                {
                    PersistID = myState.PersistID,
                    RemoteIP = "local",
                    AvatarState = myState

                };

                var server = (VMServerDriver)Driver;
                server.ConnectClient(myClient);

                GameFacade.Cursor.SetCursor(CursorType.Normal);
                ZoomLevel = 1;
            }
            vm.MyUID = myState.PersistID;
            ZoomLevel = 1;
        }

        public void BlueprintReset(string path)
        {
            string filename = Path.GetFileName(path);
            try
            {
                using (var file = new BinaryReader(File.OpenRead(Path.Combine(FSOEnvironment.UserDir, "LocalHouse/") + filename.Substring(0, filename.Length - 4) + ".fsov")))
                {
                    var marshal = new SimAntics.Marshals.VMMarshal();
                    marshal.Deserialize(file);
                    //vm.SendCommand(new VMStateSyncCmd()
                    //{
                    //    State = marshal
                    //});

                    vm.Load(marshal);
                    vm.Reset();
                }
            }
            catch (Exception)
            {
                var floorClip = Rectangle.Empty;
                var offset = new Point();
                var targetSize = 0;

                var isIff = path.EndsWith(".iff");
                short jobLevel = -1;
                if (isIff) jobLevel = short.Parse(path.Substring(path.Length - 6, 2));
                vm.SendCommand(new VMBlueprintRestoreCmd
                {
                    JobLevel = jobLevel,
                    XMLData = File.ReadAllBytes(path),
                    IffData = isIff,

                    FloorClipX = floorClip.X,
                    FloorClipY = floorClip.Y,
                    FloorClipWidth = floorClip.Width,
                    FloorClipHeight = floorClip.Height,
                    OffsetX = offset.X,
                    OffsetY = offset.Y,
                    TargetSize = targetSize
                });
            }
            vm.Tick();
        }


        private void Vm_OnGenericVMEvent(VMEventType type, object data)
        {
            //hmm...
        }

        private void VMLotSwitch(uint lotId)
        {
            if ((short)lotId == -1)
            {
                Downtown = false;
                lotId = (uint)ActiveFamily.HouseNumber;
            } else
            {
                Downtown = true;
            }
            SwitchLot = (int)lotId;
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
    }
}