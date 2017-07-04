﻿/*
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
    public class UITerrainRaiser : UICustomLotControl
    {
        VMMultitileGroup WallCursor;
        VM vm;
        LotView.World World;
        UILotControl Parent;

        private bool Drawing;
        private Point StartPosition;

        private byte StartTerrainHeight;
        private int StartMousePosition;
        private Point EndMousePosition;

        private VMArchitectureCommand LastCmd;
        private bool WasDown;

        public UITerrainRaiser(VM vm, LotView.World world, UILotControl parent, List<int> parameters)
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
                Drawing = true;
                var tilePos = World.EstTileAtPosWithScroll(new Vector2(state.MouseState.X, state.MouseState.Y) / FSOEnvironment.DPIScaleFactor);
                StartPosition = new Point((int)Math.Round(tilePos.X), (int)Math.Round(tilePos.Y));
                var terrain = vm.Context.Architecture.Terrain;
                StartTerrainHeight = terrain.Heights[StartPosition.Y*terrain.Width + StartPosition.X];
                StartMousePosition = (int)(state.MouseState.Y - World.State.WorldSpace.GetScreenOffset().Y);
            }
        }

        public void MouseUp(UpdateState state)
        {
            if (Drawing)
            {
                var cmds = new List<VMArchitectureCommand>();

                var mpos = (int)(state.MouseState.Y - World.State.WorldSpace.GetScreenOffset().Y);
                var mod = (StartMousePosition - mpos) / (15 / (1 << (3 - (int)World.State.Zoom)));

                if (mod != 0 || (state.CtrlDown))
                {
                    var newHeight = StartTerrainHeight + mod;

                    cmds.Add(new VMArchitectureCommand
                    {
                        Type = VMArchitectureCommandType.TERRAIN_RAISE,
                        x = StartPosition.X,
                        y = StartPosition.Y,
                        level = (sbyte)newHeight,
                        pattern = (ushort)((state.CtrlDown)?1:0)
                    });

                }

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
        }

        public void Update(UpdateState state, bool scrolled)
        {
            var tilePos = World.EstTileAtPosWithScroll(new Vector2(state.MouseState.X, state.MouseState.Y) / FSOEnvironment.DPIScaleFactor);
            Point cursor = new Point((int)Math.Round(tilePos.X), (int)Math.Round(tilePos.Y));

            var cmds = vm.Context.Architecture.Commands;
            cmds.Clear();
            if (Drawing)
            {
                cursor = StartPosition;
                var mpos = (int)(state.MouseState.Y - World.State.WorldSpace.GetScreenOffset().Y);
                var mod = (StartMousePosition - mpos) / (15 / (1 << (3 - (int)World.State.Zoom)));
                var newHeight = StartTerrainHeight + mod;

                cmds.Add(new VMArchitectureCommand
                {
                    Type = VMArchitectureCommandType.TERRAIN_RAISE,
                    x = StartPosition.X,
                    y = StartPosition.Y,
                    level = (sbyte)newHeight,
                    pattern = (ushort)((state.CtrlDown) ? 1 : 0)
                });
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
                    vm.Context.Architecture.SignalRedraw();
                    WasDown = false;
                }
            }

            WallCursor.SetVisualPosition(new Vector3(cursor.X, cursor.Y, (World.State.Level - 1) * 2.95f), Direction.NORTH, vm.Context);

            if (state.ShiftDown) SetCursorGraphic(3);
            else if (state.CtrlDown) SetCursorGraphic(1);
            else SetCursorGraphic(0);
        }

        public void Release()
        {
            WallCursor.Delete(vm.Context);
            vm.Context.Architecture.Commands.Clear();
            vm.Context.Architecture.SignalRedraw();
        }
    }
}