/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
RHY3756547. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Panels;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.UI.Model;
using TSOClient.Code.Rendering.City;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Utils;
using TSO.Common.rendering.framework.model;
using TSO.Common.rendering.framework.io;
using TSO.Common.rendering.framework;
using TSO.Files.formats.iff.chunks;
using TSO.HIT;

using tso.world;
using TSO.Simantics;
using tso.world.components;
using TSOClient.Code.UI.Panels.LotControls;
using Microsoft.Xna.Framework.Input;

namespace TSOClient.Code.UI.Panels
{
    /// <summary>
    /// Generates pie menus when the player clicks on objects.
    /// </summary>
    public class UILotControl : UIContainer
    {
        private UIMouseEventRef MouseEvt;
        private bool MouseIsOn;
        private UIPieMenu PieMenu;
        private bool ShowTooltip;
        public TSO.Simantics.VM vm;
        public World World;
        public VMEntity ActiveEntity;
        public short ObjectHover;
        public bool InteractionsAvailable;
        public UIImage testimg;
        public UIInteractionQueue Queue;

        public bool LiveMode = true;
        public UIObjectHolder ObjectHolder;
        public UIQueryPanel QueryPanel;

        public UICustomLotControl CustomControl;

        public int WallsMode;

        private int OldMX;
        private int OldMY;

        private bool RMBScroll;
        private int RMBScrollX;
        private int RMBScrollY;

        private bool TabLastPressed;

        private Rectangle MouseCutRect = new Rectangle(-4, -4, 4, 4);

        /// <summary>
        /// Creates a new UILotControl instance.
        /// </summary>
        /// <param name="vm">A SimAntics VM instance.</param>
        /// <param name="World">A World instance.</param>
        public UILotControl(TSO.Simantics.VM vm, World World)
        {
            this.vm = vm;
            this.World = World;

            ActiveEntity = vm.Entities.FirstOrDefault(x => x is VMAvatar);
            MouseEvt = this.ListenForMouse(new Microsoft.Xna.Framework.Rectangle(0, 0, 
                GlobalSettings.Default.GraphicsWidth, GlobalSettings.Default.GraphicsHeight), OnMouse);
            testimg = new UIImage();
            testimg.X = 20;
            testimg.Y = 20;
            this.Add(testimg);

            Queue = new UIInteractionQueue(ActiveEntity);
            this.Add(Queue);

            ObjectHolder = new UIObjectHolder(vm, World, this);
            QueryPanel = new UIQueryPanel(World);
            QueryPanel.OnSellBackClicked += ObjectHolder.SellBack;
            QueryPanel.X = 177;
            QueryPanel.Y = GlobalSettings.Default.GraphicsHeight - 228;
            this.Add(QueryPanel);

            vm.OnDialog += vm_OnDialog;
        }

        void vm_OnDialog(TSO.Simantics.model.VMDialogInfo info)
        {
            var alert = UIScreen.ShowAlert(new UIAlertOptions { Title = info.Title, Message = info.Message, Width = 325+(int)(info.Message.Length/3.5f), Alignment = TextAlignment.Left, TextSize = 12 }, true);
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

        private void OnMouse(UIMouseEventType type, UpdateState state)
        {
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
                if (PieMenu == null)
                {
                    //get new pie menu, make new pie menu panel for it
                        if (ObjectHover != 0 && InteractionsAvailable)
                        {
                            HITVM.Get().PlaySoundEvent(UISounds.PieMenuAppear);
                            var obj = vm.GetObjectById(ObjectHover);
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
                            GameFacade.Screens.TooltipProperties.Show = true;
                            GameFacade.Screens.TooltipProperties.Opacity = 1;
                            GameFacade.Screens.TooltipProperties.Position = new Vector2(state.MouseState.X,
                                state.MouseState.Y);
                            GameFacade.Screens.Tooltip = GameFacade.Strings.GetString("159", "0");
                            GameFacade.Screens.TooltipProperties.UpdateDead = false;
                            ShowTooltip = true;
                        }
                }
                else
                {
                    PieMenu.RemoveSimScene();
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
                GameFacade.Screens.TooltipProperties.Show = false;
                GameFacade.Screens.TooltipProperties.Opacity = 0;
                ShowTooltip = false;
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
            //ActiveEntity = vm.Entities.Where(x => x is VMAvatar).ElementAt(0);
            //Queue.QueueOwner = ActiveEntity;
            if (ActiveEntity == null || ActiveEntity.Dead)
            {
                ActiveEntity = vm.Entities.FirstOrDefault(x => x is VMAvatar); //try and hook onto a sim if we have none selected.
                Queue.QueueOwner = ActiveEntity;
            }

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
                            var menu = vm.GetObjectById(ObjectHover).GetPieMenu(vm, ActiveEntity);
                            InteractionsAvailable = (menu.Count > 0);
                        }
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

                CursorManager.INSTANCE.SetCursor(cursor);
            }

        }

        public override void Update(UpdateState state)
        {
            base.Update(state);


            if (state.KeyboardState.IsKeyDown(Keys.Tab))
            {
                if (!TabLastPressed)
                {
                    //switch active sim

                    ActiveEntity = vm.Entities.FirstOrDefault(x => (x is VMAvatar && x.ObjectID > ActiveEntity.ObjectID && x.Object.OBJ.GUID == 0x7FD96B54));
                    if (ActiveEntity == null) ActiveEntity = vm.Entities.FirstOrDefault(x => (x is VMAvatar && x.Object.OBJ.GUID == 0x7FD96B54));
                    HITVM.Get().PlaySoundEvent(UISounds.Speed1To3);
                    Queue.QueueOwner = ActiveEntity;

                    TabLastPressed = true;
                }
                
            } else TabLastPressed = false;

            if (Visible)
            {
                if (ShowTooltip) GameFacade.Screens.TooltipProperties.UpdateDead = false;

                bool scrolled = false;
                if (RMBScroll)
                {
                    Vector2 scrollBy = new Vector2(state.MouseState.X - RMBScrollX, state.MouseState.Y - RMBScrollY);
                    scrollBy *= 0.0005f;
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
                        if (!scrolled && GlobalSettings.Default.EdgeScroll) scrolled = World.TestScroll(state);
                    }

                }

                if (LiveMode) LiveModeUpdate(state, scrolled);
                else if (CustomControl != null) CustomControl.Update(state, scrolled);
                else ObjectHolder.Update(state, scrolled);

                //set cutaway around mouse

                var cuts = vm.Context.Blueprint.Cutaway;
                Rectangle newCut;
                if (WallsMode == 0){
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


                if (!newCut.Equals(MouseCutRect)) {
                    if (cuts.Contains(MouseCutRect)) cuts.Remove(MouseCutRect);
                    MouseCutRect = newCut;
                    cuts.Add(MouseCutRect);
                    vm.Context.Blueprint.Damage.Add(new tso.world.model.BlueprintDamage(tso.world.model.BlueprintDamageType.WALL_CUT_CHANGED));
                }
            }
        }
    }
}
