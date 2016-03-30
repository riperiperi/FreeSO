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
using FSO.HIT;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Utils;
using FSO.Client.UI.Model;

namespace FSO.Client.UI.Panels.LotControls
{
    public class UIWallPainter : UICustomLotControl
    {

        VMMultitileGroup WallCursor;
        VM vm;
        LotView.World World;
        UILotControl Parent;

        bool Drawing;
        private List<VMArchitectureCommand> Commands;
        int CursorDir = 0;

        ushort Pattern;

        public UIWallPainter (VM vm, LotView.World world, UILotControl parent, List<int> parameters)
        {
            Pattern = (ushort)parameters[0];

            this.vm = vm;
            World = parent.World;
            Parent = parent;
            WallCursor = vm.Context.CreateObjectInstance(0x00000439, LotTilePos.OUT_OF_WORLD, FSO.LotView.Model.Direction.NORTH, true);

            ((ObjectComponent)WallCursor.Objects[0].WorldUI).ForceDynamic = true;
            Commands = new List<VMArchitectureCommand>();

            SetCursorGraphic(2);
        }

        public void SetCursorGraphic(short id)
        {
            WallCursor.Objects[0].SetValue(VMStackObjectVariable.Graphic, id);
            ((VMGameObject)WallCursor.Objects[0]).RefreshGraphic();
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
            WallCursor.Delete(vm.Context);
        }

        public void Update(UpdateState state, bool scrolled)
        {
            ushort pattern = (state.KeyboardState.IsKeyDown(Keys.LeftControl)) ? (ushort)0 : Pattern;

            var tilePos = World.State.WorldSpace.GetTileAtPosWithScroll(new Vector2(state.MouseState.X, state.MouseState.Y));
            Point cursor = new Point((int)tilePos.X, (int)tilePos.Y);

            if (!Drawing && Commands.Count > 0)
            {
                vm.Context.Architecture.SignalRedraw();
                Commands.Clear();
            }
            if (state.KeyboardState.IsKeyDown(Keys.LeftShift))
            {
                if (Commands.Count == 0 || Commands[0].Type != VMArchitectureCommandType.PATTERN_FILL)
                {
                    Commands.Clear();
                    vm.Context.Architecture.SignalRedraw();
                    Commands.Add(new VMArchitectureCommand
                    {
                        Type = VMArchitectureCommandType.PATTERN_FILL,
                        level = World.State.Level,
                        pattern = pattern,
                        style = 0,
                        x = cursor.X,
                        y = cursor.Y
                    });
                }
            } else
            {
                if (Commands.Count > 0 && Commands[0].Type == VMArchitectureCommandType.PATTERN_FILL)
                {
                    Commands.Clear();
                    vm.Context.Architecture.SignalRedraw();
                }
                int dir = 0;
                int altdir = 0;
                Vector2 fract = new Vector2(tilePos.X - cursor.X, tilePos.Y - cursor.Y);
                switch (World.State.Rotation)
                {
                    case WorldRotation.BottomRight:
                        if (fract.X - fract.Y > 0) { dir = 2; altdir = 3; }
                        else { dir = 3; altdir = 2; }
                        break;
                    case WorldRotation.TopRight:
                        if (fract.X + fract.Y > 1) { dir = 3; altdir = 0; }
                        else { dir = 0; altdir = 3; }
                        break;
                    case WorldRotation.TopLeft:
                        //+x is right down. +y is left down
                        if (fract.X - fract.Y > 0) { dir = 1; altdir = 0; }
                        else { dir = 0; altdir = 1; }
                        break;
                    case WorldRotation.BottomLeft:
                        if (fract.X + fract.Y > 1) { dir = 2; altdir = 1; }
                        else { dir = 1; altdir = 2; }
                        break;
                }

                var finalDir = VMArchitectureTools.GetPatternDirection(vm.Context.Architecture, cursor, pattern, dir, altdir, World.State.Level);
                if (finalDir != -1)
                {
                    CursorDir = finalDir;
                    var cmd = new VMArchitectureCommand
                    {
                        Type = VMArchitectureCommandType.PATTERN_DOT,
                        level = World.State.Level,
                        pattern = pattern,
                        style = 0,
                        x = cursor.X,
                        y = cursor.Y,
                        x2 = dir,
                        y2 = altdir
                    };
                    if (!Commands.Contains(cmd))
                    {
                        vm.Context.Architecture.SignalRedraw();
                        Commands.Add(cmd);
                    }
                }
            }

            var cmds = vm.Context.Architecture.Commands;
            cmds.Clear();
            foreach (var cmd in Commands)
            {
                cmds.Add(cmd);
            }

            WallCursor.SetVisualPosition(new Vector3(cursor.X+0.5f, cursor.Y+0.5f, (World.State.Level-1)*2.95f), (Direction)(1<<((3-CursorDir)*2)), vm.Context);
        }
    }
}
