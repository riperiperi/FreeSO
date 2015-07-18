using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.world;
using tso.world.components;
using tso.world.model;
using TSO.Common.rendering.framework.model;
using TSO.Common.utils;
using TSO.HIT;
using TSO.Simantics;
using TSO.Simantics.entities;
using TSO.Simantics.model;
using TSOClient.Code.UI.Model;

namespace TSOClient.Code.UI.Panels.LotControls
{
    public class UIWallPlacer : UICustomLotControl
    {
        VMMultitileGroup WallCursor;
        VM vm;
        World World;
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

        public UIWallPlacer(VM vm, World world, UILotControl parent, List<int> parameters)
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
            WallCursor = vm.Context.CreateObjectInstance(0x00000439, LotTilePos.OUT_OF_WORLD, tso.world.model.Direction.NORTH);

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
                var tilePos = World.State.WorldSpace.GetTileAtPosWithScroll(new Vector2(state.MouseState.X, state.MouseState.Y));
                StartPosition = new Point((int)Math.Round(tilePos.X), (int)Math.Round(tilePos.Y));
            }
        }

        public void MouseUp(UpdateState state)
        {
            if (Drawing)
            {
                var cmds = new List<VMArchitectureCommand>();

                if (state.KeyboardState.IsKeyDown(Keys.LeftShift))
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
                            level = 1,
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
                        Type = (state.KeyboardState.IsKeyDown(Keys.LeftControl))?
                            VMArchitectureCommandType.WALL_DELETE:VMArchitectureCommandType.WALL_LINE,
                        level = 1, pattern = Pattern, style = Style, x = StartPosition.X, y = StartPosition.Y, x2 = DrawLength, y2 = DrawDir });
                }
                if (cmds.Count > 0)
                {
                    vm.Context.Architecture.RunCommands(cmds);
                    HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolPlace);
                } else HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolUp);
            }
            Drawing = false;
        }

        public void Update(UpdateState state, bool scrolled)
        {
            var tilePos = World.State.WorldSpace.GetTileAtPosWithScroll(new Vector2(state.MouseState.X, state.MouseState.Y));
            Point cursor = new Point((int)Math.Round(tilePos.X), (int)Math.Round(tilePos.Y));

            var cmds = vm.Context.Architecture.Commands;
            cmds.Clear();
            if (Drawing)
            {
                var diff = cursor - StartPosition;
                DrawLength = (int)Math.Round(Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y));
                DrawDir = (int)DirectionUtils.PosMod(Math.Round(Math.Atan2(diff.Y, diff.X) / (Math.PI / 4)), 8);
                

                if (state.KeyboardState.IsKeyDown(Keys.LeftShift))
                {
                    EndPosition = cursor;
                    int smallX = Math.Min(StartPosition.X, EndPosition.X);
                    int smallY = Math.Min(StartPosition.Y, EndPosition.Y);
                    int bigX = Math.Max(StartPosition.X, EndPosition.X);
                    int bigY = Math.Max(StartPosition.Y, EndPosition.Y);
                    cmds.Add(new VMArchitectureCommand { Type = VMArchitectureCommandType.WALL_RECT, level = 1, pattern = DrawPattern, style = DrawStyle,
                        x = smallX, y = smallY,
                        x2 = bigX-smallX, y2 = bigY-smallY
                    });
                }
                else
                {
                    cursor = StartPosition + new Point(DirUnits[DrawDir].X * DrawLength, DirUnits[DrawDir].Y * DrawLength);
                    cmds.Add(new VMArchitectureCommand { Type = (state.KeyboardState.IsKeyDown(Keys.LeftControl)) ?
                            VMArchitectureCommandType.WALL_DELETE : VMArchitectureCommandType.WALL_LINE,
                        level = 1, pattern = DrawPattern, style = DrawStyle, x = StartPosition.X, y = StartPosition.Y, x2 = DrawLength, y2 = DrawDir });
                }
            }
            WallCursor.SetVisualPosition(new Vector3(cursor.X, cursor.Y, 0), Direction.NORTH, vm.Context);

            if (state.KeyboardState.IsKeyDown(Keys.LeftShift)) SetCursorGraphic(3);
            else if (state.KeyboardState.IsKeyDown(Keys.LeftControl)) SetCursorGraphic(1);
            else SetCursorGraphic(0);
        }

        public void Release()
        {
            WallCursor.Delete(vm.Context);
        }
    }
}
