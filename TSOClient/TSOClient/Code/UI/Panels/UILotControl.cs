/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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

namespace TSOClient.Code.UI.Panels
{
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

        public UILotControl(TSO.Simantics.VM vm, World World)
        {
            this.vm = vm;
            this.World = World;

            ActiveEntity = vm.Entities.FirstOrDefault(x => x is VMAvatar);
            MouseEvt = this.ListenForMouse(new Microsoft.Xna.Framework.Rectangle(0, 0, GlobalSettings.Default.GraphicsWidth, GlobalSettings.Default.GraphicsHeight), OnMouse);
            testimg = new UIImage();
            testimg.X = 20;
            testimg.Y = 20;
            this.Add(testimg);

            Queue = new UIInteractionQueue(ActiveEntity);
            this.Add(Queue);
        }

        private void OnMouse(UIMouseEventType type, UpdateState state)
        {
            if (type == UIMouseEventType.MouseOver) MouseIsOn = true;
            else if (type == UIMouseEventType.MouseOut)
            {
                MouseIsOn = false;
                Tooltip = null;
            }
            else if (type == UIMouseEventType.MouseDown)
            {
                if (PieMenu == null)
                {
                    //get new pie menu, make new pie menu panel for it
                    if (ObjectHover != 0)
                    {
                        if (InteractionsAvailable)
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
                            }
                        }
                        else
                        {
                            HITVM.Get().PlaySoundEvent(UISounds.Error);
                            GameFacade.Screens.TooltipProperties.Show = true;
                            GameFacade.Screens.TooltipProperties.Opacity = 1;
                            GameFacade.Screens.TooltipProperties.Position = new Vector2(state.MouseState.X, state.MouseState.Y);
                            GameFacade.Screens.Tooltip = GameFacade.Strings.GetString("159", "0");
                            GameFacade.Screens.TooltipProperties.UpdateDead = false;
                            ShowTooltip = true;
                        }
                    }
                }
                else
                {
                    this.Remove(PieMenu);
                    PieMenu = null;
                }
            }
            else if (type == UIMouseEventType.MouseUp)
            {
                GameFacade.Screens.TooltipProperties.Show = false;
                GameFacade.Screens.TooltipProperties.Opacity = 0;
                ShowTooltip = false;
            }
        }

        public void ClosePie() {
            if (PieMenu != null) {
                Queue.PieMenuClickPos = PieMenu.Position;
                this.Remove(PieMenu);
                PieMenu = null;
            }
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, GlobalSettings.Default.GraphicsWidth, GlobalSettings.Default.GraphicsHeight);
        }

        public override void Update(TSO.Common.rendering.framework.model.UpdateState state)
        {
            base.Update(state);

            if (ShowTooltip) GameFacade.Screens.TooltipProperties.UpdateDead = false;

            var scrolled = World.TestScroll(state);
            if (MouseIsOn)
            {
                var newHover = World.GetObjectIDAtScreenPos(state.MouseState.X, state.MouseState.Y, GameFacade.GraphicsDevice);
                if (ObjectHover != newHover) {
                    ObjectHover = newHover;
                    if (ObjectHover != 0)
                    {
                        var menu = vm.GetObjectById(ObjectHover).GetPieMenu(vm, ActiveEntity);
                        InteractionsAvailable = (menu.Count > 0);
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
                    if (InteractionsAvailable) {
                        if (vm.GetObjectById(ObjectHover) is VMAvatar) cursor = CursorType.LivePerson;
                        else cursor = CursorType.LiveObjectAvail;
                    } else {
                        cursor = CursorType.LiveObjectUnavail;
                    }
                }
                CursorManager.INSTANCE.SetCursor(cursor);
            }
        }
    }
}
