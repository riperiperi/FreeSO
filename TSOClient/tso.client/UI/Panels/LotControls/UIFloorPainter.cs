﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using FSO.LotView;
using FSO.Common.Rendering.Framework.Model;
using FSO.HIT;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.Client.UI.Model;

namespace FSO.Client.UI.Panels.LotControls
{
    public class UIFloorPainter : UICustomLotControl
    {

        VM vm;
        LotView.World World;
        UILotControl Parent;

        bool Drawing;
        private List<VMArchitectureCommand> Commands;
        int CursorDir = 0;

        int StartX = -1;
        int StartY = -1;

        ushort Pattern;

        public UIFloorPainter (VM vm, LotView.World world, UILotControl parent, List<int> parameters)
        {
            Pattern = (ushort)parameters[0];

            this.vm = vm;
            World = parent.World;
            Parent = parent;

            Commands = new List<VMArchitectureCommand>();

        }


        public void MouseDown(UpdateState state)
        {
            HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolDown);
            Drawing = true;
        }

        public void MouseUp(UpdateState state)
        {
            HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolUp);

            vm.SendCommand(new VMNetArchitectureCmd
            {
                Commands = new List<VMArchitectureCommand>(Commands)
            });

            Commands.Clear();
            Drawing = false;
        }

        public void Release()
        {
            vm.Context.Architecture.Commands.Clear();
            vm.Context.Architecture.SignalRedraw();
        }

        public void Update(UpdateState state, bool scrolled)
        {
            ushort pattern = (state.CtrlDown) ? (ushort)0 : Pattern;
            
            var tilePos = World.EstTileAtPosWithScroll(Parent.GetScaledPoint(state.MouseState.Position).ToVector2());
            Point cursor = new Point((int)tilePos.X, (int)tilePos.Y);

            /*if (!Drawing && Commands.Count > 0)
            {
                vm.Context.Architecture.SignalRedraw();
                Commands.Clear();
            }*/
            if (state.ShiftDown && pattern < 65534)
            {
                if (Commands.Count == 0 || Commands[0].Type != VMArchitectureCommandType.FLOOR_FILL)
                {
                    Commands.Clear();
                    vm.Context.Architecture.SignalRedraw();
                    Commands.Add(new VMArchitectureCommand
                    {
                        Type = VMArchitectureCommandType.FLOOR_FILL,
                        level = World.State.Level,
                        pattern = pattern,
                        style = 0,
                        x = cursor.X,
                        y = cursor.Y,
                    });
                }
            } else
            {
                if (Commands.Count > 0 && Commands[0].Type == VMArchitectureCommandType.FLOOR_FILL)
                {
                    Commands.Clear();
                    vm.Context.Architecture.SignalRedraw();
                }

                if (!Drawing || Commands.Count == 0)
                {
                    StartX = cursor.X;
                    StartY = cursor.Y;
                }
                
                int dir = 0;
                Vector2 fract = new Vector2(tilePos.X - cursor.X, tilePos.Y - cursor.Y);
                if (fract.X-fract.Y > 0)
                {
                    dir = (fract.X + fract.Y > 1) ? 2 : 1;
                } else
                {
                    dir = (fract.X + fract.Y > 1) ? 3 : 0;
                }

                int smallX = Math.Min(StartX, cursor.X);
                int smallY = Math.Min(StartY, cursor.Y);
                int bigX = Math.Max(StartX, cursor.X);
                int bigY = Math.Max(StartY, cursor.Y);

                var cmd = new VMArchitectureCommand
                {
                    Type = VMArchitectureCommandType.FLOOR_RECT,
                    level = World.State.Level,
                    pattern = pattern,
                    style = (ushort)dir,
                    x = smallX,
                    y = smallY,
                    x2 = bigX-smallX,
                    y2 = bigY-smallY
                };
                if (!Commands.Contains(cmd))
                {
                    Commands.Clear();
                    vm.Context.Architecture.SignalRedraw();
                    Commands.Add(cmd);
                }
            }

            var cmds = vm.Context.Architecture.Commands;
            cmds.Clear();
            foreach (var cmd in Commands)
            {
                cmds.Add(cmd);
            }

            if (cmds.Count > 0)
            {
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

                    if (disallowed) HITVM.Get().PlaySoundEvent(UISounds.Error);
                }
                else
                {
                    state.UIState.TooltipProperties.Show = false;
                    state.UIState.TooltipProperties.Opacity = 0;
                }
            }

        }
    }
}
