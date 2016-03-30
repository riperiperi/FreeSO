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

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// Generates pie menus when the player clicks on objects.
    /// </summary>
    public class UILotControl : UIContainer
    {
        private UIMouseEventRef MouseEvt;
        private bool MouseIsOn;

        private UIPieMenu PieMenu;
        private UIChatPanel ChatPanel;

        private bool ShowTooltip;
        private bool TipIsError;

        public FSO.SimAntics.VM vm;
        public LotView.World World;
        public VMEntity ActiveEntity;
        public uint SelectedSimID;
        public short ObjectHover;
        public bool InteractionsAvailable;
        public UIInteractionQueue Queue;

        public bool LiveMode = true;
        public bool PanelActive = false;
        public UIObjectHolder ObjectHolder;
        public UIQueryPanel QueryPanel;

        public UICustomLotControl CustomControl;

        public int WallsMode;

        private int OldMX;
        private int OldMY;
        private bool FoundMe; //if false and avatar changes, center. Should center on join lot.

        private bool RMBScroll;
        private int RMBScrollX;
        private int RMBScrollY;

        // NOTE: Blocking dialog system assumes that nothing goes wrong with data transmission (which it shouldn't, because we're using TCP)
        // and that the code actually blocks further dialogs from appearing while waiting for a response.
        // If we are to implement controlling multiple sims, this must be changed.
        private UIAlert BlockingDialog;

        private static uint GOTO_GUID = 0x000007C4;
        public VMEntity GotoObject;

        private Rectangle MouseCutRect = new Rectangle(-4, -4, 4, 4);

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
            QueryPanel.X = 177;
            QueryPanel.Y = GlobalSettings.Default.GraphicsHeight - 228;
            this.Add(QueryPanel);

            ChatPanel = new UIChatPanel(vm, this);
            this.Add(ChatPanel);

            vm.OnChatEvent += Vm_OnChatEvent;
            vm.OnDialog += vm_OnDialog;
            vm.OnBreakpoint += Vm_OnBreakpoint;
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
            if (info.Caller != null && info.Caller != ActiveEntity) return;

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

            var alert = UIScreen.ShowAlert(options, true);

            if (info.Block) BlockingDialog = alert;

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
            if (BlockingDialog == null) return;
            UIScreen.RemoveDialog(BlockingDialog);
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
                    var tilePos = World.State.WorldSpace.GetTileAtPosWithScroll(new Vector2(state.MouseState.X, state.MouseState.Y));

                    LotTilePos targetPos = LotTilePos.FromBigTile((short)tilePos.X, (short)tilePos.Y, World.State.Level);
                    if (vm.Context.SolidToAvatars(targetPos).Solid) targetPos = LotTilePos.OUT_OF_WORLD;

                    GotoObject.SetPosition(targetPos, Direction.NORTH, vm.Context);

                    bool objSelected = ObjectHover > 0 && InteractionsAvailable;
                    if (objSelected || (GotoObject.Position != LotTilePos.OUT_OF_WORLD && ObjectHover <= 0))
                    {
                        HITVM.Get().PlaySoundEvent(UISounds.PieMenuAppear);
                        if (objSelected)
                        {
                            obj = vm.GetObjectById(ObjectHover);
                        } else
                        {
                            obj = GotoObject;
                        }
                        obj = obj.MultitileGroup.GetInteractionGroupLeader(obj);

                        var menu = obj.GetPieMenu(vm, ActiveEntity);
                        if (menu.Count != 0)
                        {
                            PieMenu = new UIPieMenu(menu, obj, ActiveEntity, this);
                            this.Add(PieMenu);
                            PieMenu.X = state.MouseState.X;
                            PieMenu.Y = state.MouseState.Y;
                            PieMenu.UpdateHeadPosition(state.MouseState.X, state.MouseState.Y);
                        }
                    }
                    else
                    {
                        HITVM.Get().PlaySoundEvent(UISounds.Error);
                        state.UIState.TooltipProperties.Show = true;
                        state.UIState.TooltipProperties.Opacity = 1;
                        state.UIState.TooltipProperties.Position = new Vector2(state.MouseState.X,
                            state.MouseState.Y);
                        state.UIState.Tooltip = GameFacade.Strings.GetString("159", "0");
                        state.UIState.TooltipProperties.UpdateDead = false;
                        ShowTooltip = true;
                        TipIsError = true;
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
                    var newHover = World.GetObjectIDAtScreenPos(state.MouseState.X, state.MouseState.Y, GameFacade.GraphicsDevice);
                    //if (newHover == 0) newHover = ActiveEntity.ObjectID;
                    if (ObjectHover != newHover)
                    {
                        ObjectHover = newHover;
                        if (ObjectHover > 0)
                        {
                            var obj = vm.GetObjectById(ObjectHover);
                            var menu = obj.GetPieMenu(vm, ActiveEntity);
                            InteractionsAvailable = (menu.Count > 0);
                        }
                    }

                    if (!TipIsError) ShowTooltip = false;
                    if (ObjectHover > 0)
                    {
                        var obj = vm.GetObjectById(ObjectHover);
                        if (obj is VMAvatar && !TipIsError)
                        {
                            state.UIState.TooltipProperties.Show = true;
                            state.UIState.TooltipProperties.Opacity = 1;
                            state.UIState.TooltipProperties.Position = new Vector2(state.MouseState.X,
                                state.MouseState.Y);
                            state.UIState.Tooltip = obj.ToString();
                            state.UIState.TooltipProperties.UpdateDead = false;
                            ShowTooltip = true;
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

        public void RefreshCut()
        {
            MouseCutRect = new Rectangle(0,0,0,0);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            if (!vm.Ready) return;

            if (ActiveEntity == null || ActiveEntity.Dead || ActiveEntity.PersistID != SelectedSimID)
            {
                ActiveEntity = vm.Entities.FirstOrDefault(x => x is VMAvatar && x.PersistID == SelectedSimID); //try and hook onto a sim if we have none selected.
                if (ActiveEntity == null) ActiveEntity = vm.Entities.FirstOrDefault(x => x is VMAvatar);

                if (!FoundMe)
                {
                    vm.Context.World.State.CenterTile = new Vector2(ActiveEntity.VisualPosition.X, ActiveEntity.VisualPosition.Y);
                    FoundMe = true;
                }
                Queue.QueueOwner = ActiveEntity;
            }

            if (GotoObject == null) GotoObject = vm.Context.CreateObjectInstance(GOTO_GUID, LotTilePos.OUT_OF_WORLD, Direction.NORTH, true).Objects[0];

            if (Visible)
            {
                if (ShowTooltip) state.UIState.TooltipProperties.UpdateDead = false;

                bool scrolled = false;
                if (RMBScroll)
                {
                    Vector2 scrollBy = new Vector2();
                    if (state.TouchMode)
                    {
                        scrollBy = new Vector2(RMBScrollX - state.MouseState.X, RMBScrollY - state.MouseState.Y);
                        RMBScrollX = state.MouseState.X;
                        RMBScrollY = state.MouseState.Y;
                        scrollBy /= 128f;
                    } else
                    {
                        scrollBy = new Vector2(state.MouseState.X - RMBScrollX, state.MouseState.Y - RMBScrollY);
                        scrollBy *= 0.0005f;
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
                        RMBScroll = false;
                        if (!scrolled && GlobalSettings.Default.EdgeScroll && !state.TouchMode) scrolled = World.TestScroll(state);
                    }

                }

                if (LiveMode) LiveModeUpdate(state, scrolled);
                else if (CustomControl != null) CustomControl.Update(state, scrolled);
                else ObjectHolder.Update(state, scrolled);

                //set cutaway around mouse

                if (vm.Context.Blueprint != null)
                {
                    var cuts = vm.Context.Blueprint.Cutaway;
                    Rectangle newCut;
                    if (WallsMode == 0)
                    {
                        newCut = new Rectangle(-1, -1, 1024, 1024); //cut all; walls down.
                    }
                    else if (WallsMode == 1)
                    {
                        var mouseTilePos = World.State.WorldSpace.GetTileAtPosWithScroll(new Vector2(state.MouseState.X, state.MouseState.Y + 128));
                        newCut = new Rectangle((int)(mouseTilePos.X - 5.5), (int)(mouseTilePos.Y - 5.5), 11, 11);
                    }
                    else
                    {
                        newCut = new Rectangle(0, 0, 0, 0); //walls up or roof
                    }


                    if (!newCut.Equals(MouseCutRect))
                    {
                        if (cuts.Contains(MouseCutRect)) cuts.Remove(MouseCutRect);
                        MouseCutRect = newCut;
                        cuts.Add(MouseCutRect);
                        vm.Context.Blueprint.Damage.Add(new FSO.LotView.Model.BlueprintDamage(FSO.LotView.Model.BlueprintDamageType.WALL_CUT_CHANGED));
                    }
                }
            }
        }
    }
}
