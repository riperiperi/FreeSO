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
using TSO.Simantics.model;

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
        private bool MouseIsDown;
        private bool MouseWasDown;
        private bool MouseClicked;
        public bool DirChanged;
        public bool ShowTooltip;

        public UIObjectSelection Holding;

        public UIObjectHolder(VM vm, World World, UILotControl parent)
        {
            this.vm = vm;
            this.World = World;
            ParentControl = parent;
        }

        public void SetSelected(VMMultitileGroup Group)
        {
            if (Holding != null) ClearSelected();
            Holding = new UIObjectSelection();
            Holding.Group = Group;
            Holding.Dir = Group.Objects[0].Direction;
            VMEntity[] CursorTiles = new VMEntity[Group.Objects.Count];
            for (int i = 0; i < Group.Objects.Count; i++)
            {
                var target = Group.Objects[i];
                if (target is VMGameObject) ((ObjectComponent)target.WorldUI).ForceDynamic = true;
                CursorTiles[i] = vm.Context.CreateObjectInstance(0x00000437, new LotTilePos(target.Position), tso.world.model.Direction.NORTH).Objects[0];
                CursorTiles[i].SetPosition(new LotTilePos(0,0,1), Direction.NORTH, vm.Context);
                ((ObjectComponent)CursorTiles[i].WorldUI).ForceDynamic = true;
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
                if (Holding.Group.ChangePosition(LotTilePos.FromBigTile((short)pos.X, (short)pos.Y, 1), dir, vm.Context))
                {
                    success = true;
                    break;
                } 
                dir = (Direction)((((int)dir << 6) & 255) | ((int)dir >> 2));
            }
            if (Holding.Dir != dir) Holding.Dir = dir;

            if (!success)
            {
                Holding.Group.SetVisualPosition(new Vector3(pos,
                ((Holding.Group.Objects[0].GetValue(VMStackObjectVariable.AllowedHeightFlags) & 1) == 1) ? 0 : 4f / 5f),
                    //^ if we can't be placed on the floor, default to table height.
                Holding.Dir, vm.Context);
            }

            for (int i = 0; i < Holding.Group.Objects.Count; i++)
            {
                var target = Holding.Group.Objects[i];
                var tpos = target.VisualPosition;
                tpos.Z = (target.Position.Level-1)*3;
                Holding.CursorTiles[i].MultitileGroup.SetVisualPosition(tpos, Holding.Dir, vm.Context);
            }
            Holding.CanPlace = success;
        }

        public void ClearSelected()
        {
            //TODO: selected items are only spooky ghosts of the items themselves.
            //      ...so that they dont cause serverside desyncs
            //      and so that clearing selections doesnt delete already placed objects.
            if (Holding != null)
            {
                for (int i = 0; i < Holding.Group.Objects.Count; i++)
                {
                    var target = Holding.Group.Objects[i];
                    if (target is VMGameObject) ((ObjectComponent)target.WorldUI).ForceDynamic = false;
                    vm.Context.RemoveObjectInstance(Holding.CursorTiles[i]);
                    ((ObjectComponent)Holding.CursorTiles[i].WorldUI).ForceDynamic = false;
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
            if (Holding != null && Holding.Clicked)
            {
                if (Holding.CanPlace)
                {
                    HITVM.Get().PlaySoundEvent(UISounds.ObjectPlace);
                    ClearSelected();
                }
                else
                {
                    
                }
            }

            GameFacade.Screens.TooltipProperties.Show = false;
            GameFacade.Screens.TooltipProperties.Opacity = 0;
            ShowTooltip = false;
        }

        public void Update(UpdateState state, bool scrolled)
        {
            if (ShowTooltip) GameFacade.Screens.TooltipProperties.UpdateDead = false;
            MouseClicked = (MouseIsDown && (!MouseWasDown));
            if (Holding != null)
            {
                if (MouseClicked) Holding.Clicked = true;
                //TODO: crash if placed out of world
                if (MouseIsDown && Holding.Clicked)
                {
                    bool updatePos = MouseClicked;
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
                        if (newDir != Holding.Dir || MouseClicked)
                        {
                            updatePos = true;
                            HITVM.Get().PlaySoundEvent(UISounds.ObjectRotate);
                            Holding.Dir = newDir;
                            DirChanged = true;
                        }
                    }
                    if (updatePos)
                    {
                        MoveSelected(Holding.TilePos, Holding.Level);
                        if (!Holding.CanPlace)
                        {
                            GameFacade.Screens.TooltipProperties.Show = true;
                            GameFacade.Screens.TooltipProperties.Opacity = 1;
                            GameFacade.Screens.TooltipProperties.Position = new Vector2(MouseDownX,
                                MouseDownY);
                            GameFacade.Screens.Tooltip = "Can't place object here"; //GameFacade.Strings.GetString("137", "0");
                            GameFacade.Screens.TooltipProperties.UpdateDead = false;
                            ShowTooltip = true;
                            HITVM.Get().PlaySoundEvent(UISounds.Error);
                        }
                    }
                }
                else
                {
                    var tilePos = World.State.WorldSpace.GetTileAtPosWithScroll(new Vector2(state.MouseState.X, state.MouseState.Y));
                    MoveSelected(tilePos, 1);
                }
            }
            else
            {
                //not holding an object, but one can be selected
                var newHover = World.GetObjectIDAtScreenPos(state.MouseState.X, state.MouseState.Y, GameFacade.GraphicsDevice);
                if (MouseClicked && (newHover != 0))
                {
                    SetSelected(vm.GetObjectById(newHover).MultitileGroup);
                }
            }

            MouseWasDown = MouseIsDown;
        }
    }

    public class UIObjectSelection
    {
        public VMMultitileGroup Group;
        public VMEntity[] CursorTiles;
        public Direction Dir = Direction.NORTH;
        public Vector2 TilePos;
        public bool Clicked;
        public bool CanPlace;
        public sbyte Level;
    }
}
