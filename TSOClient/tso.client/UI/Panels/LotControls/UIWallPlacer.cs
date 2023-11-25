using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
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

namespace FSO.Client.UI.Panels.LotControls
{
    public class UIWallPlacer : UICustomLotControl
    {
        VMMultitileGroup WallCursor;
        VM vm;
        LotView.World World;
        UILotControl Parent;

        private bool Drawing;
        private Point StartPosition;
        private int DrawDir;
        private int DrawLength;
        private Point EndPosition;

        private ushort DrawPattern;
        private ushort DrawStyle;
        private ushort Pattern;
        private ushort Style;

        private VMArchitectureCommand LastCmd;
        private bool WasDown;

        private Point[] DirUnits =
        {
            new Point(1, 0),
            new Point(1, 1),
            new Point(0, 1),
            new Point(-1, 1),
            new Point(-1, 0),
            new Point(-1, -1),
            new Point(0, -1),
            new Point(1, -1),
        };

        public UIWallPlacer(VM vm, LotView.World world, UILotControl parent, List<int> parameters)
        {
            Pattern = (ushort)parameters[0];
            Style = (ushort)parameters[1];
            if (Style == 1)
            {
                DrawPattern = 255;
                DrawStyle = 255;
            } else
            {
                DrawPattern = Pattern;
                DrawStyle = Style;
            }

            this.vm = vm;
            World = parent.World;
            Parent = parent;
            WallCursor = vm.Context.CreateObjectInstance(0x00000439, LotTilePos.OUT_OF_WORLD, FSO.LotView.Model.Direction.NORTH, true);

            ((ObjectComponent)WallCursor.Objects[0].WorldUI).ForceDynamic = true;
        }

        //0: wall
        //1: bulldoze
        //2: paint
        //3: rect
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
                var tilePos = World.EstTileAtPosWithScroll(Parent.GetScaledPoint(state.MouseState.Position).ToVector2());
                StartPosition = new Point((int)Math.Round(tilePos.X), (int)Math.Round(tilePos.Y));
            }
        }

        public void MouseUp(UpdateState state)
        {
            if (Drawing)
            {
                var cmds = new List<VMArchitectureCommand>();

                if (state.ShiftDown)
                {
                    if (StartPosition != EndPosition)
                    {
                        int smallX = Math.Min(StartPosition.X, EndPosition.X);
                        int smallY = Math.Min(StartPosition.Y, EndPosition.Y);
                        int bigX = Math.Max(StartPosition.X, EndPosition.X);
                        int bigY = Math.Max(StartPosition.Y, EndPosition.Y);

                        cmds.Add(new VMArchitectureCommand
                        {
                            Type = VMArchitectureCommandType.WALL_RECT,
                            level = World.State.Level,
                            pattern = Pattern,
                            style = Style,
                            x = smallX,
                            y = smallY,
                            x2 = bigX - smallX,
                            y2 = bigY - smallY
                        });
                    }
                }
                else
                {
                    if (DrawLength > 0) cmds.Add(new VMArchitectureCommand {
                        Type = (state.CtrlDown) ?
                            VMArchitectureCommandType.WALL_DELETE:VMArchitectureCommandType.WALL_LINE,
                        level = World.State.Level, pattern = Pattern, style = Style, x = StartPosition.X, y = StartPosition.Y, x2 = DrawLength, y2 = DrawDir });
                }
                if (cmds.Count > 0 && (Parent.ActiveEntity == null || vm.Context.Architecture.LastTestCost <= Parent.ActiveEntity.TSOState.Budget.Value))
                {
                    vm.SendCommand(new VMNetArchitectureCmd
                    {
                        Commands = new List<VMArchitectureCommand>(cmds)
                    });

                    //vm.Context.Architecture.RunCommands(cmds);
                    HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolPlace);
                } else HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolUp);
            }
            Drawing = false;
        }

        public void Update(UpdateState state, bool scrolled)
        {
            var tilePos = World.EstTileAtPosWithScroll(Parent.GetScaledPoint(state.MouseState.Position).ToVector2());
            Point cursor = new Point((int)Math.Round(tilePos.X), (int)Math.Round(tilePos.Y));

            var cmds = vm.Context.Architecture.Commands;
            cmds.Clear();
            if (Drawing)
            {
                var diff = cursor - StartPosition;
                DrawLength = (int)Math.Round(Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y));
                DrawDir = (int)DirectionUtils.PosMod(Math.Round(Math.Atan2(diff.Y, diff.X) / (Math.PI / 4)), 8);
                

                if (state.ShiftDown)
                {
                    EndPosition = cursor;
                    int smallX = Math.Min(StartPosition.X, EndPosition.X);
                    int smallY = Math.Min(StartPosition.Y, EndPosition.Y);
                    int bigX = Math.Max(StartPosition.X, EndPosition.X);
                    int bigY = Math.Max(StartPosition.Y, EndPosition.Y);
                    cmds.Add(new VMArchitectureCommand { Type = VMArchitectureCommandType.WALL_RECT, level = World.State.Level, pattern = DrawPattern, style = DrawStyle,
                        x = smallX, y = smallY,
                        x2 = bigX-smallX, y2 = bigY-smallY
                    });
                }
                else
                {
                    cursor = StartPosition + new Point(DirUnits[DrawDir].X * DrawLength, DirUnits[DrawDir].Y * DrawLength);
                    cmds.Add(new VMArchitectureCommand { Type = (state.CtrlDown) ?
                            VMArchitectureCommandType.WALL_DELETE : VMArchitectureCommandType.WALL_LINE,
                        level = World.State.Level, pattern = DrawPattern, style = DrawStyle, x = StartPosition.X, y = StartPosition.Y, x2 = DrawLength, y2 = DrawDir });
                }
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
                    state.UIState.TooltipProperties.Color = disallowed?Color.DarkRed:Color.Black;
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

            WallCursor.SetVisualPosition(new Vector3(cursor.X, cursor.Y, (World.State.Level-1)*2.95f), Direction.NORTH, vm.Context);

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
