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

namespace FSO.Client.UI.Panels.LotControls
{
    public class UIGrassPaint : UICustomLotControl
    {
        VMMultitileGroup WallCursor;
        VM vm;
        LotView.World World;
        UILotControl Parent;

        private bool Drawing;
        private Point LastPosition;
        private Dictionary<Point, VMArchitectureCommand> Commands = new Dictionary<Point, VMArchitectureCommand>();

        private VMArchitectureCommand LastCmd;
        private bool WasDown;

        public UIGrassPaint(VM vm, LotView.World world, UILotControl parent, List<int> parameters)
        {
            this.vm = vm;
            World = parent.World;
            Parent = parent;
            WallCursor = vm.Context.CreateObjectInstance(0x2F39B7A6, LotTilePos.OUT_OF_WORLD, FSO.LotView.Model.Direction.NORTH, true);

            ((ObjectComponent)WallCursor.Objects[0].WorldUI).ForceDynamic = true;
        }

        //0: up
        //1: down
        //2: error
        //3: anchor
        //4: level
        public void SetCursorGraphic(short id)
        {
            WallCursor.Objects[0].SetValue(VMStackObjectVariable.Graphic, id);
            ((VMGameObject)WallCursor.Objects[0]).RefreshGraphic();
        }

        public void MouseDown(UpdateState state)
        {
            if (!Drawing)
            {
                HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolDown);

                var tilePos = World.EstTileAtPosWithScroll(Parent.GetScaledPoint(state.MouseState.Position).ToVector2() / FSOEnvironment.DPIScaleFactor);
                LastPosition = new Point(-2, -2);
                Drawing = true;
            }
        }

        public void MouseUp(UpdateState state)
        {
            if (Drawing)
            {
                var cmds = new List<VMArchitectureCommand>(Commands.Values);

                if (cmds.Count > 0 && (Parent.ActiveEntity == null || vm.Context.Architecture.LastTestCost <= Parent.ActiveEntity.TSOState.Budget.Value))
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
            Commands.Clear();
        }

        public void Update(UpdateState state, bool scrolled)
        {
            var tilePos = World.EstTileAtPosWithScroll(Parent.GetScaledPoint(state.MouseState.Position).ToVector2() / FSOEnvironment.DPIScaleFactor);
            Point cursor = new Point((int)Math.Round(tilePos.X), (int)Math.Round(tilePos.Y));
            bool redraw = false;

            if (Drawing && cursor != LastPosition)
            {
                redraw = true;
                VMArchitectureCommand cmd;
                if (!Commands.TryGetValue(cursor, out cmd))
                {
                    cmd = new VMArchitectureCommand()
                    {
                        Type = VMArchitectureCommandType.GRASS_DOT,
                        x = cursor.X-1,
                        y = cursor.Y-1,
                        pattern = 0
                    };
                }

                var mod = (!state.ShiftDown) ? 128 : 32;
                if (!state.CtrlDown) mod *= -1;

                cmd.pattern = (ushort)(Math.Min(255, Math.Max(-255, ((short)cmd.pattern) + mod)));
                LastPosition = cursor;
                Commands[cursor] = cmd;
            }

            var cmds = vm.Context.Architecture.Commands;
            cmds.Clear();
            cmds.AddRange(Commands.Values);
            if (cmds.Count > 0)
            {
                if (!WasDown)
                {
                    vm.Context.Architecture.SignalRedraw();
                    WasDown = true;
                }
                if (redraw) vm.Context.Architecture.SignalRedraw();

                var cost = vm.Context.Architecture.LastTestCost;
                if (cost != 0)
                {
                    var disallowed = Parent.ActiveEntity != null && cost > Parent.ActiveEntity.TSOState.Budget.Value;
                    state.UIState.TooltipProperties.Show = true;
                    state.UIState.TooltipProperties.Color = disallowed ? Color.DarkRed : Color.Black;
                    state.UIState.TooltipProperties.Opacity = 1;
                    state.UIState.TooltipProperties.Position = new Vector2(state.MouseState.X, state.MouseState.Y);
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

            SetCursorGraphic(3);
        }

        public void Release()
        {
            WallCursor.Delete(vm.Context);
            vm.Context.Architecture.Commands.Clear();
            vm.Context.Architecture.SignalTerrainRedraw();
            vm.Context.Architecture.SignalRedraw();
        }
    }
}
