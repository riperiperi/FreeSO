/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Model;
using FSO.Client.Rendering.City;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.Utils;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework;
using FSO.Files.Formats.IFF.Chunks;
using FSO.HIT;

using FSO.LotView;
using FSO.SimAntics;
using FSO.LotView.Components;
using FSO.Client.UI.Panels.LotControls;
using Microsoft.Xna.Framework.Input;
using FSO.LotView.Model;
using FSO.SimAntics.Primitives;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.Client.Debug;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.Client.UI.Panels.EODs;
using FSO.SimAntics.Utils;
using FSO.Common;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// Generates pie menus when the player clicks on objects.
    /// </summary>
    public class UILotControl : UIContainer, IDisposable
    {
        private UIMouseEventRef MouseEvt;
        public bool MouseIsOn;

        private UIPieMenu PieMenu;
        private UIChatPanel ChatPanel;

        private bool ShowTooltip;
        private bool TipIsError;
        private Texture2D RMBCursor;

        public FSO.SimAntics.VM vm;
        public LotView.World World;
        public VMEntity ActiveEntity;
        public uint SelectedSimID {
            get
            {
                return (vm == null) ? 0 : vm.MyUID;
            }
        }
        public short ObjectHover;
        public bool InteractionsAvailable;
        public UIInteractionQueue Queue;

        public bool LiveMode = true;
        public bool PanelActive = false;
        public UIObjectHolder ObjectHolder;
        public UIQueryPanel QueryPanel;

        public UICustomLotControl CustomControl;
        public UIEODController EODs;

        public int WallsMode = 1;

        private int OldMX;
        private int OldMY;
        private bool FoundMe; //if false and avatar changes, center. Should center on join lot.

        public bool RMBScroll;
        private int RMBScrollX;
        private int RMBScrollY;

        public UICheatHandler Cheats;
        public UIAvatarDataServiceUpdater AvatarDS;

        // NOTE: Blocking dialog system assumes that nothing goes wrong with data transmission (which it shouldn't, because we're using TCP)
        // and that the code actually blocks further dialogs from appearing while waiting for a response.
        // If we are to implement controlling multiple sims, this must be changed.
        private UIAlert BlockingDialog;
        private ulong LastDialogID;

        private static uint GOTO_GUID = 0x000007C4;
        public VMEntity GotoObject;

        private Rectangle MouseCutRect = new Rectangle(-4, -4, 4, 4);
        private List<uint> CutRooms = new List<uint>();
        private HashSet<uint> LastCutRooms = new HashSet<uint>(); //final rooms, including those outside. used to detect dirty.
        public sbyte LastFloor = -1;
        public WorldRotation LastRotation = WorldRotation.TopLeft;
        private bool[] LastCuts; //cached roomcuts, to apply rect cut to.
        private int LastWallMode = -1; //invalidates last roomcuts
        private bool LastRectCutNotable = false; //set if the last rect cut made a noticable change to the cuts array. If true refresh regardless of new cut effect.

        /// <summary>
        /// Creates a new UILotControl instance.
        /// </summary>
        /// <param name="vm">A SimAntics VM instance.</param>
        /// <param name="World">A World instance.</param>
        public UILotControl(FSO.SimAntics.VM vm, LotView.World World)
        {
            this.vm = vm;
            this.World = World;

            ActiveEntity = vm.Entities.FirstOrDefault(x => x is VMAvatar);
            MouseEvt = this.ListenForMouse(new Microsoft.Xna.Framework.Rectangle(0, 0, 
                GlobalSettings.Default.GraphicsWidth, GlobalSettings.Default.GraphicsHeight), OnMouse);

            Queue = new UIInteractionQueue(ActiveEntity, vm);
            this.Add(Queue);

            ObjectHolder = new UIObjectHolder(vm, World, this);
            QueryPanel = new UIQueryPanel(World);
            QueryPanel.OnSellBackClicked += ObjectHolder.SellBack;
            QueryPanel.OnInventoryClicked += ObjectHolder.MoveToInventory;
            QueryPanel.OnAsyncBuyClicked += ObjectHolder.AsyncBuy;
            QueryPanel.OnAsyncSaleClicked += ObjectHolder.AsyncSale;
            QueryPanel.OnAsyncPriceClicked += ObjectHolder.AsyncSale;
            QueryPanel.OnAsyncSaleCancelClicked += ObjectHolder.AsyncCancelSale;
            QueryPanel.X = 0;
            QueryPanel.Y = -114;

            ChatPanel = new UIChatPanel(vm, this);
            this.Add(ChatPanel);

            RMBCursor = GetTexture(0x24B00000001); //exploreanchor.bmp

            vm.OnChatEvent += Vm_OnChatEvent;
            vm.OnDialog += vm_OnDialog;
            vm.OnBreakpoint += Vm_OnBreakpoint;

            Cheats = new UICheatHandler(this);
            AvatarDS = new UIAvatarDataServiceUpdater(this);
            EODs = new UIEODController(this);
        }

        private void Vm_OnChatEvent(VMChatEvent evt)
        {
            evt.Visitors = vm.Entities.Count(x => x is VMAvatar && x.PersistID != 0);
            if (evt.Type == VMChatEventType.Message && evt.SenderUID == SelectedSimID) evt.Type = VMChatEventType.MessageMe;
            ChatPanel.SetLotName(vm.LotName);
            ChatPanel.ReceiveEvent(evt);
        }

        private void Vm_OnBreakpoint(VMEntity entity)
        {
            if (IDEHook.IDE != null) IDEHook.IDE.IDEBreakpointHit(vm, entity);
        }

        public string GetLotTitle()
        {
            return vm.LotName + " - " + vm.Entities.Count(x => x is VMAvatar && x.PersistID != 0);
        }

        void vm_OnDialog(FSO.SimAntics.Model.VMDialogInfo info)
        {
            if (info != null && ((info.DialogID == LastDialogID && info.DialogID != 0 && info.Block)
                || info.Caller != null && info.Caller != ActiveEntity)) return;
            //return if same dialog as before, or not ours
            if ((info == null || info.Block) && BlockingDialog != null)
            {
                //cancel current dialog because it's no longer valid
                UIScreen.RemoveDialog(BlockingDialog);
                LastDialogID = 0;
                BlockingDialog = null;
            }
            if (info == null) return; //return if we're just clearing a dialog.

            var options = new UIAlertOptions {
                Title = info.Title,
                Message = info.Message,
                Width = 325 + (int)(info.Message.Length / 3.5f),
                Alignment = TextAlignment.Left,
                TextSize = 12 };

            var b0Event = (info.Block) ? new ButtonClickDelegate(DialogButton0) : null;
            var b1Event = (info.Block) ? new ButtonClickDelegate(DialogButton1) : null;
            var b2Event = (info.Block) ? new ButtonClickDelegate(DialogButton2) : null;

            VMDialogType type = (info.Operand == null) ? VMDialogType.Message : info.Operand.Type;

            switch (type)
            {
                default:
                case VMDialogType.Message:
                    options.Buttons = new UIAlertButton[] { new UIAlertButton(UIAlertButtonType.OK, b0Event, info.Yes) };
                    break;
                case VMDialogType.YesNo:
                    options.Buttons = new UIAlertButton[]
                    {
                        new UIAlertButton(UIAlertButtonType.Yes, b0Event, info.Yes),
                        new UIAlertButton(UIAlertButtonType.No, b1Event, info.No),
                    };
                    break;
                case VMDialogType.YesNoCancel:
                    options.Buttons = new UIAlertButton[]
                    {
                        new UIAlertButton(UIAlertButtonType.Yes, b0Event, info.Yes),
                        new UIAlertButton(UIAlertButtonType.No, b1Event, info.No),
                        new UIAlertButton(UIAlertButtonType.Cancel, b2Event, info.Cancel),
                    };
                    break;
                case VMDialogType.TextEntry:
                case VMDialogType.NumericEntry:
                    options.Buttons = new UIAlertButton[] { new UIAlertButton(UIAlertButtonType.OK, b0Event, info.Yes) };
                    options.TextEntry = true;
                    break;
            }

            var alert = UIScreen.GlobalShowAlert(options, true);

            if (info.Block)
            {
                BlockingDialog = alert;
                LastDialogID = info.DialogID;
            }

            var entity = info.Icon;
            if (entity is VMGameObject)
            {
                var objects = entity.MultitileGroup.Objects;
                ObjectComponent[] objComps = new ObjectComponent[objects.Count];
                for (int i = 0; i < objects.Count; i++)
                {
                    objComps[i] = (ObjectComponent)objects[i].WorldUI;
                }
                var thumb = World.GetObjectThumb(objComps, entity.MultitileGroup.GetBasePositions(), GameFacade.GraphicsDevice);
                alert.SetIcon(thumb, 110, 110);
            }
        }

        private void DialogButton0(UIElement button) { DialogResponse(0); }
        private void DialogButton1(UIElement button) { DialogResponse(1); }
        private void DialogButton2(UIElement button) { DialogResponse(2); }

        private void DialogResponse(byte code)
        {
            if (BlockingDialog == null || ActiveEntity == null) return;
            UIScreen.RemoveDialog(BlockingDialog);
            LastDialogID = 0;
            vm.SendCommand(new VMNetDialogResponseCmd {
                ActorUID = ActiveEntity.PersistID,
                ResponseCode = code,
                ResponseText = (BlockingDialog.ResponseText == null) ? "" : BlockingDialog.ResponseText
            });
            BlockingDialog = null;
        }

        private void OnMouse(UIMouseEventType type, UpdateState state)
        {
            if (!vm.Ready) return;

            if (type == UIMouseEventType.MouseOver)
            {
                if (QueryPanel.Mode == 1) QueryPanel.Active = false;
                MouseIsOn = true;
            }
            else if (type == UIMouseEventType.MouseOut)
            {
                MouseIsOn = false;
                GameFacade.Cursor.SetCursor(CursorType.Normal);
                Tooltip = null;
            }
            else if (type == UIMouseEventType.MouseDown)
            {
                if (!LiveMode)
                {
                    if (CustomControl != null) CustomControl.MouseDown(state);
                    else ObjectHolder.MouseDown(state);
                    return;
                }
                if (PieMenu == null && ActiveEntity != null)
                {
                    VMEntity obj;
                    //get new pie menu, make new pie menu panel for it
                    var tilePos = World.State.WorldSpace.GetTileAtPosWithScroll(new Vector2(state.MouseState.X, state.MouseState.Y) / FSOEnvironment.DPIScaleFactor);

                    LotTilePos targetPos = LotTilePos.FromBigTile((short)tilePos.X, (short)tilePos.Y, World.State.Level);
                    if (vm.Context.SolidToAvatars(targetPos).Solid) targetPos = LotTilePos.OUT_OF_WORLD;

                    GotoObject.SetPosition(targetPos, Direction.NORTH, vm.Context);

                    bool objSelected = ObjectHover > 0 && InteractionsAvailable;
                    if (objSelected || (GotoObject.Position != LotTilePos.OUT_OF_WORLD && ObjectHover <= 0))
                    {
                        if (objSelected)
                        {
                            obj = vm.GetObjectById(ObjectHover);
                        } else
                        {
                            obj = GotoObject;
                        }
                        obj = obj.MultitileGroup.GetInteractionGroupLeader(obj);
                        if (obj is VMGameObject && ((VMGameObject)obj).Disabled > 0)
                        {
                            var flags = ((VMGameObject)obj).Disabled;

                            if ((flags & VMGameObjectDisableFlags.ForSale) > 0)
                            {
                                //for sale
                                var retailPrice = obj.MultitileGroup.Price; //wrong... should get this from catalog
                                var salePrice = obj.MultitileGroup.SalePrice;
                                ShowErrorTooltip(state, 22, true, "$"+retailPrice.ToString("##,#0"), "$"+salePrice.ToString("##,#0"));
                            }
                            else if ((flags & VMGameObjectDisableFlags.LotCategoryWrong) > 0)
                                ShowErrorTooltip(state, 21, true); //category wrong
                            else if ((flags & VMGameObjectDisableFlags.TransactionIncomplete) > 0)
                                ShowErrorTooltip(state, 27, true); //transaction not yet complete
                            else if ((flags & VMGameObjectDisableFlags.ObjectLimitExceeded) > 0)
                                ShowErrorTooltip(state, 24, true); //object is temporarily disabled... todo: something more helpful
                            else if ((flags & VMGameObjectDisableFlags.PendingRoommateDeletion) > 0)
                                ShowErrorTooltip(state, 16, true); //pending roommate deletion
                        } else
                        {
                            HITVM.Get().PlaySoundEvent(UISounds.PieMenuAppear);
                            var menu = obj.GetPieMenu(vm, ActiveEntity, false);
                            if (menu.Count != 0)
                            {
                                PieMenu = new UIPieMenu(menu, obj, ActiveEntity, this);
                                this.Add(PieMenu);
                                PieMenu.X = state.MouseState.X / FSOEnvironment.DPIScaleFactor;
                                PieMenu.Y = state.MouseState.Y / FSOEnvironment.DPIScaleFactor;
                                PieMenu.UpdateHeadPosition(state.MouseState.X, state.MouseState.Y);
                            }
                        }


                    }
                    else
                    {
                        ShowErrorTooltip(state, 0, true);
                    }
                }
                else
                {
                    if (PieMenu != null) PieMenu.RemoveSimScene();
                    this.Remove(PieMenu);
                    PieMenu = null;
                }
            }
            else if (type == UIMouseEventType.MouseUp)
            {
                if (!LiveMode)
                {
                    if (CustomControl != null) CustomControl.MouseUp(state);
                    else ObjectHolder.MouseUp(state);
                    return;
                }
                state.UIState.TooltipProperties.Show = false;
                state.UIState.TooltipProperties.Opacity = 0;
                ShowTooltip = false;
                TipIsError = false;
            }
        }

        private void ShowErrorTooltip(UpdateState state, uint id, bool playSound, params string[] args)
        {
            if (playSound) HITVM.Get().PlaySoundEvent(UISounds.Error);
            state.UIState.TooltipProperties.Show = true;
            state.UIState.TooltipProperties.Color = Color.Black;
            state.UIState.TooltipProperties.Opacity = 1;
            state.UIState.TooltipProperties.Position = new Vector2(state.MouseState.X,
                state.MouseState.Y);
            state.UIState.Tooltip = GameFacade.Strings.GetString("159", id.ToString(), args);
            state.UIState.TooltipProperties.UpdateDead = false;
            ShowTooltip = true;
            TipIsError = true;
        }

        public void ClosePie() 
        {
            if (PieMenu != null) 
            {
                PieMenu.RemoveSimScene();
                Queue.PieMenuClickPos = PieMenu.Position;
                this.Remove(PieMenu);
                PieMenu = null;
            }
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, GlobalSettings.Default.GraphicsWidth, GlobalSettings.Default.GraphicsHeight);
        }

        public void LiveModeUpdate(UpdateState state, bool scrolled)
        {
            if (MouseIsOn && ActiveEntity != null)
            {

                if (state.MouseState.X != OldMX || state.MouseState.Y != OldMY)
                {
                    OldMX = state.MouseState.X;
                    OldMY = state.MouseState.Y;
                    var newHover = World.GetObjectIDAtScreenPos(state.MouseState.X/FSOEnvironment.DPIScaleFactor, 
                        state.MouseState.Y / FSOEnvironment.DPIScaleFactor, 
                        GameFacade.GraphicsDevice);

                    if (ObjectHover != newHover)
                    {
                        ObjectHover = newHover;
                        if (ObjectHover > 0)
                        {
                            var obj = vm.GetObjectById(ObjectHover);
                            if (obj != null)
                            {
                                var menu = obj.GetPieMenu(vm, ActiveEntity, false);
                                InteractionsAvailable = (menu.Count > 0);
                            }
                        }
                    }

                    if (!TipIsError) ShowTooltip = false;
                    if (ObjectHover > 0)
                    {
                        var obj = vm.GetObjectById(ObjectHover);
                        if (!TipIsError)
                        {
                            if (obj is VMAvatar)
                            {
                                state.UIState.TooltipProperties.Show = true;
                                state.UIState.TooltipProperties.Color = Color.Black;
                                state.UIState.TooltipProperties.Opacity = 1;
                                state.UIState.TooltipProperties.Position = new Vector2(state.MouseState.X,
                                    state.MouseState.Y);
                                state.UIState.Tooltip = GetAvatarString(obj as VMAvatar);
                                state.UIState.TooltipProperties.UpdateDead = false;
                                ShowTooltip = true;
                            }
                            else if (((VMGameObject)obj).Disabled > 0)
                            {
                                var flags = ((VMGameObject)obj).Disabled;
                                if ((flags & VMGameObjectDisableFlags.ForSale) > 0)
                                {
                                    //for sale
                                    var retailPrice = obj.MultitileGroup.Price; //wrong... should get this from catalog
                                    var salePrice = obj.MultitileGroup.SalePrice;
                                    ShowErrorTooltip(state, 22, false, "$" + retailPrice.ToString("##,#0"), "$" + salePrice.ToString("##,#0"));
                                    TipIsError = false;
                                }
                            }

                        }
                    }
                    if (!ShowTooltip)
                    {
                        state.UIState.TooltipProperties.Show = false;
                        state.UIState.TooltipProperties.Opacity = 0;
                    }
                }
            }
            else
            {
                ObjectHover = 0;
            }

            if (!scrolled)
            { //set cursor depending on interaction availability
                CursorType cursor;

                if (PieMenu == null && MouseIsOn)
                {
                    if (ObjectHover == 0)
                    {
                        cursor = CursorType.LiveNothing;
                    }
                    else
                    {
                        if (InteractionsAvailable)
                        {
                            if (vm.GetObjectById(ObjectHover) is VMAvatar) cursor = CursorType.LivePerson;
                            else cursor = CursorType.LiveObjectAvail;
                        }
                        else
                        {
                            cursor = CursorType.LiveObjectUnavail;
                        }
                    }
                } else
                {

                    cursor = CursorType.Normal;
                } 

                CursorManager.INSTANCE.SetCursor(cursor);
            }

        }

        private string GetAvatarString(VMAvatar ava)
        {
            int prefixNum = 3;
            if (ava.IsPet) prefixNum = 5;
            else if (ava.PersistID == 0) prefixNum = 4;
            else
            {
                var permissionsLevel = ((VMTSOAvatarState)ava.TSOState).Permissions;
                switch (permissionsLevel)
                {
                    case VMTSOAvatarPermissions.Visitor: prefixNum = 3; break;
                    case VMTSOAvatarPermissions.Roommate:
                    case VMTSOAvatarPermissions.BuildBuyRoommate: prefixNum = 2; break;
                    case VMTSOAvatarPermissions.Admin:
                    case VMTSOAvatarPermissions.Owner: prefixNum = 1; break;
                }
            }
            return GameFacade.Strings.GetString("217", prefixNum.ToString()) + ava.ToString();
        }

        public void RefreshCut()
        {
            LastFloor = -1;
            LastWallMode = -1;
            MouseCutRect = new Rectangle(0,0,0,0);
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (RMBScroll)
            {
                DrawLocalTexture(batch, RMBCursor, new Vector2(RMBScrollX - RMBCursor.Width/2, RMBScrollY - RMBCursor.Height / 2));
            }
            base.Draw(batch);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            if (!vm.Ready) return;

            Cheats.Update(state);
            AvatarDS.Update();
            if (ActiveEntity == null || ActiveEntity.Dead || ActiveEntity.PersistID != SelectedSimID)
            {
                ActiveEntity = vm.Entities.FirstOrDefault(x => x is VMAvatar && x.PersistID == SelectedSimID); //try and hook onto a sim if we have none selected.
                if (ActiveEntity == null) ActiveEntity = vm.Entities.FirstOrDefault(x => x is VMAvatar);

                if (!FoundMe && ActiveEntity != null)
                {
                    vm.Context.World.State.CenterTile = new Vector2(ActiveEntity.VisualPosition.X, ActiveEntity.VisualPosition.Y);
                    vm.Context.World.State.ScrollAnchor = null;
                    FoundMe = true;
                }
                Queue.QueueOwner = ActiveEntity;
            }

            if (GotoObject == null) GotoObject = vm.Context.CreateObjectInstance(GOTO_GUID, LotTilePos.OUT_OF_WORLD, Direction.NORTH, true).Objects[0];

            if (ActiveEntity != null && BlockingDialog != null)
            {
                //are we still waiting on a blocking dialog? if not, cancel.
                if (ActiveEntity.Thread != null && (ActiveEntity.Thread.BlockingState == null || !(ActiveEntity.Thread.BlockingState is VMDialogResult)))
                {
                    UIScreen.RemoveDialog(BlockingDialog);
                    LastDialogID = 0;
                    BlockingDialog = null;
                }
            }

            if (Visible)
            {
                if (ShowTooltip) state.UIState.TooltipProperties.UpdateDead = false;

                bool scrolled = false;
                if (RMBScroll)
                {
                    World.State.ScrollAnchor = null;
                    Vector2 scrollBy = new Vector2();
                    if (state.TouchMode)
                    {
                        scrollBy = new Vector2(RMBScrollX - state.MouseState.X, RMBScrollY - state.MouseState.Y);
                        RMBScrollX = state.MouseState.X;
                        RMBScrollY = state.MouseState.Y;
                        scrollBy /= 128f;
                        scrollBy /= FSOEnvironment.DPIScaleFactor;
                    } else
                    {
                        scrollBy = new Vector2(state.MouseState.X - RMBScrollX, state.MouseState.Y - RMBScrollY);
                        scrollBy *= 0.0005f;

                        var angle = (Math.Atan2(state.MouseState.X - RMBScrollX, (RMBScrollY - state.MouseState.Y)*2) / Math.PI) * 4;
                        angle += 8;
                        angle %= 8;

                        CursorType type = CursorType.ArrowUp;
                        switch ((int)Math.Round(angle))
                        {
                            case 0: type = CursorType.ArrowUp; break;
                            case 1: type = CursorType.ArrowUpRight; break;
                            case 2: type = CursorType.ArrowRight; break;
                            case 3: type = CursorType.ArrowDownRight; break;
                            case 4: type = CursorType.ArrowDown; break;
                            case 5: type = CursorType.ArrowDownLeft; break;
                            case 6: type = CursorType.ArrowLeft; break;
                            case 7: type = CursorType.ArrowUpLeft; break;
                        }
                        GameFacade.Cursor.SetCursor(type);
                       Console.WriteLine(Math.Round(angle) % 8);
                    }
                    World.Scroll(scrollBy);
                    scrolled = true;
                }
                if (MouseIsOn)
                {
                    if (state.MouseState.RightButton == ButtonState.Pressed)
                    {
                        if (RMBScroll == false)
                        {
                            RMBScroll = true;
                            RMBScrollX = state.MouseState.X;
                            RMBScrollY = state.MouseState.Y;
                        }
                    }
                    else
                    {
                        if (!scrolled && GlobalSettings.Default.EdgeScroll && !state.TouchMode) scrolled = World.TestScroll(state);
                    }
                }

                if (state.MouseState.RightButton != ButtonState.Pressed)
                {
                    if (RMBScroll) GameFacade.Cursor.SetCursor(CursorType.Normal);
                    RMBScroll = false;
                }

                if (LiveMode) LiveModeUpdate(state, scrolled);
                else if (CustomControl != null) CustomControl.Update(state, scrolled);
                else ObjectHolder.Update(state, scrolled);

                //set cutaway around mouse

                if (vm.Context.Blueprint != null)
                {
                    World.State.DynamicCutaway = (WallsMode == 1);
                    //first we need to cycle the rooms that are being cutaway. Keep this up even if we're in all-cut mode.
                    var mouseTilePos = World.State.WorldSpace.GetTileAtPosWithScroll(new Vector2(state.MouseState.X, state.MouseState.Y) / FSOEnvironment.DPIScaleFactor);
                    var roomHover = vm.Context.GetRoomAt(LotTilePos.FromBigTile((short)(mouseTilePos.X), (short)(mouseTilePos.Y), World.State.Level));
                    var outside = (vm.Context.RoomInfo[roomHover].Room.IsOutside);
                    if (!outside && !CutRooms.Contains(roomHover))
                        CutRooms.Add(roomHover); //outside hover should not persist like with other rooms.
                    while (CutRooms.Count > 3) CutRooms.Remove(CutRooms.ElementAt(0));

                    if (LastWallMode != WallsMode)
                    {
                        if (WallsMode == 0) //walls down
                        {
                            LastCuts = new bool[vm.Context.Architecture.Width * vm.Context.Architecture.Height];
                            vm.Context.Blueprint.Cutaway = LastCuts;
                            vm.Context.Blueprint.Damage.Add(new FSO.LotView.Model.BlueprintDamage(FSO.LotView.Model.BlueprintDamageType.WALL_CUT_CHANGED));
                            for (int i = 0; i < LastCuts.Length; i++) LastCuts[i] = true;
                        }
                        else if (WallsMode == 1)
                        {
                            MouseCutRect = new Rectangle();
                            LastCutRooms = new HashSet<uint>() { uint.MaxValue }; //must regenerate cuts
                        }
                        else //walls up or roof
                        {
                            LastCuts = new bool[vm.Context.Architecture.Width * vm.Context.Architecture.Height];
                            vm.Context.Blueprint.Cutaway = LastCuts;
                            vm.Context.Blueprint.Damage.Add(new FSO.LotView.Model.BlueprintDamage(FSO.LotView.Model.BlueprintDamageType.WALL_CUT_CHANGED));
                        }
                        LastWallMode = WallsMode;
                    }

                    if (WallsMode == 1)
                    {
                        int recut = 0;
                        var finalRooms = new HashSet<uint>(CutRooms);

                        var newCut = new Rectangle((int)(mouseTilePos.X - 2.5), (int)(mouseTilePos.Y - 2.5), 5, 5);
                        newCut.X -= VMArchitectureTools.CutCheckDir[(int)World.State.Rotation][0]*2;
                        newCut.Y -= VMArchitectureTools.CutCheckDir[(int)World.State.Rotation][1]*2;
                        if (newCut != MouseCutRect)
                        {
                            MouseCutRect = newCut;
                            recut = 1;
                        }

                        if (LastFloor != World.State.Level || LastRotation != World.State.Rotation || !finalRooms.SetEquals(LastCutRooms))
                        {
                            LastCuts = VMArchitectureTools.GenerateRoomCut(vm.Context.Architecture, World.State.Level, World.State.Rotation, finalRooms);
                            recut = 2;
                            LastFloor = World.State.Level;
                            LastRotation = World.State.Rotation;
                        }
                        LastCutRooms = finalRooms;

                        if (recut > 0)
                        {
                            var finalCut = new bool[LastCuts.Length];
                            Array.Copy(LastCuts, finalCut, LastCuts.Length);
                            var notableChange = VMArchitectureTools.ApplyCutRectangle(vm.Context.Architecture, World.State.Level, finalCut, MouseCutRect);
                            if (recut > 1 || notableChange || LastRectCutNotable)
                            {
                                vm.Context.Blueprint.Cutaway = finalCut;
                                vm.Context.Blueprint.Damage.Add(new FSO.LotView.Model.BlueprintDamage(FSO.LotView.Model.BlueprintDamageType.WALL_CUT_CHANGED));
                            }
                            LastRectCutNotable = notableChange;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            AvatarDS.ReleaseAvatars();
        }
    }
}
