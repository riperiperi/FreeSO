using FSO.Client.Controllers;
using FSO.Client.Controllers.Panels;
using FSO.Client.Debug;
using FSO.Client.Rendering;
using FSO.Client.Rendering.City;
using FSO.Client.UI.Archive;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Panels.CityPainter;
using FSO.Client.UI.Panels.Neighborhoods;
using FSO.Client.UI.Panels.WorldUI;
using FSO.Client.Utils;
using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Common.Model;
using FSO.Common.Rendering.Framework;
using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.HIT;
using FSO.LotView;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.LotView.Utils.Camera;
using FSO.Server.Clients;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Utils;
using FSO.UI.Model;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Screens
{
    public class CoreGameScreen : FSO.Client.UI.Framework.GameScreen, IGameScreen
    {
        public UIUCP ucp { get; set; }
        public UIGizmo gizmo;
        public UIInbox Inbox;
        public UIGameTitle Title;
        public UIArchiveUserList UserList;

        public UISortedContainer CityFloatingContainer;
        public UIContainer WindowContainer;
        public UIPersonPage PersonPage;
        public UILotPage LotPage;
        public UINeighPage NeighPage;
        public UIBookmarks Bookmarks;
        public UIRelationshipDialog Relationships;
        public UIMapWaypoint YouAreHere, YourHouseHere;
        internal UICityPainterAvatarLayer CityUpdateLayer;

        private Queue<SimConnectStateChange> StateChanges;

        public Terrain CityRenderer; //city view
        public UICustomTooltip CityTooltip;
        public UICustomTooltipContainer CityTooltipHitArea;
        public UIMessageTray MessageTray;
        public UIJoinLotProgress JoinLotProgress;
        private UIAlert SwitchLotDialog;

        public UILotControl LotControl { get; set; } //world, lotcontrol and vm will be null if we aren't in a lot.
        private LotView.World World;
        public bool WorldLoaded;
        public FSO.SimAntics.VM vm { get; set; }
        public VMClientDriver Driver;
        public uint VisualBudget { get; set; }

        // Simantics VMs can be kept around for a load transition.
        private VM TransitionVM;
        private World TransitionWorld;
        private UILotControl TransitionLotControl;
        private CameraControllers TransitionCameras;

        public VM VisualVM => TransitionVM ?? vm;
        public World VisualWorld => TransitionWorld ?? World;
        public VisualSurroundPuppets SurroundPuppets { get; private set; }

        public UIButton CityEditButton;
        private UICityPainter CityPainter;

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
                        CityRenderer.Camera.ClearCenter();
                        var childClone = CityFloatingContainer.GetChildren().ToList();
                        foreach (var child in childClone)
                        {
                            CityFloatingContainer.Remove(child);
                            child.Parent = null;
                        }
                        
                        CityTooltipHitArea.HideTooltip();
                        SetTitle();
                        var targ = (WorldZoom)(4 - value); //near is 3 for some reason... will probably revise
                        LotControl.SetTargetZoom(targ);
                        if (m_ZoomLevel > 3)
                        {
                            if (!ucp.SpecialMusic) HITVM.Get().PlaySoundEvent(UIMusic.None);
                            if (CityRenderer != null) CityRenderer.m_Zoomed = TerrainZoomMode.Lot;
                            gizmo.Visible = false;
                            LotControl.Visible = true;
                            World.Visible = true;
                            ucp.SetMode(UIUCP.UCPMode.LotMode);
                        } else
                        {
                            if (m_ZoomLevel != value) vm.Context.World.InitiateSmoothZoom(targ);
                        }

                        m_ZoomLevel = value;
                    }
                }
                else //cityrenderer! we'll need to recreate this if it doesn't exist...
                {
                    if (value == 5) CityTooltipHitArea.HideTooltip();
                    else CityTooltipHitArea.ShowTooltip();
                    if (CityRenderer == null) m_ZoomLevel = value; //set to far zoom... again, we should eventually create this.
                    else
                    {
                        if (m_ZoomLevel < 4)
                        { //coming from lot view... snap zoom % to 0 or 1
                            Title.SetTitle(GameFacade.CurrentCityName);
                            CityRenderer.m_ZoomProgress = 1;
                            if (!ucp.SpecialMusic) HITVM.Get().PlaySoundEvent(UIMusic.Map); //play the city music as well
                            CityRenderer.Visible = true;
                            gizmo.Visible = true;
                            if (World != null)
                            {
                                LotControl.Visible = false;
                            }
                            ucp.SetMode(UIUCP.UCPMode.CityMode);
                        }
                        if (m_ZoomLevel == 4) CityRenderer.Camera.ClearCenter();
                        m_ZoomLevel = value;
                        
                        CityRenderer.m_Zoomed = (value == 4)?TerrainZoomMode.Near:TerrainZoomMode.Far;
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

            var gd = GameFacade.GraphicsDevice;
            var custom = Content.Content.Get().CustomUI;

            CityFloatingContainer = new UISortedContainer();
            Add(CityFloatingContainer);

            ucp = new UIUCP(this);
            ucp.Y = ScreenHeight - 210;
            ucp.SetInLot(false);
            ucp.UpdateZoomButton();
            ucp.MoneyText.Caption = "0";// PlayerAccount.Money.ToString();
            this.Add(ucp);

            gizmo = new UIGizmo();
            ControllerUtils.BindController<GizmoController>(gizmo);
            gizmo.X = ScreenWidth - 430;
            gizmo.Y = ScreenHeight - 230;
            this.Add(gizmo);

            Title = new UIGameTitle();
            Title.SetTitle("");
            this.Add(Title);
            
            this.Add(FSOFacade.MessageController);

            MessageTray = new UIMessageTray();
            MessageTray.X = ScreenWidth - 70;
            MessageTray.Y = 12;
            this.Add(MessageTray);

            WindowContainer = new UIContainer();
            Add(WindowContainer);

            PersonPage = new UIPersonPage();
            PersonPage.Visible = false;
            ControllerUtils.BindController<PersonPageController>(PersonPage);
            WindowContainer.Add(PersonPage);

            LotPage = new UILotPage();
            LotPage.Visible = false;
            ControllerUtils.BindController<LotPageController>(LotPage);
            WindowContainer.Add(LotPage);

            NeighPage = new UINeighPage();
            NeighPage.Visible = false;
            ControllerUtils.BindController<NeighPageController>(NeighPage);
            WindowContainer.Add(NeighPage);

            Bookmarks = new UIBookmarks();
            Bookmarks.Visible = false;
            ControllerUtils.BindController<BookmarksController>(Bookmarks);
            WindowContainer.Add(Bookmarks);

            Relationships = new UIRelationshipDialog();
            Relationships.Visible = false;
            ControllerUtils.BindController<RelationshipDialogController>(Relationships);
            WindowContainer.Add(Relationships);

            Inbox = new UIInbox();
            Inbox.Visible = false;
            ControllerUtils.BindController<InboxController>(Inbox);
            WindowContainer.Add(Inbox);

            UserList = new UIArchiveUserList();
            UserList.Visible = false;
            ControllerUtils.BindController<UserListController>(UserList);
            var userListController = UserList.FindController<UserListController>();
            if (userListController != null)
            {
                userListController.FlashCallback = FlashUserList;
            }
            WindowContainer.Add(UserList);

            var status = new UINetStatusTray();
            Add(status);

            SurroundPuppets = new(this);

            CityEditButton = new UIButton(custom.Get("cityedit_toggle.png").Get(gd))
            {
                Position = new Vector2(10, 10),
                Tooltip = GameFacade.Strings.GetString("f130", "1")
            };
            CityEditButton.OnButtonClick += ToggleCityEdit;

            Add(CityEditButton);
        }

        private void ToggleCityEdit(UIElement button)
        {
            if (CityPainter == null)
            {
                CityPainter = new UICityPainter(CityRenderer);
                WindowContainer.Add(CityPainter);
            }

            CityPainter.Position = new Vector2(20, 20);
            CityPainter.SetActive(true);
        }

        public override void GameResized()
        {
            base.GameResized();
            CalculateMatrix();
            CityFloatingContainer.ScaleX = 1f / Scale.X;
            CityFloatingContainer.ScaleY = 1f / Scale.Y;
            CityRenderer.Camera.ProjectionDirty();
            Title.SetTitle(Title.Label.Caption);
            ucp.Y = ScreenHeight - 210;
            gizmo.X = ScreenWidth - 430;
            gizmo.Y = ScreenHeight - 230;
            MessageTray.X = ScreenWidth - 70;
            World?.GameResized();
            TransitionWorld?.GameResized();
            var oldPanel = ucp.CurrentPanel;
            ucp.SetPanel(-1);
            ucp.SetPanel(oldPanel);
            CityTooltipHitArea.SetSize(ScreenWidth, ScreenHeight);
        }

        public void Initialize(string cityName, TerrainController terrainController)
        {
            CalculateMatrix();
            CityFloatingContainer.ScaleX = 1f / Scale.X;
            CityFloatingContainer.ScaleY = 1f / Scale.Y;

            Title.SetTitle(cityName);
            GameFacade.CurrentCityName = cityName;
            InitializeMap(terrainController.Realestate);
            InitializeMouse();
            ZoomLevel = 5; //screen always starts at far zoom, city visible.
            CityRenderer.m_ZoomProgress = 0;

            JoinLotProgress = new UIJoinLotProgress();
            ControllerUtils.BindController<JoinLotProgressController>(JoinLotProgress);

            terrainController.Init(CityRenderer);
            CityRenderer.SetController(terrainController);

            GameThread.NextUpdate(x =>
            {
                var controller = FindController<CoreGameScreenController>();
                if (controller.Mode != Regulators.CityConnectionMode.ARCHIVE)
                {
                    // This hint doesn't make sense in archive mode, since players should be encouraged to join any lot.
                    // An archive specific city view hint should be triggered when returning to map, or when there's no welcome lot to join.
                    FSOFacade.Hints.TriggerHint("screen:city");
                }
                else if (!controller.ArchiveConfig.HasFlag(Common.ArchiveConfigFlags.Offline) && FSOFacade.Controller.HasServer())
                {
                    GameThreadInterval interval = null;
                    interval = GameThread.SetInterval(() =>
                    {
                        if (!FSOFacade.Hints.IsShowingHint())
                        {
                            FSOFacade.Hints.TriggerHint("screen:archive_host");
                            interval.Clear();
                        }
                    }, 1000);
                }
            });
        }

        private void InitializeMap(IShardRealestateDomain realestate)
        {
            CityRenderer = new Terrain(GameFacade.GraphicsDevice); //The Terrain class implements the ThreeDAbstract interface so that it can be treated as a scene but manage its own drawing and updates.
            CityRenderer.m_GraphicsDevice = GameFacade.GraphicsDevice;
            CityRenderer.Initialize(realestate);
            CityRenderer.LoadContent(GameFacade.GraphicsDevice);
            CityRenderer.RegenData = true;
            CityRenderer.SetTimeOfDay(0.5);
            GameFacade.Scenes.Add(CityRenderer);

            CityTooltip = new UICustomTooltip();
            Add(CityTooltip);
            CityTooltipHitArea = new UICustomTooltipContainer(CityTooltip);
            CityTooltipHitArea.OnMouseExt = new UIMouseEvent(MouseHandler);
            CityTooltipHitArea.SetSize(ScreenWidth, ScreenHeight);
            AddAt(0, CityTooltipHitArea);

            YouAreHere = new UIMapWaypoint(UIMapWaypoint.UIMapWaypointStyle.YouAreHere);
            YourHouseHere = new UIMapWaypoint(UIMapWaypoint.UIMapWaypointStyle.YourHouseHere);
            CityUpdateLayer = new UICityPainterAvatarLayer(CityRenderer);
            
            AddAt(2, YouAreHere);
            AddAt(2, YourHouseHere);
            AddAt(2, CityUpdateLayer);
        }

        private void InitializeMouse(){
        }

        public override void Update(FSO.Common.Rendering.Framework.Model.UpdateState state)
        {
            //GameFacade.Game.IsFixedTimeStep = (vm == null || vm.Ready);

            Visible = ((World?.Visible == false || World?.State.Cameras.HideUI != true) && !CityRenderer.Camera.HideUI);
            bool directControl = (VisualWorld?.State.Cameras.ActiveCamera as CameraControllerFP)?.CaptureMouse == true;
            GameFacade.Game.IsMouseVisible = Visible && !directControl;

            base.Update(state);

            if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.F1) && state.CtrlDown)
                FSOFacade.Controller.ToggleDebugMenu();

            if (CityRenderer != null)
            {
                if (ZoomLevel > 3 && (CityRenderer.m_Zoomed == TerrainZoomMode.Near) != (ZoomLevel == 4)) ZoomLevel = (CityRenderer.m_Zoomed == TerrainZoomMode.Near) ? 4 : 5;

                if (VisualWorld != null) {
                    if (CityRenderer.m_Zoomed == TerrainZoomMode.Lot)
                    {
                        if (VisualWorld.FrameCounter == 5 && GlobalSettings.Default.CompatState < GlobalSettings.TARGET_COMPAT_STATE)
                        {
                            GlobalSettings.Default.CompatState = GlobalSettings.TARGET_COMPAT_STATE;
                            GlobalSettings.Default.Save();
                        }

                       CityRenderer.InheritPosition(VisualWorld, FindController<CoreGameScreenController>(), false);
                    }
                    if (CityRenderer.m_LotZoomProgress > 0f && CityRenderer.m_LotZoomProgress < 1f)
                    {
                        if (CityRenderer.m_Zoomed == TerrainZoomMode.Lot)
                        {
                            if (CityRenderer.m_LotZoomProgress > 0.9999f)
                            {
                                CityRenderer.m_LotZoomProgress = 1f;
                                CityRenderer.Visible = false;
                            }
                        } else
                        {
                            if (CityRenderer.m_LotZoomProgress < 0.0001f)
                            {
                                CityRenderer.m_LotZoomProgress = 0f;
                                VisualWorld.Visible = false;
                            }
                        }
                        VisualWorld.Opacity = Math.Max(0, (CityRenderer.m_LotZoomProgress - 0.5f) * 2);

                        float scale = 1;
                        if (CityRenderer.Camera is CityCamera2D)
                        {
                            var cam = (CityCamera2D)CityRenderer.Camera;
                            scale =
                                1 / ((cam.LotZoomProgress * (1 / cam.m_LotZoomSize) + (1 - cam.LotZoomProgress) * (1 / (Terrain.NEAR_ZOOM_SIZE * cam.m_WheelZoom))))
                                / cam.m_LotZoomSize;
                        }

                        VisualWorld.State.PreciseZoom = scale;
                    } else
                    {
                        VisualWorld.Opacity = (CityRenderer.m_Zoomed == TerrainZoomMode.Lot)?1f:0f;
                    }
                }
                else if (CityRenderer.m_LotZoomProgress > 0)
                {
                    CityRenderer.m_LotZoomProgress = 0;
                }

                if (InLot) //if we're in a lot, use the VM's more accurate time!
                    CityRenderer.SetTimeOfDay((vm.Context.Clock.Hours / 24.0) + (vm.Context.Clock.Minutes / 1440.0) + (vm.Context.Clock.Seconds / 86400.0));
                else
                {
                    var time = DateTime.UtcNow;
                    var tsoTime = TSOTime.FromUTC(time);
                    CityRenderer.SetTimeOfDay((tsoTime.Item1 / 24.0) + (tsoTime.Item2 / 1440.0) + (tsoTime.Item3 / 86400.0));
                }
            }

            lock (StateChanges)
            {
                while (StateChanges.Count > 0)
                {
                    var e = StateChanges.Dequeue();
                    ClientStateChangeProcess(e.State, e.Progress, state);
                }
            }

            if (vm != null)
            {
                if (vm.FSOVAsyncLoading)
                {
                    if (vm.FSOVObjTotal != 0) ClientStateChange(4, vm.FSOVObjLoaded / (float)vm.FSOVObjTotal);
                } else
                {
                    vm.Update();
                }
            }

            var secret = DiscordRpcEngine.Secret;
            if (secret != null)
            {
                var joinAttempt = secret.Value;
                bool joinLot = joinAttempt.LotID != 0;
                // TODO: if the join attempt archive mode doesn't match archive mode enable, let the player know
                if (joinAttempt.ArchiveMode)
                {
                    if (joinAttempt.ServerID != DiscordRpcEngine.ArchiveID)
                    {
                        if (joinAttempt.ServerHostname == "")
                        {
                            UIAlert.Alert("", GameFacade.Strings.GetString("f128", "115"), true);
                        }
                        else
                        {
                            UIAlert.YesNo("", GameFacade.Strings.GetString("f128", "116"), true, (bool result) =>
                            {
                                if (result)
                                {
                                    FSOFacade.Controller.Disconnect(true);
                                    GameThread.SetTimeout(() => { DiscordRpcEngine.Secret = joinAttempt; }, 100);
                                }
                            });
                        }
                        joinLot = false;
                    }
                }

                if (joinLot)
                {
                    var lotId = joinAttempt.LotID;
                    FindController<CoreGameScreenController>()?.JoinLot(lotId);
                }

                DiscordRpcEngine.Secret = null;
            }

            if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.F12) && GraphicsModeControl.Mode != GlobalGraphicsMode.Full2D)
            {
                GraphicsModeControl.ChangeMode((GraphicsModeControl.Mode == GlobalGraphicsMode.Full3D) ? GlobalGraphicsMode.Hybrid2D : GlobalGraphicsMode.Full3D);
            }

            CityEditButton.Visible = FindController<CoreGameScreenController>()?.AllowCityEditor == true && ZoomLevel >= 4 && (CityPainter == null || !CityPainter.Visible);
        }

        public override void PreDraw(UISpriteBatch batch)
        {
            base.PreDraw(batch);
            SurroundPuppets?.PreDraw();

            if (vm != null)
            {
                if (vm.FSOVAsyncLoading) { }
                else if (!WorldLoaded && vm.Context.Blueprint != null)
                {
                    var result = World.Preload(GameFacade.GraphicsDevice);
                    if (result && vm.GetAvatarByPersist(vm.MyUID) != null)
                    {
                        WorldLoaded = true;
                        ClientStateChange(6, 1);
                        AssetStreaming.EndStreaming();
                    }
                    else
                    {
                        ClientStateChange(5, World.PreloadObjProgress / (float)vm.FSOVObjTotal);
                    }
                } else
                {
                    vm.PreDraw();
                }
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
            if (!Visible)
            {
                var chatballoons = LotControl?.ChatPanel?.GetChildren();
                if (chatballoons != null)
                {
                    foreach (var balloon in chatballoons)
                    {
                        if (balloon is UIChatBalloon) balloon.Draw(batch);
                    }
                }
            }
        }

        public void CleanupTransition()
        {
            if (TransitionVM != null)
            {
                TransitionVM.SuppressBHAVChanges();
                TransitionVM = null;

                if (World != null)
                {
                    World.Visible = TransitionWorld.Visible;
                }

                GameFacade.Scenes.Remove(TransitionWorld);
                TransitionWorld.Dispose();
                TransitionWorld = null;

                TransitionLotControl = null;

                CityRenderer.DisposeOnLot();
            }
        }

        public void CleanupLastWorld(bool cleanupTransition = true)
        {
            if (cleanupTransition)
            {
                CleanupTransition();
            }

            if (vm == null) return;

            // Might be mid-load.
            AssetStreaming.EndStreaming();

            //clear our cache too, if the setting lets us do that
            DiscordRpcEngine.SendFSOPresence(gizmo.CurrentAvatar.Value.Avatar_Name, null, 0, 0, 0, 0, null, gizmo.CurrentAvatar.Value.Avatar_PrivacyMode > 0);

            bool localTransition = FindController<CoreGameScreenController>()?.LocalTransition ?? false;

            if (!localTransition)
            {
                TimedReferenceController.Clear();
                TimedReferenceController.Clear();

                if (ZoomLevel < 4) ZoomLevel = 5;
            }

            if (localTransition)
            {
                vm.Context.Ambience.BeginTransition();
            }

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
            LotControl.Dispose();
            this.Remove(LotControl);
            ucp.SetPanel(-1);
            ucp.SetInLot(false);

            if (localTransition)
            {
                TransitionVM = vm;
                TransitionWorld = World;
                TransitionLotControl = LotControl;

                TransitionWorld.State.SimSpeed = 0;
            }
            else
            {
                vm.SuppressBHAVChanges();

                GameFacade.Scenes.Remove(World);
                World.Dispose();
                CityRenderer.DisposeOnLot();
            }

            vm = null;
            World = null;
            Driver = null;
            LotControl = null;
        }

        public void InitiateLotSwitch()
        {
            vm?.SendCommand(new VMNetSimLeaveCmd());
        }

        public void ShowReconnectDialog(uint id)
        {
            var controller = FindController<CoreGameScreenController>();
            if (controller != null && SwitchLotDialog == null)
            {
                SwitchLotDialog = new UIAlert(new UIAlertOptions()
                {
                    Title = GameFacade.Strings.GetString("215", "1"),
                    Message = GameFacade.Strings.GetString("215", "2"),
                    Buttons = new UIAlertButton[]
                    {
                    new UIAlertButton(UIAlertButtonType.Yes, (btn) => {
                        controller.ReconnectTransition = null;
                        controller.ReconnectLotID = id;
                        vm?.SendCommand(new VMNetSimLeaveCmd());
                        RemoveDialog(SwitchLotDialog); SwitchLotDialog = null; }),
                    new UIAlertButton(UIAlertButtonType.No, (btn) => { RemoveDialog(SwitchLotDialog); SwitchLotDialog = null; })
                    },
                });
                ShowDialog(SwitchLotDialog, true);
            }
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
            if (reason == VMCloseNetReason.NetException || reason == VMCloseNetReason.NetExceptionDirect)
            {
                UIAlert.Alert("Fatal Networking Error", Driver.CloseString, true);
            }
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

        public void ClientStateChangeProcess(int state, float progress, UpdateState updateState)
        {
            if (vm == null) return;
            switch (state) 
            {
                case 4: //loading vm (25%-75%)
                    JoinLotProgress.ProgressCaption = "Loading Objects... (" + vm.FSOVObjLoaded + "/" + vm.FSOVObjTotal + ")";
                    JoinLotProgress.Progress = 100f * (0.25f + progress * 0.5f);
                    break;
                case 5: //loading world (75%-90%)
                    if (World.PreloadProgress > 0)
                    {
                        JoinLotProgress.ProgressCaption = "Loading Surrounding Lots... (" + World.PreloadProgress + "/" + 9 + ")";
                        JoinLotProgress.Progress = 100f * (0.9f);
                    } else
                    {
                        JoinLotProgress.ProgressCaption = "Loading Graphics... (" + World.PreloadObjProgress + "/" + vm.FSOVObjTotal + ")";
                        JoinLotProgress.Progress = 100f * (0.75f + progress * 0.15f);
                    }
                    break;
                case 2: //catching up (90%-100%)
                    if (WorldLoaded)
                    {
                        JoinLotProgress.ProgressCaption = GameFacade.Strings.GetString("211", "27");
                        JoinLotProgress.Progress = 100f * (0.9f + progress * 0.1f);
                    }
                    break;

                case 3: //done update sync (disabled due to async world load)
                    break;

                case 6: //done world load
                    GameFacade.Cursor.SetCursor(CursorType.Normal);
                    UIScreen.RemoveDialog(JoinLotProgress);
                    CursorManager.INSTANCE.SetCursorPriority(0);
                    ZoomLevel = 1;

                    InheritTransition(updateState);
                    CleanupTransition();

                    ucp.SetInLot(true);
                    break;
            }
        }

        private void InheritTransition(UpdateState state)
        {
            if (TransitionCameras == null)
            {
                return;
            }

            if (vm != null && World != null)
            {
                LotControl.ChatPanel.ReceiveEvent(new SimAntics.NetPlay.Model.VMChatEvent(null, SimAntics.NetPlay.Model.VMChatEventType.SwitchLot, vm.TSOState.Name));

                var myAvatar = vm.GetAvatarByPersist(vm.MyUID);

                if (myAvatar == null)
                {
                    // Not here yet. We'll try to keep first person on a future tick.
                    return;
                }

                var info = FindController<CoreGameScreenController>().ReconnectTransition;

                if (info == null)
                {
                    // Should be impossible...

                    TransitionCameras = null;
                    return;
                }

                var lastWorld = TransitionWorld;
                CameraControllers newCameras = World.State.Cameras;

                World.State.DisableSmoothRotation = true;

                if (info.Type != LotTransitionType.Teleport)
                {
                    var position = MapCoordinates.Unpack(vm.TSOState.LotID);
                    var previousElevation = CityRenderer.GetElevationAt(position.X + info.RelativeChangeY, position.Y - info.RelativeChangeX);
                    var currentElevation = CityRenderer.GetElevationAt(position.X, position.Y);

                    var baseAltDiff = (currentElevation - previousElevation) * 100;
                    var heightDiff = baseAltDiff * World.Architecture.Blueprint.TerrainFactor * 3;

                    TransitionCameras.Camera3D.CamHeight -= heightDiff;
                }

                if (lastWorld != null)
                {
                    var surroundsToKeep = info.GetSurroundingLotMask() ^ 0b111111111;
                    var oldSubworlds = lastWorld.Architecture.Blueprint.SubWorlds;
                    var newSubworlds = World.Architecture.Blueprint.SubWorlds;
                    var size = World.Architecture.Blueprint.Width;
                    int baseHeight = VMLotTerrainRestoreTools.GetBaseLevel(vm, 1, 1);
                    bool anySubworldsMigrated = false;

                    for (int i = 0; i < 9; i++)
                    {
                        if (i == 4) continue;

                        uint bit = 1u << i;

                        if ((surroundsToKeep & bit) != 0)
                        {
                            var oldIndex = info.GetOldSubworldForIndex(i);
                            var oldSurround = oldSubworlds.Find(x => x.Index == oldIndex);

                            if (oldSurround != null)
                            {
                                oldSubworlds.Remove(oldSurround);

                                oldSurround.Index = i;
                                int x = (i % 3);
                                int y = (i / 3);
                                oldSurround.GlobalPosition = new Vector2((1 - y) * (size - 2), (x - 1) * (size - 2));
                                int newHeight = VMLotTerrainRestoreTools.GetBaseLevel(vm, x, y);
                                var bp = oldSurround.Architecture.Blueprint;
                                var oldAlt = bp.BaseAlt;
                                bp.BaseAlt = baseHeight - newHeight;

                                if (oldAlt != bp.BaseAlt)
                                {
                                    foreach (var obj in bp.Objects)
                                    {
                                        // Need to update the object altitudes
                                        obj.Position = obj.UnmoddedPosition;
                                    }

                                    bp.AdjustBaseAlt(bp.BaseAlt - oldAlt);
                                }

                                newSubworlds.Add(oldSurround);
                                anySubworldsMigrated = true;
                            }
                        }
                    }

                    if (anySubworldsMigrated)
                    {
                        World.InitSubWorlds();
                    }

                    var lastState = TransitionWorld.State;
                    if (World.State.Level != lastState.Level) World.State.Level = lastState.Level;
                    if (World.State.Rotation != lastState.Rotation) World.State.Rotation = lastState.Rotation;
                    if (World.State.Zoom != lastState.Zoom) World.State.Zoom = lastState.Zoom;
                    World.State.PreciseZoom = lastState.PreciseZoom;
                    World.State.CenterTile = lastState.CenterTile - new Vector2(info.RelativeChangeX * (TransitionWorld.Architecture.Blueprint.Width - 2), info.RelativeChangeY * (TransitionWorld.Architecture.Blueprint.Height - 2));
                    if (lastWorld.State.ScrollAnchor != null)
                    {
                        var myOldAvatar = TransitionVM?.GetAvatarByPersist(TransitionVM.MyUID);

                        if (myOldAvatar != null && lastWorld.State.ScrollAnchor == myOldAvatar.WorldUI)
                        {
                            World.State.ScrollAnchor = myAvatar.WorldUI as AvatarComponent;
                        }
                    }
                }

                // TODO: shift camera height by surrounding lot height?
                TransitionCamera(newCameras.Camera3D, TransitionCameras.Camera3D);
                //TransitionCamera(newCameras.Camera2D, TransitionCameras.Camera2D);
                //TransitionCamera(newCameras.CameraFirstPerson, TransitionCameras.CameraFirstPerson);
                newCameras.CameraDirect.Inherit(TransitionCameras.CameraDirect);

                if (myAvatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.UnusedAndDoNotUse2) == 32767)
                {
                    World.ToggleFirstPerson(CameraControllerType.Direct);
                    CityRenderer.m_LotZoomProgress = 1;

                    if (TransitionCameras != null)
                    {
                        var camera = World.State.Cameras.CameraDirect;
                        var lastCamera = TransitionCameras.CameraDirect;
                        camera.RotationX = lastCamera.RotationX;
                        camera.RotationY = lastCamera.RotationY;

                        camera.FirstPersonAvatar = (LotView.Components.AvatarComponent)myAvatar.WorldUI;
                        World.State.Cameras.Update(state, World);
                        World.State.Cameras.PreDraw(World);
                    }
                }

                World.State.DisableSmoothRotation = false;
                LotControl.ResetTargetZoom();

                if (info.Type == LotTransitionType.Teleport)
                {
                    newCameras.SetCameraType(World, lastWorld.State.Cameras.ActiveType, 0);
                    var ava = (LotView.Components.AvatarComponent)myAvatar.WorldUI;
                    World.CenterTo(ava);

                    var newDirect = newCameras.CameraDirect;
                    newDirect.FirstPersonAvatar = ava;
                    newDirect.RotationX = (float)(Math.PI / -2 - ava.RadianDirection);
                    newDirect.RotationY = MathF.PI / 2f;

                    newCameras.Camera3D.ResetCameraHeight(World);
                }
            }

            TransitionCameras = null;
        }

        private void TransitionCamera(ICameraController camera, ICameraController previousCamera)
        {
            camera.BeforeActive(previousCamera, World);
            camera.OnActive(previousCamera, World);
            camera.InvalidateCamera(World.State);
        }

        public void InitializeLot()
        {
            CleanupLastWorld(false);

            World = new World(GameFacade.GraphicsDevice)
            {
                Surroundings = CityRenderer
            };

            WorldLoaded = false;
            World.Opacity = 0;
            World.Visible = false;
            GameFacade.Scenes.Add(World);
            Driver = new VMClientDriver(ClientStateChange);
            Driver.OnClientCommand += VMSendCommand;
            Driver.OnShutdown += VMShutdown;

            vm = new VM(new VMContext(World), Driver, new UIHeadlineRendererProvider());
            AssetStreaming.BeginStreaming(AssetStreamingMode.Lot);
            vm.FSOVDoAsyncLoad = true;
            vm.ListenBHAVChanges();
            vm.Init();

            LotControl = new UILotControl(vm, World, TransitionLotControl);
            this.AddAt(1, LotControl);

            var time = DateTime.UtcNow;
            var tsoTime = TSOTime.FromUTC(time);

            vm.Context.Clock.Hours = tsoTime.Item1;
            vm.Context.Clock.Minutes = tsoTime.Item2;
            if (m_ZoomLevel > 3)
            {
                World.Visible = false;
                LotControl.Visible = false;
            }

            if (TransitionWorld == null)
            {
                ZoomLevel = Math.Max(ZoomLevel, 4);
            }

            if (IDEHook.IDE != null) IDEHook.IDE.StartIDE(vm);

            vm.OnFullRefresh += VMRefreshed;
            vm.OnChatEvent += Vm_OnChatEvent;
            vm.OnEODMessage += LotControl.EODs.OnEODMessage;
            vm.OnRequestLotSwitch += VMLotSwitch;
            vm.OnGenericVMEvent += Vm_OnGenericVMEvent;
        }

        private void Vm_OnGenericVMEvent(VMEventType type, object data)
        {
            switch (type)
            {
                case VMEventType.TSOUnignore:
                    PersonPage.ToggleBookmark(Common.DataService.Model.BookmarkType.IGNORE_AVATAR, null, (uint)data);
                    break;
                case VMEventType.TSOTimeout:
                    var dialog = new UITimeOutDialog(vm, (int)data);
                    UIScreen.GlobalShowDialog(dialog, true);
                    var rnd = new Random();
                    dialog.Position = new Vector2(rnd.Next(Math.Max(0, ScreenWidth - 380)), rnd.Next(Math.Max(0, ScreenHeight - 180)));
                    break;
                case VMEventType.Resync:
                    if (ZoomLevel < 4)
                    {
                        SetTitle();
                    }
                    break;
            }
        }

        private void VMLotSwitch(uint lotId, LotTransitionInfo transition)
        {
            TransitionCameras = World?.State?.Cameras;

            FindController<CoreGameScreenController>()?.SwitchLot(lotId, transition);
        }

        private string lastLotTitle = "";
        private void Vm_OnChatEvent(SimAntics.NetPlay.Model.VMChatEvent evt)
        {
            if (ZoomLevel < 4)
            {
                SetTitle();
            }
        }

        private void SetTitle()
        {
            var title = LotControl.GetLotTitle();
            Title.SetTitle(title);
            if (lastLotTitle != title)
            {
                bool isPrivate = false;
                if (gizmo.CurrentAvatar.Value.Avatar_PrivacyMode > 0) isPrivate = true;
                DiscordRpcEngine.SendFSOPresence(
                    gizmo.CurrentAvatar.Value.Avatar_Name,
                    vm.LotName,
                    (int)FindController<CoreGameScreenController>().GetCurrentLotID(),
                    vm.Entities.Count(x => x is VMAvatar && x.PersistID != 0),
                    vm.LotName.StartsWith("{job:") ? 4 : 24,
                    vm.TSOState.PropertyCategory,
                    ApiClient.CDNUrl,
                    isPrivate
                    );
                lastLotTitle = title;
            }
        }
        
        private void VMRefreshed()
        {
            if (vm == null) return;
            GameThread.InUpdate(() =>
            {
                if (vm.LoadErrors.Count > 0) HandleLoadErrors();
                LotControl.ActiveEntity = null;
                LotControl.RefreshCut();
            });
        }

        private void HandleLoadErrors()
        {
            UIAlert.Alert("Lot Load Error", $"An error occurred loading lot state. It is not recommended that you continue, as you will desync from the server repeatedly. \n\n" +
                $"Errors: \n{String.Join("\n", vm.LoadErrors.Select(x => x.ToString()))}\n\n" +
                $"Your best bet is reinstalling FreeSO or The Sims Online. If this appears repeatedly, you definitely have an issue. You should also post this message on discord.",
                true);
            vm.LoadErrors.Clear();
        }

        public void CloseInbox()
        {
            Inbox.Visible = false;
        }

        public void OpenInbox()
        {
            Inbox.Visible = true;
            Inbox.X = GlobalSettings.Default.GraphicsWidth / 2 - 332;
            Inbox.Y = GlobalSettings.Default.GraphicsHeight / 2 - 184;
            WindowContainer.SendToFront(Inbox);
            ucp.FlashInbox(false);
        }

        public void CloseUserList()
        {
            UserList.Visible = false;
        }

        public void OpenUserList()
        {
            UserList.Visible = true;
            UserList.X = (GlobalSettings.Default.GraphicsWidth - UserList.Width) / 2;
            UserList.Y = (GlobalSettings.Default.GraphicsHeight - UserList.Height) / 2;
            WindowContainer.SendToFront(UserList);
            ucp.FlashUserList(false);
        }

        public void FlashInbox(bool flash)
        {
            ucp.FlashInbox(flash);
        }

        public void FlashUserList(bool flash)
        {
            ucp.FlashUserList(flash);
        }

        private void MouseHandler(UIMouseEventType type, UpdateState state)
        {
            if (CityRenderer != null) CityRenderer.UIMouseEvent(type, state); //all the city renderer needs are events telling it if the mouse is over it or not.
            //if the mouse is over it, the city renderer will handle the rest.
        }

        public void ZoomToCity()
        {
            //zooming back to city using mouse wheel.
            ZoomLevel = 4;
            var cam3d = CityRenderer.Camera as CityCamera3D;
            if (cam3d != null) cam3d.TargetZoom = 2.2f;
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
