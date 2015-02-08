using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.world;
using TSO.Simantics;
using TSO.Common.rendering.framework.model;
using Microsoft.Xna.Framework;
using tso.world.components;
using TSO.Simantics.entities;
using tso.world.model;
using TSOClient.Code.UI.Model;
using TSO.HIT;

namespace TSOClient.Code.UI.Panels
{
    public class UIObjectHolder //controls the object holder interface
    {
        public VM vm;
        public World World;
        public UILotControl ParentControl;

        public Direction Rotation;
        public int MouseDownX;
        public int MouseDownY;
        public bool MouseIsDown;
        public bool DirChanged;

        public UIObjectSelection Holding;

        public UIObjectHolder(VM vm, World World, UILotControl parent)
        {
            this.vm = vm;
            this.World = World;
            ParentControl = parent;

            //Entity = (VMGameObject)vm.Context.CreateObjectInstance(0x00000437, 0, 0, 1, tso.world.model.Direction.NORTH);
        }

        public void SetSelected(VMMultitileGroup Group)
        {
            if (Holding != null) ClearSelected();
            Holding = new UIObjectSelection();
            Holding.Group = Group;
            VMEntity[] CursorTiles = new VMEntity[Group.Objects.Count];
            for (int i = 0; i < Group.Objects.Count; i++)
            {
                var target = Group.Objects[i];
                CursorTiles[i] = vm.Context.CreateObjectInstance(0x00000437, (short)target.Position.X, (short)target.Position.Y, 1, tso.world.model.Direction.NORTH).Objects[0];
            }
            Holding.CursorTiles = CursorTiles;
        }

        public void MoveSelected(Vector2 pos, sbyte level)
        {
            Holding.TilePos = pos;
            Holding.Level = level;

            //rotate through to try all configurations
            var dir = Holding.Dir;
            bool success = false;
            for (int i = 0; i < 4; i++)
            {
                if (Holding.Group.ChangePosition((short)pos.X, (short)pos.Y, 1, dir, vm.Context))
                {
                    success = true;
                    break;
                } 
                dir = (Direction)((((int)dir << 6) & 255) | ((int)dir >> 2));
            }
            if (Holding.Dir != dir) Holding.Dir = dir;

            if (!success) Holding.Group.SetVisualPosition(new Vector3(pos, 0), Holding.Dir);

            for (int i = 0; i < Holding.Group.Objects.Count; i++)
            {
                var target = Holding.Group.Objects[i];
                Holding.CursorTiles[i].MultitileGroup.SetVisualPosition(target.WorldUI.Position, Holding.Dir);
                //Holding.CursorTiles[i].SetPosition((short)target.Position.X, (short)target.Position.Y, 1, tso.world.model.Direction.NORTH, vm.Context);
            }
            Holding.CanPlace = success;
        }

        public void ClearSelected()
        {
            if (Holding != null)
            {
                for (int i = 0; i < Holding.Group.Objects.Count; i++)
                {
                    vm.Context.RemoveObjectInstance(Holding.CursorTiles[i]);
                }
            }
            Holding = null;
        }

        public void MouseDown(UpdateState state)
        {
            MouseIsDown = true;
            if (Holding != null)
            {
                Rotation = Holding.Dir;
                MouseDownX = state.MouseState.X;
                MouseDownY = state.MouseState.Y;
                DirChanged = false;
            }
        }

        public void MouseUp(UpdateState state)
        {
            MouseIsDown = false;
            if (Holding != null)
            {
                if (Holding.CanPlace)
                {
                    HITVM.Get().PlaySoundEvent(UISounds.ObjectPlace);
                    ClearSelected();
                }
                else
                {
                    HITVM.Get().PlaySoundEvent(UISounds.Error);
                }
            }
        }

        public void Update(UpdateState state, bool scrolled)
        {
            
            if (Holding != null)
            {
                if (MouseIsDown)
                {
                    int xDiff = state.MouseState.X - MouseDownX;
                    int yDiff = state.MouseState.Y - MouseDownY;
                    if (Math.Sqrt(xDiff * xDiff + yDiff * yDiff) > 64)
                    {
                        int dir;
                        if (xDiff > 0)
                        {
                            if (yDiff > 0) dir = 1;
                            else dir = 0;
                        }
                        else
                        {
                            if (yDiff > 0) dir = 2;
                            else dir = 3;
                        }
                        var newDir = (Direction)(1 << (((dir + 4 - (int)World.State.Rotation) % 4) * 2));
                        if (newDir != Holding.Dir)
                        {
                            HITVM.Get().PlaySoundEvent(UISounds.ObjectRotate);
                            Holding.Dir = newDir;
                            MoveSelected(Holding.TilePos, Holding.Level);
                            DirChanged = true;
                        }
                    }
                }
                else
                {
                    var tilePos = World.State.WorldSpace.GetTileAtPosWithScroll(new Vector2(state.MouseState.X, state.MouseState.Y));
                    MoveSelected(tilePos, 1);
                }
            }
        }
    }

    public class UIObjectSelection
    {
        public VMMultitileGroup Group;
        public VMEntity[] CursorTiles;
        public Direction Dir = Direction.NORTH;
        public Vector2 TilePos;
        public bool CanPlace;
        public sbyte Level;
    }
}
