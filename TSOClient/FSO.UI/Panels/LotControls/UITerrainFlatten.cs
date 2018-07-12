/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.LotView;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.HIT;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.Client.UI.Model;
using FSO.Common;
using FSO.UI.Panels.LotControls;

namespace FSO.Client.UI.Panels.LotControls
{
    public class UITerrainFlatten : UICustomLotControl
    {
        VMMultitileGroup WallCursor;
        VMMultitileGroup WallCursor2;
        VM vm;
        LotView.World World;
        ILotControl Parent;

        private bool Drawing;
        private Point StartPosition;
        private Point EndPosition;

        private short StartTerrainHeight;
        private int StartMousePosition;
        private Point EndMousePosition;

        private VMArchitectureCommand LastCmd;
        private bool WasDown;

        public UITerrainFlatten(VM vm, LotView.World world, ILotControl parent, List<int> parameters)
        {
            this.vm = vm;
            World = parent.World;
            Parent = parent;
            WallCursor = vm.Context.CreateObjectInstance(0x2F39B7A6, LotTilePos.OUT_OF_WORLD, FSO.LotView.Model.Direction.NORTH, true);
            WallCursor2 = vm.Context.CreateObjectInstance(0x2F39B7A6, LotTilePos.OUT_OF_WORLD, FSO.LotView.Model.Direction.NORTH, true);

            ((ObjectComponent)WallCursor.Objects[0].WorldUI).ForceDynamic = true;
            ((ObjectComponent)WallCursor2.Objects[0].WorldUI).ForceDynamic = true;
        }

        //0: up
        //1: down
        //2: error
        //3: anchor
        //4: level
        public void SetCursorGraphic(short id, VMMultitileGroup group)
        {
            group.Objects[0].SetValue(VMStackObjectVariable.Graphic, id);
            ((VMGameObject)group.Objects[0]).RefreshGraphic();
        }

        public override void MouseDown(UpdateState state)
        {
            if (!Drawing)
            {
                HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolDown);

                var tilePos = World.EstTileAtPosWithScroll(new Vector2(MousePosition.X, MousePosition.Y) / FSOEnvironment.DPIScaleFactor);
                StartPosition = new Point((int)Math.Round(tilePos.X), (int)Math.Round(tilePos.Y));
                var terrain = vm.Context.Architecture.Terrain;

                if (StartPosition.Y >= terrain.Height || StartPosition.X >= terrain.Width || StartPosition.X < 0 || StartPosition.Y < 0) return;
                Drawing = true;
                StartTerrainHeight = terrain.Heights[StartPosition.Y*terrain.Width + StartPosition.X];
                StartMousePosition = (int)(MousePosition.Y - World.State.WorldSpace.GetScreenOffset().Y);
            }
        }

        public override void MouseUp(UpdateState state)
        {
            if (Drawing)
            {
                var cmds = new List<VMArchitectureCommand>();

                var cursor = EndPosition;
                var smallX = Math.Min(StartPosition.X, cursor.X);
                var smallY = Math.Min(StartPosition.Y, cursor.Y);
                var bigX = Math.Max(StartPosition.X, cursor.X);
                var bigY = Math.Max(StartPosition.Y, cursor.Y);

                if (smallX != bigX || bigY != smallY || (Modifiers.HasFlag(UILotControlModifiers.CTRL)))
                {
                    cmds.Add(new VMArchitectureCommand
                    {
                        Type = VMArchitectureCommandType.TERRAIN_FLATTEN,
                        x = smallX,
                        y = smallY,
                        x2 = bigX - smallX,
                        y2 = bigY - smallY,
                        style = (ushort)StartTerrainHeight,
                        pattern = (ushort)((Modifiers.HasFlag(UILotControlModifiers.CTRL)) ? 1 : 0)
                    });
                }

                if (cmds.Count > 0 && (Parent.ActiveEntity == null || vm.Context.Architecture.LastTestCost <= Parent.Budget))
                {
                    vm.SendCommand(new VMNetArchitectureCmd
                    {
                        Commands = new List<VMArchitectureCommand>(cmds)
                    });
                    
                    HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolPlace);
                }
                else HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolUp);
            }
            Drawing = false;
        }

        public override void Update(UpdateState state, bool scrolled)
        {
            var tilePos = World.EstTileAtPosWithScroll(new Vector2(MousePosition.X, MousePosition.Y) / FSOEnvironment.DPIScaleFactor);
            Point cursor = new Point((int)Math.Round(tilePos.X), (int)Math.Round(tilePos.Y));

            var cmds = vm.Context.Architecture.Commands;
            cmds.Clear();
            if (Drawing)
            {
                EndPosition = cursor;
                var smallX = Math.Min(StartPosition.X, cursor.X);
                var smallY = Math.Min(StartPosition.Y, cursor.Y);
                var bigX = Math.Max(StartPosition.X, cursor.X);
                var bigY = Math.Max(StartPosition.Y, cursor.Y);

                cmds.Add(new VMArchitectureCommand
                {
                    Type = VMArchitectureCommandType.TERRAIN_FLATTEN,
                    x = smallX,
                    y = smallY,
                    x2 = bigX-smallX,
                    y2 = bigY-smallY,
                    style = (ushort)StartTerrainHeight,
                    pattern = (ushort)((Modifiers.HasFlag(UILotControlModifiers.CTRL)) ? 1 : 0)
                });
                WallCursor2.SetVisualPosition(new Vector3(StartPosition.X, StartPosition.Y, (World.State.Level - 1) * 2.95f), Direction.NORTH, vm.Context);
            } else
            {
                WallCursor2.SetVisualPosition(new Vector3(-2048, -2048, 0), Direction.NORTH, vm.Context);
            }

            if (cmds.Count > 0)
            {
                if (!WasDown || !cmds[0].Equals(LastCmd))
                {
                    vm.Context.Architecture.SignalRedraw();
                    WasDown = true;
                }

                var cost = vm.Context.Architecture.LastTestCost;
                if (cost != 0)
                {
                    var disallowed = Parent.ActiveEntity != null && cost > Parent.Budget;
                    state.UIState.TooltipProperties.Show = true;
                    state.UIState.TooltipProperties.Color = disallowed ? Color.DarkRed : Color.Black;
                    state.UIState.TooltipProperties.Opacity = 1;
                    state.UIState.TooltipProperties.Position = new Vector2(MousePosition.X, MousePosition.Y);
                    state.UIState.Tooltip = (cost < 0) ? ("-$" + (-cost)) : ("$" + cost);
                    state.UIState.TooltipProperties.UpdateDead = false;

                    if (!cmds[0].Equals(LastCmd) && disallowed)
                    {
                        HITVM.Get().PlaySoundEvent(UISounds.Error);
                    }
                }
                else
                {
                    state.UIState.TooltipProperties.Show = false;
                    state.UIState.TooltipProperties.Opacity = 0;
                }
                LastCmd = cmds[0];
            }
            else
            {
                if (WasDown)
                {
                    vm.Context.Architecture.Commands.Clear();
                    vm.Context.Architecture.SignalTerrainRedraw();
                    vm.Context.Architecture.SignalRedraw();
                    WasDown = false;
                }
            }

            WallCursor.SetVisualPosition(new Vector3(cursor.X, cursor.Y, (World.State.Level - 1) * 2.95f), Direction.NORTH, vm.Context);

            SetCursorGraphic(3, WallCursor);
            SetCursorGraphic(3, WallCursor2);
        }

        public override void Release()
        {
            WallCursor.Delete(vm.Context);
            vm.Context.Architecture.Commands.Clear();
            vm.Context.Architecture.SignalTerrainRedraw();
            vm.Context.Architecture.SignalRedraw();
        }
    }
}
